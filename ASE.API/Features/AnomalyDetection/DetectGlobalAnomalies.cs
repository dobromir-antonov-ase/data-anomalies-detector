using ASE.API.Common.Data;
using ASE.API.Features.AnomalyDetection.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Threading.Tasks;

namespace ASE.API.Features.AnomalyDetection;

public static class DetectGlobalAnomalies
{
    public static IEndpointRouteBuilder MapDetectGlobalAnomaliesEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/anomalies/detect-global/{monthsCount}", HandleAsync)
            .WithName("DetectGlobalAnomalies")
            .WithOpenApi();
        
        return app;
    }
    
    // Handler
    private static async Task<IResult> HandleAsync(int monthsCount, FinanceDbContext dbContext, AnomalyDetectionService anomalyService)
    {
        var anomalies = await anomalyService.DetectGlobalAnomalies(monthsCount);
        return Results.Ok(anomalies);
    }
} 