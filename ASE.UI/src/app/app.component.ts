import { Component } from '@angular/core';
import { MainLayoutComponent } from './core/layout';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    MainLayoutComponent
  ],
  template: `<app-main-layout></app-main-layout>`,
  styles: []
})
export class AppComponent {} 