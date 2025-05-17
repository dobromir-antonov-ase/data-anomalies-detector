using ASE.API.Common.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace ASE.API.Features.FinanceSubmissions;

public class PaginatedResult<T>
{
    public List<T> Items { get; set; } = new List<T>();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public bool HasMore { get; set; }
}

public static class GetPaginatedSubmissions
{
    public static IEndpointRouteBuilder MapGetPaginatedSubmissionsEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/finance-submissions/paginated", HandleAsync)
            .WithName("GetPaginatedSubmissions")
            .WithOpenApi();
        
        return app;
    }
    
    // Handler
    private static async Task<IResult> HandleAsync(FinanceDbContext dbContext, int pageNumber = 1, int pageSize = 10)
    {
        // Validate input parameters
        if (pageNumber < 1)
        {
            pageNumber = 1;
        }
        
        if (pageSize < 1 || pageSize > 100)
        {
            pageSize = 10; // Default page size if invalid
        }
        
        // Get total count
        var totalCount = await dbContext.FinanceSubmissions.CountAsync();
        
        // Calculate if there are more items
        var hasMore = (pageNumber * pageSize) < totalCount;
        
        // Query submissions with pagination
        var submissions = await dbContext.FinanceSubmissions
            .OrderByDescending(fs => fs.SubmissionDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
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
        
        // Create paginated result
        var result = new PaginatedResult<object>
        {
            Items = submissions.Cast<object>().ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            HasMore = hasMore
        };
        
        // Return the result
        return Results.Ok(result);
    }
} 