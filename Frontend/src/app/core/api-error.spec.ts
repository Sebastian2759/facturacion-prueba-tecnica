import { HttpErrorResponse } from '@angular/common/http';
import { describe, expect, it } from 'vitest';
import { ApiError, normalizeError, unwrap } from './api-error';
import type { Envelope } from './api-contract';
describe('API contract errors', () => {
  it('preserves indexed field paths and trace from ProblemDetails', () => {
    const failure = normalizeError(
      new HttpErrorResponse({
        status: 400,
        error: {
          title: 'Entrada inválida',
          fieldErrors: { 'details[1].quantity': ['Debe ser entero.'] },
          traceId: 'trace-demo',
        },
      }),
    );
    expect(failure.status).toBe(400);
    expect(failure.fieldErrors['details[1].quantity']).toEqual(['Debe ser entero.']);
    expect(failure.traceId).toBe('trace-demo');
  });
  it('does not mistake an unsuccessful envelope for success', () => {
    const response = {
      isSuccess: false,
      statusCode: 409,
      message: 'Reintento incompatible.',
      data: null,
    };
    expect(() => unwrap(response)).toThrow(ApiError);
    expect(normalizeError(response).status).toBe(409);
  });
  it('returns the exact server totals without calculating them', () => {
    const data = { gross: 500000, discount: 25000, tax: 90250, total: 565250 };
    expect(unwrap({ isSuccess: true, data } as Envelope<typeof data>)).toBe(data);
  });
  it('does not expose an HTML error page', () => {
    const failure = normalizeError(
      new HttpErrorResponse({ status: 502, error: '<html>internal proxy diagnostic</html>' }),
    );
    expect(failure.message).not.toContain('internal');
    expect(failure.message).toContain('reintentar');
  });
  it('ignores malformed field-error values', () => {
    expect(
      normalizeError({ fieldErrors: { document: ['Obligatorio', 123], phone: 'incorrecto' } })
        .fieldErrors,
    ).toEqual({ document: ['Obligatorio'], phone: [] });
  });
});
