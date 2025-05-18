import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';

import { DealerService } from '../../core/services/dealer.service';
import { Dealer } from '../../core/models/dealer.model';

@Component({
  selector: 'app-dealer-details',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    ReactiveFormsModule,
    MatSnackBarModule
  ],
  template: `
    <div class="container">
      <div class="header-row">
        <h2>{{ isEditMode() ? 'Edit' : 'Create' }} Dealer</h2>
        
        <div class="action-buttons" *ngIf="isEditMode()">
          <button mat-raised-button color="accent" [routerLink]="['/dealers', dealerId(), 'patterns']">
            <mat-icon>insights</mat-icon> View Patterns
          </button>
          <button mat-raised-button color="warn" [routerLink]="['/dealers', dealerId(), 'anomalies']">
            <mat-icon>warning</mat-icon> View Anomalies
          </button>
        </div>
      </div>
      
      <mat-card>
        <mat-card-content>
          <form [formGroup]="dealerForm" (ngSubmit)="saveDealer()">
            <mat-form-field class="full-width">
              <mat-label>Name</mat-label>
              <input matInput formControlName="name" required>
              <mat-error *ngIf="dealerForm.get('name')?.hasError('required')">
                Name is required
              </mat-error>
            </mat-form-field>
            
            <mat-form-field class="full-width">
              <mat-label>Address</mat-label>
              <input matInput formControlName="address" required>
              <mat-error *ngIf="dealerForm.get('address')?.hasError('required')">
                Address is required
              </mat-error>
            </mat-form-field>
            
            <mat-form-field class="full-width">
              <mat-label>Email</mat-label>
              <input matInput formControlName="contactEmail" required type="email">
              <mat-error *ngIf="dealerForm.get('contactEmail')?.hasError('required')">
                Email is required
              </mat-error>
              <mat-error *ngIf="dealerForm.get('contactEmail')?.hasError('email')">
                Please enter a valid email address
              </mat-error>
            </mat-form-field>
            
            <mat-form-field class="full-width">
              <mat-label>Phone</mat-label>
              <input matInput formControlName="contactPhone" required>
              <mat-error *ngIf="dealerForm.get('contactPhone')?.hasError('required')">
                Phone is required
              </mat-error>
            </mat-form-field>
            
            <div class="button-row">
              <button mat-raised-button color="primary" type="submit" [disabled]="dealerForm.invalid">
                {{ isEditMode() ? 'Update' : 'Create' }}
              </button>
              <button mat-button type="button" (click)="cancel()">Cancel</button>
            </div>
          </form>
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: [`
    .container {
      padding: 20px;
    }

    .header-row {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 20px;
    }

    .action-buttons {
      display: flex;
      gap: 8px;
    }

    mat-card {
      margin-bottom: 20px;
    }

    .full-width {
      width: 100%;
      margin-bottom: 16px;
    }

    .button-row {
      display: flex;
      gap: 8px;
      margin-top: 8px;
    }
  `]
})
export class DealerDetailsComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private fb = inject(FormBuilder);
  private dealerService = inject(DealerService);
  private snackBar = inject(MatSnackBar);
  
  dealerId = signal<number | null>(null);
  isEditMode = signal<boolean>(false);
  
  dealerForm: FormGroup = this.fb.group({
    name: ['', Validators.required],
    address: ['', Validators.required],
    contactEmail: ['', [Validators.required, Validators.email]],
    contactPhone: ['', Validators.required]
  });
  
  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      const id = params.get('id');
      if (id && id !== 'new') {
        this.dealerId.set(parseInt(id, 10));
        this.isEditMode.set(true);
        this.loadDealer();
      }
    });
  }
  
  loadDealer(): void {
    if (this.dealerId()) {
      this.dealerService.getDealerById(this.dealerId()!).subscribe({
        next: (dealer: Dealer) => {
          this.dealerForm.patchValue({
            name: dealer.name,
            address: dealer.address,
            contactEmail: dealer.contactEmail,
            contactPhone: dealer.contactPhone
          });
        },
        error: (error) => {
          console.error('Error loading dealer:', error);
          this.snackBar.open('Error loading dealer details', 'Close', { duration: 3000 });
        }
      });
    }
  }
  
  saveDealer(): void {
    if (this.dealerForm.valid) {
      const dealer: Dealer = {
        id: this.dealerId() || 0,
        ...this.dealerForm.value
      };
      
      if (this.isEditMode()) {
        this.dealerService.updateDealer(this.dealerId()!, dealer).subscribe({
          next: () => {
            this.snackBar.open('Dealer updated successfully', 'Close', { duration: 3000 });
            this.router.navigate(['/dealers']);
          },
          error: (error) => {
            console.error('Error updating dealer:', error);
            this.snackBar.open('Error updating dealer', 'Close', { duration: 3000 });
          }
        });
      } else {
        this.dealerService.createDealer(dealer).subscribe({
          next: () => {
            this.snackBar.open('Dealer created successfully', 'Close', { duration: 3000 });
            this.router.navigate(['/dealers']);
          },
          error: (error) => {
            console.error('Error creating dealer:', error);
            this.snackBar.open('Error creating dealer', 'Close', { duration: 3000 });
          }
        });
      }
    }
  }
  
  cancel(): void {
    this.router.navigate(['/dealers']);
  }
} 