import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { ApiClient } from './api-client';
import { AuthSession } from './auth-session';
import { RuntimeConfig } from './runtime-config';
import { ApiError } from './api-error';
describe('Local API security boundary', () => {
  const invalidate = vi.fn();
  let http: HttpTestingController;
  let api: ApiClient;
  beforeEach(() => {
    invalidate.mockReset();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        {
          provide: AuthSession,
          useValue: { state: signal({ mode: 'local', csrfToken: 'unit-csrf' }), invalidate },
        },
        { provide: RuntimeConfig, useValue: { error: signal(null), url: (path: string) => path } },
      ],
    });
    http = TestBed.inject(HttpTestingController);
    api = TestBed.inject(ApiClient);
  });
  afterEach(() => http.verify());
  it('sends antiforgery and idempotency only through the configured API request', () => {
    api.post('/api/v1/invoices', { document: 'FICTICIO' }, 'test-key').subscribe();
    const request = http.expectOne('/api/v1/invoices');
    expect(request.request.headers.get('X-CSRF-TOKEN')).toBe('unit-csrf');
    expect(request.request.headers.get('Idempotency-Key')).toBe('test-key');
    expect(request.request.withCredentials).toBe(true);
    expect(request.request.headers.has('Authorization')).toBe(false);
    request.flush({ isSuccess: true, data: {} });
  });
  it('does not send the CSRF token on read-only requests', () => {
    api.get('/api/v1/invoices').subscribe();
    const request = http.expectOne('/api/v1/invoices');
    expect(request.request.headers.has('X-CSRF-TOKEN')).toBe(false);
    request.flush({ isSuccess: true, data: [] });
  });
  it('invalidates a rejected session without automatically replaying a write', () => {
    let error: ApiError | undefined;
    api.post('/api/v1/invoices', {}).subscribe({
      error: (failure) => {
        error = failure;
      },
    });
    http
      .expectOne('/api/v1/invoices')
      .flush({ title: 'Sesión vencida' }, { status: 401, statusText: 'Unauthorized' });
    expect(invalidate).toHaveBeenCalledOnce();
    expect(error?.status).toBe(401);
    http.expectNone('/api/v1/invoices');
  });
});
