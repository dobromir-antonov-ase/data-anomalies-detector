using ASE.API.Common.Data;
using ASE.API.Features.Dealers.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace ASE.API.Features.Dealers;

public static class UpdateDealer
{
    // Request model with validation attributes
    public class UpdateDealerRequest
    {
        [Required(ErrorMessage = "Dealer name is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters")]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(200, ErrorMessage = "Address must not exceed 200 characters")]
        public string? Address { get; set; }
        
        [EmailAddress(ErrorMessage = "Invalid email address format")]
        public string? ContactEmail { get; set; }
        
        [Phone(ErrorMessage = "Invalid phone number format")]
        public string? ContactPhone { get; set; }
    }

    public static IEndpointRouteBuilder MapUpdateDealerEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPut("/api/dealers/{id}", HandleAsync)
            .WithName("UpdateDealer")
            .WithOpenApi();
        
        return app;
    }
    
    // Validator
    private static bool ValidateId(int id)
    {
        return id > 0; // Simple validation to ensure ID is positive
    }
    
    // Handler with validation
    private static async Task<IResult> HandleAsync(int id, UpdateDealerRequest request, FinanceDbContext dbContext)
    {
        // Validate input
        if (!ValidateId(id))
        {
            return Results.BadRequest("Invalid dealer ID. ID must be a positive number.");
        }
        
        // Get existing dealer
        var dealer = await dbContext.Dealers.FindAsync(id);
        
        // Check if dealer exists
        if (dealer is null)
        {
            return Results.NotFound($"Dealer with ID {id} not found.");
        }
        
        // Update dealer properties
        dealer.Name = request.Name;
        dealer.Address = request.Address;
        dealer.ContactEmail = request.ContactEmail;
        dealer.ContactPhone = request.ContactPhone;
        
        // Save changes
        await dbContext.SaveChangesAsync();
        
        // Return the result
        return Results.NoContent();
    }
} 