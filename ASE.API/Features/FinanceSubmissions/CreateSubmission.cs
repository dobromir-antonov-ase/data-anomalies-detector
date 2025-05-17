using ASE.API.Common.Data;
using ASE.API.Features.AnomalyDetection.Services;
using ASE.API.Features.FinanceSubmissions.Models;
using ASE.API.Features.MasterTemplates.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace ASE.API.Features.FinanceSubmissions;

public static class CreateSubmission
{
    // Request models with validation
    public class CreateSubmissionRequest
    {
        [Required(ErrorMessage = "Title is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Title must be between 2 and 100 characters")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Dealer ID is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Dealer ID must be positive")]
        public int DealerId { get; set; }
        
        [Required(ErrorMessage = "Master Template ID is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Master Template ID must be positive")]
        public int MasterTemplateId { get; set; }

        [Required(ErrorMessage = "Month is required")]
        [Range(1, 12, ErrorMessage = "Month must be between 1 and 12")]
        public int Month { get; set; }

        [Required(ErrorMessage = "Year is required")]
        [Range(2000, 2100, ErrorMessage = "Year must be between 2000 and 2100")]
        public int Year { get; set; }
        
        [Required(ErrorMessage = "At least one data item is required")]
        public List<CreateDataRequest> Data { get; set; } = new();
    }
    
    public class CreateDataRequest
    {
        [Required(ErrorMessage = "Cell address is required")]
        [RegularExpression(@"^[A-Z]+[1-9][0-9]*$", ErrorMessage = "Cell address must be in Excel format (e.g., A1, B2)")]
        public string CellAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "Value is required")]
        public decimal Value { get; set; }
        
        [Required(ErrorMessage = "Aggregation type is required")]
        [RegularExpression("monthly|fytd|r12", ErrorMessage = "Aggregation type must be 'monthly', 'fytd', or 'r12'")]
        public string AggregationType { get; set; } = "monthly";
    }

    public static IEndpointRouteBuilder MapCreateSubmissionEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/finance-submissions", HandleAsync)
            .WithName("CreateSubmission")
            .WithOpenApi();

        return app;
    }

    // Handler
    public static async Task<IResult> HandleAsync(
        CreateSubmissionRequest request,
        FinanceDbContext dbContext,
        AnomalyDetectionService anomalyService)
    {
        // Check if dealer exists
        var dealer = await dbContext.Dealers.FindAsync(request.DealerId);
        if (dealer is null)
        {
            return Results.NotFound($"Dealer with ID {request.DealerId} not found.");
        }
        
        // Check if master template exists
        var masterTemplate = await dbContext.MasterTemplates
            .Include(mt => mt.Sheets)
                .ThenInclude(s => s.Tables)
                    .ThenInclude(t => t.Cells)
            .FirstOrDefaultAsync(mt => mt.Id == request.MasterTemplateId);
            
        if (masterTemplate is null)
        {
            return Results.NotFound($"Master Template with ID {request.MasterTemplateId} not found.");
        }

        // Create submission
        var submission = new FinanceSubmission
        {
            Title = request.Title,
            DealerId = request.DealerId,
            MasterTemplateId = request.MasterTemplateId,
            Month = request.Month,
            Year = request.Year,
            SubmissionDate = DateTime.UtcNow,
            Status = "Pending"
        };
        
        // Add submission data
        foreach (var dataRequest in request.Data)
        {
            // Create global address by finding the template cell with this address
            string globalAddress = "";
            
            // Find the corresponding template cell to get the global address
            foreach (var sheet in masterTemplate.Sheets)
            {
                foreach (var table in sheet.Tables)
                {
                    var templateCell = table.Cells.FirstOrDefault(c => c.CellAddress == dataRequest.CellAddress);
                    if (templateCell != null)
                    {
                        globalAddress = templateCell.GlobalAddress;
                        break;
                    }
                }
                
                if (!string.IsNullOrEmpty(globalAddress))
                {
                    break;
                }
            }
            
            // If we couldn't find a matching template cell, create a fallback global address
            if (string.IsNullOrEmpty(globalAddress))
            {
                // Just use a simple Excel-style global address format as fallback
                globalAddress = $"Unknown!{dataRequest.CellAddress}";
            }
            
            var cell = new FinanceSubmissionCell
            {
                CellAddress = dataRequest.CellAddress,
                GlobalAddress = globalAddress,
                Value = dataRequest.Value,
                AggregationType = dataRequest.AggregationType
            };
            
            submission.Cells.Add(cell);
        }

        // Add to database
        dbContext.FinanceSubmissions.Add(submission);
        await dbContext.SaveChangesAsync();

        // Detect anomalies
        var anomalies = await anomalyService.DetectAnomaliesInSubmission(submission.Id);

        // Return the result
        return Results.Created($"/api/finance-submissions/{submission.Id}", submission);
    }
}