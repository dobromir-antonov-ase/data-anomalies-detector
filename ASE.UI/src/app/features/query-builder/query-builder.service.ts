import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ApiConfigService } from '../../core/services/api-config.service';

export interface QueryRequest {
  naturalLanguageQuery: string;
  queryType: string;
  targetEntity?: string;
}

export interface QueryResponse {
  generatedQuery: string;
  queryType: string;
  explanation?: string;
  isSuccessful: boolean;
  errorMessage?: string;
  previewData?: any[];
}

export interface SpeechToQueryResponse {
  transcribedText: string;
  query: QueryResponse;
}

@Injectable({
  providedIn: 'root'
})
export class QueryBuilderService {
  private http = inject(HttpClient)
  private apiConfig = inject(ApiConfigService)
  private apiUrl = `${this.apiConfig.baseUrl}/query-builder`;

  getQueryTypes(): Observable<string[]> {
    return this.http.get<string[]>(`${this.apiUrl}/query-types`);
  }

  generateQuery(request: QueryRequest): Observable<QueryResponse> {
    return this.http.post<QueryResponse>(`${this.apiUrl}/generate`, request);
  }

  processAudioQuery(audioBlob: Blob, queryType: string): Observable<SpeechToQueryResponse> {
    const formData = new FormData();
    formData.append('audio', audioBlob, 'audio.wav');
    formData.append('queryType', queryType);

    return this.http.post<SpeechToQueryResponse>(`${this.apiUrl}/speech`, formData);
  }
} 