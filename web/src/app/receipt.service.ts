import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { HttpParams } from '@angular/common/http';
import { API_BASE_URL } from './api.config';
import { Client } from './client.service';

export interface MonthlyReport {
  year: number;
  month: number;
  count: number;
  totalAmount: number;
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
  driverName?: string;
}

@Injectable({
  providedIn: 'root'
})
export class ReceiptService {
  private apiUrl = `${API_BASE_URL}/receipts`;

  constructor(private http: HttpClient) { }

  getReceipts(page: number = 1, pageSize: number = 20): Observable<PagedResponse<Receipt>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
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

  deleteReceipt(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  generateReceiptPdf(id: number): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/${id}/pdf`, { responseType: 'blob' });
  }

  getMonthlySummary(): Observable<MonthlyReport[]> {
    return this.http.get<MonthlyReport[]>(`${this.apiUrl}/monthly-summary`);
  }
}
