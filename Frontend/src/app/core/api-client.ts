import { HttpClient, HttpHeaders } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { catchError, map, Observable, throwError, timeout } from 'rxjs';
import { AuthSession } from './auth-session';
import { RuntimeConfig } from './runtime-config';
import type { Envelope } from './api-contract';
import { ApiError, decodeEnvelope, normalizeError } from './api-error';

@Injectable({ providedIn: 'root' })
export class ApiClient {
  private readonly http = inject(HttpClient);
  private readonly config = inject(RuntimeConfig);
  private readonly auth = inject(AuthSession);
  get<T>(path: string): Observable<T>;
  get<T, R>(path: string, decode: (data: T) => R): Observable<R>;
  get<T, R>(path: string, decode?: (data: T) => R): Observable<T | R> {
    return this.request<T, T | R>('GET', path, undefined, undefined, (data) =>
      decode ? decode(data) : data,
    );
  }
  post<T>(path: string, body: unknown, idempotencyKey?: string): Observable<T>;
  post<T, R>(
    path: string,
    body: unknown,
    idempotencyKey: string | undefined,
    decode: (data: T) => R,
  ): Observable<R>;
  post<T, R>(
    path: string,
    body: unknown,
    idempotencyKey?: string,
    decode?: (data: T) => R,
  ): Observable<T | R> {
    return this.request<T, T | R>('POST', path, body, idempotencyKey, (data) =>
      decode ? decode(data) : data,
    );
  }
  private request<T, R>(
    method: string,
    path: string,
    body: unknown,
    key: string | undefined,
    decode: (data: T) => R,
  ): Observable<R> {
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
        map((response) => decodeEnvelope(response, decode)),
        catchError((error) => {
          const failure = normalizeError(error);
          if (failure.status === 401) this.auth.invalidate();
          return throwError(() => failure);
        }),
      );
  }
}
