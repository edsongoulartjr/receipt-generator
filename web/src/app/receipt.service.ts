import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from './api.config';
import { Client } from './client.service';

export interface Receipt {
  id?: number;
  clientId: number;
  client?: Client; // Optional, will be populated by backend
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
}

@Injectable({
  providedIn: 'root'
})
export class ReceiptService {
  private apiUrl = `${API_BASE_URL}/receipts`;

  constructor(private http: HttpClient) { }

  getReceipts(): Observable<Receipt[]> {
    return this.http.get<Receipt[]>(this.apiUrl);
  }

  getReceipt(id: number): Observable<Receipt> {
    return this.http.get<Receipt>(`${this.apiUrl}/${id}`);
  }

  addReceipt(receipt: Receipt): Observable<Receipt> {
    return this.http.post<Receipt>(this.apiUrl, receipt);
  }

  updateReceipt(id: number, receipt: Receipt): Observable<any> {
    return this.http.put(`${this.apiUrl}/${id}`, receipt);
  }

  deleteReceipt(id: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${id}`);
  }

  generateReceiptPdf(id: number): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/${id}/pdf`, { responseType: 'blob' });
  }
}
