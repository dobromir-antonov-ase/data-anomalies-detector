import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { MasterTemplate } from '../models/master-template.model';
import { ApiConfigService } from './api-config.service';

@Injectable({
  providedIn: 'root'
})
export class MasterTemplateService {
  private apiUrl: string;

  constructor(
    private http: HttpClient,
    private apiConfig: ApiConfigService
  ) {
    this.apiUrl = this.apiConfig.baseUrl;
  }

  getAllMasterTemplates(): Observable<MasterTemplate[]> {
    return this.http.get<MasterTemplate[]>(`${this.apiUrl}/master-templates`);
  }

  getMasterTemplateById(id: number): Observable<MasterTemplate> {
    return this.http.get<MasterTemplate>(`${this.apiUrl}/master-templates/${id}`);
  }

  getMasterTemplatesByYear(year: number): Observable<MasterTemplate[]> {
    return this.http.get<MasterTemplate[]>(`${this.apiUrl}/master-templates/year/${year}`);
  }

  createMasterTemplate(template: any): Observable<MasterTemplate> {
    return this.http.post<MasterTemplate>(`${this.apiUrl}/master-templates`, template);
  }

  updateMasterTemplate(id: number, template: any): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/master-templates/${id}`, template);
  }

  deleteMasterTemplate(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/master-templates/${id}`);
  }
} 