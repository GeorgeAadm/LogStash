using Amazon.DynamoDBv2;
using EventLogger.Api.Application.Services;
using EventLogger.Api.Application.Validators;
using EventLogger.Api.Infrastructure.Configuration;
using EventLogger.Api.Infrastructure.Data;
using EventLogger.Api.Infrastructure.Repositories;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Add AWS Lambda support
builder.Services.AddAWSLambdaHosting(LambdaEventSource.RestApi);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "EventLogger API",
        Version = "v1",
        Description = "API for logging and retrieving user events"
    });
});

// Configure FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<CreateEventRequestValidator>();

// Configure Entity Framework for SQL Server
var sqlConnectionString = builder.Configuration.GetConnectionString("SqlServer");
if (string.IsNullOrEmpty(sqlConnectionString))
{
    throw new InvalidOperationException("SQL Server connection string is not configured. Please check your appsettings.json");
}
builder.Services.AddDbContext<EventsDbContext>(options =>
    options.UseSqlServer(sqlConnectionString));

// Configure DynamoDB
var dynamoDbConfig = builder.Configuration.GetSection("DynamoDb").Get<DynamoDbConfiguration>();
if (builder.Environment.IsDevelopment() && dynamoDbConfig != null)
{
    // For local development with DynamoDB Local
    var clientConfig = new AmazonDynamoDBConfig
    {
        ServiceURL = dynamoDbConfig.ServiceUrl
    };
    builder.Services.AddSingleton<IAmazonDynamoDB>(new AmazonDynamoDBClient(clientConfig));
}
else
{
    // For production/AWS environment
    builder.Services.AddSingleton<IAmazonDynamoDB, AmazonDynamoDBClient>();
}

// Register configuration
builder.Services.Configure<DynamoDbConfiguration>(builder.Configuration.GetSection("DynamoDb"));

// Register repositories
builder.Services.AddScoped<IEventMetadataRepository, EventMetadataRepository>();
builder.Services.AddScoped<IEventDetailsRepository, EventDetailsRepository>();

// Register services
builder.Services.AddScoped<IEventService, EventService>();

// Add health checks
builder.Services.AddHealthChecks()
    .AddSqlServer(sqlConnectionString, name: "sqlserver", tags: new[] { "db", "sql" })
    .AddCheck<DynamoDbHealthCheck>("dynamodb", tags: new[] { "db", "nosql" });

// Configure CORS for development
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevelopmentPolicy",
        policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "EventLogger API V1");
        c.RoutePrefix = "swagger";
    });
    app.UseCors("DevelopmentPolicy");
}

app.UseSerilogRequestLogging(); 

if (!app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/api/healthcheck");

// Ensure database is created in development
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<EventsDbContext>();
    dbContext.Database.EnsureCreated();
}

try
{
    Log.Information("Starting EventLogger API");
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

// Make the implicit Program class public so test projects can access it
public partial class Program { }