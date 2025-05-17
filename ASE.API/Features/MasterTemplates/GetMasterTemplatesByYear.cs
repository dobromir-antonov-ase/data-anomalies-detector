using ASE.API.Common.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace ASE.API.Features.MasterTemplates;

public static class GetMasterTemplatesByYear
{
    public static IEndpointRouteBuilder MapGetMasterTemplatesByYearEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/master-templates/year/{year}", HandleAsync)
            .WithName("GetMasterTemplatesByYear")
            .WithOpenApi();
        
        return app;
    }
    
    // Validator
    private static bool ValidateYear(int year)
    {
        return year >= 2000 && year <= 2100; // Reasonable year range
    }
    
    // Handler
    private static async Task<IResult> HandleAsync(int year, FinanceDbContext dbContext)
    {
        // Validate input
        if (!ValidateYear(year))
        {
            return Results.BadRequest("Invalid year. Year must be between 2000 and 2100.");
        }
        
        // Get templates by year
        var templates = await dbContext.MasterTemplates
            .Where(mt => mt.Year == year)
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
            .ToListAsync();
            
        // Check if any templates exist for this year
        if (!templates.Any())
        {
            return Results.NotFound($"No master templates found for year {year}.");
        }
        
        // Return the result
        return Results.Ok(templates);
    }
} 