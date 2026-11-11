import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from './api-config';

export interface Profile {
  id: string;
  email: string;
  displayName: string | null;
  updatedAt: string;
}

export interface TargetRole {
  id: string;
  title: string;
  company: string | null;
  description: string;
  createdAt: string;
  updatedAt: string;
}

export interface TargetRoleRequest {
  title: string;
  company: string | null;
  description: string;
}

@Injectable({ providedIn: 'root' })
export class TargetRoleService {
  private readonly http = inject(HttpClient);
  private readonly apiBaseUrl = inject(API_BASE_URL);
  private readonly endpoint = `${this.apiBaseUrl}/api/v1`;

  getProfile(): Observable<Profile> {
    return this.http.get<Profile>(`${this.endpoint}/profile`);
  }

  updateProfile(displayName: string): Observable<Profile> {
    return this.http.put<Profile>(`${this.endpoint}/profile`, { displayName });
  }

  list(): Observable<TargetRole[]> {
    return this.http.get<TargetRole[]>(`${this.endpoint}/target-roles`);
  }

  get(id: string): Observable<TargetRole> {
    return this.http.get<TargetRole>(`${this.endpoint}/target-roles/${encodeURIComponent(id)}`);
  }

  create(request: TargetRoleRequest): Observable<TargetRole> {
    return this.http.post<TargetRole>(`${this.endpoint}/target-roles`, request);
  }

  update(id: string, request: TargetRoleRequest): Observable<TargetRole> {
    return this.http.put<TargetRole>(
      `${this.endpoint}/target-roles/${encodeURIComponent(id)}`,
      request,
    );
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.endpoint}/target-roles/${encodeURIComponent(id)}`);
  }
}
