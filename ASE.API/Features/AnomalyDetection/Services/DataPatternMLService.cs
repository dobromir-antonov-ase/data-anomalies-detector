using ASE.API.Common.Data;
using ASE.API.Features.AnomalyDetection.Models;
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
    /// Detect anomalies in time series data using Spike and Change Point Detection
    /// </summary>
    public async Task<IEnumerable<DataAnomaly>> DetectTimeSeriesAnomalies(int submissionId)
    {
        var anomalies = new List<DataAnomaly>();
        
        // Get the target submission
        var submission = await _dbContext.FinanceSubmissions
            .FindAsync(submissionId);
            
        if (submission == null)
            return anomalies;
            
        // Get historical submissions for this dealer, ordered by date
        var historicalData = await _dbContext.FinanceSubmissions
            .Where(fs => fs.DealerId == submission.DealerId)
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
                .Select((s, index) => {
                    var cell = s.Cells.FirstOrDefault(c => c.GlobalAddress == address);
                    return new CellTimeSeries
                    {
                        SequenceNumber = index + 1,
                        Address = address,
                        Value = cell?.Value ?? 0,
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
                    AnomalyType = "ML-Detected Spike",
                    Description = $"Spike detected in {address} for {dataPoint.Month}/{dataPoint.Year} with confidence score {spike.Score:F2}",
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
                    AnomalyType = "ML-Detected Change Point",
                    Description = $"Change point detected in {address} starting at {dataPoint.Month}/{dataPoint.Year} with confidence score {changePoint.Score:F2}",
                    Severity = changePoint.Score > 0.7 ? "high" : "medium",
                    DetectedAt = DateTime.UtcNow
                });
            }
        }
        
        return anomalies;
    }
    
    /// <summary>
    /// Detect clusters in finance submission data to identify patterns
    /// </summary>
    public async Task<IEnumerable<DataPattern>> DetectClustersInData(int submissionId)
    {
        var patterns = new List<DataPattern>();
        
        // Get current and historical submissions
        var submission = await _dbContext.FinanceSubmissions
            .FindAsync(submissionId);
            
        if (submission == null)
            return patterns;
            
        // Get at least 30 submissions across all dealers for clustering
        var allSubmissions = await _dbContext.FinanceSubmissions
            .Include(fs => fs.Cells)
            .Include(fs => fs.Dealer)
            .OrderByDescending(fs => fs.SubmissionDate)
            .Take(50)
            .ToListAsync();
            
        if (allSubmissions.Count < 30)
            return patterns;
            
        // Find common cell addresses across majority of submissions
        var commonAddresses = allSubmissions
            .SelectMany(s => s.Cells.Select(c => c.GlobalAddress))
            .GroupBy(a => a)
            .Where(g => g.Count() >= allSubmissions.Count / 2)
            .Select(g => g.Key)
            .Take(5) // Limit to top 5 common addresses for performance
            .ToList();
            
        if (commonAddresses.Count < 2)
            return patterns;
            
        // Create feature vectors for each submission based on cell values
        var featureData = CreateFeatureData(allSubmissions, commonAddresses);
        
        // Run K-Means clustering
        var clusterResults = RunKMeansClustering(featureData, commonAddresses);
        
        // Find which cluster the current submission belongs to
        var currentSubmissionFeatures = featureData.FirstOrDefault(fd => fd.SubmissionId == submissionId);
        if (currentSubmissionFeatures == null)
            return patterns;
            
        // Get all submissions in the same cluster
        var submissionsInSameCluster = clusterResults
            .Where(cr => cr.ClusterId == currentSubmissionFeatures.PredictedClusterId)
            .ToList();
            
        if (submissionsInSameCluster.Count >= 3)
        {
            // Find the dealers in this cluster
            var dealersInCluster = submissionsInSameCluster
                .Select(s => allSubmissions.First(sub => sub.Id == s.SubmissionId).Dealer.Name)
                .Distinct()
                .ToList();
                
            patterns.Add(new DataPattern
            {
                PatternType = "ML-Detected Cluster",
                Description = $"This submission belongs to a cluster with {submissionsInSameCluster.Count} other submissions from {dealersInCluster.Count} dealers",
                Significance = submissionsInSameCluster.Count > 10 ? "high" : "medium",
                ConfidenceScore = 85,
                DetectedAt = DateTime.UtcNow,
                RelatedCellAddresses = commonAddresses
            });
            
            // Find common characteristics of this cluster
            var clusterFeatureAverages = CalculateClusterFeatureAverages(submissionsInSameCluster, featureData, commonAddresses);
            
            patterns.Add(new DataPattern
            {
                PatternType = "ML-Detected Cluster Characteristics",
                Description = $"Submissions in this cluster typically have {GetClusterDescription(clusterFeatureAverages, commonAddresses)}",
                Significance = "medium",
                ConfidenceScore = 80,
                DetectedAt = DateTime.UtcNow,
                RelatedCellAddresses = commonAddresses
            });
        }
        
        return patterns;
    }
    
    /// <summary>
    /// Use ML.NET to predict future values based on historical data
    /// </summary>
    public async Task<IEnumerable<DataPattern>> PredictFutureValues(int submissionId)
    {
        var patterns = new List<DataPattern>();
        
        // Get the target submission
        var submission = await _dbContext.FinanceSubmissions
            .FindAsync(submissionId);
            
        if (submission == null)
            return patterns;
            
        // Get historical time series data
        var historicalData = await _dbContext.FinanceSubmissions
            .Where(fs => fs.DealerId == submission.DealerId)
            .OrderBy(fs => fs.Year)
            .ThenBy(fs => fs.Month)
            .Include(fs => fs.Cells)
            .ToListAsync();
            
        if (historicalData.Count < 12) // Need at least a year of data
            return patterns;
            
        // Get important cell addresses (highest values)
        var importantAddresses = submission.Cells
            .OrderByDescending(c => c.Value)
            .Take(5)
            .Select(c => c.GlobalAddress)
            .ToList();
            
        foreach (var address in importantAddresses)
        {
            // Create time series data
            var timeSeriesData = historicalData
                .Select((s, index) => {
                    var cell = s.Cells.FirstOrDefault(c => c.GlobalAddress == address);
                    return new CellTimeSeries
                    {
                        SequenceNumber = index + 1,
                        Address = address,
                        Value = cell?.Value ?? 0,
                        Month = s.Month,
                        Year = s.Year
                    };
                })
                .Where(ts => ts.Value != 0)
                .OrderBy(ts => ts.SequenceNumber)
                .ToList();
                
            if (timeSeriesData.Count < 12)
                continue;
                
            // Run forecast
            var forecastResults = ForecastTimeSeries(timeSeriesData, 3); // Forecast next 3 periods
            
            if (forecastResults.Any())
            {
                // Calculate current trend
                var lastActualValue = timeSeriesData.Last().Value;
                var nextForecast = forecastResults.First().ForecastValue;
                var percentChange = ((decimal)nextForecast - lastActualValue) / lastActualValue * 100;
                var trendDirection = percentChange > 0 ? "increasing" : "decreasing";
                
                patterns.Add(new DataPattern
                {
                    PatternType = "ML-Based Forecast",
                    Description = $"Based on historical trends, {address} is expected to be {trendDirection} by {Math.Abs(percentChange):F1}% in the next period",
                    Significance = Math.Abs(percentChange) > 20 ? "high" : "medium",
                    ConfidenceScore = 70,
                    DetectedAt = DateTime.UtcNow,
                    RelatedCellAddresses = new List<string> { address }
                });
            }
        }
        
        return patterns;
    }
    
    #region ML Methods
    
    private List<SpikeResult> DetectSpikes(List<CellTimeSeries> timeSeriesData)
    {
        // Create an IDataView from the time series data
        var dataView = _mlContext.Data.LoadFromEnumerable(timeSeriesData);
        
        // Setup the spike detection pipeline
        // This uses the latest ML.NET SrCnn algorithm from Twitter
        var spikeDetector = _mlContext.Transforms.DetectAnomalyBySrCnn(
            outputColumnName: "Prediction",
            inputColumnName: nameof(CellTimeSeries.Value),
            windowSize: timeSeriesData.Count / 2,
            backAddWindowSize: 3,
            lookaheadWindowSize: 3,
            averagingWindowSize: 3,
            judgementWindowSize: timeSeriesData.Count / 4,
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
    
    private List<ChangePointResult> DetectChangePoints(List<CellTimeSeries> timeSeriesData)
    {
        // Create an IDataView from the time series data
        var dataView = _mlContext.Data.LoadFromEnumerable(timeSeriesData);
        
        // Setup the change point detection pipeline
        var changePointDetector = _mlContext.Transforms.DetectChangePointBySsa(
            outputColumnName: "Prediction",
            inputColumnName: nameof(CellTimeSeries.Value),
            confidence: 95,
            changeHistoryLength: timeSeriesData.Count / 4,
            trainingWindowSize: timeSeriesData.Count / 2,
            seasonalityWindowSize: 3
        );
        
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
    
    private List<SubmissionFeatures> CreateFeatureData(List<FinanceSubmission> submissions, List<string> commonAddresses)
    {
        var featureData = new List<SubmissionFeatures>();
        
        foreach (var submission in submissions)
        {
            var features = new SubmissionFeatures
            {
                SubmissionId = submission.Id,
                DealerId = submission.DealerId
            };
            
            // Extract values for common cell addresses
            var featureVector = new List<float>();
            foreach (var address in commonAddresses)
            {
                var cell = submission.Cells.FirstOrDefault(c => c.GlobalAddress == address);
                featureVector.Add(cell != null ? (float)cell.Value : 0f);
            }
            
            features.Features = featureVector.ToArray();
            featureData.Add(features);
        }
        
        return featureData;
    }
    
    private List<ClusteringResult> RunKMeansClustering(List<SubmissionFeatures> featureData, List<string> commonAddresses)
    {
        // Determine number of clusters (use heuristic: k = sqrt(n/2))
        int k = (int)Math.Sqrt(featureData.Count / 2);
        k = Math.Max(2, Math.Min(k, 10)); // Between 2 and 10 clusters
        
        // Create and configure MLContext
        var dataView = _mlContext.Data.LoadFromEnumerable(featureData);
        
        // Define the feature column to use for clustering
        var featureColumnName = "FeaturesVector";
        
        // Use a feature processing pipeline to concatenate all feature columns
        var pipeline = _mlContext.Transforms.Concatenate(
                featureColumnName,
                "Features"
            )
            .Append(_mlContext.Clustering.Trainers.KMeans(
                featureColumnName: featureColumnName,
                numberOfClusters: k));
        
        // Train the model
        var model = pipeline.Fit(dataView);
        
        // Get cluster assignments for each submission
        var transformedData = model.Transform(dataView);
        var clusterPredictions = _mlContext.Data
            .CreateEnumerable<ClusteringPrediction>(
                transformedData, 
                reuseRowObject: false)
            .ToList();
        
        // Map results back to original submissions
        var results = new List<ClusteringResult>();
        for (int i = 0; i < featureData.Count; i++)
        {
            results.Add(new ClusteringResult
            {
                SubmissionId = featureData[i].SubmissionId,
                ClusterId = clusterPredictions[i].PredictedClusterId,
                Distance = clusterPredictions[i].DistanceFromCentroid
            });
        }
        
        // Update feature data with cluster IDs
        for (int i = 0; i < featureData.Count; i++)
        {
            featureData[i].PredictedClusterId = clusterPredictions[i].PredictedClusterId;
        }
        
        return results;
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
    
    private List<ForecastResult> ForecastTimeSeries(List<CellTimeSeries> timeSeriesData, int horizon)
    {
        // Prepare data for forecasting
        var data = _mlContext.Data.LoadFromEnumerable(timeSeriesData);
        
        // Train forecasting model using Single Spectrum Analysis
        var forecastingPipeline = _mlContext.Forecasting.ForecastBySsa(
            outputColumnName: "ForecastedValues",
            inputColumnName: nameof(CellTimeSeries.Value),
            windowSize: 7, // For monthly data
            seriesLength: timeSeriesData.Count,
            trainSize: timeSeriesData.Count,
            horizon: horizon
        );
        
        var model = forecastingPipeline.Fit(data);
        
        // Forecast for the given horizon period
        var forecastResult = model.Transform(data);
        var forecast = _mlContext.Data
            .CreateEnumerable<TimeSeriesForecast>(forecastResult, reuseRowObject: false)
            .First();
            
        // Create results for each forecasted period
        var results = new List<ForecastResult>();
        
        for (int i = 0; i < horizon; i++)
        {
            // Calculate the next period date
            var lastPoint = timeSeriesData.Last();
            int newMonth = lastPoint.Month + 1 + i;
            int newYear = lastPoint.Year;
            
            while (newMonth > 12)
            {
                newMonth -= 12;
                newYear++;
            }
            
            results.Add(new ForecastResult
            {
                Position = i,
                Month = newMonth,
                Year = newYear,
                ForecastValue = forecast.ForecastedValues[i]
            });
        }
        
        return results;
    }
    
    #endregion
    
    #region ML Data Classes
    
    // Class for time series data
    private class CellTimeSeries
    {
        public int SequenceNumber { get; set; }
        public string Address { get; set; } = string.Empty;
        public decimal Value { get; set; }
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
        public decimal Value { get; set; }
    }
    
    private class ChangePointResult
    {
        public int Position { get; set; }
        public double Score { get; set; }
        public decimal Value { get; set; }
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
    
    #endregion
} 