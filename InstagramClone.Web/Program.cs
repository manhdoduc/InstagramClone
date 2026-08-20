using Blazored.LocalStorage;
using InstagramClone.Web.Authentication;
using InstagramClone.Web.Configuration;
using InstagramClone.Web.Components;
using InstagramClone.Web.Services;
using InstagramClone.Web.Services.Auth;
using InstagramClone.Web.Services.Posts;
using InstagramClone.Web.Services.Users;
using InstagramClone.Web.Services.Comments;
using InstagramClone.Web.Services.Follows;
using InstagramClone.Web.Services.Chat;
using InstagramClone.Web.Services.Notifications;
using InstagramClone.Web.Services.Stories;
using InstagramClone.Web.Services.SignalR;
using InstagramClone.Web.State;
using Microsoft.AspNetCore.Components.Authorization;

var builder = WebApplication.CreateBuilder(args);

// ─────────────────────────────────────────────────────────────
// 1. CONFIGURATION
// ─────────────────────────────────────────────────────────────
builder.Services.Configure<ApiSettings>(builder.Configuration.GetSection("ApiSettings"));
var apiBaseUrl = builder.Configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5063";

// ─────────────────────────────────────────────────────────────
// 2. BLAZOR & RAZOR COMPONENTS
// ─────────────────────────────────────────────────────────────
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ─────────────────────────────────────────────────────────────
// 3. AUTHENTICATION (JWT + Custom AuthStateProvider)
// ─────────────────────────────────────────────────────────────
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddScoped<TokenStorageService>();
builder.Services.AddScoped<CustomAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<CustomAuthStateProvider>());
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = "BlazorCookie";
    options.DefaultChallengeScheme = "BlazorCookie";
    options.DefaultSignInScheme = "BlazorCookie";
})
.AddCookie("BlazorCookie", options =>
{
    options.LoginPath = "/login";
    options.LogoutPath = "/logout";
    options.AccessDeniedPath = "/login";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});
builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();

// ─────────────────────────────────────────────────────────────
// 4. HTTP CLIENTS (Typed Clients với AuthenticationDelegatingHandler)
// ─────────────────────────────────────────────────────────────
builder.Services.AddScoped<AuthenticationDelegatingHandler>();

builder.Services.AddScoped(sp =>
{
    // Resolve the handler from the current Blazor circuit scope
    var handler = sp.GetRequiredService<AuthenticationDelegatingHandler>();
    handler.InnerHandler = new HttpClientHandler();

    var client = new HttpClient(handler)
    {
        BaseAddress = new Uri(apiBaseUrl),
        Timeout = TimeSpan.FromSeconds(30)
    };
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    return client;
});

// ─────────────────────────────────────────────────────────────
// 5. APP STATE
// ─────────────────────────────────────────────────────────────
builder.Services.AddScoped<AppState>();

// ─────────────────────────────────────────────────────────────
// 6. DOMAIN SERVICES & SIGNALR
// ─────────────────────────────────────────────────────────────
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPostService, PostService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<IFollowService, FollowService>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IStoryService, StoryService>();

// SignalR Services (Scoped per circuit/connection)
builder.Services.AddScoped<ChatHubService>();
builder.Services.AddScoped<NotificationHubService>();

// ─────────────────────────────────────────────────────────────
// 7. BUILD & PIPELINE
// ─────────────────────────────────────────────────────────────
var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // Không dùng UseHsts/UseHttpsRedirection trong Docker (chạy HTTP nội bộ)
}

// Proxy /media requests to the API container so images load correctly from the frontend
app.Map("/media", mediaApp =>
{
    mediaApp.Run(async context =>
    {
        var apiBase = app.Configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5063";
        var targetUrl = $"{apiBase}/media{context.Request.Path}{context.Request.QueryString}";
        
        Console.WriteLine($"[MediaProxy] Proxying request to: {targetUrl}");

        using var client = new HttpClient();
        using var request = new HttpRequestMessage(new HttpMethod(context.Request.Method), targetUrl);
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        Console.WriteLine($"[MediaProxy] Response from API: {(int)response.StatusCode} {response.StatusCode}");

        context.Response.StatusCode = (int)response.StatusCode;
        foreach (var header in response.Content.Headers)
        {
            context.Response.Headers[header.Key] = header.Value.ToArray();
        }
        // Cache control
        context.Response.Headers.CacheControl = "public,max-age=31536000";

        await response.Content.CopyToAsync(context.Response.Body);
    });
});

app.UseStaticFiles();
app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
