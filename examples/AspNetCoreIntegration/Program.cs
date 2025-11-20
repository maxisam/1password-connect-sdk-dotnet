// ASP.NET Core Integration Example for 1Password .NET SDK
// Demonstrates how to integrate 1Password secrets into an ASP.NET Core application

using OnePassword.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Add 1Password configuration provider
// This will automatically resolve any op:// URIs in appsettings.json
builder.Configuration.AddOnePassword();

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure database connection (uses 1Password secret from appsettings.json)
// Example: builder.Services.AddDbContext<AppDbContext>(options =>
//     options.UseSqlServer(builder.Configuration["ConnectionStrings:Database"]));

// Configure external API keys (uses 1Password secrets)
builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Example endpoint: Health check
app.MapGet("/health", () => new
{
    Status = "Healthy",
    Timestamp = DateTime.UtcNow,
    Message = "1Password integration is working!"
})
.WithName("HealthCheck")
.WithOpenApi();

// Example endpoint: Configuration status
app.MapGet("/config/status", (IConfiguration config) =>
{
    // Demonstrate configuration values are available
    // Note: We only show metadata, never actual secret values!
    var hasDbConnection = !string.IsNullOrEmpty(config["ConnectionStrings:Database"]);
    var hasApiKey = !string.IsNullOrEmpty(config["ExternalServices:ApiKey"]);
    var hasSecretKey = !string.IsNullOrEmpty(config["ExternalServices:SecretKey"]);

    return new
    {
        ConfigurationLoaded = true,
        SecretsResolved = new
        {
            DatabaseConnection = hasDbConnection ? "✅ Configured" : "❌ Missing",
            ApiKey = hasApiKey ? "✅ Configured" : "❌ Missing",
            SecretKey = hasSecretKey ? "✅ Configured" : "❌ Missing"
        },
        Message = "Configuration values are loaded (secrets are protected and not displayed)",
        Timestamp = DateTime.UtcNow
    };
})
.WithName("ConfigurationStatus")
.WithOpenApi();

// Example endpoint: Simulate database query
app.MapGet("/api/data", (IConfiguration config) =>
{
    var connectionString = config["ConnectionStrings:Database"];

    if (string.IsNullOrEmpty(connectionString))
    {
        return Results.Problem(
            title: "Configuration Error",
            detail: "Database connection string not configured",
            statusCode: StatusCodes.Status500InternalServerError);
    }

    // In a real app, you would use this connection string with Entity Framework
    // For this example, we just confirm it's available
    return Results.Ok(new
    {
        Message = "Database connection string is configured",
        Note = "In production, this would execute actual database queries",
        Timestamp = DateTime.UtcNow
    });
})
.WithName("GetData")
.WithOpenApi();

// Example endpoint: Simulate external API call
app.MapGet("/api/external", (IConfiguration config) =>
{
    var apiKey = config["ExternalServices:ApiKey"];
    var secretKey = config["ExternalServices:SecretKey"];

    if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(secretKey))
    {
        return Results.Problem(
            title: "Configuration Error",
            detail: "External service credentials not configured",
            statusCode: StatusCodes.Status500InternalServerError);
    }

    // In a real app, you would use these credentials to call external services
    return Results.Ok(new
    {
        Message = "External service credentials are configured",
        Note = "In production, this would make actual API calls using the credentials",
        Timestamp = DateTime.UtcNow
    });
})
.WithName("CallExternalService")
.WithOpenApi();

Console.WriteLine("========================================");
Console.WriteLine("1Password ASP.NET Core Integration Example");
Console.WriteLine("========================================");
Console.WriteLine();
Console.WriteLine("Available endpoints:");
Console.WriteLine("  GET /health              - Health check");
Console.WriteLine("  GET /config/status       - Configuration status");
Console.WriteLine("  GET /api/data            - Simulate database query");
Console.WriteLine("  GET /api/external        - Simulate external API call");
Console.WriteLine("  GET /swagger             - API documentation");
Console.WriteLine();
Console.WriteLine("Configuration tips:");
Console.WriteLine("  - Secrets are loaded from appsettings.json (op:// URIs)");
Console.WriteLine("  - Override with environment variables (e.g., ConnectionStrings__Database)");
Console.WriteLine("  - Check logs to see 1Password provider loading");
Console.WriteLine();

app.Run();
