using FluentValidation;
using FluentValidation.AspNetCore;
using HealthChecks.UI.Client;
using InstagramClone.API.Middlewares;
using InstagramClone.Application.Interfaces.Caching;
using InstagramClone.Application.Interfaces.Chats;
using InstagramClone.Application.Interfaces.Data;
using InstagramClone.Application.Interfaces.Services;
using InstagramClone.Application.Services;
using InstagramClone.Application.Validators;
using InstagramClone.Common.Models.Config;
using InstagramClone.Domain.Entities;
using InstagramClone.Infrastructure.Data;
using InstagramClone.Infrastructure.Hubs;
using InstagramClone.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;
using Swashbuckle.AspNetCore.Filters;
using System.Reflection;
using System.Text;
using System.Threading.RateLimiting;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.Seq("http://host.docker.internal:5341")
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting up the application...");
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
    );

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
    builder.Services.AddScoped<IChatService, ChatServices>();
    builder.Services.AddScoped<IChatNotificationService, ChatNotificationService>();
    builder.Services.AddScoped<ICacheService, MemoryCacheService>(); 
    builder.Services.AddScoped<InstagramClone.Application.Interfaces.IUnitOfWork, InstagramClone.Infrastructure.Repositories.UnitOfWork>();
    builder.Services.AddAutoMapper(cfg => cfg.AddMaps(AppDomain.CurrentDomain.GetAssemblies()));

    builder.Services.AddControllers();
    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle




    // 1. Connect to database
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

    // 1.5 Register IApplicationDbContext
    builder.Services.AddScoped<IApplicationDbContext, AppDbContext>();

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

    if(jwtSettings == null)
    {
        Log.Fatal("Failed to load JWT settings from configuration.");
        throw new Exception("JWT settings are not configured properly."); 
    }
    if (string.IsNullOrEmpty(jwtSettings.Key))
    {
        Log.Fatal("JWT Key is missing! Check your User Secrets or appsettings.json.");
        throw new Exception("JWT Key is not configured.");
    }

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

            // Cho phép lấy token từ query string khi kết nối đến SignalR Hub
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;
                    if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/chathub"))
                    {
                        context.Token = accessToken;
                    }
                    return Task.CompletedTask;
                }
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
        options.AddPolicy("InstagramCorsPolicy", policy =>
        {
            var origins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>();
            if (origins != null && origins.Length > 0)
            {
                policy.WithOrigins(origins)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
            }
            else
            {
                Log.Warning("No allowed origins configured for CORS. Please check your configuration.");
            }
        });
    });

    builder.Services.AddSignalR();

    // Dùng Redis Cache thay cho DistributedMemoryCache

    //builder.Services.AddStackExchangeRedisCache(options =>
    //{
    //    options.Configuration = "localhost:6379"; // Cấu hình port mặc định của redis
    //});
    // đang dùng IDistributedCache của Microsoft, nhưng đã cấu hình để sử dụng Redis thay vì bộ nhớ trong. Điều này giúp cải thiện hiệu suất và khả năng mở rộng khi cache dữ liệu.
    // sau này cấu hình sử dụng Redis Cache, chỉ cần thay đổi cấu hình ở đây mà không cần sửa code ở các service khác, vì chúng ta đã inject ICacheService (RedisCacheService) vào các service khác.

    builder.Services.AddDistributedMemoryCache();


    builder.Services.AddRateLimiter(options =>
    {
        // Rate Limiter cho Auth (Login, Register) - 5 requests / 5 mins
        options.AddFixedWindowLimiter("LoginLimit", limiterOptions =>
        {
            limiterOptions.PermitLimit = 5;
            limiterOptions.Window = TimeSpan.FromMinutes(5);
            limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            limiterOptions.QueueLimit = 0;
        });

        // Rate Limiter cho Upload (Post, Avatar) - 10 requests / 1 hour
        options.AddFixedWindowLimiter("UploadLimit", limiterOptions =>
        {
            limiterOptions.PermitLimit = 10;
            limiterOptions.Window = TimeSpan.FromHours(1);
            limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            limiterOptions.QueueLimit = 2;
        });

        // Rate Limiter cho Action quá nhiều (Comment) - 5 requests / 1 min
        options.AddFixedWindowLimiter("CommentLimit", limiterOptions =>
        {
            limiterOptions.PermitLimit = 5;
            limiterOptions.Window = TimeSpan.FromMinutes(1);
            limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            limiterOptions.QueueLimit = 2;
        });

        options.AddPolicy("PerUser", context =>
        {
            var username = context.User?.Identity?.Name ?? "anonymous"; // Sử dụng userId làm key, nếu không có thì dùng "anonymous"

            return RateLimitPartition.GetSlidingWindowLimiter(username, _ => new SlidingWindowRateLimiterOptions
            {
                Window = TimeSpan.FromMinutes(1), // Thời gian cửa sổ
                PermitLimit = 10,
                SegmentsPerWindow = 5,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 5
            });
        });

        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        {
            var username = context.Connection.RemoteIpAddress?.ToString() ?? "unknown"; // Sử dụng IP làm key, nếu không có thì dùng "unknown"
            return RateLimitPartition.GetFixedWindowLimiter(username, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 5
            });
        });

        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests; // Mã trạng thái trả về khi bị giới hạn

        options.OnRejected = async (context, CancellationToken) =>
        {
            if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            {
                context.HttpContext.Response.Headers.RetryAfter = retryAfter.TotalSeconds.ToString();
            }

            context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.HttpContext.Response.ContentType = "application/json";

            await context.HttpContext.Response.WriteAsJsonAsync(new
            {
                error = "Too many requests. Please try again later.",
                message = "Rate limit exceeded. Please wait before making more requests.",
                retryAfter = retryAfter.TotalSeconds
            }, cancellationToken: CancellationToken);
        };


    });

    builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy("Application is running"), tags: ["api"])
    .AddDbContextCheck<AppDbContext>(
        name: "database",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["db", "sql"]
    );

    builder.Services.AddHealthChecksUI(setup =>
    {
        setup.SetEvaluationTimeInSeconds(10); // check every 10 seconds
        setup.MaximumHistoryEntriesPerEndpoint(50);
        setup.AddHealthCheckEndpoint("Instagram Api", "/healthz");
    })
    .AddInMemoryStorage();

    // Tự động scan toàn bộ Assembly chứa class CreatePostDtoValidator để đăng ký tất cả Validator
    builder.Services.AddValidatorsFromAssemblyContaining<CreatePostDtoValidator>();
    // (Tùy chọn) Cấu hình để ASP.NET Core tự động trả về 400 nếu validation fail
    builder.Services.AddFluentValidationAutoValidation();
    var app = builder.Build();

    // 0. Middleware xử lý lỗi Global
    app.UseMiddleware<GlobalExceptionMiddleware>();

    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";

        options.GetLevel = (httpContext, elapsed, ex) => ex != null
        ? LogEventLevel.Error
        : httpContext.Response.StatusCode >= 500
            ? LogEventLevel.Error
            : httpContext.Response.StatusCode >= 400
                ? LogEventLevel.Warning
                : LogEventLevel.Information;


        /*diagnosticContext → nơi bạn “gắn thêm field”
            httpContext → request hiện tại*/
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("UserName", httpContext.User?.Identity?.Name ?? "anomynous");

            diagnosticContext.Set("RemoteIP", httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");

            if (httpContext.User?.Identity?.IsAuthenticated == true)
            {
                diagnosticContext.Set("UserName", httpContext.User.FindFirst("sub")?.Value ?? "unknown");
            }
        };
    });



    // 2. Thêm đoạn này vào phần pipeline, PHẢI NẰM TRƯỚC app.UseAuthentication() và app.UseAuthorization()
    app.UseCors("InstagramCorsPolicy");

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

    app.UseRateLimiter();

    app.MapHub<ChatHub>("/chathub");

    app.MapControllers();


    //app.MapHealthChecks("/healthz", new HealthCheckOptions
    //{
    //    ResponseWriter = async (context, report) =>
    //    {
    //        context.Response.ContentType = "application/json";

    //        var response = new
    //        {
    //            status = report.Status.ToString(),
    //            checks = report.Entries.Select(e => new
    //            {
    //                name = e.Key,
    //                status = e.Value.Status.ToString(),
    //                description = e.Value.Description,
    //                duration = e.Value.Duration.TotalMilliseconds,
    //                exception = e.Value.Exception?.Message,
    //                data = e.Value.Data
    //            }),
    //            totalDuration = report.TotalDuration.TotalMilliseconds
    //        };

    //        await context.Response.WriteAsJsonAsync(response, new JsonSerializerOptions
    //        {
    //            WriteIndented = true
    //        });
    //    }
    //});

    // 
    app.MapHealthChecks("/healthz", new HealthCheckOptions
    {
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    });

    app.MapHealthChecks("/healthz/live", new HealthCheckOptions
    {
        Predicate = _ => false // Chỉ trả về "Healthy" nếu ứng dụng đang chạy, không kiểm tra bất kỳ thành phần nào khác
    });

    app.MapHealthChecks("/healthz/ready", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("db") // cái này dùng check database hoạt động 
    });

    app.MapHealthChecksUI(options =>
    {
        options.ApiPath = "/healthchecks-api";
        options.UIPath = "/healthchecks-ui";
    });

    Log.Information("Application started successfully.");

    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var context = services.GetRequiredService<AppDbContext>();
            if (context.Database.GetPendingMigrations().Any())
            {
                Log.Information("Applying pending migrations...");
                context.Database.Migrate();
                Log.Information("Migrations applied successfully.");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while migrating the database.");
        }
    }

    app.Run();

}
catch (Exception ex)
{
    Log.Fatal(ex, "Application start-up failed");
}
finally
{
    Log.CloseAndFlush();
}

