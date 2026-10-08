using InstagramClone.API.Extensions;
using InstagramClone.API.Middlewares;
using InstagramClone.Infrastructure.Persistence;
using InstagramClone.Infrastructure.SignalR;
using InstagramClone.Infrastructure.BackgroundJobs;
using Hangfire;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;

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



    // Add services to the container using extension methods
    builder.Services.AddApplicationServices();
    builder.Services.AddInfrastructureServices(builder.Configuration);
    builder.Services.AddIdentityAndAuthServices(builder.Configuration);
    builder.Services.AddSwaggerAndApiServices(builder.Configuration);
    builder.Services.AddHealthCheckServices(builder.Configuration);

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

    // Enable Swagger UI across all environments for easy API testing
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "InstagramClone API v1");
        c.RoutePrefix = "swagger";
    });

    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
    });
    // app.UseHttpsRedirection(); // Tạm thời comment dòng này vì nó tự động redirect HTTP sang HTTPS (và thường redirect về localhost) khi chạy qua Docker/Nginx.
    app.UseStaticFiles();


    app.UseAuthentication();

    app.UseAuthorization();

    // Hangfire Dashboard to monitor background jobs
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        DashboardTitle = "InstagramClone Background Jobs",
        Authorization = new[] { new HangfireDashboardAuthorizationFilter() }
    });

    app.UseRateLimiter();

    app.UseStatusCodePages();

    app.MapHub<ChatHub>("/chathub");
    app.MapHub<NotificationHub>("/hubs/notifications");

    app.MapControllers();


    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        ResponseWriter = async (context, report) =>
        {
            context.Response.ContentType = "application/json";

            var response = new
            {
                status = report.Status.ToString(),
                totalDuration = $"{report.TotalDuration.TotalMilliseconds:F2}ms",
                checks = report.Entries.Select(e => new
                {
                    name = e.Key,
                    status = e.Value.Status.ToString(),
                    description = e.Value.Description,
                    duration = $"{e.Value.Duration.TotalMilliseconds:F2}ms",
                    exception = e.Value.Exception?.Message
                })
            };

            await context.Response.WriteAsJsonAsync(response);
        }
    });

    Log.Information("Application started successfully.");

    // Đăng ký Hangfire Recurring Background Jobs
    using (var scope = app.Services.CreateScope())
    {
        var backgroundJobService = scope.ServiceProvider.GetRequiredService<InstagramClone.Application.Interfaces.Services.IBackgroundJobService>();
        
        // 1. Dọn dẹp Refresh Token hết hạn (Hàng ngày lúc 03:00 UTC)
        backgroundJobService.AddOrUpdateRecurring<InstagramClone.Application.Interfaces.BackgroundJobs.ITokenCleanupJob>(
            "cleanup-expired-refresh-tokens",
            job => job.CleanupExpiredRefreshTokensAsync(),
            Cron.Daily(3));

        // 2. Dọn dẹp file media đã bị soft-deleted quá 30 ngày (Hàng ngày lúc 02:00 UTC)
        backgroundJobService.AddOrUpdateRecurring<InstagramClone.Application.Interfaces.BackgroundJobs.IMediaCleanupJob>(
            "cleanup-soft-deleted-media",
            job => job.CleanupSoftDeletedMediaAsync(30),
            Cron.Daily(2));
    }

    if (!app.Environment.IsProduction())
    {
        // Tự động apply migration khi app start (nếu có migration mới) - áp dụng cho dev/staging môi trường container
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

