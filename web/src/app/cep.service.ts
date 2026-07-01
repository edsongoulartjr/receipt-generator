import { Injectable } from '@angular/core';
import { HttpBackend, HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface CepResult {
  logradouro: string;
  complemento: string;
  bairro: string;
  localidade: string;
  uf: string;
  erro?: boolean;
}

@Injectable({ providedIn: 'root' })
export class CepService {
  private readonly http: HttpClient;

  constructor(handler: HttpBackend) {
    // HttpBackend bypasses all interceptors — the JWT must not leave our domain
    this.http = new HttpClient(handler);
  }

  lookup(cep: string): Observable<CepResult> {
    const digits = cep.replace(/\D/g, '');
    return this.http.get<CepResult>(`https://viacep.com.br/ws/${digits}/json/`);
  }
}
