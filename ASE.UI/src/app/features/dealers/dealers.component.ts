import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatTooltipModule } from '@angular/material/tooltip';

import { DealerService } from '../../core/services/dealer.service';
import { Dealer } from '../../core/models/dealer.model';
import { ConfirmDialogComponent } from '../../shared/components/confirm-dialog.component';

@Component({
  selector: 'app-dealers',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatCardModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatSnackBarModule,
    MatDialogModule,
    MatTooltipModule
  ],
  template: `
    <div class="container">
      <div class="header-container">
        <h2>Dealers</h2>
        <button mat-raised-button color="primary" [routerLink]="['/dealers/new']">
          <mat-icon>add</mat-icon> Add Dealer
        </button>
      </div>
      
      <mat-card>
        <mat-card-content>
          <table mat-table [dataSource]="dealers()" class="mat-elevation-z8">
            <ng-container matColumnDef="id">
              <th mat-header-cell *matHeaderCellDef>ID</th>
              <td mat-cell *matCellDef="let dealer">{{dealer.id}}</td>
            </ng-container>
            
            <ng-container matColumnDef="name">
              <th mat-header-cell *matHeaderCellDef>Name</th>
              <td mat-cell *matCellDef="let dealer">{{dealer.name}}</td>
            </ng-container>
            
            <ng-container matColumnDef="address">
              <th mat-header-cell *matHeaderCellDef>Address</th>
              <td mat-cell *matCellDef="let dealer">{{dealer.address}}</td>
            </ng-container>
            
            <ng-container matColumnDef="contactEmail">
              <th mat-header-cell *matHeaderCellDef>Email</th>
              <td mat-cell *matCellDef="let dealer">{{dealer.contactEmail}}</td>
            </ng-container>
            
            <ng-container matColumnDef="contactPhone">
              <th mat-header-cell *matHeaderCellDef>Phone</th>
              <td mat-cell *matCellDef="let dealer">{{dealer.contactPhone}}</td>
            </ng-container>
            
            <ng-container matColumnDef="actions">
              <th mat-header-cell *matHeaderCellDef>Actions</th>
              <td mat-cell *matCellDef="let dealer">
                <button mat-icon-button [routerLink]="['/dealers', dealer.id]" matTooltip="Edit">
                  <mat-icon>edit</mat-icon>
                </button>
                <button mat-icon-button color="warn" (click)="confirmDelete(dealer)" matTooltip="Delete">
                  <mat-icon>delete</mat-icon>
                </button>
              </td>
            </ng-container>
            
            <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
            <tr mat-row *matRowDef="let row; columns: displayedColumns;"></tr>
          </table>
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: [`
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
  `]
})
export class DealersComponent implements OnInit {
  private dealerService = inject(DealerService);
  private snackBar = inject(MatSnackBar);
  private dialog = inject(MatDialog);
  
  dealers = signal<Dealer[]>([]);
  displayedColumns: string[] = ['id', 'name', 'address', 'contactEmail', 'contactPhone', 'actions'];
  
  ngOnInit(): void {
    this.loadDealers();
  }
  
  loadDealers(): void {
    this.dealerService.getAllDealers().subscribe({
      next: (data) => {
        this.dealers.set(data);
      },
      error: (error) => console.error('Error loading dealers:', error)
    });
  }
  
  confirmDelete(dealer: Dealer): void {
    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      width: '350px',
      data: {
        title: 'Confirm Delete',
        message: `Are you sure you want to delete dealer "${dealer.name}"?`,
        confirmButtonText: 'Delete'
      }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.deleteDealer(dealer.id);
      }
    });
  }
  
  deleteDealer(id: number): void {
    this.dealerService.deleteDealer(id).subscribe({
      next: () => {
        this.snackBar.open('Dealer deleted successfully', 'Close', { duration: 3000 });
        this.loadDealers(); // Reload the list
      },
      error: (error) => {
        console.error('Error deleting dealer:', error);
        this.snackBar.open('Error deleting dealer', 'Close', { duration: 3000 });
      }
    });
  }
} 