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
    const response: Envelope<typeof data> = {
      isSuccess: true,
      statusCode: 200,
      message: 'Operación exitosa.',
      data,
      errors: null,
      fieldErrors: null,
      traceId: null,
    };
    expect(unwrap(response)).toBe(data);
  });
  it.each([null, undefined])('rejects a successful envelope with data %s', (data) => {
    // Simulate untrusted JSON at the transport boundary, including a missing property.
    const response = {
      isSuccess: true,
      statusCode: 200,
      message: 'Operación exitosa.',
      traceId: 'malformed-response-test',
      ...(data === undefined ? {} : { data }),
    };
    expect(() => unwrap(response as Envelope<unknown>)).toThrowError(
      expect.objectContaining({
        message: 'La aplicación devolvió una respuesta incompleta. Vuelve a intentarlo.',
        status: 200,
        traceId: 'malformed-response-test',
        code: 'response.invalid',
      }),
    );
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
