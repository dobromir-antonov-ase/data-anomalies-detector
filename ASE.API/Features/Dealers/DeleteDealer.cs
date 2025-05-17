using ASE.API.Common.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Threading.Tasks;

namespace ASE.API.Features.Dealers;

public static class DeleteDealer
{
    public static IEndpointRouteBuilder MapDeleteDealerEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/dealers/{id}", HandleAsync)
            .WithName("DeleteDealer")
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
            return Results.BadRequest("Invalid dealer ID. ID must be a positive number.");
        }
        
        // Get dealer from database
        var dealer = await dbContext.Dealers.FindAsync(id);
        
        // Check if dealer exists
        if (dealer is null)
        {
            return Results.NotFound($"Dealer with ID {id} not found.");
        }
        
        // Remove from database
        dbContext.Dealers.Remove(dealer);
        await dbContext.SaveChangesAsync();
        
        // Return the result
        return Results.NoContent();
    }
} 