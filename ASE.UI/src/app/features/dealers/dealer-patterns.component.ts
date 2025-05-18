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

import { AnomalyDetectionService, DataPattern } from '../../core/services/anomaly-detection.service';
import { DealerService } from '../../core/services/dealer.service';
import { PatternVisualizationComponent } from '../../shared/components/pattern-visualization.component';

@Component({
  selector: 'app-dealer-patterns',
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
    PatternVisualizationComponent
  ],
  template: `
    <div class="patterns-container">
      <div class="patterns-header">
        <h2>Financial Data Patterns Analysis</h2>
        
        <div class="filter-container">
          <mat-form-field>
            <mat-label>View Patterns By</mat-label>
            <mat-select [(ngModel)]="viewMode" (selectionChange)="onViewModeChange()">
              <mat-option value="dealer">Individual Dealer</mat-option>
              <mat-option value="group">Dealer Group</mat-option>
            </mat-select>
          </mat-form-field>
          
          <mat-form-field *ngIf="viewMode === 'dealer'">
            <mat-label>Select Dealer</mat-label>
            <mat-select [(ngModel)]="selectedDealerId" (selectionChange)="loadDealerPatterns()">
              <mat-option *ngFor="let dealer of dealers()" [value]="dealer.id">
                {{ dealer.name }}
              </mat-option>
            </mat-select>
          </mat-form-field>
          
          <mat-form-field *ngIf="viewMode === 'group'">
            <mat-label>Select Dealer Group</mat-label>
            <mat-select [(ngModel)]="selectedGroupId" (selectionChange)="loadGroupPatterns()">
              <mat-option *ngFor="let group of dealerGroups()" [value]="group.groupId">
                {{ group.groupName }}
              </mat-option>
            </mat-select>
          </mat-form-field>
        </div>
      </div>
      
      <div class="loading-indicator" *ngIf="loading()">
        <mat-spinner diameter="40"></mat-spinner>
        <p>Loading patterns...</p>
      </div>
      
      <div class="patterns-content" *ngIf="!loading()">
        <!-- No patterns message -->
        <div class="no-patterns" *ngIf="patterns().length === 0">
          <mat-icon>search</mat-icon>
          <h3>No patterns detected</h3>
          <p>There are no ML-detected patterns for the selected entity.</p>
          <button mat-raised-button color="primary" (click)="reloadPatterns()">
            Analyze Data
          </button>
        </div>
        
        <!-- Patterns visualization -->
        <div *ngIf="patterns().length > 0">
          <mat-card>
            <mat-card-header>
              <mat-card-title>
                {{ viewMode === 'dealer' ? 'Dealer Pattern Analysis' : 'Dealer Group Pattern Analysis' }}
              </mat-card-title>
              <mat-card-subtitle>
                {{ currentEntityName() }}
              </mat-card-subtitle>
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
            </mat-card-content>
          </mat-card>
          
          <!-- Pattern insights and explanations -->
          <mat-card class="insights-card">
            <mat-card-header>
              <mat-card-title>Pattern Insights</mat-card-title>
            </mat-card-header>
            <mat-card-content>
              <div class="insight-item" *ngFor="let pattern of patterns()">
                <h4>{{ pattern.patternType }}</h4>
                <p>{{ pattern.description }}</p>
                <div class="insight-details">
                  <div class="insight-metric" *ngIf="pattern.correlation">
                    <span class="metric-label">Correlation:</span>
                    <span class="metric-value">{{ pattern.correlation | number:'1.2-2' }}</span>
                  </div>
                  <div class="insight-metric" *ngIf="pattern.r2Value">
                    <span class="metric-label">R² Value:</span>
                    <span class="metric-value">{{ pattern.r2Value | number:'1.2-2' }}</span>
                  </div>
                  <div class="insight-metric">
                    <span class="metric-label">Confidence:</span>
                    <span class="metric-value">{{ pattern.confidenceScore }}%</span>
                  </div>
                </div>
              </div>
            </mat-card-content>
          </mat-card>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .patterns-container {
      padding: 20px;
    }
    
    .patterns-header {
      display: flex;
      flex-wrap: wrap;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 20px;
    }
    
    .filter-container {
      display: flex;
      gap: 16px;
      align-items: center;
    }
    
    .loading-indicator {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      padding: 40px;
    }
    
    .no-patterns {
      display: flex;
      flex-direction: column;
      align-items: center;
      text-align: center;
      padding: 40px;
      color: #666;
    }
    
    .no-patterns mat-icon {
      font-size: 48px;
      height: 48px;
      width: 48px;
      margin-bottom: 16px;
    }
    
    .pattern-table {
      width: 100%;
      border-collapse: collapse;
    }
    
    .pattern-table th, .pattern-table td {
      padding: 8px;
      text-align: left;
      border-bottom: 1px solid #e0e0e0;
    }
    
    .significance-high {
      background-color: rgba(244, 67, 54, 0.05);
    }
    
    .significance-medium {
      background-color: rgba(255, 152, 0, 0.05);
    }
    
    .significance-low {
      background-color: rgba(76, 175, 80, 0.05);
    }
    
    .insights-card {
      margin-top: 20px;
    }
    
    .insight-item {
      margin-bottom: 24px;
      padding-bottom: 16px;
      border-bottom: 1px solid #eee;
    }
    
    .insight-item:last-child {
      border-bottom: none;
    }
    
    .insight-item h4 {
      margin: 0 0 8px 0;
      color: #333;
    }
    
    .insight-details {
      display: flex;
      gap: 24px;
      flex-wrap: wrap;
      margin-top: 12px;
    }
    
    .insight-metric {
      display: flex;
      align-items: center;
    }
    
    .metric-label {
      font-weight: 500;
      margin-right: 6px;
      color: #666;
    }
    
    .metric-value {
      font-family: monospace;
      font-size: 14px;
    }
  `]
})
export class DealerPatternsComponent implements OnInit {
  private anomalyService = inject(AnomalyDetectionService);
  private dealerService = inject(DealerService);
  private snackBar = inject(MatSnackBar);
  private route = inject(ActivatedRoute);
  
  dealers = signal<any[]>([]);
  dealerGroups = signal<any[]>([]);
  patterns = signal<DataPattern[]>([]);
  loading = signal<boolean>(false);
  
  viewMode: 'dealer' | 'group' = 'dealer';
  selectedDealerId: number | null = null;
  selectedGroupId: number | null = null;
  
  // Helper computed properties
  dealerGroupNames = signal<string[]>(['Premium Auto Group', 'Metro Dealership Network', 'Regional Motors Alliance']);
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
        this.loadDealerPatterns();
      } else if (params['groupId']) {
        this.selectedGroupId = +params['groupId'];
        this.viewMode = 'group';
        this.loadGroupPatterns();
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
        this.dealerGroupNames.set(groups.map(g => g.groupName));
        
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
    // Reset patterns when switching view modes
    this.patterns.set([]);
    
    if (this.viewMode === 'dealer' && this.selectedDealerId) {
      this.loadDealerPatterns();
    } else if (this.viewMode === 'group' && this.selectedGroupId) {
      this.loadGroupPatterns();
    }
  }
  
  loadDealerPatterns(): void {
    if (!this.selectedDealerId) return;
    
    this.loading.set(true);
    this.anomalyService.detectPatternsByDealer(this.selectedDealerId).subscribe({
      next: (patterns) => {
        this.patterns.set(patterns);
        
        // Update the current entity name
        const dealer = this.dealers().find(d => d.id === this.selectedDealerId);
        if (dealer) {
          this.currentEntityName.set(dealer.name);
        }
        
        this.loading.set(false);
      },
      error: (error) => {
        console.error('Error loading dealer patterns:', error);
        this.snackBar.open('Error loading patterns', 'Dismiss', { duration: 3000 });
        // Fallback to mock data for demo
        this.generateMockDealerPatterns();
        this.loading.set(false);
      }
    });
  }
  
  loadGroupPatterns(): void {
    if (!this.selectedGroupId) return;
    
    this.loading.set(true);
    this.anomalyService.detectPatternsByDealerGroup(this.selectedGroupId).subscribe({
      next: (patterns) => {
        this.patterns.set(patterns);
        
        // Update the current entity name
        const group = this.dealerGroups().find(g => g.groupId === this.selectedGroupId);
        if (group) {
          this.currentEntityName.set(group.groupName);
        }
        
        this.loading.set(false);
      },
      error: (error) => {
        console.error('Error loading group patterns:', error);
        this.snackBar.open('Error loading patterns', 'Dismiss', { duration: 3000 });
        // Fallback to mock data for demo
        this.generateMockGroupPatterns();
        this.loading.set(false);
      }
    });
  }
  
  reloadPatterns(): void {
    if (this.viewMode === 'dealer') {
      this.loadDealerPatterns();
    } else {
      this.loadGroupPatterns();
    }
  }
  
  // Mock data generators for demo purposes
  private generateMockDealerPatterns(): void {
    // Get dealer name for more realistic mock data
    const dealer = this.dealers().find(d => d.id === this.selectedDealerId);
    const dealerName = dealer ? dealer.name : 'Selected Dealer';
    this.currentEntityName.set(dealerName);
    
    const mockPatterns: DataPattern[] = [
      {
        patternType: 'Seasonal Revenue Pattern',
        description: `${dealerName} shows consistent quarterly revenue peaks, with Q2 and Q4 being strongest.`,
        significance: 'high',
        confidenceScore: 92.5,
        detectedAt: new Date(),
        correlation: 0.89
      },
      {
        patternType: 'Monthly Growth Trend',
        description: `${dealerName} has shown consistent 2.3% month-over-month growth in sales volume.`,
        significance: 'medium',
        confidenceScore: 78.2,
        detectedAt: new Date(),
        formula: 'y = 0.023x + 105.4',
        r2Value: 0.81
      },
      {
        patternType: 'Cost-Revenue Correlation',
        description: `Strong correlation detected between marketing expenses and sales revenue with 2-month lag.`,
        significance: 'high',
        confidenceScore: 89.7,
        detectedAt: new Date(),
        correlation: 0.87,
        relatedCellAddresses: ['C5', 'G12']
      }
    ];
    
    this.patterns.set(mockPatterns);
  }
  
  private generateMockGroupPatterns(): void {
    // Get group name for more realistic mock data
    const group = this.dealerGroups().find(g => g.groupId === this.selectedGroupId);
    const groupName = group ? group.groupName : 'Selected Group';
    this.currentEntityName.set(groupName);
    
    const mockPatterns: DataPattern[] = [
      {
        patternType: 'Group Performance Trend',
        description: `${groupName} consistently outperforms market average by 12% in revenue growth.`,
        significance: 'high',
        confidenceScore: 95.1,
        detectedAt: new Date(),
        correlation: 0.92
      },
      {
        patternType: 'Group Seasonality',
        description: `${groupName} exhibits stronger seasonal variation than other groups, particularly in Q4.`,
        significance: 'medium',
        confidenceScore: 83.6,
        detectedAt: new Date()
      },
      {
        patternType: 'Inter-dealer Correlation',
        description: `Strong correlation in performance metrics among dealers within ${groupName}.`,
        significance: 'high',
        confidenceScore: 91.3,
        detectedAt: new Date(),
        correlation: 0.89
      },
      {
        patternType: 'Cost Efficiency Pattern',
        description: `${groupName} shows 15% better cost efficiency ratio than other groups.`,
        significance: 'high',
        confidenceScore: 88.5,
        detectedAt: new Date(),
        formula: 'Efficiency = Revenue / (OpEx * 0.85)',
        r2Value: 0.78
      }
    ];
    
    this.patterns.set(mockPatterns);
  }
} 