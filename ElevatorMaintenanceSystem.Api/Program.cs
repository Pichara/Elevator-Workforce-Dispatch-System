using ElevatorMaintenanceSystem.Api.Authentication;
using ElevatorMaintenanceSystem.Data;
using ElevatorMaintenanceSystem.Infrastructure;
using ElevatorMaintenanceSystem.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddCors(options =>
{
    options.AddPolicy(ApiKeyAuthenticationHandler.LocalhostCorsPolicy, policy =>
    {
        policy
            .WithOrigins(
                "http://localhost",
                "https://localhost",
                "http://localhost:5000",
                "https://localhost:5001",
                "http://localhost:3000",
                "http://localhost:4200",
                "http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services
    .AddAuthentication(ApiKeyAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
        ApiKeyAuthenticationHandler.SchemeName,
        options => { });

builder.Services.AddAuthorization();

builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection(MongoDbSettings.SectionName));

var mongoSettings = builder.Configuration
    .GetSection(MongoDbSettings.SectionName)
    .Get<MongoDbSettings>()
    ?? new MongoDbSettings();

builder.Services.AddSingleton(mongoSettings);
builder.Services.AddSingleton<IMongoDbContext, MongoDbContext>();
builder.Services.AddSingleton<GpsCoordinateValidator>();
builder.Services.AddSingleton<IUserContext, WindowsUserContext>();
builder.Services.AddScoped<IElevatorRepository, ElevatorRepository>();
builder.Services.AddScoped<IWorkerRepository, WorkerRepository>();
builder.Services.AddScoped<ITicketRepository, TicketRepository>();
builder.Services.AddScoped<IElevatorService, ElevatorService>();
builder.Services.AddScoped<IWorkerService, WorkerService>();
builder.Services.AddScoped<ITicketService, TicketService>();

var app = builder.Build();

app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (KeyNotFoundException ex)
    {
        await WriteProblemAsync(context, StatusCodes.Status404NotFound, "Not Found", ex.Message);
    }
    catch (ArgumentOutOfRangeException ex)
    {
        await WriteProblemAsync(context, StatusCodes.Status400BadRequest, "Bad Request", ex.Message);
    }
    catch (ArgumentException ex)
    {
        await WriteProblemAsync(context, StatusCodes.Status400BadRequest, "Bad Request", ex.Message);
    }
    catch (InvalidOperationException ex)
    {
        await WriteProblemAsync(context, StatusCodes.Status409Conflict, "Conflict", ex.Message);
    }
});

if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseHttpsRedirection();
}

app.UseCors(ApiKeyAuthenticationHandler.LocalhostCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers().RequireAuthorization();

app.Run();

static async Task WriteProblemAsync(HttpContext context, int statusCode, string title, string detail)
{
    if (context.Response.HasStarted)
    {
        return;
    }

    context.Response.Clear();
    context.Response.StatusCode = statusCode;
    context.Response.ContentType = "application/problem+json";

    var payload = new ProblemDetails
    {
        Status = statusCode,
        Title = title,
        Detail = detail,
        Instance = context.Request.Path
    };

    await context.Response.WriteAsJsonAsync(payload);
}

public partial class Program;
