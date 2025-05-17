using ASE.API.Common.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace ASE.API.Features.FinanceSubmissions;

public static class GetSubmissionById
{
    public static IEndpointRouteBuilder MapGetSubmissionByIdEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/finance-submissions/{id}", HandleAsync)
            .WithName("GetSubmissionById")
            .WithOpenApi();
        
        return app;
    }
    
    // Validator
    private static bool ValidateId(int id)
    {
        return id > 0; // Simple validation to ensure ID is positive
    }
    
    // Handler
    private static async Task<IResult> HandleAsync(int id, FinanceDbContext dbContext)
    {
        // Validate input
        if (!ValidateId(id))
        {
            return Results.BadRequest("Invalid submission ID. ID must be a positive number.");
        }
        
        // Query submission with projection to avoid circular references
        var submission = await dbContext.FinanceSubmissions
            .Where(fs => fs.Id == id)
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
                Cells = fs.Cells.Select(c => new
                {
                    c.Id,
                    c.GlobalAddress,
                    c.Value,
                    c.AggregationType
                }).ToList()
            })
            .FirstOrDefaultAsync();
        
        // Check if submission exists
        if (submission is null)
        {
            return Results.NotFound($"Finance submission with ID {id} not found.");
        }
        
        // Return the result
        return Results.Ok(submission);
    }
} 