using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ServiceBridge.Api.Middleware;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Application.Orders;
using ServiceBridge.Application.Services;
using ServiceBridge.Application.Settings;
using ServiceBridge.Infrastructure.Data;
using ServiceBridge.Infrastructure.Repositories;
using ServiceBridge.Infrastructure.Services;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ===================================================
// 1. SERVICES REGISTRATION
// ===================================================

builder.Services.AddControllers();
builder.Services.AddDataProtection();
builder.Services.Configure<MessageOutboxSettings>(
    builder.Configuration.GetSection(MessageOutboxSettings.SectionName));

// EF Core with SQL Server
builder.Services.AddDbContext<ServiceBridgeDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// JWT settings
var jwtIssuer = builder.Configuration["Jwt:Issuer"]!;
var jwtAudience = builder.Configuration["Jwt:Audience"]!;
var jwtSigningKey = builder.Configuration["Jwt:SigningKey"]!;
var jwtExpirationMinutes = int.Parse(builder.Configuration["Jwt:ExpirationMinutes"]!);

// Application services
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAdminSeeder, AdminSeeder>();
builder.Services.AddScoped<IDevelopmentDataSeeder, DevelopmentDataSeeder>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IPasswordResetService, PasswordResetService>();
builder.Services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
builder.Services.AddScoped<IAdminUserQueryRepository, AdminUserQueryRepository>();
builder.Services.AddScoped<IAdminUserQueryService, AdminUserQueryService>();
builder.Services.AddScoped<IAdminUserCommandRepository, AdminUserCommandRepository>();
builder.Services.AddScoped<IAdminUserManagementService, AdminUserManagementService>();
builder.Services.AddScoped<IServiceCatalogRepository, ServiceCatalogRepository>();
builder.Services.AddScoped<IServiceCatalogQueryService, ServiceCatalogQueryService>();
builder.Services.AddScoped<IAdminCatalogService, AdminCatalogService>();
builder.Services.AddScoped<IPricingRepository, PricingRepository>();
builder.Services.AddScoped<IPricingResolver, PricingResolver>();
builder.Services.AddScoped<IAdminPricingRepository, AdminPricingRepository>();
builder.Services.AddScoped<IAdminPricingQueryService, AdminPricingQueryService>();
builder.Services.AddScoped<IAdminPricingService, AdminPricingService>();
builder.Services.AddScoped<IPricingQuoteService, PricingQuoteService>();
builder.Services.AddScoped<IOrderCreationRepository, OrderCreationRepository>();
builder.Services.AddScoped<IOrderCreationService, OrderCreationService>();
builder.Services.AddScoped<IUserOrderQueryRepository, UserOrderQueryRepository>();
builder.Services.AddScoped<IUserOrderCommandRepository, UserOrderCommandRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IMockPaymentGateway, MockPaymentGateway>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IMessageOutboxRepository, MessageOutboxRepository>();
builder.Services.AddScoped<IMessageOutboxService, MessageOutboxService>();
builder.Services.AddScoped<IMessageDeliveryProvider, NoOpMessageDeliveryProvider>();
builder.Services.AddScoped<IMessageOutboxProcessor, MessageOutboxProcessor>();
builder.Services.AddHostedService<MessageOutboxBackgroundService>();
builder.Services.AddScoped<IAdminOrderQueryRepository, AdminOrderQueryRepository>();
builder.Services.AddScoped<IAdminOrderCommandRepository, AdminOrderCommandRepository>();
builder.Services.AddScoped<IAdminOrderQueryService, AdminOrderQueryService>();
builder.Services.AddScoped<IAdminOrderAssignmentService, AdminOrderAssignmentService>();
builder.Services.AddScoped<IAgentOrderQueryRepository, AgentOrderQueryRepository>();
builder.Services.AddScoped<IAgentOrderCommandRepository, AgentOrderCommandRepository>();
builder.Services.AddScoped<IAgentOrderQueryService, AgentOrderQueryService>();
builder.Services.AddScoped<IAgentOrderProcessingService, AgentOrderProcessingService>();
builder.Services.AddScoped<GetMyOrdersQuery>();
builder.Services.AddScoped<GetMyOrderDetailQuery>();
builder.Services.AddScoped<CancelMyOrderCommand>();
builder.Services.AddScoped<RespondToCustomerActionCommand>();
builder.Services.AddSingleton<IOrderFieldProtector, DataProtectionOrderFieldProtector>();
builder.Services.AddSingleton<IOrderFieldValidator, OrderFieldValidator>();
builder.Services.AddSingleton<IOrderNumberGenerator, OrderNumberGenerator>();
builder.Services.AddSingleton<IOrderFactory, OrderFactory>();
builder.Services.AddSingleton<IOrderStatusPolicy, OrderStatusPolicy>();
builder.Services.AddSingleton<IPricingCalculator, PricingCalculator>();
builder.Services.AddSingleton<IJwtTokenService>(new JwtTokenService(
    jwtIssuer, jwtAudience, jwtSigningKey, jwtExpirationMinutes));

// JWT Bearer Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSigningKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

// Swagger (development only)
builder.Services.AddEndpointsApiExplorer();


builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ServiceBridge.Api",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

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
            Array.Empty<string>()
        }
    });
});

// Invalid ModelState → ServiceResult.ValidationFailed
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = new Dictionary<string, string[]>();
        foreach (var (key, value) in context.ModelState)
        {
            if (value.Errors.Count > 0)
            {
                errors[key] = value.Errors.Select(e => e.ErrorMessage).ToArray();
            }
        }

        var result = ServiceBridge.Application.Results.ServiceResult.ValidationFailed(errors);
        return new ObjectResult(result) { StatusCode = 400 };
    };
});

// Global exception handler
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// ===================================================
// 2. HEALTH CHECKS (Step 9 - Hardening)
// ===================================================
builder.Services.AddHealthChecks();

// ===================================================
// 3. CORS (Step 9 - Hardening) — for future browser-based API access
// ===================================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("ServiceBridgeWebPolicy", policy =>
    {
        policy.WithOrigins("https://localhost:7077")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// ===================================================
// 4. RESPONSE COMPRESSION (Step 9 - Hardening)
// ===================================================
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProvider>();
    options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProvider>();
});

builder.Services.Configure<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProviderOptions>(
    options => options.Level = System.IO.Compression.CompressionLevel.Fastest);

var app = builder.Build();

// ===================================================
// 5. DATABASE MIGRATION (Step 9 - Hardening)
// ===================================================
/*
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ServiceBridgeDbContext>();

    try
    {
        if (app.Environment.IsDevelopment())
        {
            db.Database.Migrate();
        }
        else
        {
            // Production: never use EnsureCreated, always Migrate
            db.Database.Migrate();
        }
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
    }
}*/

// ===================================================
// 6. PIPELINE CONFIGURATION
// ===================================================

// Global exception handler — must be first
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Security: HTTPS redirection + HSTS
app.UseHttpsRedirection();
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

// CORS (must be before UseAuthentication/UseAuthorization)
app.UseCors("ServiceBridgeWebPolicy");

// Response compression
app.UseResponseCompression();

// Security headers middleware
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; img-src 'self' data: https:;";
    context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
    context.Response.Headers["X-Permitted-Cross-Domain-Policies"] = "none";
    context.Response.Headers.Remove("Server");
    await next();
});

// Request logging middleware (Step 9 - Hardening)
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    var startTime = DateTime.UtcNow;

    try
    {
        await next();
    }
    finally
    {
        var elapsed = DateTime.UtcNow - startTime;
        logger.LogInformation(
            "Request {Method} {Path} -> {StatusCode} in {ElapsedMs}ms",
            context.Request.Method,
            context.Request.Path,
            context.Response.StatusCode,
            elapsed.TotalMilliseconds);
    }
});

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

using (var adminSeedScope = app.Services.CreateScope())
{
    await adminSeedScope.ServiceProvider
        .GetRequiredService<IAdminSeeder>()
        .SeedAsync();
}

if (app.Environment.IsDevelopment())
{
    using var developmentSeedScope = app.Services.CreateScope();
    await developmentSeedScope.ServiceProvider
        .GetRequiredService<IDevelopmentDataSeeder>()
        .SeedAsync();
}

// Health check endpoints
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.Run();
