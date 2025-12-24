using System.Text;
using Api.Application.Interfaces;
using Api.Application.Services;
using Api.Entities;
using Api.Infrastructure.Auth;
using Api.Infrastructure.Data;
using Api.Infrastructure.Notifications;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/api-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Add Serilog
    builder.Host.UseSerilog();

    // Database configuration
    builder.Services.AddDbContext<shift_change_bdContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

    // JWT Configuration
    var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
        ?? throw new InvalidOperationException("JWT configuration is missing");

    builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));

    // N8n Configuration
    builder.Services.Configure<N8nOptions>(builder.Configuration.GetSection(N8nOptions.SectionName));
    builder.Services.AddHttpClient<INotifier, N8nNotifier>();

    // Authentication
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtOptions.Issuer,
                ValidAudience = jwtOptions.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtOptions.Key))
            };
        });

    // Services
    builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IShiftService, ShiftService>();
    builder.Services.AddScoped<IShiftSwapService, ShiftSwapService>();

    builder.Services.AddControllers();
    builder.Services.AddOpenApi();

    // Swagger (Swashbuckle) generator
    builder.Services.AddSwaggerGen();

    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend", policy =>
        {
            if (allowedOrigins.Length == 0 || (allowedOrigins.Length == 1 && allowedOrigins[0] == "*"))
            {
                policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
            }
            else
            {
                policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
            }
        });
    });

    var app = builder.Build();

    // Use Serilog request logging
    app.UseSerilogRequestLogging();

    // Configure the HTTP request pipeline
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "ShiftChange API v1");
        options.RoutePrefix = string.Empty;
    });

    app.UseHttpsRedirection();

    app.UseAuthentication();
    app.UseAuthorization();
    app.UseCors("AllowFrontend");
    app.MapControllers();

    // Initialize database
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<shift_change_bdContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        
        try
        {
            context.Database.Migrate();
            await DatabaseSeeder.SeedAsync(context);
            logger.LogInformation("Database initialized successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error initializing database");
            throw;
        }
    }

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
