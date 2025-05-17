using ASE.API.Common.Data;
using ASE.API.Features.AnomalyDetection.Services;


namespace ASE.API.Features.AnomalyDetection;

public static class DetectAnomaliesByDealer
{
    public static IEndpointRouteBuilder MapDetectAnomaliesByDealerEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/dealers/{id}/anomalies", HandleAsync)
            .WithName("DetectAnomaliesByDealer")
            .WithOpenApi();
        
        return app;
    }
    
    // Handler
    private static async Task<IResult> HandleAsync(int id, FinanceDbContext dbContext, AnomalyDetectionService anomalyService)
    {
        var dealer = await dbContext.Dealers.FindAsync(id);
        
        if (dealer == null)
            return Results.NotFound();
            
        var anomalies = await anomalyService.DetectAnomaliesByDealer(id);
        return Results.Ok(anomalies);
    }
} 