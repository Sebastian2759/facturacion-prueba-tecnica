import { HttpClient, HttpHeaders } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { catchError, map, Observable, throwError, timeout } from 'rxjs';
import { AuthSession } from './auth-session';
import { RuntimeConfig } from './runtime-config';
import type { Envelope } from './api-contract';
import { ApiError, normalizeError, unwrap } from './api-error';

@Injectable({ providedIn: 'root' })
export class ApiClient {
  private readonly http = inject(HttpClient);
  private readonly config = inject(RuntimeConfig);
  private readonly auth = inject(AuthSession);
  get<T>(path: string): Observable<T> {
    return this.request<T>('GET', path);
  }
  post<T>(path: string, body: unknown, idempotencyKey?: string): Observable<T> {
    return this.request<T>('POST', path, body, idempotencyKey);
  }
  private request<T>(method: string, path: string, body?: unknown, key?: string): Observable<T> {
    if (this.config.error()) return throwError(() => new ApiError(this.config.error()!));
    let headers = new HttpHeaders();
    const session = this.auth.state();
    if (method === 'POST' && session?.mode === 'local' && session.csrfToken) {
      headers = headers.set('X-CSRF-TOKEN', session.csrfToken);
    }
    if (key) headers = headers.set('Idempotency-Key', key);
    return this.http
      .request<Envelope<T>>(method, this.config.url(path), {
        body,
        headers,
        withCredentials: session?.mode === 'local',
      })
      .pipe(
        timeout(30000),
        map(unwrap),
        catchError((error) => {
          const failure = normalizeError(error);
          if (failure.status === 401) this.auth.invalidate();
          return throwError(() => failure);
        }),
      );
  }
}
