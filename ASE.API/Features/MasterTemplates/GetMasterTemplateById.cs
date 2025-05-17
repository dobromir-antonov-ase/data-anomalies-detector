using ASE.API.Common.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ASE.API.Features.MasterTemplates;

public static class GetMasterTemplateById
{
    public static IEndpointRouteBuilder MapGetMasterTemplateByIdEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/master-templates/{id}", HandleAsync)
            .WithName("GetMasterTemplateById")
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
            return Results.BadRequest("Invalid template ID. ID must be a positive number.");
        }
        
        // Get template with related data
        var template = await dbContext.MasterTemplates
            .Include(mt => mt.Sheets)
                .ThenInclude(s => s.Tables)
                    .ThenInclude(t => t.Cells)
            .Select(mt => new
            {
                mt.Id,
                mt.Name,
                mt.Year,
                SheetCount = mt.Sheets.Count,
                Cells = mt.Sheets
                    .SelectMany(s => s.Tables.SelectMany(t => t.Cells))
                    .Select(c => c.GlobalAddress)
            })  
            .FirstOrDefaultAsync(mt => mt.Id == id);
            
        // Check if template exists
        if (template is null)
        {
            return Results.NotFound($"Master Template with ID {id} not found.");
        }
        
        return Results.Ok(template);
    }
} 