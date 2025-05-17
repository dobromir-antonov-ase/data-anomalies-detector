using ASE.API.Common.Data;
using ASE.API.Features.AnomalyDetection.Models;
using ASE.API.Features.Dealers.Models;
using ASE.API.Features.FinanceSubmissions.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms.TimeSeries;

namespace ASE.API.Features.AnomalyDetection.Services;

public class DataPatternMLService
{
    private readonly FinanceDbContext _dbContext;
    private readonly MLContext _mlContext;

    public DataPatternMLService(FinanceDbContext dbContext)
    {
        _dbContext = dbContext;
        _mlContext = new MLContext(seed: 42); // Fixed seed for reproducibility
    }

    /// <summary>
    /// Detect anomalies in time series data for a specific dealer
    /// </summary>
    public async Task<IEnumerable<DataAnomaly>> DetectDealerTimeSeriesAnomalies(int dealerId)
    {
        var anomalies = new List<DataAnomaly>();

        // Get latest submission from this dealer to use as reference point
        var latestSubmission = await _dbContext.FinanceSubmissions
            .Where(fs => fs.DealerId == dealerId)
            .OrderByDescending(fs => fs.Year)
            .ThenByDescending(fs => fs.Month)
            .FirstOrDefaultAsync();

        if (latestSubmission == null)
            return anomalies;

        // Get historical submissions for this dealer, ordered by date
        var historicalData = await _dbContext.FinanceSubmissions
            .Where(fs => fs.DealerId == dealerId)
            .Include(x => x.Cells)
            .OrderBy(fs => fs.Year)
            .ThenBy(fs => fs.Month)
            .ToListAsync();

        if (historicalData.Count < 10) // Need sufficient data points for time series analysis
            return anomalies;

        // Get unique cell addresses that appear in multiple submissions
        var cellAddresses = historicalData
            .SelectMany(s => s.Cells.Select(c => c.GlobalAddress))
            .GroupBy(address => address)
            .Where(g => g.Count() >= 10) // Need at least 10 data points
            .Select(g => g.Key)
            .ToList();

        foreach (var address in cellAddresses)
        {
            // Create time series for this cell address
            var timeSeriesData = historicalData
                .Select((s, index) =>
                {
                    var cell = s.Cells.FirstOrDefault(c => c.GlobalAddress == address);
                    return new CellTimeSeries
                    {
                        SequenceNumber = index + 1,
                        Address = address,
                        Value = cell?.Value != null ? (float)cell.Value : 0f,
                        Month = s.Month,
                        Year = s.Year
                    };
                })
                .Where(ts => ts.Value != 0) // Filter out missing values
                .OrderBy(ts => ts.SequenceNumber)
                .ToList();

            if (timeSeriesData.Count < 10)
                continue;

            // Run spike detection
            var spikeDetectionResults = DetectSpikes(timeSeriesData);
            foreach (var spike in spikeDetectionResults)
            {
                var dataPoint = timeSeriesData[spike.Position];
                anomalies.Add(new DataAnomaly
                {
                    AnomalyType = "ML-Detected Dealer Spike",
                    Description = $"Spike detected in {address} for dealer {dealerId} at {dataPoint.Month}/{dataPoint.Year} with confidence score {spike.Score:F2}",
                    Severity = spike.Score > 0.7 ? "high" : "medium",
                    DetectedAt = DateTime.UtcNow
                });
            }

            // Run change point detection
            var changePointResults = DetectChangePoints(timeSeriesData);
            foreach (var changePoint in changePointResults)
            {
                var dataPoint = timeSeriesData[changePoint.Position];
                anomalies.Add(new DataAnomaly
                {
                    AnomalyType = "ML-Detected Dealer Change Point",
                    Description = $"Change point detected in {address} for dealer {dealerId} starting at {dataPoint.Month}/{dataPoint.Year} with confidence score {changePoint.Score:F2}",
                    Severity = changePoint.Score > 0.7 ? "high" : "medium",
                    DetectedAt = DateTime.UtcNow
                });
            }
        }

        return anomalies;
    }

    /// <summary>
    /// Detect anomalies in time series data for a dealer group
    /// </summary>
    public async Task<IEnumerable<DataAnomaly>> DetectDealerGroupTimeSeriesAnomalies(int groupId)
    {
        var anomalies = new List<DataAnomaly>();

        // Get dealers in this group
        var dealersInGroup = await _dbContext.Dealers
            .Where(d => d.GroupId == groupId)
            .ToListAsync();

        if (!dealersInGroup.Any())
            return anomalies;

        // Get all submissions from dealers in this group
        var historicalData = await _dbContext.FinanceSubmissions
            .Where(fs => dealersInGroup.Select(d => d.Id).Contains(fs.DealerId))
            .Include(x => x.Cells)
            .Include(x => x.Dealer)
            .OrderBy(fs => fs.Year)
            .ThenBy(fs => fs.Month)
            .ToListAsync();

        if (historicalData.Count < 10) // Need sufficient data points for time series analysis
            return anomalies;

        // Group submissions by month/year to get aggregate values
        var groupedByPeriod = historicalData
            .GroupBy(fs => new { fs.Year, fs.Month })
            .OrderBy(g => g.Key.Year)
            .ThenBy(g => g.Key.Month)
            .ToList();

        if (groupedByPeriod.Count < 10) // Need sufficient periods for time series analysis
            return anomalies;

        // Get unique cell addresses that appear across submissions
        var cellAddresses = historicalData
            .SelectMany(s => s.Cells.Select(c => c.GlobalAddress))
            .GroupBy(address => address)
            .Where(g => g.Count() >= groupedByPeriod.Count / 2) // Address appears in at least half of periods
            .Select(g => g.Key)
            .ToList();

        foreach (var address in cellAddresses)
        {
            // Create aggregate time series for this cell address across all dealers in group
            var timeSeriesData = new List<CellTimeSeries>();

            for (int i = 0; i < groupedByPeriod.Count; i++)
            {
                var periodGroup = groupedByPeriod[i];
                var year = periodGroup.Key.Year;
                var month = periodGroup.Key.Month;

                // Get the average value for this cell across all dealers in the group for this period
                var periodCells = periodGroup
                    .SelectMany(fs => fs.Cells)
                    .Where(c => c.GlobalAddress == address)
                    .ToList();

                if (periodCells.Any())
                {
                    // Calculate average value
                    float avgValue = (float)periodCells.Average(c => c.Value);

                    timeSeriesData.Add(new CellTimeSeries
                    {
                        SequenceNumber = i + 1,
                        Address = address,
                        Value = avgValue,
                        Month = month,
                        Year = year
                    });
                }
            }

            if (timeSeriesData.Count < 10)
                continue;

            // Run spike detection on the group's aggregate data
            var spikeDetectionResults = DetectSpikes(timeSeriesData);
            foreach (var spike in spikeDetectionResults)
            {
                var dataPoint = timeSeriesData[spike.Position];
                anomalies.Add(new DataAnomaly
                {
                    AnomalyType = "ML-Detected Group Spike",
                    Description = $"Spike detected in {address} for dealer group {groupId} at {dataPoint.Month}/{dataPoint.Year} with confidence score {spike.Score:F2}",
                    Severity = spike.Score > 0.7 ? "high" : "medium",
                    DetectedAt = DateTime.UtcNow
                });
            }

            // Run change point detection on the group's aggregate data
            var changePointResults = DetectChangePoints(timeSeriesData);
            foreach (var changePoint in changePointResults)
            {
                var dataPoint = timeSeriesData[changePoint.Position];
                anomalies.Add(new DataAnomaly
                {
                    AnomalyType = "ML-Detected Group Change Point",
                    Description = $"Change point detected in {address} for dealer group {groupId} starting at {dataPoint.Month}/{dataPoint.Year} with confidence score {changePoint.Score:F2}",
                    Severity = changePoint.Score > 0.7 ? "high" : "medium",
                    DetectedAt = DateTime.UtcNow
                });
            }
        }

        return anomalies;
    }

    /// <summary>
    /// Detect patterns by dealer using clustering
    /// </summary>
    public async Task<IEnumerable<DataPattern>> DetectDealerPatterns(int dealerId)
    {
        var patterns = new List<DataPattern>();

        // Get the dealer
        var dealer = await _dbContext.Dealers.FindAsync(dealerId);
        if (dealer == null)
            return patterns;

        // Get all submissions for this dealer
        var dealerSubmissions = await _dbContext.FinanceSubmissions
            .Where(fs => fs.DealerId == dealerId)
            .Include(fs => fs.Cells)
            .OrderByDescending(fs => fs.SubmissionDate)
            .Take(60) // Get up to 5 years of monthly data
            .ToListAsync();

        if (dealerSubmissions.Count < 12) // Need at least a year of data
            return patterns;

        // Identify patterns over time
        var yearlyPatterns = await DetectDealerYearlyPatterns(dealerId, dealerSubmissions);
        var monthlyPatterns = await DetectDealerMonthlyPatterns(dealerId, dealerSubmissions);

        patterns.AddRange(yearlyPatterns);
        patterns.AddRange(monthlyPatterns);

        // Also detect if this dealer follows their group's general pattern
        var groupDeviation = await DetectDealerGroupDeviation(dealer);
        if (groupDeviation != null)
            patterns.Add(groupDeviation);

        return patterns;
    }

    /// <summary>
    /// Detect patterns by dealer group using clustering
    /// </summary>
    public async Task<IEnumerable<DataPattern>> DetectDealerGroupPatterns(int groupId)
    {
        var patterns = new List<DataPattern>();

        // Get all dealers in this group
        var dealersInGroup = await _dbContext.Dealers
            .Where(d => d.GroupId == groupId)
            .ToListAsync();

        if (!dealersInGroup.Any())
            return patterns;

        string groupName = dealersInGroup.First().GroupName;

        // Get recent submissions for all dealers in this group
        var allSubmissions = await _dbContext.FinanceSubmissions
            .Where(fs => dealersInGroup.Select(d => d.Id).Contains(fs.DealerId))
            .Include(fs => fs.Cells)
            .Include(fs => fs.Dealer)
            .OrderByDescending(fs => fs.SubmissionDate)
            .Take(200) // Get a larger sample for group analysis
            .ToListAsync();

        if (allSubmissions.Count < 30) // Need sufficient data for group analysis
            return patterns;

        // Find common cell addresses across majority of submissions
        var commonAddresses = allSubmissions
            .SelectMany(s => s.Cells.Select(c => c.GlobalAddress))
            .GroupBy(a => a)
            .Where(g => g.Count() >= allSubmissions.Count / 3) // Address appears in at least 1/3 of submissions
            .Select(g => g.Key)
            .Take(50)
            .ToList();

        if (commonAddresses.Count < 2)
            return patterns;

        // Create feature vectors for each submission based on cell values
        var featureData = CreateFeatureData(allSubmissions, commonAddresses);

        // Run K-Means clustering
        var clusterResults = RunKMeansClustering(featureData, commonAddresses);

        // Analyze the clusters to identify group patterns
        var clusterGroups = clusterResults
            .GroupBy(cr => cr.ClusterId)
            .Where(g => g.Count() >= 3) // Only consider clusters with at least 3 submissions
            .ToList();

        foreach (var clusterGroup in clusterGroups)
        {
            var clusterSubmissions = clusterGroup.ToList();

            // Calculate the percentage of the group's submissions in this cluster
            int totalGroupSubmissions = allSubmissions.Count;
            decimal percentageInCluster = (decimal)clusterSubmissions.Count / totalGroupSubmissions * 100;

            // Get statistics about this cluster
            var submissionDealers = clusterSubmissions
                .Select(cs => allSubmissions.First(s => s.Id == cs.SubmissionId).Dealer)
                .Distinct()
                .ToList();

            var clusterYears = clusterSubmissions
                .Select(cs => allSubmissions.First(s => s.Id == cs.SubmissionId).Year)
                .Distinct()
                .OrderBy(y => y)
                .ToList();

            // Find common characteristics of this cluster
            var clusterFeatureData = featureData.Where(fd =>
                clusterSubmissions.Any(cs => cs.SubmissionId == fd.SubmissionId)).ToList();

            var clusterFeatureAverages = CalculateClusterFeatureAverages(
                clusterSubmissions, featureData, commonAddresses);

            patterns.Add(new DataPattern
            {
                PatternType = $"Group {groupId} Cluster Pattern",
                Description = $"Dealer group {groupName} shows a pattern where {percentageInCluster:F1}% of submissions ({clusterSubmissions.Count} out of {totalGroupSubmissions}) share characteristics: {GetClusterDescription(clusterFeatureAverages, commonAddresses)}",
                Significance = percentageInCluster > 70 ? "high" : percentageInCluster > 40 ? "medium" : "low",
                ConfidenceScore = 85,
                DetectedAt = DateTime.UtcNow,
                RelatedCellAddresses = commonAddresses.Take(5).ToList()
            });

            // If cluster spans multiple years, it's a stable pattern
            if (clusterYears.Count > 1)
            {
                patterns.Add(new DataPattern
                {
                    PatternType = $"Group {groupId} Stable Pattern",
                    Description = $"Dealer group {groupName} shows a stable pattern from {clusterYears.First()} to {clusterYears.Last()} with consistent financial characteristics",
                    Significance = "high",
                    ConfidenceScore = 90,
                    DetectedAt = DateTime.UtcNow,
                    RelatedCellAddresses = commonAddresses.Take(3).ToList()
                });
            }
        }

        return patterns;
    }

    private async Task<List<DataPattern>> DetectDealerYearlyPatterns(int dealerId, List<FinanceSubmission> submissions)
    {
        var patterns = new List<DataPattern>();

        // Group submissions by year
        var submissionsByYear = submissions
            .GroupBy(fs => fs.Year)
            .OrderBy(g => g.Key)
            .ToList();

        if (submissionsByYear.Count < 2) // Need at least 2 years of data
            return patterns;

        // Analyze year-over-year changes
        for (int i = 1; i < submissionsByYear.Count; i++)
        {
            var prevYear = submissionsByYear[i - 1];
            var currYear = submissionsByYear[i];

            // Find common cell addresses between years
            var prevYearAddresses = prevYear
                .SelectMany(fs => fs.Cells.Select(c => c.GlobalAddress))
                .Distinct()
                .ToList();

            var currYearAddresses = currYear
                .SelectMany(fs => fs.Cells.Select(c => c.GlobalAddress))
                .Distinct()
                .ToList();

            var commonAddresses = prevYearAddresses
                .Intersect(currYearAddresses)
                .ToList();

            if (!commonAddresses.Any())
                continue;

            // Calculate average values for each cell address in each year
            var prevYearAvgValues = new Dictionary<string, decimal>();
            var currYearAvgValues = new Dictionary<string, decimal>();

            foreach (var address in commonAddresses)
            {
                var prevYearCells = prevYear
                    .SelectMany(fs => fs.Cells)
                    .Where(c => c.GlobalAddress == address)
                    .ToList();

                var currYearCells = currYear
                    .SelectMany(fs => fs.Cells)
                    .Where(c => c.GlobalAddress == address)
                    .ToList();

                if (prevYearCells.Any() && currYearCells.Any())
                {
                    prevYearAvgValues[address] = prevYearCells.Average(c => c.Value);
                    currYearAvgValues[address] = currYearCells.Average(c => c.Value);
                }
            }

            // Find cells with significant changes
            var significantChanges = new List<(string Address, decimal PrevValue, decimal CurrValue, decimal PercentChange)>();

            foreach (var address in commonAddresses)
            {
                if (prevYearAvgValues.ContainsKey(address) && currYearAvgValues.ContainsKey(address))
                {
                    var prevValue = prevYearAvgValues[address];
                    var currValue = currYearAvgValues[address];

                    if (prevValue != 0)
                    {
                        var percentChange = (currValue - prevValue) / prevValue * 100;

                        if (Math.Abs(percentChange) >= 15) // 15% or more change is significant
                        {
                            significantChanges.Add((address, prevValue, currValue, percentChange));
                        }
                    }
                }
            }

            // Create patterns based on significant changes
            if (significantChanges.Any())
            {
                // Sort by absolute percent change (descending)
                significantChanges = significantChanges
                    .OrderByDescending(sc => Math.Abs(sc.PercentChange))
                    .ToList();

                // Take top 3 changes
                var topChanges = significantChanges.Take(3).ToList();

                var changeDescriptions = topChanges
                    .Select(c => $"{c.Address}: {c.PercentChange:F1}% change from {c.PrevValue:F0} to {c.CurrValue:F0}")
                    .ToList();

                var changeDescription = string.Join(", ", changeDescriptions);

                patterns.Add(new DataPattern
                {
                    PatternType = "Yearly Change Pattern",
                    Description = $"Dealer {dealerId} showed significant changes from {prevYear.Key} to {currYear.Key}: {changeDescription}",
                    Significance = topChanges.Any(c => Math.Abs(c.PercentChange) >= 30) ? "high" : "medium",
                    ConfidenceScore = 80,
                    DetectedAt = DateTime.UtcNow,
                    RelatedCellAddresses = topChanges.Select(c => c.Address).ToList()
                });
            }
        }

        return patterns;
    }

    private async Task<List<DataPattern>> DetectDealerMonthlyPatterns(int dealerId, List<FinanceSubmission> submissions)
    {
        var patterns = new List<DataPattern>();

        // Only analyze if we have at least a full year
        if (submissions.Count < 12)
            return patterns;

        // Find cell addresses that appear in most submissions
        var commonAddresses = submissions
            .SelectMany(fs => fs.Cells.Select(c => c.GlobalAddress))
            .GroupBy(a => a)
            .Where(g => g.Count() >= submissions.Count * 0.8) // Address appears in at least 80% of submissions
            .Select(g => g.Key)
            .ToList();

        if (commonAddresses.Count < 3)
            return patterns;

        // Analyze monthly patterns for each common address
        foreach (var address in commonAddresses.Take(10)) // Limit to top 10 addresses
        {
            // Create month-by-month values for this address
            var monthlyValues = new Dictionary<int, List<decimal>>();
            for (int month = 1; month <= 12; month++)
            {
                monthlyValues[month] = new List<decimal>();
            }

            // Collect values by month
            foreach (var submission in submissions)
            {
                var cell = submission.Cells.FirstOrDefault(c => c.GlobalAddress == address);
                if (cell != null)
                {
                    monthlyValues[submission.Month].Add(cell.Value);
                }
            }

            // Calculate average for each month
            var monthlyAverages = new Dictionary<int, decimal>();
            for (int month = 1; month <= 12; month++)
            {
                if (monthlyValues[month].Any())
                {
                    monthlyAverages[month] = monthlyValues[month].Average();
                }
            }

            // Skip if we don't have enough monthly data
            if (monthlyAverages.Count < 6)
                continue;

            // Find seasonal patterns
            var seasonalPatterns = DetectSeasonalPatterns(monthlyAverages);
            if (seasonalPatterns.Any())
            {
                var seasonDesc = string.Join(", ", seasonalPatterns
                    .Select(s => $"{GetMonthName(s.Month)}: {s.Pattern}"));

                patterns.Add(new DataPattern
                {
                    PatternType = "Seasonal Pattern",
                    Description = $"Dealer {dealerId} shows seasonal patterns for {address}: {seasonDesc}",
                    Significance = "medium",
                    ConfidenceScore = 75,
                    DetectedAt = DateTime.UtcNow,
                    RelatedCellAddresses = new List<string> { address }
                });
            }
        }

        return patterns;
    }

    private List<(int Month, string Pattern)> DetectSeasonalPatterns(Dictionary<int, decimal> monthlyAverages)
    {
        var patterns = new List<(int Month, string Pattern)>();

        if (monthlyAverages.Count < 6)
            return patterns;

        // Calculate overall average
        decimal overallAvg = monthlyAverages.Values.Average();

        // Identify months that deviate significantly from the average
        foreach (var kvp in monthlyAverages)
        {
            int month = kvp.Key;
            decimal value = kvp.Value;

            decimal deviation = (value - overallAvg) / overallAvg * 100;

            if (Math.Abs(deviation) >= 15) // 15% or more deviation is significant
            {
                string pattern = deviation > 0 ? "Peak" : "Trough";
                patterns.Add((month, pattern));
            }
        }

        return patterns;
    }

    private string GetMonthName(int month)
    {
        return month switch
        {
            1 => "January",
            2 => "February",
            3 => "March",
            4 => "April",
            5 => "May",
            6 => "June",
            7 => "July",
            8 => "August",
            9 => "September",
            10 => "October",
            11 => "November",
            12 => "December",
            _ => $"Month {month}"
        };
    }

    private async Task<DataPattern> DetectDealerGroupDeviation(Dealer dealer)
    {
        // Get all dealers in the same group
        var dealersInGroup = await _dbContext.Dealers
            .Where(d => d.GroupId == dealer.GroupId && d.Id != dealer.Id)
            .ToListAsync();

        if (!dealersInGroup.Any())
            return null;

        // Get recent submissions for this dealer and group
        var dealerSubmissions = await _dbContext.FinanceSubmissions
            .Where(fs => fs.DealerId == dealer.Id)
            .Include(fs => fs.Cells)
            .OrderByDescending(fs => fs.Year)
            .ThenByDescending(fs => fs.Month)
            .Take(12) // Last year
            .ToListAsync();

        var groupSubmissions = await _dbContext.FinanceSubmissions
            .Where(fs => dealersInGroup.Select(d => d.Id).Contains(fs.DealerId))
            .Include(fs => fs.Cells)
            .OrderByDescending(fs => fs.Year)
            .ThenByDescending(fs => fs.Month)
            .Take(60) // More data for better averages
            .ToListAsync();

        if (dealerSubmissions.Count < 3 || groupSubmissions.Count < 10)
            return null;

        // Find common cell addresses
        var dealerAddresses = dealerSubmissions
            .SelectMany(fs => fs.Cells.Select(c => c.GlobalAddress))
            .Distinct()
            .ToList();

        var groupAddresses = groupSubmissions
            .SelectMany(fs => fs.Cells.Select(c => c.GlobalAddress))
            .GroupBy(a => a)
            .Where(g => g.Count() >= 10) // Address appears in at least 10 submissions
            .Select(g => g.Key)
            .ToList();

        var commonAddresses = dealerAddresses
            .Intersect(groupAddresses)
            .ToList();

        if (commonAddresses.Count < 3)
            return null;

        // Calculate averages for dealer and group
        var dealerAvgValues = new Dictionary<string, decimal>();
        var groupAvgValues = new Dictionary<string, decimal>();

        foreach (var address in commonAddresses)
        {
            var dealerCells = dealerSubmissions
                .SelectMany(fs => fs.Cells)
                .Where(c => c.GlobalAddress == address)
                .ToList();

            var groupCells = groupSubmissions
                .SelectMany(fs => fs.Cells)
                .Where(c => c.GlobalAddress == address)
                .ToList();

            if (dealerCells.Any() && groupCells.Any())
            {
                dealerAvgValues[address] = dealerCells.Average(c => c.Value);
                groupAvgValues[address] = groupCells.Average(c => c.Value);
            }
        }

        // Detect deviations from group pattern
        var deviations = new List<(string Address, decimal DealerValue, decimal GroupValue, decimal PercentDiff)>();

        foreach (var address in commonAddresses)
        {
            if (dealerAvgValues.ContainsKey(address) && groupAvgValues.ContainsKey(address))
            {
                var dealerValue = dealerAvgValues[address];
                var groupValue = groupAvgValues[address];

                if (groupValue != 0)
                {
                    var percentDiff = (dealerValue - groupValue) / groupValue * 100;

                    if (Math.Abs(percentDiff) >= 20) // 20% or more difference is significant
                    {
                        deviations.Add((address, dealerValue, groupValue, percentDiff));
                    }
                }
            }
        }

        // Create pattern if significant deviations exist
        if (deviations.Any())
        {
            // Sort by absolute percent difference (descending)
            deviations = deviations
                .OrderByDescending(d => Math.Abs(d.PercentDiff))
                .ToList();

            // Take top 3 deviations
            var topDeviations = deviations.Take(3).ToList();

            var deviationDescriptions = topDeviations
                .Select(d => $"{d.Address}: {d.PercentDiff:F1}% different ({d.DealerValue:F0} vs. group avg {d.GroupValue:F0})")
                .ToList();

            var deviationDescription = string.Join(", ", deviationDescriptions);

            return new DataPattern
            {
                PatternType = "Group Deviation Pattern",
                Description = $"Dealer {dealer.Name} shows {(topDeviations.All(d => d.PercentDiff > 0) ? "higher" : topDeviations.All(d => d.PercentDiff < 0) ? "lower" : "different")} values than other {dealer.GroupName} dealers: {deviationDescription}",
                Significance = topDeviations.Any(d => Math.Abs(d.PercentDiff) >= 40) ? "high" : "medium",
                ConfidenceScore = 75,
                DetectedAt = DateTime.UtcNow,
                RelatedCellAddresses = topDeviations.Select(d => d.Address).ToList()
            };
        }

        return null;
    }


    private List<SpikeResult> DetectSpikes(List<CellTimeSeries> timeSeriesData)
    {
        // Create an IDataView from the time series data
        var dataView = _mlContext.Data.LoadFromEnumerable(timeSeriesData);

        // Calculate appropriate window sizes based on data length
        int trainSize = timeSeriesData.Count;

        // SrCNN has different constraints, but we'll be conservative
        int maxWindowSize = Math.Max(3, trainSize / 3);
        int windowSize = Math.Min(maxWindowSize, Math.Max(3, timeSeriesData.Count / 5));

        // Safety check - if we don't have enough data, return empty results
        if (trainSize < 8 || windowSize < 3)
        {
            return new List<SpikeResult>();
        }

        try
        {
            // Setup the spike detection pipeline
            // This uses the latest ML.NET SrCnn algorithm from Twitter
            var spikeDetector = _mlContext.Transforms.DetectAnomalyBySrCnn(
                outputColumnName: "Prediction",
                inputColumnName: nameof(CellTimeSeries.Value),
                windowSize: windowSize,
                backAddWindowSize: Math.Min(3, Math.Max(1, windowSize / 4)),
                lookaheadWindowSize: Math.Min(3, Math.Max(1, windowSize / 4)),
                averagingWindowSize: Math.Min(3, Math.Max(1, windowSize / 4)),
                judgementWindowSize: Math.Max(3, Math.Min(timeSeriesData.Count / 8, windowSize)),
                threshold: 0.3
            );

            // Train the model
            var transformedData = spikeDetector.Fit(dataView).Transform(dataView);

            // Extract the detection results
            var predictions = _mlContext.Data.CreateEnumerable<SpikeResultPrediction>(
                transformedData, reuseRowObject: false).ToList();

            // Convert predictions to more user-friendly results
            var results = new List<SpikeResult>();
            for (int i = 0; i < predictions.Count; i++)
            {
                var prediction = predictions[i];
                if (prediction.Prediction[0] == 1) // 1 indicates a detected spike
                {
                    results.Add(new SpikeResult
                    {
                        Position = i,
                        Score = prediction.Prediction[1], // Confidence score
                        Value = timeSeriesData[i].Value
                    });
                }
            }

            return results;
        }
        catch
        {
            // If ML.NET still fails despite our precautions, return empty results
            return new List<SpikeResult>();
        }
    }

    private List<ChangePointResult> DetectChangePoints(List<CellTimeSeries> timeSeriesData)
    {
        // Create an IDataView from the time series data
        var dataView = _mlContext.Data.LoadFromEnumerable(timeSeriesData);

        // Strict enforcement of ML.NET requirements:
        // - trainSize must be > 2 * windowSize
        int trainSize = timeSeriesData.Count;
        // Calculate maximum allowable window size (strictly less than half of train size)
        int maxWindowSize = (trainSize / 2) - 1;

        // Ensure window size is valid (at least 3, but meeting the constraint)
        int windowSize = Math.Max(3, Math.Min(maxWindowSize, timeSeriesData.Count / 5));

        // Safety check - if we can't satisfy requirements, return empty results
        if (trainSize <= 2 * windowSize || windowSize < 3 || trainSize < 8)
        {
            return new List<ChangePointResult>();
        }

        // Setup the change point detection pipeline with conservative parameters
        var changePointDetector = _mlContext.Transforms.DetectChangePointBySsa(
            outputColumnName: "Prediction",
            inputColumnName: nameof(CellTimeSeries.Value),
            confidence: 95,
            changeHistoryLength: Math.Max(3, timeSeriesData.Count / 10),
            trainingWindowSize: windowSize,
            seasonalityWindowSize: Math.Max(2, Math.Min(timeSeriesData.Count / 12, 3))
        );

        try
        {
            // Train the model
            var transformedData = changePointDetector.Fit(dataView).Transform(dataView);

            // Extract the detection results
            var predictions = _mlContext.Data.CreateEnumerable<ChangePointResultPrediction>(
                transformedData, reuseRowObject: false).ToList();

            // Convert predictions to more user-friendly results
            var results = new List<ChangePointResult>();
            for (int i = 0; i < predictions.Count; i++)
            {
                var prediction = predictions[i];
                if (prediction.Prediction[0] == 1) // 1 indicates a detected change point
                {
                    results.Add(new ChangePointResult
                    {
                        Position = i,
                        Score = prediction.Prediction[1], // Confidence score
                        Value = timeSeriesData[i].Value
                    });
                }
            }

            return results;
        }
        catch
        {
            // If ML.NET still fails despite our precautions, return empty results
            return new List<ChangePointResult>();
        }
    }

    private List<SubmissionFeatures> CreateFeatureData(List<FinanceSubmission> submissions, List<string> commonAddresses)
    {
        var featureData = new List<SubmissionFeatures>();

        // Ensure we have a valid feature set
        if (submissions.Count == 0 || commonAddresses.Count == 0)
        {
            return featureData;
        }

        // Fixed feature vector size based on the number of common addresses
        int featureVectorSize = commonAddresses.Count;

        foreach (var submission in submissions)
        {
            var features = new SubmissionFeatures
            {
                SubmissionId = submission.Id,
                DealerId = submission.DealerId
            };

            // Extract values for common cell addresses
            var featureVector = new float[featureVectorSize];

            for (int i = 0; i < commonAddresses.Count; i++)
            {
                var address = commonAddresses[i];
                var cell = submission.Cells.FirstOrDefault(c => c.GlobalAddress == address);
                featureVector[i] = cell != null ? (float)cell.Value : 0f;
            }

            features.Features = featureVector;
            featureData.Add(features);
        }

        return featureData;
    }

    private List<ClusteringResult> RunKMeansClustering(List<SubmissionFeatures> featureData, List<string> commonAddresses)
    {
        try
        {
            // Determine number of clusters (use heuristic: k = sqrt(n/2))
            int k = (int)Math.Sqrt(featureData.Count / 2);
            k = Math.Max(2, Math.Min(k, 10)); // Between 2 and 10 clusters

            // Safety check for small datasets
            if (featureData.Count < 4)
            {
                return new List<ClusteringResult>();
            }

            // Create and configure MLContext
            var mlContext = new MLContext(seed: 42);

            // Prepare the data in a custom class with individually defined features
            var flatFeatures = new List<FlatFeatureVector>();

            foreach (var item in featureData)
            {
                var flat = new FlatFeatureVector
                {
                    SubmissionId = item.SubmissionId
                };

                // Create individual feature columns from the array
                for (int i = 0; i < item.Features.Length && i < 50; i++)
                {
                    // Use reflection to set the property dynamically
                    typeof(FlatFeatureVector).GetProperty($"Feature{i}")?.SetValue(flat, item.Features[i]);
                }

                flatFeatures.Add(flat);
            }

            // Create data view from the flat features
            var dataView = mlContext.Data.LoadFromEnumerable(flatFeatures);

            // Build a list of feature column names to use
            var featureColumns = new List<string>();
            for (int i = 0; i < Math.Min(commonAddresses.Count, 50); i++)
            {
                featureColumns.Add($"Feature{i}");
            }

            // Define the feature column to use for clustering
            var featureColumnName = "FeaturesVector";

            // Setup the clustering pipeline with explicit feature columns
            var pipeline = mlContext.Transforms.Concatenate(
                    featureColumnName,
                    featureColumns.ToArray()
                )
                .Append(mlContext.Transforms.NormalizeMinMax(featureColumnName))
                .Append(mlContext.Clustering.Trainers.KMeans(
                    featureColumnName: featureColumnName,
                    numberOfClusters: k));

            // Train the model
            var model = pipeline.Fit(dataView);

            // Get cluster assignments for each submission
            var transformedData = model.Transform(dataView);
            var clusterPredictions = mlContext.Data
                .CreateEnumerable<ClusteringPrediction>(
                    transformedData,
                    reuseRowObject: false)
                .ToList();

            // Map results back to original submissions
            var results = new List<ClusteringResult>();
            for (int i = 0; i < flatFeatures.Count; i++)
            {
                results.Add(new ClusteringResult
                {
                    SubmissionId = flatFeatures[i].SubmissionId,
                    ClusterId = clusterPredictions[i].PredictedClusterId,
                    Distance = clusterPredictions[i].DistanceFromCentroid
                });
            }

            // Update feature data with cluster IDs
            foreach (var result in results)
            {
                var feature = featureData.FirstOrDefault(fd => fd.SubmissionId == result.SubmissionId);
                if (feature != null)
                {
                    feature.PredictedClusterId = result.ClusterId;
                }
            }

            return results;
        }
        catch (Exception ex)
        {
            // If clustering fails, return empty results
            Console.WriteLine($"Clustering error: {ex.Message}");
            return new List<ClusteringResult>();
        }
    }

    private Dictionary<string, double> CalculateClusterFeatureAverages(
        List<ClusteringResult> clusterMembers,
        List<SubmissionFeatures> featureData,
        List<string> featureNames)
    {
        var memberIds = clusterMembers.Select(cm => cm.SubmissionId).ToHashSet();
        var memberFeatures = featureData.Where(fd => memberIds.Contains(fd.SubmissionId)).ToList();

        var averages = new Dictionary<string, double>();

        // Calculate average for each feature
        for (int i = 0; i < featureNames.Count; i++)
        {
            var featureName = featureNames[i];
            var values = memberFeatures.Select(mf => mf.Features[i]).ToList();
            averages[featureName] = values.Average();
        }

        return averages;
    }

    private string GetClusterDescription(Dictionary<string, double> featureAverages, List<string> featureNames)
    {
        var description = new List<string>();

        // Find the top 3 most significant features (highest values)
        var topFeatures = featureAverages
            .OrderByDescending(kv => kv.Value)
            .Take(3)
            .ToList();

        foreach (var feature in topFeatures)
        {
            description.Add($"{feature.Key}: {feature.Value:F0}");
        }

        return string.Join(", ", description);
    }



    // Class for time series data
    private class CellTimeSeries
    {
        public int SequenceNumber { get; set; }
        public string Address { get; set; } = string.Empty;
        public float Value { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
    }

    // ML.NET prediction types
    private class SpikeResultPrediction
    {
        [VectorType(3)]
        public double[] Prediction { get; set; } = Array.Empty<double>();
    }

    private class ChangePointResultPrediction
    {
        [VectorType(3)]
        public double[] Prediction { get; set; } = Array.Empty<double>();
    }

    private class ClusteringPrediction
    {
        [ColumnName("PredictedLabel")]
        public uint PredictedClusterId { get; set; }

        [ColumnName("Score")]
        public float[] DistanceFromCentroid { get; set; } = Array.Empty<float>();
    }

    private class TimeSeriesForecast
    {
        [VectorType(0)]
        public float[] ForecastedValues { get; set; } = Array.Empty<float>();
    }

    // Result classes
    private class SpikeResult
    {
        public int Position { get; set; }
        public double Score { get; set; }
        public float Value { get; set; }
    }

    private class ChangePointResult
    {
        public int Position { get; set; }
        public double Score { get; set; }
        public float Value { get; set; }
    }

    private class ClusteringResult
    {
        public int SubmissionId { get; set; }
        public uint ClusterId { get; set; }
        public float[] Distance { get; set; } = Array.Empty<float>();
    }

    private class ForecastResult
    {
        public int Position { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public float ForecastValue { get; set; }
    }

    private class SubmissionFeatures
    {
        public int SubmissionId { get; set; }
        public int DealerId { get; set; }
        public float[] Features { get; set; } = Array.Empty<float>();
        public uint PredictedClusterId { get; set; }
    }

    // Define a class with individual feature properties (up to 50) for ML.NET
    private class FlatFeatureVector
    {
        public int SubmissionId { get; set; }
        public float Feature0 { get; set; }
        public float Feature1 { get; set; }
        public float Feature2 { get; set; }
        public float Feature3 { get; set; }
        public float Feature4 { get; set; }
        public float Feature5 { get; set; }
        public float Feature6 { get; set; }
        public float Feature7 { get; set; }
        public float Feature8 { get; set; }
        public float Feature9 { get; set; }
        public float Feature10 { get; set; }
        public float Feature11 { get; set; }
        public float Feature12 { get; set; }
        public float Feature13 { get; set; }
        public float Feature14 { get; set; }
        public float Feature15 { get; set; }
        public float Feature16 { get; set; }
        public float Feature17 { get; set; }
        public float Feature18 { get; set; }
        public float Feature19 { get; set; }
        public float Feature20 { get; set; }
        public float Feature21 { get; set; }
        public float Feature22 { get; set; }
        public float Feature23 { get; set; }
        public float Feature24 { get; set; }
        public float Feature25 { get; set; }
        public float Feature26 { get; set; }
        public float Feature27 { get; set; }
        public float Feature28 { get; set; }
        public float Feature29 { get; set; }
        public float Feature30 { get; set; }
        public float Feature31 { get; set; }
        public float Feature32 { get; set; }
        public float Feature33 { get; set; }
        public float Feature34 { get; set; }
        public float Feature35 { get; set; }
        public float Feature36 { get; set; }
        public float Feature37 { get; set; }
        public float Feature38 { get; set; }
        public float Feature39 { get; set; }
        public float Feature40 { get; set; }
        public float Feature41 { get; set; }
        public float Feature42 { get; set; }
        public float Feature43 { get; set; }
        public float Feature44 { get; set; }
        public float Feature45 { get; set; }
        public float Feature46 { get; set; }
        public float Feature47 { get; set; }
        public float Feature48 { get; set; }
        public float Feature49 { get; set; }
    }

}