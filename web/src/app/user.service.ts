import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from './api.config';

export interface User {
  id: number;
  username: string;
  fullName: string;
  role: 'SystemAdmin' | 'CoopAdmin' | 'Driver';
  isActive: boolean;
}

export interface CreateUserRequest {
  username: string;
  password: string;
  role: 'SystemAdmin' | 'CoopAdmin' | 'Driver';
  fullName?: string;
}

@Injectable({
  providedIn: 'root'
})
export class UserService {
  private apiUrl = `${API_BASE_URL}/users`;

  constructor(private http: HttpClient) { }

  getUsers(): Observable<User[]> {
    return this.http.get<User[]>(this.apiUrl);
  }

  getDrivers(): Observable<User[]> {
    return this.http.get<User[]>(`${this.apiUrl}/drivers`);
  }

  createUser(request: CreateUserRequest): Observable<User> {
    return this.http.post<User>(this.apiUrl, request);
  }

  activateUser(id: number): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}/activate`, {});
  }

  deactivateUser(id: number): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}/deactivate`, {});
  }
}
