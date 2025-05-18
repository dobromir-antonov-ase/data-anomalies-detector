import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Dealer } from '../models/dealer.model';
import { ApiConfigService } from './api-config.service';

@Injectable({
  providedIn: 'root'
})
export class DealerService {
  private apiUrl: string;

  constructor(
    private http: HttpClient,
    private apiConfig: ApiConfigService
  ) {
    this.apiUrl = this.apiConfig.baseUrl;
  }

  getAllDealers(): Observable<Dealer[]> {
    return this.http.get<Dealer[]>(`${this.apiUrl}/dealers`);
  }

  getDealerById(id: number): Observable<Dealer> {
    return this.http.get<Dealer>(`${this.apiUrl}/dealers/${id}`);
  }

  createDealer(dealer: Dealer): Observable<Dealer> {
    return this.http.post<Dealer>(`${this.apiUrl}/dealers`, dealer);
  }

  updateDealer(id: number, dealer: Dealer): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/dealers/${id}`, dealer);
  }

  deleteDealer(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/dealers/${id}`);
  }
} 