using HealthChecks.UI.Client;
using InstagramClone.API.Extensions;
using InstagramClone.API.Middlewares;
using InstagramClone.Infrastructure.Persistence;
using InstagramClone.Infrastructure.SignalR;
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
    builder.Services.AddHealthCheckServices();

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

    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
    });
    // app.UseHttpsRedirection(); // Tạm thời comment dòng này vì nó tự động redirect HTTP sang HTTPS (và thường redirect về localhost) khi chạy qua Docker/Nginx.
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
        
        app.Run();
    }
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application start-up failed");
}
finally
{
    Log.CloseAndFlush();
}

