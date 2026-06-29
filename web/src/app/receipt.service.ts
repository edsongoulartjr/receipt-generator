import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { HttpParams } from '@angular/common/http';
import { API_BASE_URL } from './api.config';
import { Client } from './client.service';

export interface MonthlyReportRow {
  year: number;
  month: number;
  count: number;
  totalAmount: number;
  averageAmount: number;
}

export interface ReportSummaryResponse {
  rows: MonthlyReportRow[];
  totalCount: number;
  totalAmount: number;
  averageAmount: number;
}

export interface PagedResponse<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface Receipt {
  id?: number;
  number?: number;
  clientId: number;
  client?: Client;
  description: string;
  amount: number;
  date?: string;
  startTime?: string;
  endTime?: string;
  serviceDates?: string;
  issuerName?: string;
  issuerPhone?: string;
  issuerEmail?: string;
  driverName?: string;
  driverUserId?: number;
  cancelledAt?: string;
  cancelReason?: string;
}

@Injectable({
  providedIn: 'root'
})
export class ReceiptService {
  private apiUrl = `${API_BASE_URL}/receipts`;

  constructor(private http: HttpClient) { }

  getReceipts(page: number = 1, pageSize: number = 20, month?: number, year?: number): Observable<PagedResponse<Receipt>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (month !== undefined) params = params.set('month', month);
    if (year !== undefined) params = params.set('year', year);
    return this.http.get<PagedResponse<Receipt>>(this.apiUrl, { params });
  }

  getReceipt(id: number): Observable<Receipt> {
    return this.http.get<Receipt>(`${this.apiUrl}/${id}`);
  }

  addReceipt(receipt: Receipt): Observable<Receipt> {
    return this.http.post<Receipt>(this.apiUrl, receipt);
  }

  updateReceipt(id: number, receipt: Receipt): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}`, receipt);
  }

  deleteReceipt(id: number, reason?: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`, { body: { reason: reason ?? null } });
  }

  generateReceiptPdf(id: number): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/${id}/pdf`, { responseType: 'blob' });
  }

  getMonthlySummary(year?: number, month?: number, driverId?: number): Observable<ReportSummaryResponse> {
    let params = new HttpParams();
    if (year !== undefined) params = params.set('year', year);
    if (month !== undefined) params = params.set('month', month);
    if (driverId !== undefined) params = params.set('driverId', driverId);
    return this.http.get<ReportSummaryResponse>(`${API_BASE_URL}/reports/monthly-summary`, { params });
  }
}
