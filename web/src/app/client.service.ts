import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from './api.config';

export interface Client {
  id?: number;
  name: string;
  address: string;
  taxId: string;
}

@Injectable({
  providedIn: 'root'
})
export class ClientService {
  private apiUrl = `${API_BASE_URL}/clients`;

  constructor(private http: HttpClient) { }

  getClients(): Observable<Client[]> {
    return this.http.get<Client[]>(this.apiUrl);
  }

  getClient(id: number): Observable<Client> {
    return this.http.get<Client>(`${this.apiUrl}/${id}`);
  }

  addClient(client: Client): Observable<Client> {
    return this.http.post<Client>(this.apiUrl, client);
  }

  updateClient(id: number, client: Client): Observable<any> {
    return this.http.put(`${this.apiUrl}/${id}`, client);
  }

  deleteClient(id: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${id}`);
  }
}
