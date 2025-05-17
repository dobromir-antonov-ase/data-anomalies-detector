using ASE.API.Common.Data;
using ASE.API.Features.Dealers.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace ASE.API.Features.Dealers;

public static class CreateDealer
{
    // Request model with validation attributes
    public class CreateDealerRequest
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

    public static IEndpointRouteBuilder MapCreateDealerEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/dealers", HandleAsync)
            .WithName("CreateDealer")
            .WithOpenApi();
        
        return app;
    }
    
    // Handler with built-in validation
    private static async Task<IResult> HandleAsync(CreateDealerRequest request, FinanceDbContext dbContext)
    {
        // Convert request to entity
        var dealer = new Dealer
        {
            Name = request.Name,
            Address = request.Address,
            ContactEmail = request.ContactEmail,
            ContactPhone = request.ContactPhone
        };
        
        // Add to database
        dbContext.Dealers.Add(dealer);
        await dbContext.SaveChangesAsync();
        
        // Return the result
        return Results.Created($"/dealers/{dealer.Id}", dealer);
    }
} 