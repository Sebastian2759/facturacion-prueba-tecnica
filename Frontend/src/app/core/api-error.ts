import { HttpErrorResponse } from '@angular/common/http';
import type { Envelope } from './api-contract';

export class ApiError extends Error {
  constructor(
    message: string,
    readonly status = 0,
    readonly fieldErrors: Record<string, string[]> = {},
    readonly traceId?: string,
    readonly code?: string,
  ) {
    super(message);
  }
}
function record(value: unknown): Record<string, unknown> {
  return typeof value === 'object' && value !== null ? (value as Record<string, unknown>) : {};
}
function fieldErrors(value: unknown): Record<string, string[]> {
  return Object.fromEntries(
    Object.entries(record(value)).map(([key, errors]) => [
      key,
      Array.isArray(errors) ? errors.filter((v): v is string => typeof v === 'string') : [],
    ]),
  );
}
export function normalizeError(error: unknown): ApiError {
  if (error instanceof ApiError) return error;
  const body = record(error instanceof HttpErrorResponse ? error.error : error);
  const declaredStatus = body['statusCode'] ?? body['status'];
  const status =
    error instanceof HttpErrorResponse
      ? error.status
      : typeof declaredStatus === 'number'
        ? declaredStatus
        : 0;
  const fallback =
    status === 401
      ? 'La sesión local venció. Vuelve a conectar para continuar.'
      : status === 403
        ? 'Tu identidad no tiene permiso para esta operación.'
        : status === 404
          ? 'No encontramos este registro o no tienes acceso a él.'
          : status === 429
            ? 'Hay demasiadas solicitudes. Espera un momento e inténtalo de nuevo.'
            : 'No pudimos completar la solicitud. Conservamos tus datos para que puedas reintentar.';
  const message = body['message'] ?? body['detail'] ?? body['title'];
  return new ApiError(
    typeof message === 'string' && message ? message : fallback,
    status,
    fieldErrors(body['fieldErrors'] ?? body['errors']),
    typeof body['traceId'] === 'string' ? body['traceId'] : undefined,
    typeof body['code'] === 'string' ? body['code'] : undefined,
  );
}
export function unwrap<T>(response: Envelope<T>): T {
  if (!response || response.isSuccess !== true) throw normalizeError(response);
  if (response.data == null) {
    throw new ApiError(
      'La aplicación devolvió una respuesta incompleta. Vuelve a intentarlo.',
      response.statusCode,
      {},
      response.traceId ?? undefined,
      'response.invalid',
    );
  }
  return response.data;
}

export function decodeEnvelope<T, R>(response: Envelope<T>, decode: (value: T) => R): R {
  const data = unwrap(response);
  try {
    return decode(data);
  } catch (error) {
    const failure = normalizeError(error);
    throw new ApiError(
      failure.message,
      failure.status || response.statusCode || 0,
      failure.fieldErrors,
      failure.traceId ?? response.traceId ?? undefined,
      failure.code,
    );
  }
}
