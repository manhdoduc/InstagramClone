using InstagramClone.API.Hubs;
using InstagramClone.Application.Interfaces.Services;
using InstagramClone.Common.Models.Config;
using InstagramClone.Domain.Entities;
using InstagramClone.Infrastructure.Data;
using InstagramClone.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;
using System.Reflection;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Đăng ký HttpContextAccessor để có thể inject vào các service khác, giúp lấy thông tin user hiện tại (userId)
builder.Services.AddHttpContextAccessor();


builder.Services.AddScoped<IAuthServices, AuthServices>();
builder.Services.AddScoped<IStorageServices, LocalStorageServices>();
builder.Services.AddScoped<IUserServices, UserServices>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserServices>();
builder.Services.AddScoped<IPostServices, PostServices>();
builder.Services.AddScoped<ICommentServices, CommentServices>();
builder.Services.AddScoped<IFollowService, FollowServices>();


builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle




// 1. Connect to database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// 2. Configure DbContext with connection pooling and retry logic
builder.Services.AddDbContextPool<AppDbContext>(options =>
{
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.CommandTimeout(30);
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorNumbersToAdd: null
            );
    });
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }

    options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
}, poolSize: 128
);
// no tracking sẽ giúp tăng hiệu suất khi chỉ đọc dữ liệu mà không cần theo dõi thay đổi. Pooling giúp tái sử dụng các instance của DbContext, giảm overhead khi tạo mới nhiều lần.


// 3. Add Identity
builder.Services.AddIdentity<AppUser, IdentityRole>(options => {
    // Tùy chỉnh policy password
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();


builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>() ?? new JwtSettings();


builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["JwtSettings:Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key ?? string.Empty)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // API Information
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "InstagramClone API",
        Description = "API for user",
        Contact = new OpenApiContact
        {
            Name = "Support Team",
            Email = "support@hotellisting.com"
        },
        License = new OpenApiLicense
        {
            Name = "MIT License",
            Url = new Uri("https://opensource.org/licenses/MIT")
        }
    });

    // Include XML comments
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    } // viet chu thich /// trên controler action swagger sẽ đọc xml để hiển thị lên giao diện

    // Enable annotations
    options.EnableAnnotations(); //Cho phép bạn sử dụng các thuộc tính (Attributes) như [SwaggerOperation], [SwaggerResponse]

    // Security Definitions
    // JWT Bearer Authentication
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });

    // Add security requirements
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                new string[] {}
            }
        }); // mặc định

    // Add operation filters for examples
    options.ExampleFilters(); //Tự động hiển thị các ví dụ (Example data) cho Request/Response nếu bạn có cấu hình các lớp Example.

    // Custom operation filter for handling multiple auth schemes
    options.OperationFilter<InstagramClone.API.Filters.SecurityRequirementsOperationFilter>();
    // Đây là một bộ lọc tùy chỉnh. Nó giúp xử lý logic hiển thị các yêu cầu bảo mật khác nhau cho từng Endpoint cụ thể (ví dụ: cái nào dùng API Key, cái nào dùng Bearer).

    // Order actions by method
    options.OrderActionsBy(apiDesc => $"{apiDesc.RelativePath}_{apiDesc.HttpMethod}");
    //Sắp xếp danh sách các API trên giao diện Swagger theo đường dẫn (RelativePath) và sau đó là phương thức HTTP (GET, POST...). Điều này giúp tài liệu trông gọn gàng, dễ tìm kiếm.
});

builder.Services.AddSwaggerExamplesFromAssemblyOf<Program>();

// 1. Thêm đoạn này lên phía trên, ngay sau builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddSignalR();

var app = builder.Build();
// 2. Thêm đoạn này vào phần pipeline, PHẢI NẰM TRƯỚC app.UseAuthentication() và app.UseAuthorization()
app.UseCors("AllowAll");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseAuthentication();

app.UseAuthorization();

app.MapHub<ChatHub>("/chathub");

app.MapControllers();

app.Run();
