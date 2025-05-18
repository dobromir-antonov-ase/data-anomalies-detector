import { Component, Input, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NgxEchartsDirective } from 'ngx-echarts';
import { DataPattern } from '../../core/services/anomaly-detection.service';

@Component({
  selector: 'app-pattern-visualization',
  standalone: true,
  imports: [
    CommonModule,
    NgxEchartsDirective
  ],
  template: `
    <div class="pattern-visualization-container">
      <div echarts [options]="chartOption" class="pattern-chart"></div>
    </div>
  `,
  styles: [`
    .pattern-visualization-container {
      width: 100%;
      height: 100%;
    }
    
    .pattern-chart {
      height: 400px;
      width: 100%;
    }
  `]
})
export class PatternVisualizationComponent implements OnChanges {
  @Input() patterns: DataPattern[] = [];
  @Input() dealerGroups: string[] = ['Premium Auto Group', 'Metro Dealership Network', 'Regional Motors Alliance'];
  @Input() chartType: 'radar' | 'scatter' | 'line' = 'radar';
  
  chartOption: any = {};
  
  ngOnChanges(changes: SimpleChanges): void {
    if (changes['patterns'] || changes['dealerGroups'] || changes['chartType']) {
      this.prepareChart();
    }
  }
  
  private prepareChart(): void {
    if (!this.patterns.length) {
      return;
    }
    
    switch (this.chartType) {
      case 'radar':
        this.prepareRadarChart();
        break;
      case 'scatter':
        this.prepareScatterChart();
        break;
      case 'line':
        this.prepareLineChart();
        break;
      default:
        this.prepareRadarChart();
    }
  }
  
  private prepareRadarChart(): void {
    // Prepare data for radar chart to show patterns
    const indicators = [
      { name: 'Seasonality', max: 100 },
      { name: 'Growth Trend', max: 100 },
      { name: 'Cyclical Pattern', max: 100 },
      { name: 'Correlation Strength', max: 100 },
      { name: 'Predictability', max: 100 }
    ];

    // Create data for radar chart based on patterns
    const seriesData = this.dealerGroups.map(group => {
      return {
        value: [
          this.patterns.some(p => p.description.includes(group) && p.patternType.includes('Season')) ? 
            this.getRandomInRange(70, 90) : this.getRandomInRange(30, 50),
          this.patterns.some(p => p.description.includes(group) && p.patternType.includes('Growth')) ? 
            this.getRandomInRange(70, 90) : this.getRandomInRange(30, 50),
          this.patterns.some(p => p.description.includes(group) && p.patternType.includes('Cyclic')) ? 
            this.getRandomInRange(70, 90) : this.getRandomInRange(30, 50),
          this.patterns.some(p => p.description.includes(group) && p.patternType.includes('Correlation')) ? 
            this.getRandomInRange(70, 90) : this.getRandomInRange(30, 50),
          this.getRandomInRange(40, 60) // Predictability is more random
        ],
        name: group
      };
    });

    // Set up the chart options
    this.chartOption = {
      title: {
        text: 'ML-Detected Data Patterns',
        subtext: 'Patterns across dealer groups'
      },
      tooltip: {},
      legend: {
        data: this.dealerGroups
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
  
  private prepareScatterChart(): void {
    // Create scatter data to show confidence vs significance
    const data = this.patterns.map((pattern, index) => {
      // Convert significance to numeric value
      const significanceValue = 
        pattern.significance === 'high' ? 80 + this.getRandomInRange(0, 20) :
        pattern.significance === 'medium' ? 50 + this.getRandomInRange(0, 30) :
        20 + this.getRandomInRange(0, 30);
      
      return [
        pattern.confidenceScore, 
        significanceValue,
        pattern.patternType
      ];
    });
    
    this.chartOption = {
      title: {
        text: 'Pattern Confidence vs. Significance',
        subtext: 'Size indicates correlation strength'
      },
      tooltip: {
        formatter: (params: any) => {
          const pattern = this.patterns[params.dataIndex];
          return `<strong>${pattern.patternType}</strong><br>` +
                 `Confidence: ${pattern.confidenceScore.toFixed(1)}%<br>` +
                 `Significance: ${pattern.significance}<br>` +
                 `${pattern.description}`;
        }
      },
      xAxis: {
        type: 'value',
        name: 'Confidence Score (%)',
        min: 0,
        max: 100
      },
      yAxis: {
        type: 'value',
        name: 'Significance',
        min: 0,
        max: 100
      },
      series: [
        {
          type: 'scatter',
          symbolSize: (data: any) => {
            // Find the pattern
            const pattern = this.patterns.find(p => p.patternType === data[2]);
            // Use correlation or r2Value for size if available, otherwise random
            const size = pattern?.correlation ? 
              pattern.correlation * 50 : 
              pattern?.r2Value ? 
                pattern.r2Value * 50 : 
                20 + this.getRandomInRange(0, 20);
            return size;
          },
          data: data,
          itemStyle: {
            color: (params: any) => {
              const pattern = this.patterns[params.dataIndex];
              return pattern.significance === 'high' ? '#f5222d' : 
                     pattern.significance === 'medium' ? '#faad14' : '#52c41a';
            }
          }
        }
      ]
    };
  }
  
  private prepareLineChart(): void {
    // Create time series data for patterns over time
    // In a real implementation, this would use actual timestamps from patterns
    const months = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
    const patternTypes = [...new Set(this.patterns.map(p => p.patternType))];
    const series = patternTypes.map(type => {
      // Generate mock pattern strength data over time
      let baseValue = 50 + this.getRandomInRange(0, 30);
      const data = months.map((month, index) => {
        // Seasonal patterns should show cyclical behavior
        if (type.includes('Season')) {
          return baseValue + 15 * Math.sin(index / 3 * Math.PI);
        }
        // Growth patterns should show upward trend
        else if (type.includes('Growth')) {
          return baseValue + index * 2;
        }
        // Cyclic patterns should show regular ups and downs
        else if (type.includes('Cyclic')) {
          return baseValue + 20 * Math.sin(index / 2 * Math.PI);
        }
        // Others should have some randomness
        else {
          return baseValue + this.getRandomInRange(-10, 10);
        }
      });
      
      return {
        name: type,
        type: 'line',
        data: data
      };
    });
    
    this.chartOption = {
      title: {
        text: 'Pattern Strength Over Time',
        subtext: 'Monthly progression of detected patterns'
      },
      tooltip: {
        trigger: 'axis'
      },
      legend: {
        data: patternTypes
      },
      xAxis: {
        type: 'category',
        data: months
      },
      yAxis: {
        type: 'value',
        name: 'Pattern Strength'
      },
      series: series
    };
  }
  
  private getRandomInRange(min: number, max: number): number {
    return Math.floor(Math.random() * (max - min + 1)) + min;
  }
} 