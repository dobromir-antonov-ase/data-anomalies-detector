import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class ApiConfigService {
  // API base URL that can be configured for different environments
  private _baseUrl: string;
  
  constructor() {
    // For GitHub Pages deployment, API is hosted on Azure App Service
    if (window.location.href.includes('github.io')) {
      // Azure App Service URL
      this._baseUrl = 'https://data-anomaly-api-befrcvgeesfnbge8.westeurope-01.azurewebsites.net/api';
    } else {
      // Development environment
      this._baseUrl = 'http://localhost:5034/api';
    }
  }
  
  get baseUrl(): string {
    return this._baseUrl;
  }
} 