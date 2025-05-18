import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal, ViewChild, ElementRef, AfterViewInit, NgZone } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { RouterModule } from '@angular/router';
import { ScrollingModule } from '@angular/cdk/scrolling';

import { FinanceSubmission } from '../../core/models/finance-submission.model';
import { FinanceSubmissionService, PaginatedResult } from '../../core/services/finance-submission.service';

@Component({
  selector: 'app-finance-submissions',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatCardModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatTooltipModule,
    MatSnackBarModule,
    MatDialogModule,
    MatProgressSpinnerModule,
    ScrollingModule
  ],
  template: `
    <div class="container">
      <div class="header-container">
        <h2>Finance Submissions</h2>
        <button mat-raised-button color="primary" [routerLink]="['/submissions/new']">
          <mat-icon>add</mat-icon> Add Submission
        </button>
      </div>
      
      <mat-card>
        <mat-card-content>
          <div #scrollContainer class="submissions-container" (scroll)="onScroll($event)">
            <table mat-table [dataSource]="submissions()" class="mat-elevation-z8">
              <ng-container matColumnDef="id">
                <th mat-header-cell *matHeaderCellDef>ID</th>
                <td mat-cell *matCellDef="let submission">{{submission.id}}</td>
              </ng-container>
              
              <ng-container matColumnDef="title">
                <th mat-header-cell *matHeaderCellDef>Title</th>
                <td mat-cell *matCellDef="let submission">{{submission.title}}</td>
              </ng-container>
              
              <ng-container matColumnDef="dealerId">
                <th mat-header-cell *matHeaderCellDef>Dealer ID</th>
                <td mat-cell *matCellDef="let submission">{{submission.dealerId}}</td>
              </ng-container>
              
              <ng-container matColumnDef="month">
                <th mat-header-cell *matHeaderCellDef>Month</th>
                <td mat-cell *matCellDef="let submission">{{getMonthName(submission.month)}}</td>
              </ng-container>
              
              <ng-container matColumnDef="year">
                <th mat-header-cell *matHeaderCellDef>Year</th>
                <td mat-cell *matCellDef="let submission">{{submission.year}}</td>
              </ng-container>
              
              <ng-container matColumnDef="status">
                <th mat-header-cell *matHeaderCellDef>Status</th>
                <td mat-cell *matCellDef="let submission">{{submission.status}}</td>
              </ng-container>
              
              <ng-container matColumnDef="actions">
                <th mat-header-cell *matHeaderCellDef>Actions</th>
                <td mat-cell *matCellDef="let submission">
                  <button mat-icon-button [routerLink]="['/submissions', submission.id]" matTooltip="Edit">
                    <mat-icon>edit</mat-icon>
                  </button>
                </td>
              </ng-container>
              
              <tr mat-header-row *matHeaderRowDef="displayedColumns; sticky: true"></tr>
              <tr mat-row *matRowDef="let row; columns: displayedColumns;"></tr>
            </table>
            
            <div *ngIf="loading()" class="loading-container">
              <mat-spinner diameter="40"></mat-spinner>
              <p>Loading more submissions...</p>
            </div>
            
            <div *ngIf="noMoreData()" class="no-more-data">
              <p>No more submissions to load</p>
            </div>
          </div>
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: [`
    table {
      width: 100%;
    }
    
    mat-card {
      margin-bottom: 20px;
    }
    
    .header-container {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 20px;
    }
    
    .actions-cell {
      display: flex;
      gap: 8px;
    }
    
    .submissions-container {
      height: 65vh;
      overflow-y: auto;
      position: relative;
    }
    
    .loading-container {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      padding: 20px;
    }
    
    .no-more-data {
      text-align: center;
      padding: 20px;
      color: #888;
    }
  `]
})
export class FinanceSubmissionsComponent implements OnInit, AfterViewInit {
  private submissionService = inject(FinanceSubmissionService);
  private snackBar = inject(MatSnackBar);
  private ngZone = inject(NgZone);
  
  @ViewChild('scrollContainer') scrollContainer!: ElementRef<HTMLDivElement>;
  
  submissions = signal<FinanceSubmission[]>([]);
  displayedColumns: string[] = ['id', 'title', 'dealerId', 'month', 'year', 'status', 'actions'];
  loading = signal<boolean>(false);
  noMoreData = signal<boolean>(false);
  
  // Pagination
  currentPage = 1;
  pageSize = 20;
  totalItems = 0;
  
  months: string[] = [
    'January', 'February', 'March', 'April', 'May', 'June', 
    'July', 'August', 'September', 'October', 'November', 'December'
  ];
  
  ngOnInit(): void {
    this.loadSubmissions();
  }
  
  ngAfterViewInit(): void {
    // Initial setup is done in onInit
  }
  
  loadSubmissions(append: boolean = false): void {
    if (this.loading()) return;
    
    this.loading.set(true);
    
    this.submissionService.getPaginatedSubmissions(this.currentPage, this.pageSize).subscribe({
      next: (result: PaginatedResult<FinanceSubmission>) => {
        if (append) {
          this.submissions.update(submissions => [...submissions, ...result.items]);
        } else {
          this.submissions.set(result.items);
        }
        
        this.totalItems = result.totalCount;
        this.noMoreData.set(!result.hasMore);
        this.loading.set(false);
      },
      error: (error) => {
        console.error('Error loading submissions:', error);
        this.snackBar.open('Error loading submissions', 'Close', { duration: 3000 });
        this.loading.set(false);
        
        // For demo purposes, use the regular endpoint as fallback
        if (!append) {
          this.loadAllSubmissions();
        }
      }
    });
  }
  
  loadAllSubmissions(): void {
    this.submissionService.getAllSubmissions().subscribe({
      next: (data) => {
        this.submissions.set(data);
        this.noMoreData.set(true); // No more pagination with this fallback
      },
      error: (error) => console.error('Error loading all submissions:', error)
    });
  }
  
  onScroll(event: Event): void {
    if (this.loading() || this.noMoreData()) return;
    
    const element = event.target as HTMLElement;
    const scrollPosition = element.scrollTop + element.clientHeight;
    const scrollHeight = element.scrollHeight;
    
    // Load more when user scrolls to 80% of the visible content
    if (scrollPosition > scrollHeight * 0.8) {
      this.ngZone.run(() => {
        this.currentPage++;
        this.loadSubmissions(true);
      });
    }
  }
  
  getMonthName(monthNumber: number): string {
    return this.months[monthNumber - 1] || '';
  }
} 