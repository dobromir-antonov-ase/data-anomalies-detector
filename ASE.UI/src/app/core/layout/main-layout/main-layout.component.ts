import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatListModule } from '@angular/material/list';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatMenuModule } from '@angular/material/menu';
import { ThemeService, ThemeColor, ThemeMode } from '../../services/theme.service';
import { ThemeSelectorComponent } from '../../../shared/components/theme-selector.component';

@Component({
  selector: 'app-main-layout',
  standalone: true,
  imports: [
    CommonModule,
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    MatToolbarModule,
    MatButtonModule,
    MatSidenavModule,
    MatListModule,
    MatIconModule,
    MatTooltipModule,
    MatMenuModule,
    ThemeSelectorComponent
  ],
  template: `
    <div class="app-container">
      <mat-toolbar color="primary" class="app-toolbar">
        <button mat-icon-button (click)="toggleSidenav()">
          <mat-icon>menu</mat-icon>
        </button>
        <span>Automotive Sales Engineering</span>
        <span class="toolbar-spacer"></span>
        <app-theme-selector></app-theme-selector>
      </mat-toolbar>

      <mat-sidenav-container class="sidenav-container">
        <mat-sidenav #sidenav mode="side" [opened]="sidenavOpened" class="sidenav">
          <mat-nav-list>
            <a mat-list-item routerLink="/dashboard" routerLinkActive="active-link">
              <mat-icon>dashboard</mat-icon> Dashboard
            </a>
            <a mat-list-item routerLink="/dealers" routerLinkActive="active-link">
              <mat-icon>store</mat-icon> Dealers
            </a>
            <a mat-list-item routerLink="/submissions" routerLinkActive="active-link">
              <mat-icon>description</mat-icon> Finance Submissions
            </a>
            <a mat-list-item routerLink="/templates" routerLinkActive="active-link">
              <mat-icon>grid_on</mat-icon> Master Templates
            </a>
            <a mat-list-item routerLink="/patterns" routerLinkActive="active-link">
              <mat-icon>insights</mat-icon> Data Patterns
            </a>
            <a mat-list-item routerLink="/anomalies" routerLinkActive="active-link">
              <mat-icon>warning</mat-icon> Data Anomalies
            </a>
            <a mat-list-item routerLink="/query-builder" routerLinkActive="active-link">
              <mat-icon>search</mat-icon> AI Query Builder
            </a>
          </mat-nav-list>
        </mat-sidenav>
        
        <mat-sidenav-content>
          <div class="content-container">
            <router-outlet></router-outlet>
          </div>
        </mat-sidenav-content>
      </mat-sidenav-container>
    </div>
  `,
  styles: [`
    .app-container {
      display: flex;
      flex-direction: column;
      height: 100vh;
    }
    
    .app-toolbar {
      position: fixed;
      top: 0;
      left: 0;
      right: 0;
      z-index: 2;
    }
    
    .toolbar-spacer {
      flex: 1 1 auto;
    }
    
    .sidenav-container {
      flex: 1;
      margin-top: 64px;
    }
    
    .sidenav {
      width: 250px;
    }
    
    .active-link {
      background-color: rgba(0, 0, 0, 0.04);
    }
    
    mat-icon {
      margin-right: 8px;
    }
  `]
})
export class MainLayoutComponent {
  private themeService = inject(ThemeService);
  sidenavOpened = true;
  
  toggleSidenav(): void {
    this.sidenavOpened = !this.sidenavOpened;
  }
} 