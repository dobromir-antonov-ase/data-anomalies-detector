using ASE.API.Features.Dealers.Models;
using ASE.API.Features.FinanceSubmissions.Models;
using ASE.API.Features.MasterTemplates.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ASE.API.Common.Data;

public static class DbInitializer
{
    public static async Task Initialize(FinanceDbContext context)
    {
        // Ensure database is created
        await context.Database.EnsureCreatedAsync();
        
        // Check if we already have data
        if (context.Dealers.Any())
        {
            return; // Database was already seeded
        }
        
        // Seed 10 dealers
        var dealers = new Dealer[]
        {
            new Dealer { Name = "Ace Auto Group", Address = "123 Main St, Anytown, CA 90210", ContactPhone = "555-123-4567", ContactEmail = "contact@aceautogroup.com" },
            new Dealer { Name = "Bayside Motors", Address = "456 Ocean Ave, Bayville, NY 11709", ContactPhone = "555-987-6543", ContactEmail = "info@baysidemotors.com" },
            new Dealer { Name = "Crestview Automotive", Address = "789 Highland Dr, Crestview, TX 75001", ContactPhone = "555-456-7890", ContactEmail = "sales@crestviewauto.com" },
            new Dealer { Name = "Downtown Cars", Address = "101 Market St, Downtown, IL 60601", ContactPhone = "555-789-0123", ContactEmail = "info@downtowncars.com" },
            new Dealer { Name = "Eastern Motors", Address = "202 East Ave, Eastville, FL 33101", ContactPhone = "555-234-5678", ContactEmail = "contact@easternmotors.com" },
            new Dealer { Name = "Fairview Dealership", Address = "303 Fair St, Fairview, WA 98101", ContactPhone = "555-345-6789", ContactEmail = "sales@fairviewdealership.com" },
            new Dealer { Name = "Gateway Auto", Address = "404 Gateway Blvd, Gateway, AZ 85001", ContactPhone = "555-456-7890", ContactEmail = "info@gatewayauto.com" },
            new Dealer { Name = "Highland Vehicles", Address = "505 High St, Highland, CO 80202", ContactPhone = "555-567-8901", ContactEmail = "contact@highlandvehicles.com" },
            new Dealer { Name = "Island Cars", Address = "606 Island Ave, Islandtown, HI 96801", ContactPhone = "555-678-9012", ContactEmail = "sales@islandcars.com" },
            new Dealer { Name = "Junction Motors", Address = "707 Junction Rd, Junction City, OR 97301", ContactPhone = "555-789-0123", ContactEmail = "info@junctionmotors.com" }
        };
        
        await context.Dealers.AddRangeAsync(dealers);
        await context.SaveChangesAsync();
    
        
        var masterTemplate2024 = new MasterTemplate
        {
            Name = "Standard Financial Template 2024",
            Year = 2024,
            IsActive = true,
            CreatedDate = new DateTime(2024, 1, 1)
        };
        
        await context.MasterTemplates.AddRangeAsync(masterTemplate2024);
        await context.SaveChangesAsync();
        
        // Add sheets, tables and cells to 2024 template
        await AddTemplateStructure(context, masterTemplate2024);
        
        // Add submissions for 2024
        await AddSubmissions(context, dealers, masterTemplate2024, 2024, 1, 12);
    }
    
    private static async Task AddTemplateStructure(FinanceDbContext context, MasterTemplate template)
    {
        // Create Income Statement sheet
        var incomeSheet = new MasterTemplateSheet
        {
            Name = "Income Statement",
            PageNumber = 1,
            MasterTemplateId = template.Id,
            MasterTemplate = template
        };
        
        // Create Balance Sheet sheet
        var balanceSheet = new MasterTemplateSheet
        {
            Name = "Balance Sheet",
            PageNumber = 2,
            MasterTemplateId = template.Id,
            MasterTemplate = template
        };
        
        await context.MasterTemplateSheets.AddRangeAsync(incomeSheet, balanceSheet);
        await context.SaveChangesAsync();
        
        // Create Income Statement table
        var incomeTable = new MasterTemplateTable
        {
            Name = "Income Statement Summary",
            RowCount = 5,
            ColumnCount = 4,  // Expanded to include both A and B columns
            MasterTemplateSheetId = incomeSheet.Id,
            MasterTemplateSheet = incomeSheet
        };
        
        // Create Balance Sheet table
        var balanceTable = new MasterTemplateTable
        {
            Name = "Balance Sheet Summary",
            RowCount = 5,
            ColumnCount = 4,  // Expanded to include both A and B columns
            MasterTemplateSheetId = balanceSheet.Id,
            MasterTemplateSheet = balanceSheet
        };
        
        await context.MasterTemplateTables.AddRangeAsync(incomeTable, balanceTable);
        await context.SaveChangesAsync();
        
        // Create cells for Income Statement
        var incomeCells = new MasterTemplateCell[]
        {
            new MasterTemplateCell { 
                CellAddress = "A1", 
                GlobalAddress = "Income Statement!A1",
                DataType = "text", 
                Value = "Revenue", 
                MasterTemplateTableId = incomeTable.Id, 
                MasterTemplateTable = incomeTable 
            },
            new MasterTemplateCell { 
                CellAddress = "B1", 
                GlobalAddress = "Income Statement!B1",
                DataType = "number", 
                Value = "", 
                MasterTemplateTableId = incomeTable.Id, 
                MasterTemplateTable = incomeTable 
            },
            new MasterTemplateCell { 
                CellAddress = "A2", 
                GlobalAddress = "Income Statement!A2",
                DataType = "text", 
                Value = "Cost of Sales", 
                MasterTemplateTableId = incomeTable.Id, 
                MasterTemplateTable = incomeTable 
            },
            new MasterTemplateCell { 
                CellAddress = "B2", 
                GlobalAddress = "Income Statement!B2",
                DataType = "number", 
                Value = "", 
                MasterTemplateTableId = incomeTable.Id, 
                MasterTemplateTable = incomeTable 
            },
            new MasterTemplateCell { 
                CellAddress = "A3", 
                GlobalAddress = "Income Statement!A3",
                DataType = "text", 
                Value = "Gross Profit", 
                MasterTemplateTableId = incomeTable.Id, 
                MasterTemplateTable = incomeTable 
            },
            new MasterTemplateCell { 
                CellAddress = "B3", 
                GlobalAddress = "Income Statement!B3",
                DataType = "number", 
                Value = "", 
                MasterTemplateTableId = incomeTable.Id, 
                MasterTemplateTable = incomeTable 
            },
            new MasterTemplateCell { 
                CellAddress = "A4", 
                GlobalAddress = "Income Statement!A4",
                DataType = "text", 
                Value = "Operating Expenses", 
                MasterTemplateTableId = incomeTable.Id, 
                MasterTemplateTable = incomeTable 
            },
            new MasterTemplateCell { 
                CellAddress = "B4", 
                GlobalAddress = "Income Statement!B4",
                DataType = "number", 
                Value = "", 
                MasterTemplateTableId = incomeTable.Id, 
                MasterTemplateTable = incomeTable 
            },
            new MasterTemplateCell { 
                CellAddress = "A5", 
                GlobalAddress = "Income Statement!A5",
                DataType = "text", 
                Value = "Net Income", 
                MasterTemplateTableId = incomeTable.Id, 
                MasterTemplateTable = incomeTable 
            },
            new MasterTemplateCell { 
                CellAddress = "B5", 
                GlobalAddress = "Income Statement!B5",
                DataType = "number", 
                Value = "", 
                MasterTemplateTableId = incomeTable.Id, 
                MasterTemplateTable = incomeTable 
            }
        };
        
        // Create cells for Balance Sheet
        var balanceCells = new MasterTemplateCell[]
        {
            new MasterTemplateCell { 
                CellAddress = "A1", 
                GlobalAddress = "Balance Sheet!A1",
                DataType = "text", 
                Value = "Assets", 
                MasterTemplateTableId = balanceTable.Id, 
                MasterTemplateTable = balanceTable 
            },
            new MasterTemplateCell { 
                CellAddress = "B1", 
                GlobalAddress = "Balance Sheet!B1",
                DataType = "number", 
                Value = "", 
                MasterTemplateTableId = balanceTable.Id, 
                MasterTemplateTable = balanceTable 
            },
            new MasterTemplateCell { 
                CellAddress = "A2", 
                GlobalAddress = "Balance Sheet!A2",
                DataType = "text", 
                Value = "Liabilities", 
                MasterTemplateTableId = balanceTable.Id, 
                MasterTemplateTable = balanceTable 
            },
            new MasterTemplateCell { 
                CellAddress = "B2", 
                GlobalAddress = "Balance Sheet!B2",
                DataType = "number", 
                Value = "", 
                MasterTemplateTableId = balanceTable.Id, 
                MasterTemplateTable = balanceTable 
            },
            new MasterTemplateCell { 
                CellAddress = "A3", 
                GlobalAddress = "Balance Sheet!A3",
                DataType = "text", 
                Value = "Equity", 
                MasterTemplateTableId = balanceTable.Id, 
                MasterTemplateTable = balanceTable 
            },
            new MasterTemplateCell { 
                CellAddress = "B3", 
                GlobalAddress = "Balance Sheet!B3",
                DataType = "number", 
                Value = "", 
                MasterTemplateTableId = balanceTable.Id, 
                MasterTemplateTable = balanceTable 
            },
            new MasterTemplateCell { 
                CellAddress = "A4", 
                GlobalAddress = "Balance Sheet!A4",
                DataType = "text", 
                Value = "Current Assets", 
                MasterTemplateTableId = balanceTable.Id, 
                MasterTemplateTable = balanceTable 
            },
            new MasterTemplateCell { 
                CellAddress = "B4", 
                GlobalAddress = "Balance Sheet!B4",
                DataType = "number", 
                Value = "", 
                MasterTemplateTableId = balanceTable.Id, 
                MasterTemplateTable = balanceTable 
            },
            new MasterTemplateCell { 
                CellAddress = "A5", 
                GlobalAddress = "Balance Sheet!A5",
                DataType = "text", 
                Value = "Fixed Assets", 
                MasterTemplateTableId = balanceTable.Id, 
                MasterTemplateTable = balanceTable 
            },
            new MasterTemplateCell { 
                CellAddress = "B5", 
                GlobalAddress = "Balance Sheet!B5",
                DataType = "number", 
                Value = "", 
                MasterTemplateTableId = balanceTable.Id, 
                MasterTemplateTable = balanceTable 
            }
        };
        
        await context.MasterTemplateCells.AddRangeAsync(incomeCells);
        await context.MasterTemplateCells.AddRangeAsync(balanceCells);
        await context.SaveChangesAsync();
    }
    
    private static async Task AddSubmissions(FinanceDbContext context, Dealer[] dealers, MasterTemplate template, int year, int startMonth, int endMonth)
    {
        // Generate a random number generator with a fixed seed for consistent results
        var random = new Random(year * 100);
        
        // Get all template cells for this template (do this once to avoid multiple queries)
        var templateCells = await GetTemplateCells(context, template.Id);
        
        // For each dealer, create submissions for each month
        for (int dealerIndex = 0; dealerIndex < dealers.Length; dealerIndex++)
        {
            var dealer = dealers[dealerIndex];
            
            // For dealer 8 (Highland Vehicles at index 7), initialize arithmetic progression
            Dictionary<string, decimal> dealer8CellBaseValues = new Dictionary<string, decimal>();
            Dictionary<string, decimal> dealer8CellIncrements = new Dictionary<string, decimal>();
            
            if (dealerIndex == 7)
            {
                InitializeDealer8ProgressionValues(templateCells, dealer8CellBaseValues, dealer8CellIncrements);
            }
            
            for (int month = startMonth; month <= endMonth; month++)
            {
                // Create a new submission
                var submission = await CreateSubmission(context, dealer, template, year, month);
                
                // Create cells for this submission
                var submissionCells = CreateSubmissionCells(
                    templateCells, 
                    submission.Id, 
                    dealerIndex, 
                    month, 
                    random, 
                    dealer8CellBaseValues, 
                    dealer8CellIncrements);
                
                // Add all submission cells at once for better performance
                await context.AddRangeAsync(submissionCells);
                await context.SaveChangesAsync();
            }
        }
    }
    
    private static async Task<List<MasterTemplateCell>> GetTemplateCells(FinanceDbContext context, int templateId)
    {
        return await context.MasterTemplateCells
            .Where(c => c.MasterTemplateTable.MasterTemplateSheet.MasterTemplateId == templateId)
            .ToListAsync();
    }
    
    private static void InitializeDealer8ProgressionValues(
        List<MasterTemplateCell> templateCells,
        Dictionary<string, decimal> baseValues,
        Dictionary<string, decimal> increments)
    {
        foreach (var cell in templateCells)
        {
            if (cell.DataType == "text")
                continue;
            
            // Set initial value
            if (cell.CellAddress.StartsWith("A"))
            {
                baseValues[cell.CellAddress] = 100; // Start at 100
                increments[cell.CellAddress] = 10;  // Increase by 10 each month
            }
            else if (cell.CellAddress.StartsWith("B"))
            {
                baseValues[cell.CellAddress] = 300; // Start at 300
                increments[cell.CellAddress] = 15;  // Increase by 15 each month
            }
        }
    }
    
    private static async Task<FinanceSubmission> CreateSubmission(
        FinanceDbContext context, 
        Dealer dealer, 
        MasterTemplate template, 
        int year, 
        int month)
    {
        var submission = new FinanceSubmission
        {
            Title = $"{dealer.Name} - {month}/{year} Financial Report",
            DealerId = dealer.Id,
            MasterTemplateId = template.Id,
            Month = month,
            Year = year,
            SubmissionDate = new DateTime(year, month, 15),
            Status = "Submitted"
        };
        
        context.FinanceSubmissions.Add(submission);
        await context.SaveChangesAsync();
        
        return submission;
    }
    
    private static List<FinanceSubmissionCell> CreateSubmissionCells(
        List<MasterTemplateCell> templateCells,
        int submissionId,
        int dealerIndex,
        int month,
        Random random,
        Dictionary<string, decimal> dealer8CellBaseValues,
        Dictionary<string, decimal> dealer8CellIncrements)
    {
        var submissionCells = new List<FinanceSubmissionCell>();
        
        foreach (var templateCell in templateCells)
        {
            // Skip cells with data type "text" as they're headers
            if (templateCell.DataType == "text")
                continue;
            
            decimal cellValue = CalculateCellValue(
                templateCell.CellAddress, 
                dealerIndex, 
                month, 
                random, 
                dealer8CellBaseValues, 
                dealer8CellIncrements);
            
            // Create submission cell
            var cell = new FinanceSubmissionCell
            {
                CellAddress = templateCell.CellAddress,
                GlobalAddress = templateCell.GlobalAddress,
                Value = cellValue,
                AggregationType = "monthly",
                FinanceSubmissionId = submissionId
            };
            
            submissionCells.Add(cell);
        }
        
        return submissionCells;
    }
    
    private static decimal CalculateCellValue(
        string cellAddress,
        int dealerIndex,
        int month,
        Random random,
        Dictionary<string, decimal> dealer8CellBaseValues,
        Dictionary<string, decimal> dealer8CellIncrements)
    {
        // For dealer 8 (Highland Vehicles at index 7), use arithmetic progression
        if (dealerIndex == 7)
        {
            // Calculate value based on month and base values
            return dealer8CellBaseValues[cellAddress] + 
                 (month - 1) * dealer8CellIncrements[cellAddress];
        }
        // Apply the specific ranges based on cell address and dealer
        else if (cellAddress.StartsWith("A"))
        {
            // For dealer 9 (index 8), A cells should be in range 400-600
            if (dealerIndex == 8)
            {
                return random.Next(400, 601);
            }
            // For all other dealers, A cells should be in range 100-200
            else
            {
                return random.Next(100, 201);
            }
        }
        else if (cellAddress.StartsWith("B"))
        {
            // For dealer 10 (index 9), B cells should be in range 10-90
            if (dealerIndex == 9)
            {
                return random.Next(10, 91);
            }
            // For all other dealers, B cells should be in range 300-400
            else
            {
                return random.Next(300, 401);
            }
        }
        
        // Default fallback
        return 0;
    }
} 