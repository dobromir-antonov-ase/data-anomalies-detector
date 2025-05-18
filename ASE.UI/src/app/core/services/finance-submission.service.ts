import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { FinanceSubmission } from '../models/finance-submission.model';
import { ApiConfigService } from './api-config.service';

export interface PaginatedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  hasMore: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class FinanceSubmissionService {
  private apiUrl: string;

  constructor(
    private http: HttpClient,
    private apiConfig: ApiConfigService
  ) {
    this.apiUrl = this.apiConfig.baseUrl;
  }

  getAllSubmissions(): Observable<FinanceSubmission[]> {
    return this.http.get<FinanceSubmission[]>(`${this.apiUrl}/finance-submissions`);
  }

  getPaginatedSubmissions(pageNumber: number = 1, pageSize: number = 10): Observable<PaginatedResult<FinanceSubmission>> {
    let params = new HttpParams()
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString());
    
    return this.http.get<PaginatedResult<FinanceSubmission>>(`${this.apiUrl}/finance-submissions/paginated`, { params });
  }

  getSubmissionById(id: number): Observable<FinanceSubmission> {
    return this.http.get<FinanceSubmission>(`${this.apiUrl}/finance-submissions/${id}`);
  }

  getSubmissionsByDealer(dealerId: number): Observable<FinanceSubmission[]> {
    return this.http.get<FinanceSubmission[]>(`${this.apiUrl}/dealers/${dealerId}/finance-submissions`);
  }

  createSubmission(submission: any): Observable<FinanceSubmission> {
    return this.http.post<FinanceSubmission>(`${this.apiUrl}/finance-submissions`, submission);
  }

  updateSubmission(id: number, submission: any): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/finance-submissions/${id}`, submission);
  }

  deleteSubmission(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/finance-submissions/${id}`);
  }
} 