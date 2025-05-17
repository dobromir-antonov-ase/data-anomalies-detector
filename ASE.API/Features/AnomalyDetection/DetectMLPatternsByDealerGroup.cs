using ASE.API.Common.Data;
using ASE.API.Features.AnomalyDetection.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace ASE.API.Features.AnomalyDetection;

public static class DetectMLPatternsByDealerGroup
{
    public static IEndpointRouteBuilder MapDealerGroupPatternsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/dealer-groups/{groupId}/patterns", HandleDealerGroupPatternsAsync)
            .WithName("DetectDealerGroupPatterns")
            .WithOpenApi();
            
        app.MapGet("/api/dealer-groups/{groupId}/anomalies", HandleDealerGroupAnomaliesAsync)
            .WithName("DetectDealerGroupAnomalies")
            .WithOpenApi();
            
        // Also add endpoints to get all dealer groups
        app.MapGet("/api/dealer-groups", HandleGetDealerGroupsAsync)
            .WithName("GetDealerGroups")
            .WithOpenApi();
        
        return app;
    }
    
    // Handler for dealer group pattern detection
    private static async Task<IResult> HandleDealerGroupPatternsAsync(int groupId, FinanceDbContext dbContext, DataPatternMLService mlService)
    {
        // Verify group exists
        var dealersInGroup = await dbContext.Dealers.Where(d => d.GroupId == groupId).ToListAsync();
        
        if (!dealersInGroup.Any())
            return Results.NotFound();
            
        var patterns = await mlService.DetectDealerGroupPatterns(groupId);
        return Results.Ok(patterns);
    }
    
    // Handler for dealer group anomaly detection
    private static async Task<IResult> HandleDealerGroupAnomaliesAsync(int groupId, FinanceDbContext dbContext, DataPatternMLService mlService)
    {
        // Verify group exists
        var dealersInGroup = await dbContext.Dealers.Where(d => d.GroupId == groupId).ToListAsync();
        
        if (!dealersInGroup.Any())
            return Results.NotFound();
            
        var anomalies = await mlService.DetectDealerGroupTimeSeriesAnomalies(groupId);
        return Results.Ok(anomalies);
    }
    
    // Handler to get all dealer groups
    private static async Task<IResult> HandleGetDealerGroupsAsync(FinanceDbContext dbContext)
    {
        var dealerGroups = await dbContext.Dealers
            .GroupBy(d => new { d.GroupId, d.GroupName })
            .Select(g => new { 
                g.Key.GroupId, 
                g.Key.GroupName,
                DealerCount = g.Count() 
            })
            .ToListAsync();
            
        return Results.Ok(dealerGroups);
    }
} 