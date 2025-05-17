using ASE.API.Common.Data;
using ASE.API.Features.AnomalyDetection.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Threading.Tasks;

namespace ASE.API.Features.AnomalyDetection;

public static class DetectDataPatterns
{
    public static IEndpointRouteBuilder MapDetectDataPatternsEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/finance-submissions/{id}/patterns", HandleAsync)
            .WithName("DetectDataPatterns")
            .WithOpenApi();
        
        return app;
    }
    
    // Handler
    private static async Task<IResult> HandleAsync(int id, FinanceDbContext dbContext, AnomalyDetectionService anomalyService)
    {
        var submission = await dbContext.FinanceSubmissions.FindAsync(id);
        
        if (submission == null)
            return Results.NotFound();
            
        var patterns = await anomalyService.DetectDataPatterns(id);
        return Results.Ok(patterns);
    }
} 