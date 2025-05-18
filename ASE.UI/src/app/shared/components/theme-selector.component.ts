import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatIconModule } from '@angular/material/icon';
import { FormsModule } from '@angular/forms';
import { ThemeService, ThemeColor, ThemeMode } from '../../core/services/theme.service';

@Component({
  selector: 'app-theme-selector',
  standalone: true,
  imports: [
    CommonModule,
    MatButtonToggleModule,
    MatIconModule,
    FormsModule
  ],
  template: `
    <div class="theme-selector">
      <mat-button-toggle-group name="themeColor" [(ngModel)]="selectedTheme" (change)="onThemeChange()">
        <mat-button-toggle value="azure">Azure</mat-button-toggle>
        <mat-button-toggle value="cyan">Cyan</mat-button-toggle>
      </mat-button-toggle-group>
      
      <mat-button-toggle-group name="themeMode" [(ngModel)]="selectedMode" (change)="onThemeChange()">
        <mat-button-toggle value="light">
          <mat-icon>light_mode</mat-icon>
        </mat-button-toggle>
        <mat-button-toggle value="dark">
          <mat-icon>dark_mode</mat-icon>
        </mat-button-toggle>
      </mat-button-toggle-group>
    </div>
  `,
  styles: [`
    .theme-selector {
      display: flex;
      gap: 16px;
      align-items: center;
    }
  `]
})
export class ThemeSelectorComponent implements OnInit {
  private themeService = inject(ThemeService);
  
  selectedTheme: ThemeColor = 'azure';
  selectedMode: ThemeMode = 'light';
  
  ngOnInit(): void {
    this.themeService.currentTheme$.subscribe(theme => {
      this.selectedTheme = theme.color;
      this.selectedMode = theme.mode;
    });
  }
  
  onThemeChange(): void {
    this.themeService.setTheme(this.selectedTheme, this.selectedMode);
  }
} 