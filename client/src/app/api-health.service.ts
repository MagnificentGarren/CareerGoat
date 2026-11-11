import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { API_BASE_URL } from './api-config';

interface HealthResponse {
  status: string;
}

@Injectable({ providedIn: 'root' })
export class ApiHealthService {
  private readonly http = inject(HttpClient);
  private readonly apiBaseUrl = inject(API_BASE_URL).replace(/\/+$/, '');

  readonly healthUrl = `${this.apiBaseUrl}/health`;

  check(): Observable<void> {
    return this.http.get<HealthResponse>(this.healthUrl).pipe(
      map((response) => {
        if (response.status !== 'healthy') {
          throw new Error('The API health endpoint returned an unhealthy status.');
        }
      }),
    );
  }
}
