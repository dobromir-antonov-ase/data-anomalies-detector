using ASE.API.Common.Data;
using ASE.API.Features.AnomalyDetection.Models;
using ASE.API.Features.FinanceSubmissions.Models;
using ASE.API.Features.MasterTemplates.Models;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace ASE.API.Features.AnomalyDetection.Services;

public class AnomalyDetectionService
{
    private readonly FinanceDbContext _dbContext;
    
    public AnomalyDetectionService(FinanceDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    // Method to detect anomalies across all submissions (global)
    public async Task<IEnumerable<DataAnomaly>> DetectGlobalAnomalies(int lastMonthsCount = 3)
    {
        var allAnomalies = new List<DataAnomaly>();
        
        // Get all finance submissions (limit to last 3 months for performance)
        var nMonthsAgo = DateTime.UtcNow.AddMonths(lastMonthsCount * -1);
        var recentSubmissions = await _dbContext.FinanceSubmissions
            .Where(fs => fs.SubmissionDate >= nMonthsAgo)
            .Include(fs => fs.MasterTemplate)
            .Include(fs => fs.Cells)
            .Include(fs => fs.Dealer)
            .ToListAsync();
            
        if (!recentSubmissions.Any())
        {
            return allAnomalies;
        }
        
        // 1. Look for global patterns across all submissions
        allAnomalies.AddRange(await DetectGlobalPatterns(recentSubmissions));
        
        // 2. Detect cross-dealer anomalies
        allAnomalies.AddRange(await DetectCrossDealerAnomalies(recentSubmissions));
        
        // 3. Find temporal anomalies (trends over time)
        allAnomalies.AddRange(await DetectTemporalAnomalies(recentSubmissions));
        
        return allAnomalies.OrderByDescending(a => a.DetectedAt);
    }
    
    // Method to detect anomalies for a specific dealer
    public async Task<IEnumerable<DataAnomaly>> DetectAnomaliesByDealer(int dealerId)
    {
        var dealerAnomalies = new List<DataAnomaly>();
        
        // Get dealer information
        var dealer = await _dbContext.Dealers.FindAsync(dealerId);
        if (dealer == null)
        {
            return dealerAnomalies;
        }
        
        // Get all submissions for this dealer
        var dealerSubmissions = await _dbContext.FinanceSubmissions
            .Where(fs => fs.DealerId == dealerId)
            .Include(fs => fs.MasterTemplate)
            .Include(fs => fs.Cells)
            .OrderByDescending(fs => fs.SubmissionDate)
            .ToListAsync();
            
        if (!dealerSubmissions.Any())
        {
            return dealerAnomalies;
        }
        
        // 1. Detect submission-specific anomalies for the most recent submission
        var mostRecentSubmission = dealerSubmissions.First();
        dealerAnomalies.AddRange(await DetectAnomaliesInSubmission(mostRecentSubmission.Id));
        
        // 2. Detect dealer-specific patterns and trends
        dealerAnomalies.AddRange(await DetectDealerPatterns(dealerSubmissions));
        
        // 3. Compare with industry averages
        dealerAnomalies.AddRange(await CompareWithIndustryAverages(dealerId));
        
        return dealerAnomalies;
    }
    
    public async Task<IEnumerable<DataAnomaly>> DetectAnomaliesInSubmission(int submissionId)
    {
        // Get the finance submission including all related data
        var submission = await _dbContext.FinanceSubmissions
            .Include(fs => fs.MasterTemplate)
                .ThenInclude(mt => mt.Sheets)
                    .ThenInclude(sheet => sheet.Tables)
                        .ThenInclude(table => table.Cells)
            .Include(fs => fs.Cells)
            .Include(fs => fs.Dealer)
            .FirstOrDefaultAsync(fs => fs.Id == submissionId);
            
        if (submission == null)
        {
            return Enumerable.Empty<DataAnomaly>();
        }
        
        var anomalies = new List<DataAnomaly>();
        
        // Get template-based anomalies
        anomalies.AddRange(await DetectTemplateBasedAnomalies(submission));
        
        // Get dealer historical anomalies
        anomalies.AddRange(await DetectDealerHistoricalAnomalies(submission));
        
        return anomalies;
    }
    
    // ADVANCED PATTERN DETECTION METHODS
    
    private async Task<IEnumerable<DataAnomaly>> DetectGlobalPatterns(List<FinanceSubmission> submissions)
    {
        var anomalies = new List<DataAnomaly>();
        
        // Group cell values by their global address to detect global patterns
        var cellsByAddress = submissions
            .SelectMany(s => s.Cells)
            .GroupBy(c => c.GlobalAddress)
            .Where(g => g.Count() >= 5) // Only consider addresses with enough data points
            .ToDictionary(g => g.Key, g => g.ToList());
            
        foreach (var cellGroup in cellsByAddress)
        {
            var address = cellGroup.Key;
            var values = cellGroup.Value.Select(c => c.Value).ToList();
            
            // Calculate statistics
            var avg = values.Average();
            var stdDev = Math.Sqrt(values.Select(v => Math.Pow((double)(v - avg), 2)).Average());
            var median = values.OrderBy(v => v).ElementAt(values.Count / 2);
            
            // Look for bimodal distributions (could indicate two distinct groups)
            var histogram = CreateHistogram(values, 10);
            if (IsBimodalDistribution(histogram))
            {
                anomalies.Add(new DataAnomaly
                {
                    AnomalyType = "Bimodal Distribution",
                    Description = $"Cell {address} shows a bimodal distribution pattern, suggesting two distinct groups of values",
                    Severity = "medium",
                    DetectedAt = DateTime.UtcNow
                });
            }
            
            // Check for extreme skewness
            var skewness = CalculateSkewness(values);
            if (Math.Abs(skewness) > 1.5)
            {
                string direction = skewness > 0 ? "positively" : "negatively";
                anomalies.Add(new DataAnomaly
                {
                    AnomalyType = "Skewed Distribution",
                    Description = $"Cell {address} values are strongly {direction} skewed (skewness: {skewness:F2})",
                    Severity = "low",
                    DetectedAt = DateTime.UtcNow
                });
            }
        }
        
        return anomalies;
    }
    
    private async Task<IEnumerable<DataAnomaly>> DetectCrossDealerAnomalies(List<FinanceSubmission> submissions)
    {
        var anomalies = new List<DataAnomaly>();
        
        // Get submissions from the last 3 months
        var threeMonthsAgo = DateTime.UtcNow.AddMonths(-3);
        var recentSubmissionsWithSameMonth = submissions
            .Where(s => s.SubmissionDate >= threeMonthsAgo)
            .GroupBy(s => new { s.Month, s.Year })
            .OrderByDescending(g => g.Key.Year)
            .ThenByDescending(g => g.Key.Month)
            .FirstOrDefault();
            
        if (recentSubmissionsWithSameMonth == null)
        {
            return anomalies;
        }
        
        // Group submissions by dealer to compare dealers within the same period
        var submissionsByDealer = recentSubmissionsWithSameMonth.GroupBy(s => s.DealerId).ToDictionary(g => g.Key, g => g.First());
        
        // Common cell addresses across dealers
        var commonAddresses = submissionsByDealer.Values
            .SelectMany(s => s.Cells.Select(c => c.GlobalAddress))
            .GroupBy(address => address)
            .Where(g => g.Count() >= 3) // Only consider addresses shared by at least 3 dealers
            .Select(g => g.Key)
            .ToList();
            
        foreach (var address in commonAddresses)
        {
            var valuesForAddress = new Dictionary<int, decimal>();
            
            // Collect values for this address from each dealer
            foreach (var submission in submissionsByDealer.Values)
            {
                var cell = submission.Cells.FirstOrDefault(c => c.GlobalAddress == address);
                if (cell != null)
                {
                    valuesForAddress[submission.DealerId] = cell.Value;
                }
            }
            
            if (valuesForAddress.Count < 3) continue;
            
            // Calculate statistics
            var values = valuesForAddress.Values.ToList();
            var avg = values.Average();
            var stdDev = Math.Sqrt(values.Select(v => Math.Pow((double)(v - avg), 2)).Average());
            
            // Find outlier dealers
            foreach (var dealerValue in valuesForAddress)
            {
                var dealerId = dealerValue.Key;
                var value = dealerValue.Value;
                var dealer = await _dbContext.Dealers.FindAsync(dealerId);
                
                if (dealer == null) continue;
                
                // Check if this dealer's value is an outlier compared to other dealers
                if (Math.Abs((double)(value - avg)) > stdDev * 2)
                {
                    var deviation = Math.Round(((double)(value - avg) / (double)avg) * 100, 1);
                    var direction = deviation > 0 ? "higher" : "lower";
                    
                    anomalies.Add(new DataAnomaly
                    {
                        AnomalyType = "Cross-Dealer Outlier",
                        Description = $"Dealer '{dealer.Name}' reported value for {address} is {Math.Abs(deviation)}% {direction} than average",
                        Severity = Math.Abs(deviation) > 50 ? "high" : "medium",
                        DetectedAt = DateTime.UtcNow
                    });
                }
            }
        }
        
        return anomalies;
    }
    
    private async Task<IEnumerable<DataAnomaly>> DetectTemporalAnomalies(List<FinanceSubmission> submissions)
    {
        var anomalies = new List<DataAnomaly>();
        
        // Group submissions by month and year
        var submissionsByMonth = submissions
            .GroupBy(s => new { s.Month, s.Year })
            .OrderBy(g => g.Key.Year)
            .ThenBy(g => g.Key.Month)
            .ToList();
            
        if (submissionsByMonth.Count < 3)
        {
            // Not enough temporal data to analyze
            return anomalies;
        }
        
        // Look for consistently increasing/decreasing trends over the last 3 months
        var lastThreeMonths = submissionsByMonth.TakeLast(3).ToList();
        
        // Get common cell addresses across the last 3 months
        var commonAddresses = lastThreeMonths
            .SelectMany(g => g.SelectMany(s => s.Cells.Select(c => c.GlobalAddress)))
            .GroupBy(address => address)
            .Where(g => g.Count() >= 3) // Only consider addresses present in all 3 months
            .Select(g => g.Key)
            .ToList();
            
        foreach (var address in commonAddresses)
        {
            // Collect monthly totals for this address
            var monthlyValues = new List<(string Period, decimal Total)>();
            
            foreach (var monthGroup in lastThreeMonths)
            {
                var month = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(monthGroup.Key.Month);
                var total = monthGroup.Sum(s => 
                    s.Cells.Where(c => c.GlobalAddress == address).Sum(c => c.Value));
                
                monthlyValues.Add(($"{month} {monthGroup.Key.Year}", total));
            }
            
            // Check for consistent increasing trend
            if (IsConsistentlyIncreasing(monthlyValues.Select(m => m.Total).ToList()))
            {
                var increasePercent = Math.Round(
                    ((monthlyValues.Last().Total - monthlyValues.First().Total) / monthlyValues.First().Total) * 100, 1);
                
                anomalies.Add(new DataAnomaly
                {
                    AnomalyType = "Increasing Trend",
                    Description = $"Cell {address} shows a consistent increase over the last 3 months, with {increasePercent}% total increase",
                    Severity = increasePercent > 30 ? "high" : "medium",
                    DetectedAt = DateTime.UtcNow
                });
            }
            
            // Check for consistent decreasing trend
            if (IsConsistentlyDecreasing(monthlyValues.Select(m => m.Total).ToList()))
            {
                var decreasePercent = Math.Round(
                    ((monthlyValues.First().Total - monthlyValues.Last().Total) / monthlyValues.First().Total) * 100, 1);
                
                anomalies.Add(new DataAnomaly
                {
                    AnomalyType = "Decreasing Trend",
                    Description = $"Cell {address} shows a consistent decrease over the last 3 months, with {decreasePercent}% total decrease",
                    Severity = decreasePercent > 30 ? "high" : "medium",
                    DetectedAt = DateTime.UtcNow
                });
            }
        }
        
        return anomalies;
    }
    
    private async Task<IEnumerable<DataAnomaly>> DetectDealerPatterns(List<FinanceSubmission> dealerSubmissions)
    {
        var anomalies = new List<DataAnomaly>();
        
        if (dealerSubmissions.Count < 3)
        {
            // Not enough submissions to detect patterns
            return anomalies;
        }
        
        // Sort by submission date to analyze in chronological order
        var sortedSubmissions = dealerSubmissions.OrderBy(s => s.SubmissionDate).ToList();
        var dealer = sortedSubmissions.First().Dealer;
        
        // Look for cyclical patterns (e.g., quarterly peaks)
        var submissionsByMonth = sortedSubmissions
            .GroupBy(s => s.Month)
            .Where(g => g.Count() >= 2) // At least 2 entries for the same month
            .ToDictionary(g => g.Key, g => g.ToList());
            
        // Get common cell addresses for this dealer
        var commonAddresses = sortedSubmissions
            .SelectMany(s => s.Cells.Select(c => c.GlobalAddress))
            .GroupBy(a => a)
            .Where(g => g.Count() >= sortedSubmissions.Count / 2) // Present in at least half of submissions
            .Select(g => g.Key)
            .ToList();
            
        foreach (var address in commonAddresses)
        {
            // Check for end-of-quarter patterns
            var endOfQuarterMonths = new[] { 3, 6, 9, 12 };
            var regularMonths = Enumerable.Range(1, 12).Except(endOfQuarterMonths).ToArray();
            
            var quarterValues = sortedSubmissions
                .Where(s => endOfQuarterMonths.Contains(s.Month))
                .SelectMany(s => s.Cells.Where(c => c.GlobalAddress == address))
                .Select(c => c.Value)
                .ToList();
                
            var nonQuarterValues = sortedSubmissions
                .Where(s => regularMonths.Contains(s.Month))
                .SelectMany(s => s.Cells.Where(c => c.GlobalAddress == address))
                .Select(c => c.Value)
                .ToList();
                
            if (quarterValues.Count >= 2 && nonQuarterValues.Count >= 2)
            {
                var quarterAvg = quarterValues.Average();
                var nonQuarterAvg = nonQuarterValues.Average();
                
                // If quarter-end values are consistently higher
                if (quarterAvg > nonQuarterAvg * 1.2m)
                {
                    var increasePercent = Math.Round(((quarterAvg - nonQuarterAvg) / nonQuarterAvg) * 100, 1);
                    
                    anomalies.Add(new DataAnomaly
                    {
                        AnomalyType = "Quarterly Pattern",
                        Description = $"Dealer '{dealer.Name}' shows {increasePercent}% higher values for {address} at quarter-end months",
                        Severity = increasePercent > 40 ? "high" : "medium",
                        DetectedAt = DateTime.UtcNow
                    });
                }
            }
        }
        
        return anomalies;
    }
    
    private async Task<IEnumerable<DataAnomaly>> CompareWithIndustryAverages(int dealerId)
    {
        var anomalies = new List<DataAnomaly>();
        
        // Get most recent submission for this dealer
        var dealerSubmission = await _dbContext.FinanceSubmissions
            .Where(fs => fs.DealerId == dealerId)
            .Include(fs => fs.Cells)
            .Include(fs => fs.Dealer)
            .OrderByDescending(fs => fs.SubmissionDate)
            .FirstOrDefaultAsync();
            
        if (dealerSubmission == null)
        {
            return anomalies;
        }
        
        // Get submissions from other dealers in the same month/year
        var otherDealerSubmissions = await _dbContext.FinanceSubmissions
            .Where(fs => 
                fs.DealerId != dealerId && 
                fs.Month == dealerSubmission.Month && 
                fs.Year == dealerSubmission.Year)
            .Include(fs => fs.Cells)
            .ToListAsync();
            
        if (!otherDealerSubmissions.Any())
        {
            return anomalies;
        }
        
        // For each cell in dealer's submission, compare with industry average
        foreach (var cell in dealerSubmission.Cells)
        {
            // Find same cell in other dealer submissions
            var industryCells = otherDealerSubmissions
                .SelectMany(s => s.Cells)
                .Where(c => c.GlobalAddress == cell.GlobalAddress)
                .ToList();
                
            if (industryCells.Count >= 3) // Only if we have enough data points
            {
                var industryAvg = industryCells.Average(c => c.Value);
                var deviation = Math.Round((cell.Value - industryAvg) / industryAvg * 100, 1);
                
                // If value deviates significantly from industry average
                if (Math.Abs(deviation) > 30)
                {
                    var direction = deviation > 0 ? "above" : "below";
                    
                    anomalies.Add(new DataAnomaly
                    {
                        AnomalyType = "Industry Deviation",
                        Description = $"Dealer '{dealerSubmission.Dealer.Name}' is {Math.Abs(deviation)}% {direction} industry average for {cell.GlobalAddress}",
                        Severity = Math.Abs(deviation) > 50 ? "high" : "medium",
                        DetectedAt = DateTime.UtcNow
                    });
                }
            }
        }
        
        return anomalies;
    }
    
    private async Task<IEnumerable<DataAnomaly>> DetectTemplateBasedAnomalies(FinanceSubmission submission)
    {
        var anomalies = new List<DataAnomaly>();
        
        // Simple anomaly detection logic based on template structure
        foreach (var sheet in submission.MasterTemplate.Sheets)
        {
            string sheetName = sheet.Name.Replace(" ", "");
            
            foreach (var table in sheet.Tables)
            {
                string tableName = table.Name.Replace(" ", "");
                
                // Look for empty cells in tables
                var emptyCells = table.Cells.Where(c => string.IsNullOrWhiteSpace(c.Value)).ToList();
                if (emptyCells.Any())
                {
                    anomalies.Add(new DataAnomaly
                    {
                        AnomalyType = "Missing Data",
                        Description = $"Found {emptyCells.Count} empty cells in table '{table.Name}' on sheet '{sheet.Name}'",
                        Severity = emptyCells.Count > 5 ? "high" : "medium",
                        DetectedAt = DateTime.UtcNow
                    });
                }
                
                // Look for numeric outliers in number cells
                var numberCells = table.Cells.Where(c => c.DataType == "number" && !string.IsNullOrWhiteSpace(c.Value)).ToList();
                if (numberCells.Count > 5)
                {
                    var values = numberCells.Select(c => decimal.TryParse(c.Value, out var num) ? num : 0).ToList();
                    var avg = values.Average();
                    var stdDev = Math.Sqrt(values.Select(v => Math.Pow((double)(v - avg), 2)).Average());
                    
                    var outliers = values.Where(v => Math.Abs((double)(v - avg)) > stdDev * 2).ToList();
                    if (outliers.Any())
                    {
                        anomalies.Add(new DataAnomaly
                        {
                            AnomalyType = "Statistical Outlier",
                            Description = $"Found {outliers.Count} outlier values in table '{table.Name}' on sheet '{sheet.Name}'",
                            Severity = outliers.Count > 2 ? "high" : "medium",
                            DetectedAt = DateTime.UtcNow
                        });
                    }
                }
            }
        }
        
        return anomalies;
    }
    
    private async Task<IEnumerable<DataAnomaly>> DetectDealerHistoricalAnomalies(FinanceSubmission currentSubmission)
    {
        var anomalies = new List<DataAnomaly>();
        
        // Get submissions from previous year for the same dealer and same month
        var previousYearSubmissions = await _dbContext.FinanceSubmissions
            .Where(fs => 
                fs.DealerId == currentSubmission.DealerId && 
                fs.Month == currentSubmission.Month && 
                fs.Year == currentSubmission.Year - 1)
            .Include(fs => fs.Cells)
            .ToListAsync();
            
        if (!previousYearSubmissions.Any())
        {
            // No previous year data to compare with
            return anomalies;
        }
        
        var previousYearSubmission = previousYearSubmissions.First();
        var monthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(currentSubmission.Month);
        
        // Compare current submission cells with previous year's corresponding cells
        foreach (var currentCell in currentSubmission.Cells)
        {
            // Find matching cell from previous year (same global address)
            var previousYearCell = previousYearSubmission.Cells
                .FirstOrDefault(c => c.GlobalAddress == currentCell.GlobalAddress);
                
            if (previousYearCell != null)
            {
                // Check for significant changes (more than 20% difference)
                if (previousYearCell.Value != 0 && Math.Abs((currentCell.Value - previousYearCell.Value) / previousYearCell.Value) > 0.2m)
                {
                    var changePercentage = Math.Round(((currentCell.Value - previousYearCell.Value) / previousYearCell.Value) * 100, 1);
                    var direction = changePercentage > 0 ? "increase" : "decrease";
                    
                    anomalies.Add(new DataAnomaly
                    {
                        AnomalyType = "Year-Over-Year Variance",
                        Description = $"Cell {currentCell.GlobalAddress} shows {Math.Abs(changePercentage)}% {direction} compared to {monthName} last year",
                        Severity = Math.Abs(changePercentage) > 50 ? "high" : "medium",
                        DetectedAt = DateTime.UtcNow
                    });
                }
            }
        }
        
        // Check for missing data compared to previous year
        var missingCells = previousYearSubmission.Cells
            .Where(prevCell => !currentSubmission.Cells.Any(currCell => currCell.GlobalAddress == prevCell.GlobalAddress))
            .ToList();
            
        if (missingCells.Any())
        {
            anomalies.Add(new DataAnomaly
            {
                AnomalyType = "Missing Historical Data",
                Description = $"Found {missingCells.Count} cells that were reported last year but missing in current submission",
                Severity = missingCells.Count > 5 ? "high" : "medium",
                DetectedAt = DateTime.UtcNow
            });
        }
        
        return anomalies;
    }
    
    // HELPER METHODS FOR STATISTICAL ANALYSIS
    
    private Dictionary<decimal, int> CreateHistogram(List<decimal> values, int bins)
    {
        if (!values.Any()) return new Dictionary<decimal, int>();
        
        var min = values.Min();
        var max = values.Max();
        var range = max - min;
        var binWidth = range / bins;
        
        var histogram = new Dictionary<decimal, int>();
        
        for (int i = 0; i < bins; i++)
        {
            var binStart = min + (i * binWidth);
            var binEnd = min + ((i + 1) * binWidth);
            var count = values.Count(v => v >= binStart && (i == bins - 1 ? v <= binEnd : v < binEnd));
            
            histogram[binStart] = count;
        }
        
        return histogram;
    }
    
    private bool IsBimodalDistribution(Dictionary<decimal, int> histogram)
    {
        if (histogram.Count < 4) return false;
        
        // Sort bins by their frequency
        var sortedBins = histogram.OrderByDescending(kv => kv.Value).ToList();
        
        // Check if there are two clear peaks
        if (sortedBins.Count >= 2)
        {
            var firstPeak = sortedBins[0];
            var secondPeak = sortedBins[1];
            
            // Check if the second peak is at least 75% of the first peak
            if (secondPeak.Value >= firstPeak.Value * 0.75m)
            {
                // Check if the peaks are separated (not adjacent bins)
                var firstPeakBin = histogram.Keys.ToList().IndexOf(firstPeak.Key);
                var secondPeakBin = histogram.Keys.ToList().IndexOf(secondPeak.Key);
                
                return Math.Abs(firstPeakBin - secondPeakBin) > 1;
            }
        }
        
        return false;
    }
    
    private double CalculateSkewness(List<decimal> values)
    {
        if (values.Count < 3) return 0;
        
        var avg = values.Average();
        var stdDev = Math.Sqrt(values.Select(v => Math.Pow((double)(v - avg), 2)).Average());
        
        if (stdDev == 0) return 0;
        
        // Calculate skewness using the third moment
        var thirdMoment = values.Select(v => Math.Pow((double)(v - avg), 3)).Average();
        return thirdMoment / Math.Pow(stdDev, 3);
    }
    
    private bool IsConsistentlyIncreasing(List<decimal> values)
    {
        if (values.Count < 3) return false;
        
        for (int i = 1; i < values.Count; i++)
        {
            if (values[i] <= values[i - 1])
                return false;
        }
        
        return true;
    }
    
    private bool IsConsistentlyDecreasing(List<decimal> values)
    {
        if (values.Count < 3) return false;
        
        for (int i = 1; i < values.Count; i++)
        {
            if (values[i] >= values[i - 1])
                return false;
        }
        
        return true;
    }

    // Added method to detect data patterns in a submission
    public async Task<IEnumerable<DataPattern>> DetectDataPatterns(int submissionId)
    {
        var patterns = new List<DataPattern>();
        
        // Get the finance submission including all related data
        var submission = await _dbContext.FinanceSubmissions
            .Include(fs => fs.MasterTemplate)
            .Include(fs => fs.Cells)
            .Include(fs => fs.Dealer)
            .FirstOrDefaultAsync(fs => fs.Id == submissionId);
            
        if (submission == null)
        {
            return patterns;
        }
        
        // Get historical submissions for this dealer
        var historicalSubmissions = await _dbContext.FinanceSubmissions
            .Where(fs => fs.DealerId == submission.DealerId && fs.Id != submissionId)
            .Include(fs => fs.Cells)
            .OrderByDescending(fs => fs.SubmissionDate)
            .ToListAsync();
            
        // Perform correlation analysis between cells
        patterns.AddRange(DetectCellCorrelations(submission, historicalSubmissions));
        
        // Detect mathematical relationships
        patterns.AddRange(DetectMathematicalRelationships(submission));
        
        // Detect seasonal patterns
        patterns.AddRange(await DetectSeasonalPatterns(submission, historicalSubmissions));
        
        return patterns;
    }
    
    // PATTERN DETECTION METHODS
    
    private List<DataPattern> DetectCellCorrelations(FinanceSubmission submission, List<FinanceSubmission> historicalSubmissions)
    {
        var patterns = new List<DataPattern>();
        
        // We need at least the current submission + 5 historical submissions for meaningful correlations
        if (historicalSubmissions.Count < 5)
        {
            return patterns;
        }
        
        // Get common cell addresses across submissions
        var currentCellAddresses = submission.Cells.Select(c => c.GlobalAddress).ToList();
        var cellsToAnalyze = currentCellAddresses.Where(address => 
            historicalSubmissions.All(s => s.Cells.Any(c => c.GlobalAddress == address))).ToList();
            
        // If we don't have enough common cells, can't perform correlation
        if (cellsToAnalyze.Count < 2)
        {
            return patterns;
        }
        
        // Create combinations of cell pairs to check correlations
        for (int i = 0; i < cellsToAnalyze.Count; i++)
        {
            for (int j = i + 1; j < cellsToAnalyze.Count; j++)
            {
                var cell1Address = cellsToAnalyze[i];
                var cell2Address = cellsToAnalyze[j];
                
                // Get value series for both cells from historical submissions + current submission
                var cell1Values = new List<decimal>();
                var cell2Values = new List<decimal>();
                
                // Add current submission values
                cell1Values.Add(submission.Cells.First(c => c.GlobalAddress == cell1Address).Value);
                cell2Values.Add(submission.Cells.First(c => c.GlobalAddress == cell2Address).Value);
                
                // Add historical values
                foreach (var historicalSubmission in historicalSubmissions)
                {
                    var cell1 = historicalSubmission.Cells.FirstOrDefault(c => c.GlobalAddress == cell1Address);
                    var cell2 = historicalSubmission.Cells.FirstOrDefault(c => c.GlobalAddress == cell2Address);
                    
                    if (cell1 != null && cell2 != null)
                    {
                        cell1Values.Add(cell1.Value);
                        cell2Values.Add(cell2.Value);
                    }
                }
                
                // Calculate correlation coefficient
                var correlation = CalculateCorrelation(cell1Values, cell2Values);
                
                // If strong correlation (positive or negative), add as a pattern
                if (Math.Abs(correlation) > 0.7m)
                {
                    var correlationType = correlation > 0 ? "positive" : "negative";
                    var absCorrelation = Math.Abs(correlation);
                    
                    patterns.Add(new DataPattern
                    {
                        PatternType = "Cell Correlation",
                        Description = $"Strong {correlationType} correlation (r = {correlation:F2}) between {cell1Address} and {cell2Address}",
                        Significance = absCorrelation > 0.9m ? "high" : "medium",
                        ConfidenceScore = absCorrelation * 100,
                        Correlation = correlation,
                        RelatedCellAddresses = new List<string> { cell1Address, cell2Address },
                        DetectedAt = DateTime.UtcNow
                    });
                }
            }
        }
        
        return patterns;
    }
    
    private List<DataPattern> DetectMathematicalRelationships(FinanceSubmission submission)
    {
        var patterns = new List<DataPattern>();
        
        // Group cells by their sheet to analyze relationships within sheets
        var cellsBySheet = submission.Cells
            .GroupBy(c => c.GlobalAddress.Split('!')[0])
            .ToDictionary(g => g.Key, g => g.ToList());
            
        foreach (var sheetCells in cellsBySheet.Values)
        {
            // Need at least 3 cells for basic relationships
            if (sheetCells.Count < 3)
                continue;
                
            // Look for sum relationships (A + B = C)
            for (int i = 0; i < sheetCells.Count; i++)
            {
                for (int j = i + 1; j < sheetCells.Count; j++)
                {
                    for (int k = 0; k < sheetCells.Count; k++)
                    {
                        if (k == i || k == j) continue;
                        
                        var cellA = sheetCells[i];
                        var cellB = sheetCells[j];
                        var cellC = sheetCells[k];
                        
                        // Check if A + B = C (with small margin for rounding errors)
                        if (Math.Abs(cellA.Value + cellB.Value - cellC.Value) < 0.01m)
                        {
                            patterns.Add(new DataPattern
                            {
                                PatternType = "Sum Relationship",
                                Description = $"{cellA.GlobalAddress} + {cellB.GlobalAddress} = {cellC.GlobalAddress}",
                                Formula = $"{cellA.GlobalAddress} + {cellB.GlobalAddress} = {cellC.GlobalAddress}",
                                Significance = "high",
                                ConfidenceScore = 95,
                                RelatedCellAddresses = new List<string> { cellA.GlobalAddress, cellB.GlobalAddress, cellC.GlobalAddress },
                                DetectedAt = DateTime.UtcNow
                            });
                        }
                        
                        // Check if A - B = C
                        if (Math.Abs(cellA.Value - cellB.Value - cellC.Value) < 0.01m)
                        {
                            patterns.Add(new DataPattern
                            {
                                PatternType = "Difference Relationship",
                                Description = $"{cellA.GlobalAddress} - {cellB.GlobalAddress} = {cellC.GlobalAddress}",
                                Formula = $"{cellA.GlobalAddress} - {cellB.GlobalAddress} = {cellC.GlobalAddress}",
                                Significance = "high",
                                ConfidenceScore = 95,
                                RelatedCellAddresses = new List<string> { cellA.GlobalAddress, cellB.GlobalAddress, cellC.GlobalAddress },
                                DetectedAt = DateTime.UtcNow
                            });
                        }
                    }
                }
            }
        }
        
        return patterns;
    }
    
    private async Task<List<DataPattern>> DetectSeasonalPatterns(FinanceSubmission currentSubmission, List<FinanceSubmission> historicalSubmissions)
    {
        var patterns = new List<DataPattern>();
        
        // Need at least 12 months of data to detect seasonal patterns
        if (historicalSubmissions.Count < 11) // 11 historical + 1 current = 12 months
            return patterns;
            
        // Get all dealer submissions ordered by month/year
        var allSubmissions = new List<FinanceSubmission>(historicalSubmissions);
        allSubmissions.Add(currentSubmission);
        
        // Order by year and month
        var orderedSubmissions = allSubmissions
            .OrderBy(s => s.Year)
            .ThenBy(s => s.Month)
            .ToList();
            
        // Check for common cell addresses
        var commonAddresses = currentSubmission.Cells
            .Select(c => c.GlobalAddress)
            .Where(address => historicalSubmissions.All(s => s.Cells.Any(c => c.GlobalAddress == address)))
            .ToList();
            
        foreach (var address in commonAddresses)
        {
            // Get monthly values for this cell
            var monthlyValues = new decimal[12];
            Array.Fill(monthlyValues, 0m);
            
            var valueCount = new int[12];
            Array.Fill(valueCount, 0);
            
            // Accumulate values by month
            foreach (var submission in orderedSubmissions)
            {
                var cell = submission.Cells.FirstOrDefault(c => c.GlobalAddress == address);
                if (cell != null)
                {
                    var monthIndex = submission.Month - 1; // 0-based index for array
                    monthlyValues[monthIndex] += cell.Value;
                    valueCount[monthIndex]++;
                }
            }
            
            // Calculate average for each month (where we have data)
            for (int i = 0; i < 12; i++)
            {
                if (valueCount[i] > 0)
                {
                    monthlyValues[i] /= valueCount[i];
                }
            }
            
            // Find the highest and lowest months
            int highestMonth = 0;
            int lowestMonth = 0;
            decimal highestValue = decimal.MinValue;
            decimal lowestValue = decimal.MaxValue;
            
            for (int i = 0; i < 12; i++)
            {
                if (valueCount[i] > 0) // Only consider months with data
                {
                    if (monthlyValues[i] > highestValue)
                    {
                        highestValue = monthlyValues[i];
                        highestMonth = i;
                    }
                    if (monthlyValues[i] < lowestValue)
                    {
                        lowestValue = monthlyValues[i];
                        lowestMonth = i;
                    }
                }
            }
            
            // If there is a significant difference between highest and lowest months (>= 20%)
            if (lowestValue > 0 && ((highestValue - lowestValue) / lowestValue) >= 0.2m)
            {
                var highMonthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(highestMonth + 1);
                var lowMonthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(lowestMonth + 1);
                var percentDiff = Math.Round(((highestValue - lowestValue) / lowestValue) * 100, 1);
                
                patterns.Add(new DataPattern
                {
                    PatternType = "Seasonal Pattern",
                    Description = $"Cell {address} shows seasonal pattern with highest values in {highMonthName} and lowest in {lowMonthName} ({percentDiff}% difference)",
                    Significance = percentDiff > 50 ? "high" : "medium",
                    ConfidenceScore = Math.Min(95, 60 + (percentDiff / 2)),
                    RelatedCellAddresses = new List<string> { address },
                    DetectedAt = DateTime.UtcNow
                });
            }
        }
        
        return patterns;
    }
    
    // Helper method to calculate correlation coefficient
    private decimal CalculateCorrelation(List<decimal> xValues, List<decimal> yValues)
    {
        if (xValues.Count != yValues.Count || xValues.Count < 2)
            return 0;
            
        int n = xValues.Count;
        
        decimal sumX = 0;
        decimal sumY = 0;
        decimal sumXY = 0;
        decimal sumX2 = 0;
        decimal sumY2 = 0;
        
        for (int i = 0; i < n; i++)
        {
            sumX += xValues[i];
            sumY += yValues[i];
            sumXY += xValues[i] * yValues[i];
            sumX2 += xValues[i] * xValues[i];
            sumY2 += yValues[i] * yValues[i];
        }
        
        decimal numerator = n * sumXY - sumX * sumY;
        decimal denominator = (decimal)Math.Sqrt((double)(n * sumX2 - sumX * sumX) * (double)(n * sumY2 - sumY * sumY));
        
        if (denominator == 0)
            return 0;
            
        return numerator / denominator;
    }
} 