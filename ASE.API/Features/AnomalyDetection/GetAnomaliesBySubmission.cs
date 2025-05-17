using ASE.API.Common.Data;
using ASE.API.Features.AnomalyDetection.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace ASE.API.Features.AnomalyDetection;

public static class GetAnomaliesBySubmission
{
    public static IEndpointRouteBuilder MapGetAnomaliesBySubmissionEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/finance-submissions/{id}/anomalies", HandleAsync)
            .WithName("GetAnomaliesBySubmission")
            .WithOpenApi();
        
        return app;
    }
    
    // Handler
    private static async Task<IResult> HandleAsync(int id, FinanceDbContext dbContext, AnomalyDetectionService anomalyService)
    {
        var submission = await dbContext.FinanceSubmissions.FindAsync(id);
        
        if (submission == null)
            return Results.NotFound();
            
        var anomalies = await anomalyService.DetectAnomaliesInSubmission(id);
        return Results.Ok(anomalies);
    }
} 