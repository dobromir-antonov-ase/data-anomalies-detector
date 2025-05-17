using ASE.API.Common.Data;
using ASE.API.Features.AnomalyDetection.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Threading.Tasks;

namespace ASE.API.Features.AnomalyDetection;

public static class DetectMLAnomalies
{
    public static IEndpointRouteBuilder MapDetectMLAnomaliesEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/finance-submissions/{id}/ml-anomalies", HandleAnomaliesAsync)
            .WithName("DetectMLAnomalies")
            .WithOpenApi();
            
        app.MapGet("/api/finance-submissions/{id}/ml-patterns", HandlePatternsAsync)
            .WithName("DetectMLPatterns")
            .WithOpenApi();
            
        app.MapGet("/api/finance-submissions/{id}/ml-forecast", HandleForecastAsync)
            .WithName("DetectMLForecast")
            .WithOpenApi();
        
        return app;
    }
    
    // Handler for anomaly detection
    private static async Task<IResult> HandleAnomaliesAsync(int id, FinanceDbContext dbContext, DataPatternMLService mlService)
    {
        var submission = await dbContext.FinanceSubmissions.FindAsync(id);
        
        if (submission == null)
            return Results.NotFound();
            
        var anomalies = await mlService.DetectTimeSeriesAnomalies(id);
        return Results.Ok(anomalies);
    }
    
    // Handler for pattern detection
    private static async Task<IResult> HandlePatternsAsync(int id, FinanceDbContext dbContext, DataPatternMLService mlService)
    {
        var submission = await dbContext.FinanceSubmissions.FindAsync(id);
        
        if (submission == null)
            return Results.NotFound();
            
        var patterns = await mlService.DetectClustersInData(id);
        return Results.Ok(patterns);
    }
    
    // Handler for forecasting
    private static async Task<IResult> HandleForecastAsync(int id, FinanceDbContext dbContext, DataPatternMLService mlService)
    {
        var submission = await dbContext.FinanceSubmissions.FindAsync(id);
        
        if (submission == null)
            return Results.NotFound();
            
        var forecasts = await mlService.PredictFutureValues(id);
        return Results.Ok(forecasts);
    }
} 