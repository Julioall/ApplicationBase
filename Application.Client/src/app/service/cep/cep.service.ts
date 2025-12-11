import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { catchError, map, throwError } from 'rxjs';

export interface CepResult {
  cep: string;
  street?: string;
  district?: string;
  city?: string;
  state?: string;
}

interface ViaCepResponse {
  cep: string;
  logradouro: string;
  complemento: string;
  bairro: string;
  localidade: string;
  uf: string;
  ibge: string;
  gia: string;
  ddd: string;
  siafi: string;
  erro?: boolean;
}

@Injectable({
  providedIn: 'root',
})
export class CepService {
  private readonly apiUrl = 'https://viacep.com.br/ws';

  constructor(private readonly http: HttpClient) {}

  lookup(cep: string) {
    const sanitized = this.sanitize(cep);
    return this.http.get<ViaCepResponse>(`${this.apiUrl}/${sanitized}/json/`).pipe(
      map((resp) => {
        if (resp.erro) {
          throw new Error('CEP_NOT_FOUND');
        }
        return {
          cep: this.sanitize(resp.cep),
          street: resp.logradouro,
          district: resp.bairro,
          city: resp.localidade,
          state: resp.uf,
        } as CepResult;
      }),
      catchError((err) => throwError(() => err)),
    );
  }

  sanitize(value: string): string {
    return (value || '').replace(/\D/g, '').slice(0, 8);
  }
}
