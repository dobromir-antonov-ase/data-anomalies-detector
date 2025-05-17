using ASE.API.Common.Data;
using ASE.API.Features.Dealers.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ASE.API.Features.Dealers;

public static class GetAllDealers
{
    public static IEndpointRouteBuilder MapGetAllDealersEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/dealers", HandleAsync)
            .WithName("GetAllDealers")
            .WithOpenApi();
        
        return app;
    }
    
    // Validator isn't needed for this simple Get All operation
    
    // Handler
    private static async Task<IResult> HandleAsync(FinanceDbContext dbContext)
    {
        // Validation could be added here if needed
        
        // Get all dealers from database
        var dealers = await dbContext.Dealers.ToListAsync();
        
        // Return the result
        return Results.Ok(dealers);
    }
} 