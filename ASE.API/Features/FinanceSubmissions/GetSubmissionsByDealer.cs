using ASE.API.Common.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace ASE.API.Features.FinanceSubmissions;

public static class GetSubmissionsByDealer
{
    public static IEndpointRouteBuilder MapGetSubmissionsByDealerEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/dealers/{dealerId}/finance-submissions/{year}", HandleAsync)
            .WithName("GetSubmissionsByDealer")
            .WithOpenApi();
        
        return app;
    }
    
    // Validator
    private static bool ValidateId(int id)
    {
        return id > 0; // Simple validation to ensure ID is positive
    }
    
    // Handler
    private static async Task<IResult> HandleAsync(int dealerId, int year, FinanceDbContext dbContext)
    {
        // Validate input
        if (!ValidateId(dealerId))
        {
            return Results.BadRequest("Invalid dealer ID. ID must be a positive number.");
        }
        
        // Check if dealer exists
        var dealer = await dbContext.Dealers.FindAsync(dealerId);
        if (dealer is null)
        {
            return Results.NotFound($"Dealer with ID {dealerId} not found.");
        }
        
        // Query submissions with projection to avoid circular references
        var submissions = await dbContext.FinanceSubmissions
            .Where(fs => fs.DealerId == dealerId)
            .Where(fs => fs.Year == year)
            .OrderByDescending(fs => fs.Year)
            .ThenByDescending(fs => fs.Month)
            .Select(fs => new
            {
                fs.Id,
                fs.Title,
                fs.SubmissionDate,
                fs.Status,
                fs.Month,
                fs.Year,
                fs.MasterTemplateId,
                Cells = fs.Cells.Select(c => new
                {
                    c.Id,
                    c.GlobalAddress,
                    c.Value,
                    c.AggregationType
                })
            })
            .ToListAsync();
            
        // Return the result
        return Results.Ok(submissions);
    }
} 