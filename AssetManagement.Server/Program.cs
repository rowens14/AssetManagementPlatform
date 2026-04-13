/*
 * FILE: Program.cs
 * PROJECT: AssetManagement.Server
 * PURPOSE: Application entry point.
 *          - Serilog: structured logging to console + rolling daily log files
 *          - JWT Bearer authentication replacing the X-User-Id header pattern
 *          - Global exception handler returning clean JSON errors (no stack traces)
 *          - EnsureCreated + DatabaseSeeder on startup
 */

using System.Text;
using AssetManagement.Server.Data;
using AssetManagement.Server.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text.Json.Serialization;
using System.Net;

// ── Serilog bootstrap ─────────────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File(
        path: "logs/assetmgmt-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 90,       // keep 90 days of logs
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("AssetManagement.Server starting up");

    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    // ── Services ──────────────────────────────────────────────────────────────
    builder.Services.AddControllersWithViews()
        .AddJsonOptions(o =>
        {
            o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        });

    builder.Services.AddRazorPages();

    // JWT authentication
    var jwtKey = builder.Configuration["Jwt:Key"]!;
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer           = true,
                ValidateAudience         = true,
                ValidateLifetime         = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer              = builder.Configuration["Jwt:Issuer"],
                ValidAudience            = builder.Configuration["Jwt:Audience"],
                IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                ClockSkew                = TimeSpan.Zero   // no grace period on expiry
            };
        });

    builder.Services.AddAuthorization();
    builder.Services.AddScoped<JwtService>();

    // Database
    builder.Services.AddDbContext<AssetDbContext>(options =>
        options.UseNpgsql(
            builder.Configuration.GetConnectionString("AssetDb"),
            npg => npg.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)
        )
    );

    // ── App ───────────────────────────────────────────────────────────────────
    var app = builder.Build();

    // Global exception handler — catches unhandled exceptions and returns
    // a clean JSON error without exposing stack traces to the client
    app.UseExceptionHandler(errApp =>
    {
        errApp.Run(async context =>
        {
            var feature = context.Features.Get<IExceptionHandlerFeature>();
            var ex      = feature?.Error;

            Log.Error(ex, "Unhandled exception on {Method} {Path}",
                context.Request.Method, context.Request.Path);

            context.Response.StatusCode  = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                error   = "An unexpected error occurred. Please try again.",
                traceId = context.TraceIdentifier
            });
        });
    });

    if (app.Environment.IsDevelopment())
        app.UseWebAssemblyDebugging();
    else
        app.UseHsts();

    app.UseHttpsRedirection();
    app.UseBlazorFrameworkFiles();
    app.UseStaticFiles();
    app.UseRouting();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapRazorPages();
    app.MapControllers();
    app.MapFallbackToFile("index.html");

    // ── Database init ─────────────────────────────────────────────────────────
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AssetDbContext>();
        try
        {
            await db.Database.EnsureCreatedAsync();
            Log.Information("Migrations applied");
            await DatabaseSeeder.SeedAsync(db);
            Log.Information("Seed complete");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Database initialisation failed");
            throw;
        }
    }

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Server terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
