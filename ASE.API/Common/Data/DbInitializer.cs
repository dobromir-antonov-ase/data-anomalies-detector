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
        
        // Seed 10 dealers in 3 groups
        // Group 1: 3 dealers
        var dealerGroup1 = new Dealer[]
        {
            new Dealer { 
                Name = "Ace Auto Group", 
                Address = "123 Main St, Anytown, CA 90210", 
                ContactPhone = "555-123-4567", 
                ContactEmail = "contact@aceautogroup.com",
                GroupId = 1,
                GroupName = "Premium Auto Group"
            },
            new Dealer { 
                Name = "Bayside Motors", 
                Address = "456 Ocean Ave, Bayville, NY 11709", 
                ContactPhone = "555-987-6543", 
                ContactEmail = "info@baysidemotors.com",
                GroupId = 1,
                GroupName = "Premium Auto Group"
            },
            new Dealer { 
                Name = "Crestview Automotive", 
                Address = "789 Highland Dr, Crestview, TX 75001", 
                ContactPhone = "555-456-7890", 
                ContactEmail = "sales@crestviewauto.com",
                GroupId = 1,
                GroupName = "Premium Auto Group"
            }
        };
        
        // Group 2: 3 dealers
        var dealerGroup2 = new Dealer[]
        {
            new Dealer { 
                Name = "Downtown Cars", 
                Address = "101 Market St, Downtown, IL 60601", 
                ContactPhone = "555-789-0123", 
                ContactEmail = "info@downtowncars.com",
                GroupId = 2,
                GroupName = "Metro Dealership Network"
            },
            new Dealer { 
                Name = "Eastern Motors", 
                Address = "202 East Ave, Eastville, FL 33101", 
                ContactPhone = "555-234-5678", 
                ContactEmail = "contact@easternmotors.com",
                GroupId = 2,
                GroupName = "Metro Dealership Network"
            },
            new Dealer { 
                Name = "Fairview Dealership", 
                Address = "303 Fair St, Fairview, WA 98101", 
                ContactPhone = "555-345-6789", 
                ContactEmail = "sales@fairviewdealership.com",
                GroupId = 2,
                GroupName = "Metro Dealership Network"
            }
        };
        
        // Group 3: 4 dealers
        var dealerGroup3 = new Dealer[]
        {
            new Dealer { 
                Name = "Gateway Auto", 
                Address = "404 Gateway Blvd, Gateway, AZ 85001", 
                ContactPhone = "555-456-7890", 
                ContactEmail = "info@gatewayauto.com",
                GroupId = 3,
                GroupName = "Regional Motors Alliance"
            },
            new Dealer { 
                Name = "Highland Vehicles", 
                Address = "505 High St, Highland, CO 80202", 
                ContactPhone = "555-567-8901", 
                ContactEmail = "contact@highlandvehicles.com",
                GroupId = 3,
                GroupName = "Regional Motors Alliance"
            },
            new Dealer { 
                Name = "Island Cars", 
                Address = "606 Island Ave, Islandtown, HI 96801", 
                ContactPhone = "555-678-9012", 
                ContactEmail = "sales@islandcars.com",
                GroupId = 3,
                GroupName = "Regional Motors Alliance"
            },
            new Dealer { 
                Name = "Junction Motors", 
                Address = "707 Junction Rd, Junction City, OR 97301", 
                ContactPhone = "555-789-0123", 
                ContactEmail = "info@junctionmotors.com",
                GroupId = 3,
                GroupName = "Regional Motors Alliance"
            }
        };
        
        // Combine all dealers into one array
        var allDealers = dealerGroup1.Concat(dealerGroup2).Concat(dealerGroup3).ToArray();
        
        await context.Dealers.AddRangeAsync(allDealers);
        await context.SaveChangesAsync();
        
        // Create master templates for each year (2020-2024)
        var masterTemplates = new List<MasterTemplate>();
        
        for (int year = 2020; year <= 2024; year++)
        {
            var template = new MasterTemplate
            {
                Name = $"Standard Financial Template {year}",
                Year = year,
                IsActive = year == 2024, // Only the latest is active
                CreatedDate = new DateTime(year, 1, 1)
            };
            
            masterTemplates.Add(template);
        }
        
        await context.MasterTemplates.AddRangeAsync(masterTemplates);
        await context.SaveChangesAsync();
        
        // Add sheets, tables and cells to each template
        foreach (var template in masterTemplates)
        {
            await AddTemplateStructure(context, template);
        }
        
        // Add submissions for each year and template
        foreach (var template in masterTemplates)
        {
            await AddSubmissions(context, allDealers, template, template.Year, 1, 12);
        }
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
            
            // Setup special behavior for certain dealers
            Dictionary<string, decimal> specialDealerBaseValues = new Dictionary<string, decimal>();
            Dictionary<string, decimal> specialDealerIncrements = new Dictionary<string, decimal>();
            
            // For certain dealers, use progression values based on their group
            if (dealer.GroupId == 1)
            {
                // Group 1 dealers have higher base values with moderate increments
                InitializeProgressionValues(templateCells, specialDealerBaseValues, specialDealerIncrements, 
                    baseValue: 200, increment: 15);
            }
            else if (dealer.GroupId == 2)
            {
                // Group 2 dealers have medium base values with steady increments
                InitializeProgressionValues(templateCells, specialDealerBaseValues, specialDealerIncrements, 
                    baseValue: 150, increment: 10);
            }
            else if (dealer.GroupId == 3)
            {
                // Group 3 dealers have lower base values with faster increments
                InitializeProgressionValues(templateCells, specialDealerBaseValues, specialDealerIncrements, 
                    baseValue: 100, increment: 20);
            }
            
            for (int month = startMonth; month <= endMonth; month++)
            {
                // Create a new submission
                var submission = await CreateSubmission(context, dealer, template, year, month);
                
                // Create cells for this submission
                var submissionCells = CreateSubmissionCells(
                    templateCells, 
                    submission.Id, 
                    dealer.GroupId, 
                    month, 
                    random, 
                    specialDealerBaseValues, 
                    specialDealerIncrements,
                    year);
                
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
    
    private static void InitializeProgressionValues(
        List<MasterTemplateCell> templateCells,
        Dictionary<string, decimal> baseValues,
        Dictionary<string, decimal> increments,
        decimal baseValue,
        decimal increment)
    {
        foreach (var cell in templateCells)
        {
            if (cell.DataType == "text")
                continue;
            
            // Set initial value
            if (cell.CellAddress.StartsWith("A"))
            {
                baseValues[cell.CellAddress] = baseValue;
                increments[cell.CellAddress] = increment;
            }
            else if (cell.CellAddress.StartsWith("B"))
            {
                baseValues[cell.CellAddress] = baseValue * 1.5m; // B values are generally higher
                increments[cell.CellAddress] = increment * 1.2m;
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
        int groupId,
        int month,
        Random random,
        Dictionary<string, decimal> baseValues,
        Dictionary<string, decimal> increments,
        int year)
    {
        var submissionCells = new List<FinanceSubmissionCell>();
        
        foreach (var templateCell in templateCells)
        {
            // Skip cells with data type "text" as they're headers
            if (templateCell.DataType == "text")
                continue;
            
            decimal cellValue = CalculateCellValue(
                templateCell.CellAddress, 
                groupId, 
                month, 
                random, 
                baseValues, 
                increments,
                year);
            
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
        int groupId,
        int month,
        Random random,
        Dictionary<string, decimal> baseValues,
        Dictionary<string, decimal> increments,
        int year)
    {
        // Calculate the year factor (gradual growth over years)
        decimal yearFactor = 1 + (year - 2020) * 0.05m;
        
        // Use arithmetic progression for all dealers with group-specific characteristics
        if (baseValues.ContainsKey(cellAddress))
        {
            // Calculate value based on month, base values and year
            decimal value = baseValues[cellAddress] + 
                (month - 1) * increments[cellAddress];
            
            // Adjust for yearly growth
            value *= yearFactor;
            
            // Add some randomness (±5%)
            decimal randomFactor = (decimal)(random.NextDouble() * 0.1) - 0.05m;
            value *= (1 + randomFactor);
            
            return Math.Round(value, 2);
        }
        // Apply fallback ranges if the key isn't found
        else
        {
            decimal baseVal;
            
            if (cellAddress.StartsWith("A"))
            {
                baseVal = groupId == 1 ? random.Next(180, 240) :
                          groupId == 2 ? random.Next(140, 200) :
                                         random.Next(90, 150);
            }
            else // B cells
            {
                baseVal = groupId == 1 ? random.Next(250, 350) :
                          groupId == 2 ? random.Next(200, 300) :
                                         random.Next(150, 250);
            }
            
            // Adjust for yearly growth
            baseVal *= yearFactor;
            
            return Math.Round(baseVal, 2);
        }
    }
} 