using ASE.API.Common.Data;
using ASE.API.Features.MasterTemplates.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace ASE.API.Features.MasterTemplates;

public static class GetAllMasterTemplates
{
    public static IEndpointRouteBuilder MapGetAllMasterTemplatesEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/master-templates", HandleAsync)
            .WithName("GetAllMasterTemplates")
            .WithOpenApi();
        
        return app;
    }
    
    // Handler
    private static async Task<IResult> HandleAsync(FinanceDbContext dbContext)
    {
        // Get templates with sheet counts
        var templates = await dbContext.MasterTemplates
            .Include(mt => mt.Sheets)
            .Select(mt => new 
            {
                mt.Id,
                mt.Name,
                mt.Year,
                SheetCount = mt.Sheets.Count,
                TableCount = mt.Sheets.Sum(s => s.Tables.Count),
                CellCount = mt.Sheets.Sum(s => s.Tables.Sum(t => t.Cells.Count))
            })
            .ToListAsync();
        
        return Results.Ok(templates);
    }
} 