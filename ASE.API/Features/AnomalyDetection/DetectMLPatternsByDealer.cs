using ASE.API.Common.Data;
using ASE.API.Features.AnomalyDetection.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Threading.Tasks;

namespace ASE.API.Features.AnomalyDetection;

public static class DetectMLPatternsByDealer
{
    public static IEndpointRouteBuilder MapDealerPatternsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/dealers/{id}/patterns", HandleDealerPatternsAsync)
            .WithName("DetectDealerPatterns")
            .WithOpenApi();
            
        app.MapGet("/api/dealers/{id}/anomalies", HandleDealerAnomaliesAsync)
            .WithName("DetectDealerAnomalies")
            .WithOpenApi();
        
        return app;
    }
    
    // Handler for dealer pattern detection
    private static async Task<IResult> HandleDealerPatternsAsync(int id, FinanceDbContext dbContext, DataPatternMLService mlService)
    {
        var dealer = await dbContext.Dealers.FindAsync(id);
        
        if (dealer == null)
            return Results.NotFound();
            
        var patterns = await mlService.DetectDealerPatterns(id);
        return Results.Ok(patterns);
    }
    
    // Handler for dealer anomaly detection
    private static async Task<IResult> HandleDealerAnomaliesAsync(int id, FinanceDbContext dbContext, DataPatternMLService mlService)
    {
        var dealer = await dbContext.Dealers.FindAsync(id);
        
        if (dealer == null)
            return Results.NotFound();
            
        var anomalies = await mlService.DetectDealerTimeSeriesAnomalies(id);
        return Results.Ok(anomalies);
    }
} 