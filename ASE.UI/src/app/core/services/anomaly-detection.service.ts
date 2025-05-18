import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ApiConfigService } from './api-config.service';

export interface DataAnomaly {
  id?: number;
  anomalyType: string;
  description: string;
  severity: string;
  detectedAt: Date;
  
  // Enhanced properties
  anomalyScore?: number;
  affectedEntity?: string;
  affectedMetric?: string;
  actualValue?: number;
  expectedValue?: number;
  threshold?: number;
  relatedCellAddresses?: string[];
  relatedAnomalies?: number[];
  businessImpact?: { description: string, estimatedValue?: number };
  recommendedAction?: string;
  timeRange?: { start: Date, end: Date };
}

export interface DataPattern {
  id?: number;
  patternType: string;
  description: string;
  significance: string;
  confidenceScore: number;
  detectedAt: Date;
  correlation?: number;
  formula?: string;
  r2Value?: number;
  relatedCellAddresses?: string[];
  timeRange?: { start: Date, end: Date };
  industryComparison?: { benchmark: number, deviation: number };
  businessImpact?: { description: string, estimatedValue?: number };
  relatedPatterns?: number[];
}

@Injectable({
  providedIn: 'root'
})
export class AnomalyDetectionService {
  private apiUrl: string;

  constructor(
    private http: HttpClient,
    private apiConfig: ApiConfigService
  ) {
    this.apiUrl = this.apiConfig.baseUrl;
  }


  detectGlobalAnomalies(): Observable<DataAnomaly[]> {
    return this.http.get<DataAnomaly[]>(`${this.apiUrl}/anomalies/detect-global`);
  }

  detectAnomaliesByDealer(dealerId: number): Observable<DataAnomaly[]> {
    return this.http.get<DataAnomaly[]>(`${this.apiUrl}/dealers/${dealerId}/anomalies`);
  }

  detectPatternsByDealer(dealerId: number): Observable<DataPattern[]> {
    return this.http.get<DataPattern[]>(`${this.apiUrl}/dealers/${dealerId}/patterns`);
  }

  detectPatternsByDealerGroup(groupId: number): Observable<DataPattern[]> {
    return this.http.get<DataPattern[]>(`${this.apiUrl}/dealer-groups/${groupId}/patterns`);
  }

  getDealerGroups(): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/dealer-groups`);
  }

  detectAnomaliesByDealerGroup(groupId: number): Observable<DataAnomaly[]> {
    return this.http.get<DataAnomaly[]>(`${this.apiUrl}/dealer-groups/${groupId}/anomalies`);
  }
} 