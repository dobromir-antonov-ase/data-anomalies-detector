import { Injectable, Renderer2, RendererFactory2, Inject } from '@angular/core';
import { DOCUMENT } from '@angular/common';
import { BehaviorSubject, Observable } from 'rxjs';

export type ThemeColor = 'azure' | 'cyan';
export type ThemeMode = 'light' | 'dark';

export interface Theme {
  color: ThemeColor;
  mode: ThemeMode;
}

@Injectable({
  providedIn: 'root'
})
export class ThemeService {
  private renderer: Renderer2;
  private currentTheme = new BehaviorSubject<Theme>({ color: 'azure', mode: 'light' });

  // Expose theme as an observable
  public currentTheme$: Observable<Theme> = this.currentTheme.asObservable();

  constructor(
    private rendererFactory: RendererFactory2,
    @Inject(DOCUMENT) private document: Document
  ) {
    this.renderer = this.rendererFactory.createRenderer(null, null);
    
    // Restore theme from local storage if it exists
    const savedThemeColor = localStorage.getItem('ase-theme-color') as ThemeColor || 'azure';
    const savedThemeMode = localStorage.getItem('ase-theme-mode') as ThemeMode || 'light';
    
    this.setTheme(savedThemeColor, savedThemeMode);
  }

  /**
   * Set the current theme
   */
  setTheme(color: ThemeColor, mode: ThemeMode): void {
    const newTheme = { color, mode };
    this.currentTheme.next(newTheme);
    
    localStorage.setItem('ase-theme-color', color);
    localStorage.setItem('ase-theme-mode', mode);
    
    // Remove all theme classes
    this.renderer.removeClass(this.document.body, 'azure-theme');
    this.renderer.removeClass(this.document.body, 'cyan-theme');
    this.renderer.removeClass(this.document.body, 'light-mode');
    this.renderer.removeClass(this.document.body, 'dark-mode');
    
    // Add the selected theme classes (color first, then mode)
    this.renderer.addClass(this.document.body, `${color}-theme`);
    
    // Only add dark-mode class if needed (light is default)
    if (mode === 'dark') {
      this.renderer.addClass(this.document.body, 'dark-mode');
    }
  }

  toggleThemeMode(): void {
    const current = this.currentTheme.value;
    const nextMode: ThemeMode = current.mode === 'light' ? 'dark' : 'light';
    this.setTheme(current.color, nextMode);
  }

  toggleThemeColor(): void {
    const current = this.currentTheme.value;
    const nextColor: ThemeColor = current.color === 'azure' ? 'cyan' : 'azure';
    this.setTheme(nextColor, current.mode);
  }

  /**
   * Get the current theme
   */
  getCurrentTheme(): Theme {
    return this.currentTheme.value;
  }
} 