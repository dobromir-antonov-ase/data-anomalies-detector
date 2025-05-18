import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatTabsModule } from '@angular/material/tabs';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { HttpClient } from '@angular/common/http';
import { Observable, finalize } from 'rxjs';
import { NgxEchartsDirective } from 'ngx-echarts';

import { DealerService } from '../../core/services/dealer.service';
import { ApiConfigService } from '../../core/services/api-config.service';
import { AnomalyDetectionService, DataAnomaly, DataPattern } from '../../core/services/anomaly-detection.service';
import { Dealer } from '../../core/models/dealer.model';
import { PatternVisualizationComponent } from '../../shared/components/pattern-visualization.component';

interface DealerWithValues extends Dealer {
  cellValues?: { [key: string]: number };
}

interface FinanceSubmission {
  id: number;
  month: number;
  dealerId: number;
}

interface MasterTemplate {
  id: number;
  name: string;
  cells: string[];
}

interface DealerAnomaly {
  dealerId: number;
  dealerName: string;
  cellName: string;
  value: number;
  expectedRange: string;
  severity: number;
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatTabsModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    NgxEchartsDirective,
    PatternVisualizationComponent
  ],
  template: `
    <div class="dashboard-container">
      <div class="dashboard-header">
        <h2>Automotive Sales Engineering Dashboard</h2>
        <div class="dashboard-actions">
          <button mat-raised-button color="warn" (click)="clearDashboard()">
            <mat-icon>delete_sweep</mat-icon> Clear Dashboard
          </button>
          <button mat-raised-button color="primary" (click)="analyzeData()" [disabled]="isAnalyzing()">
            <mat-icon>analytics</mat-icon> Analyze Data
            <mat-spinner *ngIf="isAnalyzing()" diameter="20" class="button-spinner"></mat-spinner>
          </button>
        </div>
      </div>

      <div class="charts-section" *ngIf="!isDashboardEmpty()">
        <div class="chart-container">
          <div echarts [options]="dealerChartOption()" class="chart"></div>
        </div>
        <div class="chart-container">
          <div echarts [options]="submissionChartOption()" class="chart"></div>
        </div>
      </div>

      <mat-card *ngIf="apiAnomalies().length > 0">
        <mat-card-header>
          <mat-card-title>API Detected Anomalies</mat-card-title>
          <mat-card-subtitle>Anomalies detected from API analysis</mat-card-subtitle>
        </mat-card-header>
        <mat-card-content>
          <table class="anomaly-table">
            <thead>
              <tr>
                <th>Type</th>
                <th>Description</th>
                <th>Severity</th>
                <th>Detected At</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let anomaly of apiAnomalies()" [class]="'severity-' + anomaly.severity">
                <td>{{ anomaly.anomalyType }}</td>
                <td>{{ anomaly.description }}</td>
                <td>{{ anomaly.severity }}</td>
                <td>{{ anomaly.detectedAt | date:'medium' }}</td>
              </tr>
            </tbody>
          </table>
          <div class="card-actions">
            <button mat-button color="primary" routerLink="/anomalies">
              <mat-icon>visibility</mat-icon> View All Anomalies
            </button>
          </div>
        </mat-card-content>
      </mat-card>

      <mat-card *ngIf="patterns().length > 0">
        <mat-card-header>
          <mat-card-title>Detected Patterns</mat-card-title>
          <mat-card-subtitle>ML-detected patterns in financial data</mat-card-subtitle>
        </mat-card-header>
        <mat-card-content>
          <mat-tab-group>
            <mat-tab label="Radar View">
              <app-pattern-visualization 
                [patterns]="patterns()" 
                [dealerGroups]="dealerGroupNames()" 
                chartType="radar">
              </app-pattern-visualization>
            </mat-tab>
            <mat-tab label="Scatter View">
              <app-pattern-visualization 
                [patterns]="patterns()" 
                [dealerGroups]="dealerGroupNames()" 
                chartType="scatter">
              </app-pattern-visualization>
            </mat-tab>
            <mat-tab label="Time Series View">
              <app-pattern-visualization 
                [patterns]="patterns()" 
                [dealerGroups]="dealerGroupNames()" 
                chartType="line">
              </app-pattern-visualization>
            </mat-tab>
            <mat-tab label="Data Table">
              <table class="pattern-table">
                <thead>
                  <tr>
                    <th>Type</th>
                    <th>Description</th>
                    <th>Significance</th>
                    <th>Confidence</th>
                  </tr>
                </thead>
                <tbody>
                  <tr *ngFor="let pattern of patterns()" [class]="'significance-' + pattern.significance.toLowerCase()">
                    <td>{{ pattern.patternType }}</td>
                    <td>{{ pattern.description }}</td>
                    <td>{{ pattern.significance }}</td>
                    <td>{{ pattern.confidenceScore }}%</td>
                  </tr>
                </tbody>
              </table>
            </mat-tab>
          </mat-tab-group>
          <div class="card-actions">
            <button mat-button color="primary" routerLink="/patterns">
              <mat-icon>visibility</mat-icon> View All Patterns
            </button>
          </div>
        </mat-card-content>
      </mat-card>

      <mat-card *ngIf="anomalies().length > 0">
        <mat-card-header>
          <mat-card-title>Anomaly Detection</mat-card-title>
        </mat-card-header>
        <mat-card-content>
          <div echarts [options]="anomalyChartOption()" class="chart anomaly-chart"></div>
        </mat-card-content>
      </mat-card>

      <mat-card *ngIf="!isDashboardEmpty()">
        <mat-card-header>
          <mat-card-title>Dashboard Summary</mat-card-title>
        </mat-card-header>
        <mat-card-content>
          <p>Welcome to the ASE Dashboard. Use the navigation menu to access data.</p>
          <ul>
            <li>Dealers: {{dealers().length}}</li>
            <li>Submissions: {{submissions().length}}</li>
            <li>Templates: {{templates().length}}</li>
            <li>Anomalies: {{anomalies().length + apiAnomalies().length}}</li>
            <li>Patterns: {{patterns().length}}</li>
          </ul>
        </mat-card-content>
      </mat-card>

      <div class="empty-dashboard" *ngIf="isDashboardEmpty()">
        <mat-icon class="empty-icon">dashboard</mat-icon>
        <h3>Dashboard is empty</h3>
        <p>Click the Analyze Data button to load dashboard data and detect anomalies</p>
      </div>
    </div>
  `,
  styles: [`
    .dashboard-container {
      padding: 20px;
    }

    .dashboard-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 20px;
    }

    .dashboard-actions {
      display: flex;
      gap: 10px;
    }

    .button-spinner {
      display: inline-block;
      margin-left: 8px;
    }

    .charts-section {
      display: flex;
      flex-wrap: wrap;
      gap: 20px;
      margin-bottom: 20px;
    }

    .chart-container {
      flex: 1;
      min-width: 300px;
    }

    .chart {
      height: 300px;
      width: 100%;
    }

    .anomaly-chart, .pattern-chart {
      height: 400px;
    }

    mat-card {
      margin-bottom: 20px;
    }

    .empty-dashboard {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      margin-top: 50px;
      color: #888;
      text-align: center;
    }

    .empty-icon {
      font-size: 64px;
      height: 64px;
      width: 64px;
      margin-bottom: 16px;
    }

    .anomaly-table, .pattern-table {
      width: 100%;
      border-collapse: collapse;
    }

    .anomaly-table th, .anomaly-table td,
    .pattern-table th, .pattern-table td {
      padding: 8px;
      text-align: left;
      border-bottom: 1px solid #e0e0e0;
    }

    .card-actions {
      display: flex;
      justify-content: flex-end;
      margin-top: 16px;
    }

    .severity-high, .significance-high {
      background-color: rgba(244, 67, 54, 0.05);
    }

    .severity-medium, .significance-medium {
      background-color: rgba(255, 152, 0, 0.05);
    }

    .severity-low, .significance-low {
      background-color: rgba(76, 175, 80, 0.05);
    }
  `]
})
export class DashboardComponent implements OnInit {
  private http = inject(HttpClient);
  private dealerService = inject(DealerService);
  private apiConfig = inject(ApiConfigService);
  private anomalyService = inject(AnomalyDetectionService);
  private snackBar = inject(MatSnackBar);
  private apiUrl = this.apiConfig.baseUrl;

  dealers = signal<DealerWithValues[]>([]);
  submissions = signal<FinanceSubmission[]>([]);
  templates = signal<MasterTemplate[]>([]);
  anomalies = signal<DealerAnomaly[]>([]);
  apiAnomalies = signal<DataAnomaly[]>([]);
  patterns = signal<DataPattern[]>([]);
  dealerGroups = signal<any[]>([]);
  isAnalyzing = signal<boolean>(false);

  dealerChartOption = signal<any>(null);
  submissionChartOption = signal<any>(null);
  anomalyChartOption = signal<any>(null);
  
  // Compute dealer group names from the dealerGroups or use defaults
  dealerGroupNames = computed(() => {
    if (this.dealerGroups().length > 0) {
      return this.dealerGroups().map(g => g.groupName);
    }
    return ['Premium Auto Group', 'Metro Dealership Network', 'Regional Motors Alliance'];
  });

  isDashboardEmpty = computed(() => {
    return this.dealers().length === 0 && 
           this.submissions().length === 0 && 
           this.templates().length === 0 && 
           this.anomalies().length === 0 &&
           this.patterns().length === 0;
  });

  ngOnInit(): void {
    // Initial empty state
  }

  clearDashboard(): void {
    this.dealers.set([]);
    this.submissions.set([]);
    this.templates.set([]);
    this.anomalies.set([]);
    this.apiAnomalies.set([]);
    this.patterns.set([]);
    this.dealerGroups.set([]);
    this.dealerChartOption.set(null);
    this.submissionChartOption.set(null);
    this.anomalyChartOption.set(null);
  }

  analyzeData(): void {
    this.isAnalyzing.set(true);
    this.loadData();
    this.detectAnomaliesFromApi();
    this.detectPatternsFromApi();
  }

  loadData(): void {
    this.dealerService.getAllDealers().subscribe({
      next: (dealers) => {
        this.dealers.set(dealers);
        
        // Get dealer groups
        this.anomalyService.getDealerGroups().subscribe({
          next: (groups) => {
            this.dealerGroups.set(groups);
          },
          error: (error) => {
            console.error('Error loading dealer groups:', error);
          }
        });
        
        // Add mock cell values for demo
        this.addMockCellValues();
        this.prepareDealerChart();
      },
      error: (error) => {
        console.error('Error loading dealers:', error);
        this.isAnalyzing.set(false);
      }
    });

    this.getAllSubmissions().subscribe({
      next: (submissions) => {
        this.submissions.set(submissions);
        this.prepareSubmissionChart();
        this.detectAnomalies();
      },
      error: (error) => {
        console.error('Error loading submissions:', error);
        this.isAnalyzing.set(false);
      }
    });

    this.getAllMasterTemplates().subscribe({
      next: (templates) => {
        this.templates.set(templates);
      },
      error: (error) => {
        console.error('Error loading templates:', error);
        this.isAnalyzing.set(false);
      }
    });
  }

  detectAnomaliesFromApi(): void {
    // Call the anomaly detection API endpoint
    this.anomalyService.detectGlobalAnomalies().subscribe({
      next: (anomalies: DataAnomaly[]) => {
        this.apiAnomalies.set(anomalies);
        this.isAnalyzing.set(false);
        this.snackBar.open('Anomaly analysis completed', 'Dismiss', { duration: 3000 });
      },
      error: (error: any) => {
        console.error('Error analyzing anomalies:', error);
        // Fallback to mock data for demo purposes
        this.generateMockApiAnomalies();
        this.isAnalyzing.set(false);
        this.snackBar.open('Using mock anomaly data (API error)', 'Dismiss', { duration: 3000 });
      }
    });
  }

  generateMockApiAnomalies(): void {
    const mockAnomalies: DataAnomaly[] = [
      {
        anomalyType: 'Year-Over-Year Variance',
        description: 'Cell A3 shows 45.5% increase compared to June last year',
        severity: 'medium',
        detectedAt: new Date()
      },
      {
        anomalyType: 'Statistical Outlier',
        description: 'Found 3 outlier values in table "Revenue" on sheet "Income Statement"',
        severity: 'high',
        detectedAt: new Date()
      },
      {
        anomalyType: 'Missing Data',
        description: 'Found 7 empty cells in table "Expenses" on sheet "Income Statement"',
        severity: 'high',
        detectedAt: new Date()
      },
      {
        anomalyType: 'Missing Historical Data',
        description: 'Found 2 cells that were reported last year but missing in current submission',
        severity: 'low',
        detectedAt: new Date()
      }
    ];
    
    this.apiAnomalies.set(mockAnomalies);
  }

  detectPatternsFromApi(): void {
    // Use the first dealer for demo purposes
    const firstDealer = this.dealers()[0]?.id;
    if (!firstDealer) {
      this.generateMockPatterns();
      return;
    }

    this.anomalyService.detectPatternsByDealer(firstDealer).subscribe({
      next: (patterns: DataPattern[]) => {
        this.patterns.set(patterns);
      },
      error: (error: any) => {
        console.error('Error detecting patterns:', error);
        // Fallback to mock data for demo
        this.generateMockPatterns();
      }
    });

    // If we have dealer groups, get patterns for the first group
    if (this.dealerGroups().length > 0) {
      const firstGroup = this.dealerGroups()[0]?.groupId;
      
      if (firstGroup) {
        this.anomalyService.detectPatternsByDealerGroup(firstGroup).subscribe({
          next: (groupPatterns: DataPattern[]) => {
            // Combine with dealer patterns
            const allPatterns = [...this.patterns(), ...groupPatterns];
            this.patterns.set(allPatterns);
          },
          error: (error) => {
            console.error('Error detecting group patterns:', error);
          }
        });
      }
    }
  }

  generateMockPatterns(): void {
    const mockPatterns: DataPattern[] = [
      {
        patternType: 'Seasonal Trend',
        description: 'Detected consistent quarterly pattern in revenue cells',
        significance: 'high',
        confidenceScore: 87.5,
        detectedAt: new Date(),
        correlation: 0.89
      },
      {
        patternType: 'Linear Growth',
        description: 'Linear growth trend in monthly sales figures',
        significance: 'medium',
        confidenceScore: 75.2,
        detectedAt: new Date(),
        formula: 'y = 1.2x + 105.4',
        r2Value: 0.78
      },
      {
        patternType: 'Cyclic Pattern',
        description: 'Recurring 3-month cycle in marketing expenses',
        significance: 'medium',
        confidenceScore: 68.7,
        detectedAt: new Date()
      },
      {
        patternType: 'Cross-Correlation',
        description: 'Strong correlation between cells C5 and G12',
        significance: 'high',
        confidenceScore: 92.3,
        detectedAt: new Date(),
        correlation: 0.94,
        relatedCellAddresses: ['C5', 'G12']
      },
      {
        patternType: 'Dealer Group Trend',
        description: 'Premium Auto Group shows consistent revenue growth compared to other groups',
        significance: 'high',
        confidenceScore: 89.1,
        detectedAt: new Date()
      }
    ];
    
    this.patterns.set(mockPatterns);
  }

  // Add mock cell values for demo purposes
  addMockCellValues(): void {
    const updatedDealers = [...this.dealers()].map((dealer, index) => {
      const cellValues: { [key: string]: number } = {};

      // Normal ranges
      for (let i = 1; i <= 5; i++) {
        // A cells normally 100-200
        cellValues[`A${i}`] = Math.floor(Math.random() * 100) + 100;
        // B cells normally 300-400  
        cellValues[`B${i}`] = Math.floor(Math.random() * 100) + 300;
      }

      // Special cases for dealers
      if (dealer.id === 9 || index === 8) {
        // Dealer 9: A cells in range 400-600
        for (let i = 1; i <= 5; i++) {
          cellValues[`A${i}`] = Math.floor(Math.random() * 200) + 400;
        }
      } else if (dealer.id === 10 || index === 9) {
        // Dealer 10: B cells in range 10-90
        for (let i = 1; i <= 5; i++) {
          cellValues[`B${i}`] = Math.floor(Math.random() * 80) + 10;
        }
      } else if (dealer.id === 8 || index === 7) {
        // Dealer 8: arithmetic progression
        for (let i = 1; i <= 5; i++) {
          cellValues[`A${i}`] = 100 + (i * 30);
          cellValues[`B${i}`] = 300 + (i * 30);
        }
      }

      return { ...dealer, cellValues };
    });

    this.dealers.set(updatedDealers);
  }

  // API methods for other entities
  getAllSubmissions(): Observable<FinanceSubmission[]> {
    return this.http.get<FinanceSubmission[]>(`${this.apiUrl}/finance-submissions`);
  }

  getAllMasterTemplates(): Observable<MasterTemplate[]> {
    return this.http.get<MasterTemplate[]>(`${this.apiUrl}/master-templates`);
  }

  prepareDealerChart(): void {
    // Create a simple bar chart showing number of submissions per dealer
    const dealerNames = this.dealers().map(d => d.name);

    this.dealerChartOption.set({
      title: {
        text: 'Dealers Overview'
      },
      tooltip: {},
      xAxis: {
        data: dealerNames,
        axisLabel: {
          rotate: 45,
          overflow: 'truncate'
        }
      },
      yAxis: {},
      series: [
        {
          name: 'Dealers',
          type: 'bar',
          data: this.dealers().map(() => Math.floor(Math.random() * 100))
        }
      ]
    });
  }

  prepareSubmissionChart(): void {
    // For demo purposes, create mock submission data
    const monthlyData = Array(12).fill(0).map(() => Math.floor(Math.random() * 30));

    this.submissionChartOption.set({
      title: {
        text: 'Monthly Submissions'
      },
      tooltip: {},
      xAxis: {
        data: ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec']
      },
      yAxis: {},
      series: [
        {
          name: 'Submissions',
          type: 'line',
          data: monthlyData
        }
      ]
    });
  }

  detectAnomalies(): void {
    const newAnomalies: DealerAnomaly[] = [];

    // Process dealer data to find anomalies
    this.dealers().forEach(dealer => {
      if (!dealer.cellValues) return;

      Object.entries(dealer.cellValues).forEach(([cellName, value]) => {
        // Define normal ranges based on cell name prefix
        const isACell = cellName.startsWith('A');
        const isBCell = cellName.startsWith('B');
        const normalRange = isACell ? '100-200' : isBCell ? '300-400' : '0-100';

        let isAnomaly = false;
        let severity = 0;

        // Detect value anomalies based on our known data patterns
        if (isACell && (value < 100 || value > 200)) {
          isAnomaly = true;
          severity = Math.min(10, Math.abs(value - 150) / 15); // Calculate severity
        } else if (isBCell && (value < 300 || value > 400)) {
          isAnomaly = true;
          severity = Math.min(10, Math.abs(value - 350) / 15);
        }

        // Special case for dealer 9 (high A values) and dealer 10 (low B values)
        if ((dealer.id === 9 && isACell && value >= 400 && value <= 600) ||
          (dealer.id === 10 && isBCell && value >= 10 && value <= 90)) {
          isAnomaly = true;
          severity = 8; // High severity for these known anomalies
        }

        if (isAnomaly) {
          newAnomalies.push({
            dealerId: dealer.id,
            dealerName: dealer.name,
            cellName,
            value,
            expectedRange: normalRange,
            severity
          });
        }
      });
    });

    // Sort anomalies by severity
    newAnomalies.sort((a, b) => b.severity - a.severity);
    this.anomalies.set(newAnomalies);

    // Prepare the anomaly chart
    this.prepareAnomalyChart();
  }

  prepareAnomalyChart(): void {
    // Take top 10 anomalies for visualization
    const topAnomalies = this.anomalies().slice(0, 10);

    const data = topAnomalies.map(anomaly => ({
      name: `${anomaly.dealerName}: ${anomaly.cellName}`,
      value: anomaly.value,
      severity: anomaly.severity,
      itemStyle: {
        color: this.getSeverityColor(anomaly.severity)
      }
    }));

    this.anomalyChartOption.set({
      title: {
        text: 'Value Anomalies',
        subtext: 'Cell values outside expected ranges'
      },
      tooltip: {
        formatter: function (params: any) {
          const anomaly = topAnomalies[params.dataIndex];
          return `<strong>${anomaly.dealerName}</strong><br/>` +
            `Cell: ${anomaly.cellName}<br/>` +
            `Value: ${anomaly.value}<br/>` +
            `Expected Range: ${anomaly.expectedRange}<br/>` +
            `Severity: ${anomaly.severity.toFixed(1)}/10`;
        }
      },
      xAxis: {
        type: 'category',
        data: data.map(item => item.name),
        axisLabel: {
          rotate: 45,
          overflow: 'truncate'
        }
      },
      yAxis: {
        type: 'value',
        name: 'Cell Value'
      },
      series: [
        {
          type: 'bar',
          data: data,
          label: {
            show: true,
            position: 'top',
            formatter: '{c}'
          }
        }
      ],
      visualMap: {
        show: true,
        min: 0,
        max: 10,
        dimension: 2,
        inRange: {
          color: ['#52c41a', '#faad14', '#f5222d']
        },
        text: ['High', 'Low'],
        calculable: true,
        orient: 'horizontal',
        left: 'center',
        bottom: 0
      }
    });
  }

  getSeverityColor(severity: number): string {
    // Return color based on severity
    if (severity >= 7) return '#f5222d'; // Red for high severity
    if (severity >= 4) return '#faad14'; // Yellow for medium
    return '#52c41a'; // Green for low severity
  }
} 