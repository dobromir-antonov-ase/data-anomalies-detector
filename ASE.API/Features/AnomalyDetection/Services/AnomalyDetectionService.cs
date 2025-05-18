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
        allAnomalies.AddRange(await DetectCrossDealerAnomalies(recentSubmissions, lastMonthsCount));
        
        // 3. Find temporal anomalies (trends over time)
        allAnomalies.AddRange(await DetectTemporalAnomalies(recentSubmissions));
        
        return allAnomalies.OrderByDescending(a => a.DetectedAt);
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
    
    private async Task<IEnumerable<DataAnomaly>> DetectCrossDealerAnomalies(List<FinanceSubmission> submissions, int lastMonthsCount)
    {
        var anomalies = new List<DataAnomaly>();
        
        // Get submissions from the last 3 months
        var threeMonthsAgo = DateTime.UtcNow.AddMonths(lastMonthsCount * -1);
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
            var cellValues = submissionsByDealer.Values
                .Select(s => s.Cells.FirstOrDefault(c => c.GlobalAddress == address))
                .Where(c => c != null)
                .ToDictionary(c => submissionsByDealer.First(d => d.Value.Id == c!.FinanceSubmissionId).Key, c => c!.Value);
                
            if (cellValues.Count < 3)
                continue;
                
            // Calculate statistics
            var values = cellValues.Values.ToList();
            var avg = values.Average();
            var stdDev = Math.Sqrt(values.Select(v => Math.Pow((double)(v - avg), 2)).Average());
            
            // Check for outliers (values more than 2 standard deviations from the mean)
            foreach (var dealerCell in cellValues)
            {
                var dealerId = dealerCell.Key;
                var value = dealerCell.Value;
                var zScore = Math.Abs((double)(value - avg) / stdDev);
                
                // If value is an outlier
                if (zScore > 2)
                {
                    // Get dealer info for better context
                    var dealer = await _dbContext.Dealers.FindAsync(dealerId);
                    var dealerName = dealer?.Name ?? $"Dealer {dealerId}";
                    
                    // Calculate anomaly score based on z-score (0-100 scale)
                    var anomalyScore = Math.Min(100m, (decimal)(zScore * 25));
                    
                    // Determine severity based on z-score
                    string severity = zScore > 3 ? "high" : zScore > 2.5 ? "medium" : "low";
                    
                    // Get metric name from cell address (simplified example)
                    string metricName = GetMetricNameFromCellAddress(address);
                    
                    // Create enhanced anomaly object
                    anomalies.Add(new DataAnomaly
                    {
                        AnomalyType = "Cross-Dealer Outlier",
                        Description = $"Value for {address} from {dealerName} significantly deviates from other dealers",
                        Severity = severity,
                        DetectedAt = DateTime.UtcNow,
                        AnomalyScore = anomalyScore,
                        AffectedEntity = dealerName,
                        AffectedMetric = metricName,
                        ActualValue = value,
                        ExpectedValue = avg,
                        Threshold = (decimal)(avg + (2 * (decimal)stdDev)),
                        RelatedCellAddresses = new List<string> { address },
                        BusinessImpact = new BusinessImpact 
                        { 
                            Description = $"Potential misreporting or performance issue affecting {metricName}",
                            EstimatedValue = Math.Abs(value - avg) * 1000 // Simplified business impact calculation
                        },
                        RecommendedAction = severity == "high" 
                            ? $"Immediately verify {address} value with {dealerName} and investigate cause"
                            : $"Review {address} data entry processes at {dealerName}",
                        TimeRange = new TimeRange
                        {
                            Start = threeMonthsAgo,
                            End = DateTime.UtcNow
                        }
                    });
                }
            }
        }
        
        return anomalies;
    }
    
    // Helper method to get a user-friendly metric name from a cell address
    private string GetMetricNameFromCellAddress(string address)
    {
        // This is a simplified example - in a real app, you'd map cell addresses to actual metric names
        var metricMap = new Dictionary<string, string>
        {
            { "A1", "Monthly Revenue" },
            { "A2", "Quarterly Revenue" },
            { "B1", "Monthly Expenses" },
            { "B2", "Quarterly Expenses" },
            { "C1", "Profit Margin" },
            { "D1", "Vehicle Sales Count" },
            { "E1", "Marketing Spend" }
        };
        
        return metricMap.ContainsKey(address) ? metricMap[address] : $"Metric at {address}";
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