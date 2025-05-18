import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class ApiConfigService {
  // API base URL that can be configured for different environments
  private _baseUrl: string;
  
  constructor() {
    // For GitHub Pages deployment, API might be hosted elsewhere
    if (window.location.href.includes('github.io')) {
      // Replace with your production/deployed API URL
      this._baseUrl = 'https://your-deployed-api-url.com/api';
    } else {
      // Development environment
      this._baseUrl = 'http://localhost:5034/api';
    }
  }
  
  get baseUrl(): string {
    return this._baseUrl;
  }
} 