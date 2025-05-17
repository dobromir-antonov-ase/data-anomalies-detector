using ASE.API.Common.Data;
using ASE.API.Features.MasterTemplates.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace ASE.API.Features.MasterTemplates;

public static class CreateMasterTemplate
{
    // Request models with validation
    public class CreateMasterTemplateRequest
    {
        [Required(ErrorMessage = "Name is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Year is required")]
        [Range(2000, 2100, ErrorMessage = "Year must be between 2000 and 2100")]
        public int Year { get; set; }

        [Required(ErrorMessage = "At least one sheet is required")]
        public List<CreateSheetRequest> Sheets { get; set; } = new();
    }

    public class CreateSheetRequest
    {
        [Required(ErrorMessage = "Sheet name is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters")]
        public string Name { get; set; } = string.Empty;
        
        public int PageNumber { get; set; }

        [Required(ErrorMessage = "At least one table is required")]
        public List<CreateTableRequest> Tables { get; set; } = new();
    }

    public class CreateTableRequest
    {
        [Required(ErrorMessage = "Table name is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters")]
        public string Name { get; set; } = string.Empty;
        
        public int RowCount { get; set; }
        
        public int ColumnCount { get; set; }

        [Required(ErrorMessage = "At least one cell is required")]
        public List<CreateCellRequest> Cells { get; set; } = new();
    }

    public class CreateCellRequest
    {
        [Required(ErrorMessage = "Cell address is required")]
        [RegularExpression(@"^[A-Z]+[1-9][0-9]*$", ErrorMessage = "Cell address must be in Excel format (e.g., A1, B2)")]
        public string CellAddress { get; set; } = string.Empty;

        public string DataType { get; set; } = "text";
    }

    public static IEndpointRouteBuilder MapCreateMasterTemplateEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/master-templates", HandleAsync)
            .WithName("CreateMasterTemplate")
            .WithOpenApi();

        return app;
    }

    // Handler
    public static async Task<IResult> HandleAsync(
        CreateMasterTemplateRequest request,
        FinanceDbContext dbContext)
    {
        // Create the master template
        var masterTemplate = new MasterTemplate
        {
            Name = request.Name,
            Year = request.Year,
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };
        
        // Add sheets, tables, and cells
        foreach (var sheetRequest in request.Sheets)
        {
            var sheet = new MasterTemplateSheet
            {
                Name = sheetRequest.Name,
                PageNumber = sheetRequest.PageNumber
            };
            
            foreach (var tableRequest in sheetRequest.Tables)
            {
                var table = new MasterTemplateTable
                {
                    Name = tableRequest.Name,
                    RowCount = tableRequest.RowCount,
                    ColumnCount = tableRequest.ColumnCount
                };
                
                foreach (var cellRequest in tableRequest.Cells)
                {
                    // Create Excel-style global address (SheetName!CellAddress)
                    string globalAddress = $"{sheetRequest.Name}!{cellRequest.CellAddress}";
                    
                    var cell = new MasterTemplateCell
                    {
                        CellAddress = cellRequest.CellAddress,
                        GlobalAddress = globalAddress,
                        DataType = cellRequest.DataType,
                        Value = string.Empty // Template cells don't have values, just structure
                    };
                    
                    table.Cells.Add(cell);
                }
                
                sheet.Tables.Add(table);
            }
            
            masterTemplate.Sheets.Add(sheet);
        }

        // Add to database
        dbContext.MasterTemplates.Add(masterTemplate);
        await dbContext.SaveChangesAsync();

        // Return the result
        return Results.Created($"/api/master-templates/{masterTemplate.Id}", masterTemplate);
    }
} 