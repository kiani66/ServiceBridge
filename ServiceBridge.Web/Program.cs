using Microsoft.AspNetCore.Authentication.Cookies;
using MudBlazor.Services;
using ServiceBridge.Web.Components;
using ServiceBridge.Shared.Constants;
using ServiceBridge.Web.Handlers;
using ServiceBridge.Web.MockData;
using ServiceBridge.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Razor Pages
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/User");
    options.Conventions.AuthorizeFolder("/Admin", AppConstants.Roles.Admin);
    options.Conventions.AuthorizeFolder("/Agent", AppConstants.Roles.Agent);
});

// Blazor Web App foundation
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddMudServices();
builder.Services.AddSingleton<ServiceBridgeMockDataProvider>();
builder.Services.AddScoped<PublicCatalogClient>();
builder.Services.AddScoped<CheckoutClient>();
builder.Services.AddScoped<PaymentClient>();
builder.Services.AddScoped<UserOrdersClient>();
builder.Services.AddScoped<AgentOrdersClient>();
builder.Services.AddScoped<AdminOrdersClient>();
builder.Services.AddScoped<RefundsClient>();
builder.Services.AddScoped<AdminRefundsClient>();
builder.Services.AddScoped<NotificationsClient>();
builder.Services.AddScoped<TicketsClient>();
builder.Services.AddScoped<OrderRealtimeClient>();

// Cookie Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/Login";
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.Name = "ServiceBridge.Auth";
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AppConstants.Roles.Admin, p =>
        p.RequireRole(AppConstants.Roles.Admin));
    options.AddPolicy(AppConstants.Roles.Agent, p =>
        p.RequireRole(AppConstants.Roles.Agent));
});

// HttpClient for API communication
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<JwtForwardingHandler>();

builder.Services.AddHttpClient(AppConstants.Api.HttpClientName, client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiSettings:BaseUrl"]!);
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddHttpMessageHandler<JwtForwardingHandler>();

// Response compression
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

var app = builder.Build();

// Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseResponseCompression();

// Security headers
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Content-Security-Policy"] =
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; " +
        "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; " +
        "img-src 'self' data: https:; " +
        "font-src 'self' data:; " +
        "connect-src 'self' ws: wss:;";
    context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
    context.Response.Headers.Remove("Server");
    await next();
});

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapRazorPages();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
