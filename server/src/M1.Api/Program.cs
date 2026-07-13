using System.Text;
using System.Threading.RateLimiting;
using FluentValidation;
using M1.Api.Filters;
using M1.Api.Middleware;
using M1.Application.Auth;
using M1.Infrastructure;
using M1.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Structured logging to stdout — Render/Azure capture and index console output.
builder.Host.UseSerilog((context, config) => config
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

builder.Services.AddControllers(options => options.Filters.Add<ValidationFilter>());
builder.Services.AddValidatorsFromAssemblyContaining<RegisterValidator>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "M1 Stores API",
        Version = "v1",
        Description = "REST API for the M1 Stores e-commerce marketplace — shoes, handbags, cosmetics, jewelry and accessories.",
        Contact = new() { Name = "Mary Wainaina", Email = "waynmary9@gmail.com" }
    });
    options.AddSecurityDefinition("Bearer", new()
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Paste an access token from /api/v1/auth/login"
    });
    options.AddSecurityRequirement(new()
    {
        {
            new() { Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" } },
            []
        }
    });
});

// JWT bearer authentication — tokens issued by JwtService.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "M1Stores",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "M1Stores",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key missing"))),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });
builder.Services.AddAuthorization();

// Brute-force protection on the auth endpoints: 10 requests/min per client IP.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "anon",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));
});

// CORS origins come from configuration so prod (Vercel) and dev (Vite) differ by env only.
var corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddPolicy("Frontend", policy =>
    policy.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>();

var app = builder.Build();

// Database init: migrations on PostgreSQL (production), EnsureCreated for the
// zero-setup SQLite/SQL Server dev paths. Seeding is idempotent.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var provider = builder.Configuration["Database:Provider"] ?? "Sqlite";
    if (provider == "Postgres")
        await db.Database.MigrateAsync();
    else
        await db.Database.EnsureCreatedAsync();

    await DbSeeder.SeedAsync(db, builder.Configuration, app.Logger);
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseSerilogRequestLogging();

// Swagger stays on in production deliberately: interactive API docs are part of the demo.
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "M1 Stores API v1");
    options.DocumentTitle = "M1 Stores API";
});

app.UseCors("Frontend");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

app.Run();
