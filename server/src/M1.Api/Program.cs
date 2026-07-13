using M1.Api.Middleware;
using M1.Infrastructure;
using M1.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Structured logging to stdout — Render/Azure capture and index console output.
builder.Host.UseSerilog((context, config) => config
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

builder.Services.AddControllers();
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
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

app.Run();
