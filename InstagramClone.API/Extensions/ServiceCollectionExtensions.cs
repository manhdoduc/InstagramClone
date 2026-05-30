using FluentValidation;
using FluentValidation.AspNetCore;
using InstagramClone.API.Filters;
using InstagramClone.Application.Interfaces;
using InstagramClone.Application.Interfaces.Caching;
using InstagramClone.Application.Interfaces.Chats;
using InstagramClone.Application.Interfaces.Data;
using InstagramClone.Application.Interfaces.Services;
using InstagramClone.Application.Services;
using InstagramClone.Application.Validators;
using InstagramClone.Common.Models.Config;
using InstagramClone.Domain.Entities;
using InstagramClone.Infrastructure.Data;
using InstagramClone.Infrastructure.Repositories;
using InstagramClone.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Swashbuckle.AspNetCore.Filters;
using System.Reflection;
using System.Text;
using System.Threading.RateLimiting;

namespace InstagramClone.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        
        // Application layer services
        services.AddScoped<IAuthServices, AuthServices>();
        services.AddScoped<IUserServices, UserServices>();
        services.AddScoped<IPostServices, PostServices>();
        services.AddScoped<ICommentServices, CommentServices>();
        services.AddScoped<IFollowService, FollowServices>();
        services.AddScoped<IChatService, ChatServices>();
        
        services.AddAutoMapper(cfg => cfg.AddMaps(AppDomain.CurrentDomain.GetAssemblies()));
        
        services.AddValidatorsFromAssemblyContaining<CreatePostDtoValidator>();
        services.AddFluentValidationAutoValidation();
        
        return services;
    }

    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Infrastructure layer services
        services.AddScoped<IStorageServices, LocalStorageServices>();
        services.AddScoped<ICurrentUserService, CurrentUserServices>();
        services.AddScoped<IChatNotificationService, ChatNotificationService>();
        services.AddScoped<ICacheService, MemoryCacheService>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        
        // Database
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddScoped<IApplicationDbContext, AppDbContext>();
        services.AddDbContextPool<AppDbContext>(options =>
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
            options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        }, poolSize: 128);

        return services;
    }

    public static IServiceCollection AddIdentityAndAuthServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddIdentity<AppUser, IdentityRole>(options => {
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 6;
            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

        services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));
        var jwtSettings = configuration.GetSection("JwtSettings").Get<JwtSettings>() ?? new JwtSettings();

        if(jwtSettings == null || string.IsNullOrEmpty(jwtSettings.Key))
        {
            Log.Fatal("JWT settings are not configured properly.");
            throw new Exception("JWT settings are not configured properly."); 
        }

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = configuration["JwtSettings:Issuer"],
                ValidateAudience = true,
                ValidAudience = configuration["JwtSettings:Audience"],
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

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

        return services;
    }

    public static IServiceCollection AddSwaggerAndApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Version = "v1",
                Title = "InstagramClone API",
                Description = "API for user",
                Contact = new OpenApiContact { Name = "Support Team", Email = "support@hotellisting.com" },
                License = new OpenApiLicense { Name = "MIT License", Url = new Uri("https://opensource.org/licenses/MIT") }
            });

            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }

            options.EnableAnnotations();
            
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer",
                BearerFormat = "JWT"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                    },
                    Array.Empty<string>()
                }
            });

            options.ExampleFilters();
            options.OperationFilter<InstagramClone.API.Filters.SecurityRequirementsOperationFilter>();
            options.OrderActionsBy(apiDesc => $"{apiDesc.RelativePath}_{apiDesc.HttpMethod}");
        });

        services.AddSwaggerExamplesFromAssemblyOf<Program>();

        services.AddCors(options =>
        {
            options.AddPolicy("InstagramCorsPolicy", policy =>
            {
                var origins = configuration.GetSection("AllowedOrigins").Get<string[]>();
                if (origins != null && origins.Length > 0)
                {
                    policy.WithOrigins(origins)
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
                }
                else
                {
                    Log.Warning("No allowed origins configured for CORS.");
                }
            });
        });

        services.AddSignalR();
        services.AddDistributedMemoryCache();

        services.AddRateLimiter(options =>
        {
            options.AddFixedWindowLimiter("LoginLimit", limiterOptions =>
            {
                limiterOptions.PermitLimit = 5;
                limiterOptions.Window = TimeSpan.FromMinutes(5);
                limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            });

            options.AddFixedWindowLimiter("UploadLimit", limiterOptions =>
            {
                limiterOptions.PermitLimit = 10;
                limiterOptions.Window = TimeSpan.FromHours(1);
                limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                limiterOptions.QueueLimit = 2;
            });

            options.AddFixedWindowLimiter("CommentLimit", limiterOptions =>
            {
                limiterOptions.PermitLimit = 5;
                limiterOptions.Window = TimeSpan.FromMinutes(1);
                limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                limiterOptions.QueueLimit = 2;
            });

            options.AddPolicy("PerUser", context =>
            {
                var username = context.User?.Identity?.Name ?? "anonymous";
                return RateLimitPartition.GetSlidingWindowLimiter(username, _ => new SlidingWindowRateLimiterOptions
                {
                    Window = TimeSpan.FromMinutes(1),
                    PermitLimit = 10,
                    SegmentsPerWindow = 5,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 5
                });
            });

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                var username = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(username, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 100,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 5
                });
            });

            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

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

        return services;
    }

    public static IServiceCollection AddHealthCheckServices(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Application is running"), tags: ["api"])
            .AddDbContextCheck<AppDbContext>(
                name: "database",
                failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy,
                tags: ["db", "sql"]
            );

        services.AddHealthChecksUI(setup =>
        {
            setup.SetEvaluationTimeInSeconds(10);
            setup.MaximumHistoryEntriesPerEndpoint(50);
            setup.AddHealthCheckEndpoint("Instagram Api", "/healthz");
        })
        .AddInMemoryStorage();

        return services;
    }
}
