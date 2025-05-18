import { Component, Input, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NgxEchartsDirective } from 'ngx-echarts';
import { DataAnomaly } from '../../core/services/anomaly-detection.service';

@Component({
  selector: 'app-anomaly-visualization',
  standalone: true,
  imports: [
    CommonModule,
    NgxEchartsDirective
  ],
  template: `
    <div class="anomaly-visualization-container">
      <div echarts [options]="chartOption" class="anomaly-chart"></div>
    </div>
  `,
  styles: [`
    .anomaly-visualization-container {
      width: 100%;
      height: 100%;
    }
    
    .anomaly-chart {
      height: 400px;
      width: 100%;
    }
  `]
})
export class AnomalyVisualizationComponent implements OnChanges {
  @Input() anomalies: DataAnomaly[] = [];
  @Input() chartType: 'pie' | 'timeline' | 'scatter' | 'radar' | 'heatmap' | 'bubble' = 'pie';
  
  chartOption: any = {};
  
  ngOnChanges(changes: SimpleChanges): void {
    if (changes['anomalies'] || changes['chartType']) {
      this.prepareChart();
    }
  }
  
  private prepareChart(): void {
    if (!this.anomalies.length) {
      return;
    }
    
    switch (this.chartType) {
      case 'pie':
        this.preparePieChart();
        break;
      case 'timeline':
        this.prepareTimelineChart();
        break;
      case 'scatter':
        this.prepareScatterChart();
        break;
      case 'radar':
        this.prepareRadarChart();
        break;
      case 'heatmap':
        this.prepareHeatmapChart();
        break;
      case 'bubble':
        this.prepareBubbleChart();
        break;
      default:
        this.preparePieChart();
    }
  }
  
  private preparePieChart(): void {
    // Group anomalies by severity and count them
    const severityCounts: Record<string, number> = {
      'high': 0,
      'medium': 0,
      'low': 0
    };
    
    this.anomalies.forEach(anomaly => {
      const severity = anomaly.severity.toLowerCase();
      severityCounts[severity] = (severityCounts[severity] || 0) + 1;
    });
    
    // Create data for pie chart
    const data = Object.entries(severityCounts)
      .filter(([_, count]) => count > 0)
      .map(([severity, count]) => {
        return {
          name: severity.charAt(0).toUpperCase() + severity.slice(1),
          value: count
        };
      });
    
    // Set up the chart options
    this.chartOption = {
      title: {
        text: 'Anomaly Severity Distribution',
        left: 'center'
      },
      tooltip: {
        trigger: 'item',
        formatter: '{b}: {c} ({d}%)'
      },
      legend: {
        orient: 'vertical',
        left: 'left',
        data: data.map(item => item.name)
      },
      series: [
        {
          type: 'pie',
          radius: '65%',
          center: ['50%', '50%'],
          data: data,
          emphasis: {
            itemStyle: {
              shadowBlur: 10,
              shadowOffsetX: 0,
              shadowColor: 'rgba(0, 0, 0, 0.5)'
            }
          },
          itemStyle: {
            color: (params: any) => {
              const severity = params.name.toLowerCase();
              return severity === 'high' ? '#f44336' : 
                     severity === 'medium' ? '#ff9800' : '#4caf50';
            }
          },
          label: {
            formatter: '{b}: {c} ({d}%)'
          }
        }
      ]
    };
  }
  
  private prepareTimelineChart(): void {
    // Sort anomalies by detected date
    const sortedAnomalies = [...this.anomalies].sort((a, b) => {
      return new Date(a.detectedAt).getTime() - new Date(b.detectedAt).getTime();
    });
    
    // Format dates for x-axis
    const dates = sortedAnomalies.map(anomaly => {
      const date = new Date(anomaly.detectedAt);
      return date.toLocaleDateString();
    });
    
    // Group anomalies by type for stacked bars
    const anomalyTypes = [...new Set(sortedAnomalies.map(a => a.anomalyType))];
    
    // Create series for each anomaly type
    const series = anomalyTypes.map(type => {
      // Create data array with counts for each date
      const data = dates.map((date, index) => {
        const isType = sortedAnomalies[index].anomalyType === type;
        const severity = sortedAnomalies[index].severity.toLowerCase();
        // Use severity value for visualization (high=3, medium=2, low=1)
        const value = isType ? (severity === 'high' ? 3 : severity === 'medium' ? 2 : 1) : 0;
        return value;
      });
      
      return {
        name: type,
        type: 'bar',
        stack: 'anomaly',
        data: data
      };
    });
    
    this.chartOption = {
      title: {
        text: 'Anomalies Timeline',
        subtext: 'Bar height represents severity'
      },
      tooltip: {
        trigger: 'axis',
        axisPointer: {
          type: 'shadow'
        },
        formatter: (params: any) => {
          const date = dates[params[0].dataIndex];
          const anomaly = sortedAnomalies[params[0].dataIndex];
          
          let tooltipText = `<strong>${date}</strong><br>` +
                 `Type: ${anomaly.anomalyType}<br>` +
                 `Severity: ${anomaly.severity}<br>` +
                 `${anomaly.description}`;
          
          // Add enhanced information if available
          if (anomaly.affectedEntity) {
            tooltipText += `<br>Entity: ${anomaly.affectedEntity}`;
          }
          if (anomaly.affectedMetric) {
            tooltipText += `<br>Metric: ${anomaly.affectedMetric}`;
          }
          if (anomaly.actualValue != null && anomaly.expectedValue != null) {
            tooltipText += `<br>Actual: ${anomaly.actualValue}`;
            tooltipText += `<br>Expected: ${anomaly.expectedValue}`;
          }
          
          return tooltipText;
        }
      },
      legend: {
        data: anomalyTypes
      },
      xAxis: {
        type: 'category',
        data: dates,
        axisLabel: {
          rotate: 45
        }
      },
      yAxis: {
        type: 'value',
        name: 'Severity',
        max: 3
      },
      series: series
    };
  }
  
  private prepareScatterChart(): void {
    // Group anomalies by type and count
    const typeCounts: Record<string, { count: number, high: number, medium: number, low: number }> = {};
    
    this.anomalies.forEach(anomaly => {
      const type = anomaly.anomalyType;
      const severity = anomaly.severity.toLowerCase();
      
      if (!typeCounts[type]) {
        typeCounts[type] = { count: 0, high: 0, medium: 0, low: 0 };
      }
      
      typeCounts[type].count++;
      typeCounts[type][severity as 'high' | 'medium' | 'low']++;
    });
    
    // Convert to array for chart
    const data = Object.entries(typeCounts).map(([type, counts]) => {
      return {
        value: [counts.count, type],
        itemStyle: {
          // Color based on highest severity count
          color: counts.high > 0 ? '#f44336' : counts.medium > 0 ? '#ff9800' : '#4caf50'
        }
      };
    });
    
    // Sort by count
    data.sort((a, b) => (b.value[0] as number) - (a.value[0] as number));
    
    this.chartOption = {
      title: {
        text: 'Anomaly Type Distribution',
        left: 'center'
      },
      tooltip: {
        formatter: (params: any) => {
          const type = params.value[1];
          const counts = typeCounts[type];
          
          return `<strong>${type}</strong><br>` +
                 `Total: ${counts.count}<br>` +
                 `High: ${counts.high}<br>` +
                 `Medium: ${counts.medium}<br>` +
                 `Low: ${counts.low}`;
        }
      },
      xAxis: {
        type: 'value',
        name: 'Count'
      },
      yAxis: {
        type: 'category',
        data: data.map(item => item.value[1]),
        axisLabel: {
          rotate: 30
        }
      },
      series: [
        {
          type: 'bar',
          data: data,
          label: {
            show: true,
            position: 'right',
            formatter: '{c}'
          }
        }
      ]
    };
  }
  
  private prepareRadarChart(): void {
    // This chart type requires anomalyScore to be present
    const hasAnomalyScores = this.anomalies.some(a => a.anomalyScore != null);
    
    // Group metrics by affected metric type
    const metricGroups = hasAnomalyScores
      ? this.groupByMetric()
      : this.createMockMetricGroups();
    
    // Create radar indicators (axes) for each metric
    const indicators = Object.keys(metricGroups).map(metric => {
      return { name: metric, max: 100 };
    });
    
    // Count high-severity anomalies by entity
    const entitiesWithHighSeverity = this.anomalies
      .filter(a => a.severity.toLowerCase() === 'high')
      .map(a => a.affectedEntity || 'Unknown');
      
    // Get unique entities
    const entities = [...new Set(entitiesWithHighSeverity)];
    
    // Create series data for the radar chart
    const seriesData = entities.map(entity => {
      const values = Object.keys(metricGroups).map(metric => {
        const entityMetricAnomalies = this.anomalies.filter(a => 
          (a.affectedEntity === entity || !a.affectedEntity) && 
          (a.affectedMetric === metric || !a.affectedMetric));
          
        // Calculate score based on severity and anomaly score if available
        return entityMetricAnomalies.reduce((sum, a) => {
          const severityScore = a.severity.toLowerCase() === 'high' ? 80 : 
                               a.severity.toLowerCase() === 'medium' ? 50 : 30;
          return sum + (a.anomalyScore || severityScore);
        }, 0) / Math.max(1, entityMetricAnomalies.length);
      });
      
      return {
        name: entity,
        value: values
      };
    });
    
    this.chartOption = {
      title: {
        text: 'Anomaly Metric Comparison',
        left: 'center'
      },
      tooltip: {},
      legend: {
        data: entities,
        bottom: 10
      },
      radar: {
        indicator: indicators
      },
      series: [
        {
          type: 'radar',
          data: seriesData
        }
      ]
    };
  }
  
  private prepareHeatmapChart(): void {
    // Group anomalies by entity and metric
    const entities = [...new Set(this.anomalies
      .map(a => a.affectedEntity || 'Unknown')
      .filter(e => e !== 'Unknown'))];
      
    const metrics = [...new Set(this.anomalies
      .map(a => a.affectedMetric || 'Unknown')
      .filter(m => m !== 'Unknown'))];
    
    // Create data for the heatmap
    const data: [number, number, number][] = [];
    entities.forEach((entity, entityIndex) => {
      metrics.forEach((metric, metricIndex) => {
        const matchingAnomalies = this.anomalies.filter(a => 
          a.affectedEntity === entity && a.affectedMetric === metric);
          
        if (matchingAnomalies.length > 0) {
          // Calculate max severity (high = 3, medium = 2, low = 1)
          const maxSeverity = matchingAnomalies.reduce((max, a) => {
            const severityValue = a.severity.toLowerCase() === 'high' ? 3 : 
                                a.severity.toLowerCase() === 'medium' ? 2 : 1;
            return Math.max(max, severityValue);
          }, 0);
          
          data.push([metricIndex, entityIndex, maxSeverity]);
        }
      });
    });
    
    this.chartOption = {
      title: {
        text: 'Anomaly Heatmap by Entity and Metric',
        left: 'center'
      },
      tooltip: {
        formatter: (params: any) => {
          const entity = entities[params.value[1]];
          const metric = metrics[params.value[0]];
          const severityValue = params.value[2];
          const severity = severityValue === 3 ? 'High' : 
                          severityValue === 2 ? 'Medium' : 'Low';
          
          return `<strong>${entity}</strong><br>` +
                `Metric: ${metric}<br>` +
                `Severity: ${severity}`;
        }
      },
      grid: {
        height: '70%',
        top: '15%'
      },
      xAxis: {
        type: 'category',
        data: metrics,
        splitArea: {
          show: true
        },
        axisLabel: {
          rotate: 45
        }
      },
      yAxis: {
        type: 'category',
        data: entities,
        splitArea: {
          show: true
        }
      },
      visualMap: {
        min: 1,
        max: 3,
        calculable: true,
        orient: 'horizontal',
        left: 'center',
        bottom: '5%',
        text: ['High Severity', 'Low Severity'],
        color: ['#f44336', '#ff9800', '#4caf50']
      },
      series: [
        {
          name: 'Anomaly Severity',
          type: 'heatmap',
          data: data,
          label: {
            show: true
          },
          emphasis: {
            itemStyle: {
              shadowBlur: 10,
              shadowColor: 'rgba(0, 0, 0, 0.5)'
            }
          }
        }
      ]
    };
  }
  
  private prepareBubbleChart(): void {
    // This chart uses actual vs expected values
    const hasValues = this.anomalies.some(a => a.actualValue != null && a.expectedValue != null);
    
    if (!hasValues) {
      this.preparePieChart(); // Fallback to pie chart if no values are available
      return;
    }
    
    // Filter anomalies with both actual and expected values
    const anomaliesWithValues = this.anomalies.filter(a => 
      a.actualValue != null && a.expectedValue != null);
    
    // Prepare data: x = expected, y = actual, size = deviation percentage
    const data = anomaliesWithValues.map(a => {
      const expected = Number(a.expectedValue);
      const actual = Number(a.actualValue);
      const deviation = Math.abs(((actual - expected) / expected) * 100);
      
      return [
        expected,
        actual,
        deviation,
        a // Store the full anomaly for tooltip
      ];
    });
    
    this.chartOption = {
      title: {
        text: 'Anomaly Values: Actual vs. Expected',
        left: 'center'
      },
      tooltip: {
        formatter: (params: any) => {
          const anomaly = params.data[3] as DataAnomaly;
          const expected = params.data[0];
          const actual = params.data[1];
          const deviation = params.data[2].toFixed(1);
          
          return `<strong>${anomaly.anomalyType}</strong><br>` +
                `${anomaly.description}<br>` +
                `Entity: ${anomaly.affectedEntity || 'Unknown'}<br>` +
                `Metric: ${anomaly.affectedMetric || 'Unknown'}<br>` +
                `Expected: ${expected}<br>` +
                `Actual: ${actual}<br>` +
                `Deviation: ${deviation}%<br>` +
                `Severity: ${anomaly.severity}`;
        }
      },
      xAxis: {
        type: 'value',
        name: 'Expected Value',
        nameLocation: 'middle',
        nameGap: 30
      },
      yAxis: {
        type: 'value',
        name: 'Actual Value',
        nameLocation: 'middle',
        nameGap: 30
      },
      series: [
        {
          type: 'scatter',
          data: data,
          symbolSize: (data: any) => {
            // Size based on deviation percentage (min 10, max 50)
            return Math.min(50, Math.max(10, data[2] / 2));
          },
          itemStyle: {
            color: (params: any) => {
              const anomaly = params.data[3] as DataAnomaly;
              return anomaly.severity.toLowerCase() === 'high' ? '#f44336' : 
                     anomaly.severity.toLowerCase() === 'medium' ? '#ff9800' : '#4caf50';
            }
          }
        },
        {
          // Diagonal reference line (x = y)
          type: 'line',
          data: this.generateDiagonalLine(data),
          showSymbol: false,
          lineStyle: {
            color: '#999',
            type: 'dashed'
          },
          tooltip: {
            show: false
          }
        }
      ]
    };
  }
  
  // Helper method to group anomalies by metric
  private groupByMetric() {
    const metricGroups: Record<string, DataAnomaly[]> = {};
    
    this.anomalies.forEach(anomaly => {
      const metric = anomaly.affectedMetric || 'Other';
      if (!metricGroups[metric]) {
        metricGroups[metric] = [];
      }
      metricGroups[metric].push(anomaly);
    });
    
    return metricGroups;
  }
  
  // Create mock metric groups for radar chart when affectedMetric is not available
  private createMockMetricGroups() {
    const metrics = ['Revenue', 'Expenses', 'Profit Margin', 'Sales Count', 'Marketing'];
    const result: Record<string, DataAnomaly[]> = {};
    
    metrics.forEach(metric => {
      result[metric] = this.anomalies.filter((_, i) => i % metrics.length === metrics.indexOf(metric));
    });
    
    return result;
  }
  
  // Generate diagonal reference line for the bubble chart
  private generateDiagonalLine(data: any[]): number[][] {
    if (data.length === 0) return [];
    
    // Find min and max values
    let minValue = Number.MAX_VALUE;
    let maxValue = Number.MIN_VALUE;
    
    data.forEach(point => {
      const x = point[0];
      const y = point[1];
      minValue = Math.min(minValue, x, y);
      maxValue = Math.max(maxValue, x, y);
    });
    
    // Add some padding
    minValue = Math.max(0, minValue * 0.9);
    maxValue = maxValue * 1.1;
    
    // Return diagonal line points
    return [[minValue, minValue], [maxValue, maxValue]];
  }
} 