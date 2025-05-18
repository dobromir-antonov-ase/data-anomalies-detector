import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatTabsModule } from '@angular/material/tabs';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatSelectModule } from '@angular/material/select';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { NgxEchartsDirective } from 'ngx-echarts';

import { AnomalyDetectionService, DataAnomaly } from '../../core/services/anomaly-detection.service';
import { DealerService } from '../../core/services/dealer.service';
import { AnomalyVisualizationComponent } from '../../shared/components/anomaly-visualization.component';

@Component({
  selector: 'app-dealer-anomalies',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatTabsModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    MatSelectModule,
    FormsModule,
    NgxEchartsDirective,
    AnomalyVisualizationComponent
  ],
  templateUrl: './dealer-anomalies.component.html',
  styleUrls: ['./dealer-anomalies.component.css']
})
export class DealerAnomaliesComponent implements OnInit {
  private anomalyService = inject(AnomalyDetectionService);
  private dealerService = inject(DealerService);
  private snackBar = inject(MatSnackBar);
  private route = inject(ActivatedRoute);
  
  dealers = signal<any[]>([]);
  dealerGroups = signal<any[]>([]);
  anomalies = signal<DataAnomaly[]>([]);
  loading = signal<boolean>(false);
  
  viewMode: 'dealer' | 'group' = 'dealer';
  selectedDealerId: number | null = null;
  selectedGroupId: number | null = null;
  
  // Helper signal for entity name
  currentEntityName = signal<string>('');
  
  ngOnInit(): void {
    // Load initial data
    this.loadDealers();
    this.loadDealerGroups();
    
    // Check if we have a dealer ID in the route
    this.route.params.subscribe(params => {
      if (params['id']) {
        this.selectedDealerId = +params['id'];
        this.viewMode = 'dealer';
        this.loadDealerAnomalies();
      } else if (params['groupId']) {
        this.selectedGroupId = +params['groupId'];
        this.viewMode = 'group';
        this.loadGroupAnomalies();
      }
    });
  }
  
  loadDealers(): void {
    this.dealerService.getAllDealers().subscribe({
      next: (dealers) => {
        this.dealers.set(dealers);
        
        // If we have a selectedDealerId but no currentEntityName, set it now
        if (this.selectedDealerId && !this.currentEntityName()) {
          const dealer = dealers.find(d => d.id === this.selectedDealerId);
          if (dealer) {
            this.currentEntityName.set(dealer.name);
          }
        }
      },
      error: (error) => {
        console.error('Error loading dealers:', error);
        this.snackBar.open('Error loading dealers', 'Dismiss', { duration: 3000 });
      }
    });
  }
  
  loadDealerGroups(): void {
    this.anomalyService.getDealerGroups().subscribe({
      next: (groups) => {
        this.dealerGroups.set(groups);
        
        // If we have a selectedGroupId but no currentEntityName, set it now
        if (this.selectedGroupId && !this.currentEntityName()) {
          const group = groups.find(g => g.groupId === this.selectedGroupId);
          if (group) {
            this.currentEntityName.set(group.groupName);
          }
        }
      },
      error: (error) => {
        console.error('Error loading dealer groups:', error);
        this.snackBar.open('Error loading dealer groups', 'Dismiss', { duration: 3000 });
      }
    });
  }
  
  onViewModeChange(): void {
    // Reset anomalies when switching view modes
    this.anomalies.set([]);
    
    if (this.viewMode === 'dealer' && this.selectedDealerId) {
      this.loadDealerAnomalies();
    } else if (this.viewMode === 'group' && this.selectedGroupId) {
      this.loadGroupAnomalies();
    }
  }
  
  loadDealerAnomalies(): void {
    if (!this.selectedDealerId) return;
    
    this.loading.set(true);
    this.anomalyService.detectAnomaliesByDealer(this.selectedDealerId).subscribe({
      next: (anomalies) => {
        this.anomalies.set(anomalies);
        
        // Update the current entity name
        const dealer = this.dealers().find(d => d.id === this.selectedDealerId);
        if (dealer) {
          this.currentEntityName.set(dealer.name);
        }
        
        this.loading.set(false);
      },
      error: (error) => {
        console.error('Error loading dealer anomalies:', error);
        this.snackBar.open('Error loading anomalies', 'Dismiss', { duration: 3000 });
        // Fallback to mock data for demo
        this.generateMockDealerAnomalies();
        this.loading.set(false);
      }
    });
  }
  
  loadGroupAnomalies(): void {
    if (!this.selectedGroupId) return;
    
    this.loading.set(true);
    this.anomalyService.detectAnomaliesByDealerGroup(this.selectedGroupId).subscribe({
      next: (anomalies) => {
        this.anomalies.set(anomalies);
        
        // Update the current entity name
        const group = this.dealerGroups().find(g => g.groupId === this.selectedGroupId);
        if (group) {
          this.currentEntityName.set(group.groupName);
        }
        
        this.loading.set(false);
      },
      error: (error) => {
        console.error('Error loading group anomalies:', error);
        this.snackBar.open('Error loading anomalies', 'Dismiss', { duration: 3000 });
        // Fallback to mock data for demo
        this.generateMockGroupAnomalies();
        this.loading.set(false);
      }
    });
  }
  
  reloadAnomalies(): void {
    if (this.viewMode === 'dealer') {
      this.loadDealerAnomalies();
    } else {
      this.loadGroupAnomalies();
    }
  }
  
  // Helper methods
  getTopAnomalies(): DataAnomaly[] {
    const sortedAnomalies = [...this.anomalies()].sort((a, b) => {
      const severityOrder: Record<string, number> = { high: 3, medium: 2, low: 1 };
      const aSeverity = severityOrder[a.severity.toLowerCase()] || 0;
      const bSeverity = severityOrder[b.severity.toLowerCase()] || 0;
      return bSeverity - aSeverity;
    });
    
    return sortedAnomalies.slice(0, 3);
  }
  
  getRecommendation(anomaly: DataAnomaly): string {
    // Use the recommended action from the anomaly if available
    if (anomaly.recommendedAction) {
      return anomaly.recommendedAction;
    }
    
    // Generate a relevant recommendation based on anomaly type and severity
    if (anomaly.severity.toLowerCase() === 'high') {
      if (anomaly.anomalyType.includes('Spike')) {
        return 'Investigate this spike immediately. Compare with historical data and determine if there are matching business events that explain this significant deviation.';
      } else if (anomaly.anomalyType.includes('Missing')) {
        return 'Address data completeness issue urgently. Missing data is creating significant analysis gaps.';
      } else if (anomaly.anomalyType.includes('Outlier')) {
        return 'Review this outlier value and verify with the source. If correct, document the business reason for this unusual value.';
      }
      return 'Immediate review required. Prioritize investigation of this anomaly as it represents a significant deviation from expected patterns.';
    } else if (anomaly.severity.toLowerCase() === 'medium') {
      return 'Review during the next analysis cycle. Document findings and monitor for any pattern development.';
    } else {
      return 'Informational only. Monitor for any pattern development over time.';
    }
  }
  
  // Mock data generators for demo purposes
  private generateMockDealerAnomalies(): void {
    // Get dealer name for more realistic mock data
    const dealer = this.dealers().find(d => d.id === this.selectedDealerId);
    const dealerName = dealer ? dealer.name : 'Selected Dealer';
    this.currentEntityName.set(dealerName);
    
    const mockAnomalies: DataAnomaly[] = [
      {
        anomalyType: 'Revenue Spike',
        description: `Unexpected 35% spike in monthly revenue for ${dealerName} in current period`,
        severity: 'high',
        detectedAt: new Date(Date.now() - 24 * 60 * 60 * 1000), // 1 day ago
        anomalyScore: 89.5,
        affectedEntity: dealerName,
        affectedMetric: 'Monthly Revenue',
        actualValue: 285000,
        expectedValue: 211000,
        threshold: 250000,
        relatedCellAddresses: ['A1', 'A5'],
        businessImpact: {
          description: 'Potential revenue recognition error or legitimate business growth',
          estimatedValue: 74000
        },
        recommendedAction: 'Verify revenue sources and validate against historical patterns',
        timeRange: {
          start: new Date(Date.now() - 60 * 24 * 60 * 60 * 1000), // 60 days ago
          end: new Date()
        }
      },
      {
        anomalyType: 'Missing Data',
        description: `Multiple expense entries missing in monthly report from ${dealerName}`,
        severity: 'medium',
        detectedAt: new Date(Date.now() - 3 * 24 * 60 * 60 * 1000), // 3 days ago
        anomalyScore: 65.3,
        affectedEntity: dealerName,
        affectedMetric: 'Expense Reporting',
        businessImpact: {
          description: 'Incomplete financial analysis and potential for misreporting',
          estimatedValue: 15000
        },
        recommendedAction: 'Request missing expense data and update financial submissions',
        timeRange: {
          start: new Date(Date.now() - 30 * 24 * 60 * 60 * 1000), // 30 days ago
          end: new Date()
        }
      },
      {
        anomalyType: 'Statistical Outlier',
        description: `Vehicle sales count deviates significantly from 12-month trend for ${dealerName}`,
        severity: 'high',
        detectedAt: new Date(Date.now() - 2 * 24 * 60 * 60 * 1000), // 2 days ago
        anomalyScore: 92.1,
        affectedEntity: dealerName,
        affectedMetric: 'Vehicle Sales Count',
        actualValue: 45,
        expectedValue: 78,
        threshold: 65,
        businessImpact: {
          description: 'Significant drop in sales affecting revenue forecast',
          estimatedValue: 825000
        },
        recommendedAction: 'Analyze market conditions and competitor activity; review sales strategy',
        timeRange: {
          start: new Date(Date.now() - 365 * 24 * 60 * 60 * 1000), // 1 year ago
          end: new Date()
        }
      },
      {
        anomalyType: 'Sequential Pattern Break',
        description: `Break in the sequential pattern of inventory updates for ${dealerName}`,
        severity: 'low',
        detectedAt: new Date(Date.now() - 7 * 24 * 60 * 60 * 1000), // 7 days ago
        anomalyScore: 42.8,
        affectedEntity: dealerName,
        affectedMetric: 'Inventory Management',
        recommendedAction: 'Review inventory update procedures with dealership staff',
        timeRange: {
          start: new Date(Date.now() - 90 * 24 * 60 * 60 * 1000), // 90 days ago
          end: new Date()
        }
      },
      {
        anomalyType: 'Year-Over-Year Variance',
        description: `Unusual 45% decrease in marketing expenses compared to same period last year`,
        severity: 'medium',
        detectedAt: new Date(Date.now() - 5 * 24 * 60 * 60 * 1000), // 5 days ago
        anomalyScore: 76.5,
        affectedEntity: dealerName,
        affectedMetric: 'Marketing Spend',
        actualValue: 22500,
        expectedValue: 41000,
        threshold: 30750,
        businessImpact: {
          description: 'Potential impact on brand visibility and future sales',
          estimatedValue: 18500
        },
        recommendedAction: 'Review marketing strategy changes and correlate with sales performance',
        timeRange: {
          start: new Date(Date.now() - 365 * 24 * 60 * 60 * 1000), // 1 year ago
          end: new Date()
        }
      }
    ];
    
    this.anomalies.set(mockAnomalies);
  }
  
  private generateMockGroupAnomalies(): void {
    // Get group name for more realistic mock data
    const group = this.dealerGroups().find(g => g.groupId === this.selectedGroupId);
    const groupName = group ? group.groupName : 'Selected Group';
    this.currentEntityName.set(groupName);
    
    const mockAnomalies: DataAnomaly[] = [
      {
        anomalyType: 'Group Performance Anomaly',
        description: `${groupName} showing 30% below expected performance metrics`,
        severity: 'high',
        detectedAt: new Date(Date.now() - 24 * 60 * 60 * 1000), // 1 day ago
        anomalyScore: 88.3,
        affectedEntity: groupName,
        affectedMetric: 'Group Performance',
        actualValue: 70,
        expectedValue: 100,
        threshold: 85,
        businessImpact: {
          description: 'Significant underperformance affecting multiple dealerships',
          estimatedValue: 1250000
        },
        recommendedAction: 'Conduct full performance review across all dealerships in the group',
        timeRange: {
          start: new Date(Date.now() - 180 * 24 * 60 * 60 * 1000), // 180 days ago
          end: new Date()
        }
      },
      {
        anomalyType: 'Data Consistency Issue',
        description: `Inconsistent reporting formats detected across dealers in ${groupName}`,
        severity: 'medium',
        detectedAt: new Date(Date.now() - 4 * 24 * 60 * 60 * 1000), // 4 days ago
        anomalyScore: 68.7,
        affectedEntity: groupName,
        affectedMetric: 'Reporting Standards',
        businessImpact: {
          description: 'Difficult data comparison and potential for misanalysis',
          estimatedValue: 50000
        },
        recommendedAction: 'Implement standardized reporting templates and train finance staff',
        timeRange: {
          start: new Date(Date.now() - 90 * 24 * 60 * 60 * 1000), // 90 days ago
          end: new Date()
        }
      },
      {
        anomalyType: 'Regional Variance',
        description: `Unusual variance in sales performance within ${groupName} by region`,
        severity: 'medium',
        detectedAt: new Date(Date.now() - 3 * 24 * 60 * 60 * 1000), // 3 days ago
        anomalyScore: 72.1,
        affectedEntity: groupName,
        affectedMetric: 'Regional Sales Performance',
        businessImpact: {
          description: 'Uneven business performance with some regions significantly underperforming',
          estimatedValue: 580000
        },
        recommendedAction: 'Analyze regional market conditions and adjust strategies accordingly',
        timeRange: {
          start: new Date(Date.now() - 120 * 24 * 60 * 60 * 1000), // 120 days ago
          end: new Date()
        }
      },
      {
        anomalyType: 'Group-wide Revenue Drop',
        description: `Synchronized 18% revenue decrease across all dealers in ${groupName}`,
        severity: 'high',
        detectedAt: new Date(Date.now() - 2 * 24 * 60 * 60 * 1000), // 2 days ago
        anomalyScore: 91.5,
        affectedEntity: groupName,
        affectedMetric: 'Group Revenue',
        actualValue: 8200000,
        expectedValue: 10000000,
        threshold: 9000000,
        businessImpact: {
          description: 'Major financial impact affecting all dealerships simultaneously',
          estimatedValue: 1800000
        },
        recommendedAction: 'Urgent executive review to identify systemic issues affecting all locations',
        timeRange: {
          start: new Date(Date.now() - 30 * 24 * 60 * 60 * 1000), // 30 days ago
          end: new Date()
        }
      },
      {
        anomalyType: 'Reporting Delay',
        description: `Multiple dealers in ${groupName} showing unusual delays in submission timing`,
        severity: 'low',
        detectedAt: new Date(Date.now() - 6 * 24 * 60 * 60 * 1000), // 6 days ago
        anomalyScore: 45.6,
        affectedEntity: groupName,
        affectedMetric: 'Reporting Timeliness',
        recommendedAction: 'Review submission deadlines and process obstacles with dealership managers',
        timeRange: {
          start: new Date(Date.now() - 60 * 24 * 60 * 60 * 1000), // 60 days ago
          end: new Date()
        }
      },
      {
        anomalyType: 'Expense Ratio Anomaly',
        description: `The expense to revenue ratio for ${groupName} exceeds historical norms by 25%`,
        severity: 'medium',
        detectedAt: new Date(Date.now() - 5 * 24 * 60 * 60 * 1000), // 5 days ago
        anomalyScore: 78.9,
        affectedEntity: groupName,
        affectedMetric: 'Expense Ratio',
        actualValue: 0.42,
        expectedValue: 0.336,
        threshold: 0.37,
        businessImpact: {
          description: 'Reduced profitability across the group due to expense management issues',
          estimatedValue: 840000
        },
        recommendedAction: 'Conduct expense audit to identify inefficiencies and cost-saving opportunities',
        timeRange: {
          start: new Date(Date.now() - 365 * 24 * 60 * 60 * 1000), // 365 days ago
          end: new Date()
        }
      }
    ];
    
    this.anomalies.set(mockAnomalies);
  }
} 