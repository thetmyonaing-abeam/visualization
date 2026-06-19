using Microsoft.EntityFrameworkCore;
using Visualization.API.Data;
using Visualization.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
        options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Visualization API", Version = "v1" });
});

// MySQL with Entity Framework Core
var connectionString = builder.Configuration.GetConnectionString("MySQL")
    ?? "Server=localhost;Port=3306;Database=visualization;User=root;Password=password123;";
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySQL(connectionString));

// Neo4j
builder.Services.AddSingleton<INeo4jService, Neo4jService>();

// Power BI
builder.Services.AddHttpClient("PowerBI");
builder.Services.AddSingleton<IPowerBIService, PowerBIService>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure pipeline
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors();
app.UseStaticFiles();
app.MapControllers();

// Auto-migrate and seed on startup
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        await context.Database.EnsureCreatedAsync();
        await SeedData.InitializeAsync(context);
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogWarning(ex, "Database migration/seed skipped - ensure MySQL is running");
    }
}

// Seed Neo4j
try
{
    var neo4jService = app.Services.GetRequiredService<INeo4jService>();
    await neo4jService.SeedGraphDataAsync();
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogWarning(ex, "Neo4j seed skipped - ensure Neo4j is running");
}

app.MapFallbackToFile("index.html");

app.Run();
