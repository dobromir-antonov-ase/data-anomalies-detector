using ASE.API.Common.Data;
using ASE.API.Features.AnomalyDetection;
using ASE.API.Features.AnomalyDetection.Services;
using ASE.API.Features.Dealers;
using ASE.API.Features.FinanceSubmissions;
using ASE.API.Features.MasterTemplates;
using ASE.API.Features.QueryBuilder;
using ASE.API.Features.QueryBuilder.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();

// Add CORS services
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.WithOrigins("https://github.io", "https://dobromir-antonov-ase.github.io", "http://localhost:4200")
                  .SetIsOriginAllowedToAllowWildcardSubdomains()
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

// Add database context
builder.Services.AddDbContext<FinanceDbContext>(options =>
    options.UseInMemoryDatabase("FinanceDb"));

// Register services
builder.Services.AddScoped<AnomalyDetectionService>();
builder.Services.AddScoped<DataPatternMLService>();

// Register QueryBuilder services
builder.Services.AddScoped<QueryBuilderService>();
builder.Services.AddScoped<SpeechToTextService>();
builder.Services.AddHttpClient();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Swagger is commented out since it's not used in this project
     //app.UseSwagger();
     //app.UseSwaggerUI();
}

// Use CORS before other middleware
app.UseCors("AllowAll");

app.UseHttpsRedirection();

// Initialize the database with seed data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<FinanceDbContext>();
        await DbInitializer.Initialize(context);
        Console.WriteLine("Database seeded successfully.");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

// Map endpoints
// Map Dealer endpoints from individual use case files
app.MapGetAllDealersEndpoint();
app.MapGetDealerByIdEndpoint();
app.MapCreateDealerEndpoint();
app.MapUpdateDealerEndpoint();
app.MapDeleteDealerEndpoint();

// Map FinanceSubmission endpoints from individual use case files
app.MapGetAllSubmissionsEndpoint();
app.MapGetSubmissionsByDealerEndpoint();
app.MapGetSubmissionByIdEndpoint();
app.MapCreateSubmissionEndpoint();
app.MapGetPaginatedSubmissionsEndpoint();

// Map MasterTemplate endpoints from individual use case files
app.MapGetAllMasterTemplatesEndpoint();
app.MapGetMasterTemplateByIdEndpoint();
app.MapGetMasterTemplatesByYearEndpoint();
app.MapCreateMasterTemplateEndpoint();

// Map Anomaly Detection endpoints
app.MapDealerPatternsEndpoints();
app.MapDealerGroupPatternsEndpoints();

// Map QueryBuilder endpoints
app.MapQueryBuilderEndpoints();

app.Run();
