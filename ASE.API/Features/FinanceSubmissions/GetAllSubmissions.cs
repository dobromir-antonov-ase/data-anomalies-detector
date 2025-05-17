using ASE.API.Common.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace ASE.API.Features.FinanceSubmissions;

public static class GetAllSubmissions
{
    public static IEndpointRouteBuilder MapGetAllSubmissionsEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/finance-submissions", HandleAsync)
            .WithName("GetAllSubmissions")
            .WithOpenApi();
        
        return app;
    }
    
    // Handler
    private static async Task<IResult> HandleAsync(FinanceDbContext dbContext)
    {
        // Query submissions with projection to avoid circular references
        var submissions = await dbContext.FinanceSubmissions
            .Select(fs => new
            {
                fs.Id,
                fs.Title,
                fs.SubmissionDate,
                fs.Status,
                fs.Month,
                fs.Year,
                fs.DealerId,
                fs.MasterTemplateId,
                CellCount = fs.Cells.Count
            })
            .ToListAsync();
            
        // Return the result
        return Results.Ok(submissions);
    }
} 