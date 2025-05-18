import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

import { MasterTemplateService } from '../../core/services/master-template.service';
import { MasterTemplate } from '../../core/models/master-template.model';

@Component({
  selector: 'app-master-templates',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatCardModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule
  ],
  template: `
    <div class="container">
      <h2>Master Templates</h2>
      
      <mat-card>
        <mat-card-content>
          <table mat-table [dataSource]="templates()" class="mat-elevation-z8">
            <ng-container matColumnDef="id">
              <th mat-header-cell *matHeaderCellDef>ID</th>
              <td mat-cell *matCellDef="let template">{{template.id}}</td>
            </ng-container>
            
            <ng-container matColumnDef="name">
              <th mat-header-cell *matHeaderCellDef>Name</th>
              <td mat-cell *matCellDef="let template">{{template.name}}</td>
            </ng-container>
            
            <ng-container matColumnDef="year">
              <th mat-header-cell *matHeaderCellDef>Year</th>
              <td mat-cell *matCellDef="let template">{{template.year}}</td>
            </ng-container>
            
            <ng-container matColumnDef="isActive">
              <th mat-header-cell *matHeaderCellDef>Active</th>
              <td mat-cell *matCellDef="let template">{{template.isActive ? 'Yes' : 'No'}}</td>
            </ng-container>
            
            <ng-container matColumnDef="sheetCount">
              <th mat-header-cell *matHeaderCellDef>Sheets</th>
              <td mat-cell *matCellDef="let template">{{template.sheetCount}}</td>
            </ng-container>
            
            <ng-container matColumnDef="cellCount">
              <th mat-header-cell *matHeaderCellDef>Cells</th>
              <td mat-cell *matCellDef="let template">{{template.cellCount}}</td>
            </ng-container>
            
            <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
            <tr mat-row *matRowDef="let row; columns: displayedColumns;"></tr>
          </table>
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
    
    mat-card-actions {
      padding: 16px;
    }
  `]
})
export class MasterTemplatesComponent implements OnInit {
  private templateService = inject(MasterTemplateService);
  
  templates = signal<MasterTemplate[]>([]);
  displayedColumns: string[] = ['id', 'name', 'year', 'isActive', 'sheetCount', 'cellCount'];
  
  ngOnInit(): void {
    this.loadTemplates();
  }
  
  loadTemplates(): void {
    this.templateService.getAllMasterTemplates().subscribe({
      next: (data) => {
        this.templates.set(data);
      },
      error: (error) => console.error('Error loading templates:', error)
    });
  }
} 