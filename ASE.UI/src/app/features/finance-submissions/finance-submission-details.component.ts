import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatTableModule } from '@angular/material/table';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatTabsModule } from '@angular/material/tabs';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators, FormArray } from '@angular/forms';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';

import { FinanceSubmissionService } from '../../core/services/finance-submission.service';
import { DealerService } from '../../core/services/dealer.service';
import { FinanceSubmission, FinanceSubmissionCell } from '../../core/models/finance-submission.model';
import { Dealer } from '../../core/models/dealer.model';

@Component({
  selector: 'app-finance-submission-details',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatSelectModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatTableModule,
    MatExpansionModule,
    MatTabsModule,
    ReactiveFormsModule,
    MatSnackBarModule
  ],
  template: `
    <div class="container">
      <h2>{{ isEditMode() ? 'Edit' : 'Create' }} Finance Submission</h2>
      
      <mat-card>
        <mat-card-content>
          <mat-tab-group>
            <mat-tab label="Submission Details">
              <form [formGroup]="submissionForm" (ngSubmit)="saveSubmission()" class="tab-content">
                <mat-form-field class="full-width">
                  <mat-label>Title</mat-label>
                  <input matInput formControlName="title" required>
                  <mat-error *ngIf="submissionForm.get('title')?.hasError('required')">
                    Title is required
                  </mat-error>
                </mat-form-field>
                
                <div class="form-row">
                  <mat-form-field class="form-field">
                    <mat-label>Month</mat-label>
                    <mat-select formControlName="month" required>
                      <mat-option *ngFor="let month of months; let i = index" [value]="i + 1">
                        {{ month }}
                      </mat-option>
                    </mat-select>
                    <mat-error *ngIf="submissionForm.get('month')?.hasError('required')">
                      Month is required
                    </mat-error>
                  </mat-form-field>
                  
                  <mat-form-field class="form-field">
                    <mat-label>Year</mat-label>
                    <mat-select formControlName="year" required>
                      <mat-option *ngFor="let year of years" [value]="year">
                        {{ year }}
                      </mat-option>
                    </mat-select>
                    <mat-error *ngIf="submissionForm.get('year')?.hasError('required')">
                      Year is required
                    </mat-error>
                  </mat-form-field>
                </div>
                
                <mat-form-field class="full-width">
                  <mat-label>Dealer</mat-label>
                  <mat-select formControlName="dealerId" required>
                    <mat-option *ngFor="let dealer of dealers()" [value]="dealer.id">
                      {{ dealer.name }}
                    </mat-option>
                  </mat-select>
                  <mat-error *ngIf="submissionForm.get('dealerId')?.hasError('required')">
                    Dealer is required
                  </mat-error>
                </mat-form-field>
                
                <mat-form-field class="full-width">
                  <mat-label>Status</mat-label>
                  <mat-select formControlName="status" required>
                    <mat-option value="Draft">Draft</mat-option>
                    <mat-option value="Submitted">Submitted</mat-option>
                    <mat-option value="Approved">Approved</mat-option>
                    <mat-option value="Rejected">Rejected</mat-option>
                  </mat-select>
                  <mat-error *ngIf="submissionForm.get('status')?.hasError('required')">
                    Status is required
                  </mat-error>
                </mat-form-field>
                
                <div class="button-row">
                  <button mat-raised-button color="primary" type="submit" [disabled]="submissionForm.invalid">
                    {{ isEditMode() ? 'Update' : 'Create' }}
                  </button>
                  <button mat-button type="button" (click)="cancel()">Cancel</button>
                </div>
              </form>
            </mat-tab>
            
            <mat-tab label="Cell Values" *ngIf="isEditMode()">
              <div class="tab-content">
                <div class="cells-header">
                  <h3>Submission Cell Values</h3>
                  <div class="cells-actions">
                    <button mat-raised-button color="primary" 
                            (click)="saveCellChanges()" 
                            [disabled]="!hasModifiedCells()">
                      Save Cell Changes
                    </button>
                  </div>
                </div>
                
                <div class="cells-section">
                  <div class="cells-table">
                    <form [formGroup]="cellsForm">
                      <table class="cell-table">
                        <thead>
                          <tr>
                            <th>Cell Address</th>
                            <th>Value</th>
                            <th>Aggregation Type</th>
                          </tr>
                        </thead>
                        <tbody formArrayName="cells">
                          <tr *ngFor="let cell of cellsArray.controls; let i = index" 
                              [formGroupName]="i"
                              [class.modified]="isCellModified(i)">
                            <td>{{ getCellAddress(i) }}</td>
                            <td>
                              <mat-form-field class="cell-value-field">
                                <input matInput type="number" formControlName="value">
                              </mat-form-field>
                            </td>
                            <td>{{ getCellAggregationType(i) }}</td>
                          </tr>
                        </tbody>
                      </table>
                    </form>
                  </div>
                  
                  <div class="aggregation-panel">
                    <mat-card>
                      <mat-card-header>
                        <mat-card-title>Summary</mat-card-title>
                      </mat-card-header>
                      <mat-card-content>
                        <div class="aggregation-item">
                          <span>Total:</span>
                          <strong>{{ calculateTotal() }}</strong>
                        </div>
                        <div class="aggregation-item">
                          <span>Average:</span>
                          <strong>{{ calculateAverage() }}</strong>
                        </div>
                        <div class="aggregation-item">
                          <span>Min:</span>
                          <strong>{{ calculateMin() }}</strong>
                        </div>
                        <div class="aggregation-item">
                          <span>Max:</span>
                          <strong>{{ calculateMax() }}</strong>
                        </div>
                      </mat-card-content>
                    </mat-card>
                  </div>
                </div>
              </div>
            </mat-tab>
          </mat-tab-group>
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: [`
    mat-card {
      margin-bottom: 20px;
    }

    .full-width {
      width: 100%;
      margin-bottom: 15px;
    }

    .form-row {
      display: flex;
      gap: 16px;
      margin-bottom: 15px;
    }

    .form-field {
      flex: 1;
    }

    .button-row {
      display: flex;
      gap: 10px;
      margin-top: 20px;
    }
    
    .tab-content {
      padding: 20px 0;
    }
    
    .cells-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 20px;
    }
    
    .cells-section {
      display: flex;
      gap: 20px;
    }
    
    .cells-table {
      flex: 3;
    }
    
    .aggregation-panel {
      flex: 1;
    }
    
    .cell-table {
      width: 100%;
      border-collapse: collapse;
    }
    
    .cell-table th, .cell-table td {
      padding: 8px;
      border-bottom: 1px solid #e0e0e0;
      text-align: left;
    }
    
    .cell-value-field {
      width: 100px;
    }
    
    .modified {
      background-color: rgba(25, 118, 210, 0.05);
    }
    
    .aggregation-item {
      display: flex;
      justify-content: space-between;
      margin-bottom: 10px;
    }
  `]
})
export class FinanceSubmissionDetailsComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private fb = inject(FormBuilder);
  private submissionService = inject(FinanceSubmissionService);
  private dealerService = inject(DealerService);
  private snackBar = inject(MatSnackBar);
  
  submissionId = signal<number | null>(null);
  isEditMode = signal<boolean>(false);
  dealers = signal<Dealer[]>([]);
  originalCellValues: number[] = [];
  
  months: string[] = [
    'January', 'February', 'March', 'April', 'May', 'June', 
    'July', 'August', 'September', 'October', 'November', 'December'
  ];
  
  years: number[] = [];
  
  submissionForm: FormGroup = this.fb.group({
    title: ['', Validators.required],
    month: ['', Validators.required],
    year: ['', Validators.required],
    dealerId: ['', Validators.required],
    status: ['Draft', Validators.required]
  });
  
  cellsForm: FormGroup = this.fb.group({
    cells: this.fb.array([])
  });
  
  get cellsArray() {
    return this.cellsForm.get('cells') as FormArray;
  }
  
  ngOnInit(): void {
    // Generate year options (current year - 5 to current year + 5)
    const currentYear = new Date().getFullYear();
    for (let i = currentYear - 5; i <= currentYear + 5; i++) {
      this.years.push(i);
    }
    
    // Set default year and month
    this.submissionForm.patchValue({
      year: currentYear,
      month: new Date().getMonth() + 1
    });
    
    // Load dealers for dropdown
    this.loadDealers();
    
    // Check if we're in edit mode
    this.route.paramMap.subscribe(params => {
      const id = params.get('id');
      if (id && id !== 'new') {
        this.submissionId.set(parseInt(id, 10));
        this.isEditMode.set(true);
        this.loadSubmission();
      }
    });
  }
  
  loadDealers(): void {
    this.dealerService.getAllDealers().subscribe({
      next: (data) => {
        this.dealers.set(data);
      },
      error: (error) => {
        console.error('Error loading dealers:', error);
        this.snackBar.open('Error loading dealers', 'Close', { duration: 3000 });
      }
    });
  }
  
  loadSubmission(): void {
    if (this.submissionId()) {
      this.submissionService.getSubmissionById(this.submissionId()!).subscribe({
        next: (submission: FinanceSubmission) => {
          this.submissionForm.patchValue({
            title: submission.title,
            month: submission.month,
            year: submission.year,
            dealerId: submission.dealerId,
            status: submission.status
          });
          
          // Load cells into form array if they exist
          if (submission.cells && submission.cells.length > 0) {
            this.loadCellsToForm(submission.cells);
          }
        },
        error: (error) => {
          console.error('Error loading submission:', error);
          this.snackBar.open('Error loading submission details', 'Close', { duration: 3000 });
        }
      });
    }
  }
  
  loadCellsToForm(cells: FinanceSubmissionCell[]): void {
    // Clear existing form array
    while (this.cellsArray.length !== 0) {
      this.cellsArray.removeAt(0);
    }
    
    // Store original values for comparison
    this.originalCellValues = cells.map(cell => cell.value);
    
    // Add each cell to form array
    cells.forEach(cell => {
      this.cellsArray.push(
        this.fb.group({
          id: [cell.id],
          globalAddress: [cell.globalAddress],
          value: [cell.value, [Validators.required]],
          aggregationType: [cell.aggregationType]
        })
      );
    });
  }
  
  saveSubmission(): void {
    if (this.submissionForm.valid) {
      // Get current date
      const currentDate = new Date().toISOString();
      
      const submission: Partial<FinanceSubmission> = {
        id: this.submissionId() || 0,
        ...this.submissionForm.value,
        submissionDate: currentDate,
        masterTemplateId: 1 // Default to template ID 1 for now
      };
      
      if (this.isEditMode()) {
        // If cells have been modified, include them in the update
        if (this.hasModifiedCells()) {
          submission.cells = this.getCellsFromForm();
        }
        
        this.submissionService.updateSubmission(this.submissionId()!, submission).subscribe({
          next: () => {
            this.snackBar.open('Submission updated successfully', 'Close', { duration: 3000 });
            this.router.navigate(['/submissions']);
          },
          error: (error) => {
            console.error('Error updating submission:', error);
            this.snackBar.open('Error updating submission', 'Close', { duration: 3000 });
          }
        });
      } else {
        this.submissionService.createSubmission(submission).subscribe({
          next: () => {
            this.snackBar.open('Submission created successfully', 'Close', { duration: 3000 });
            this.router.navigate(['/submissions']);
          },
          error: (error) => {
            console.error('Error creating submission:', error);
            this.snackBar.open('Error creating submission', 'Close', { duration: 3000 });
          }
        });
      }
    }
  }
  
  saveCellChanges(): void {
    if (this.isEditMode() && this.cellsForm.valid) {
      const updatedSubmission: Partial<FinanceSubmission> = {
        id: this.submissionId()!,
        cells: this.getCellsFromForm()
      };
      
      this.submissionService.updateSubmission(this.submissionId()!, updatedSubmission).subscribe({
        next: () => {
          // Update original values to match current values
          this.originalCellValues = this.cellsArray.controls.map(control => 
            control.get('value')?.value
          );
          
          this.snackBar.open('Cell values updated successfully', 'Close', { duration: 3000 });
        },
        error: (error) => {
          console.error('Error updating cell values:', error);
          this.snackBar.open('Error updating cell values', 'Close', { duration: 3000 });
        }
      });
    }
  }
  
  getCellsFromForm(): FinanceSubmissionCell[] {
    return this.cellsArray.controls.map((control, index) => {
      return {
        id: control.get('id')?.value,
        globalAddress: control.get('globalAddress')?.value,
        value: control.get('value')?.value,
        aggregationType: control.get('aggregationType')?.value
      };
    });
  }
  
  isCellModified(index: number): boolean {
    const currentValue = this.cellsArray.at(index).get('value')?.value;
    const originalValue = this.originalCellValues[index];
    return currentValue !== originalValue;
  }
  
  hasModifiedCells(): boolean {
    return this.cellsArray.controls.some((control, index) => 
      this.isCellModified(index)
    );
  }
  
  getCellAddress(index: number): string {
    return this.cellsArray.at(index).get('globalAddress')?.value || '';
  }
  
  getCellAggregationType(index: number): string {
    return this.cellsArray.at(index).get('aggregationType')?.value || '';
  }
  
  calculateTotal(): number {
    return this.cellsArray.controls.reduce((sum, control) => 
      sum + (control.get('value')?.value || 0), 0
    );
  }
  
  calculateAverage(): number {
    const total = this.calculateTotal();
    const count = this.cellsArray.length;
    return count > 0 ? total / count : 0;
  }
  
  calculateMin(): number {
    if (this.cellsArray.length === 0) return 0;
    return Math.min(...this.cellsArray.controls.map(control => 
      control.get('value')?.value || 0
    ));
  }
  
  calculateMax(): number {
    if (this.cellsArray.length === 0) return 0;
    return Math.max(...this.cellsArray.controls.map(control => 
      control.get('value')?.value || 0
    ));
  }
  
  cancel(): void {
    this.router.navigate(['/submissions']);
  }
} 