import { describe, expect, it } from 'vitest';
import type { AccountWire, Envelope, InvoiceWire, SessionWire, TotalsWire } from './api-contract';
import {
  decodeAcceptedAccount,
  decodeAccountsResult,
  decodeInvoice,
  decodeInvoicePage,
  decodeSession,
  decodeTotals,
} from './api-decoders';
import { ApiError, decodeEnvelope } from './api-error';

const invoice: InvoiceWire = {
  id: 'fictitious-id',
  number: '42',
  date: '2026-10-09T00:00:00',
  document: 'TEST-42',
  firstName: 'Cliente',
  lastName: 'Ficticio',
  address: 'Dirección ficticia',
  phone: '3000000000',
  details: [{ code: '10', quantity: '2', unitPrice: '250000.01', lineTotal: '500000.02' }],
  gross: '500000.02',
  discount: '25000.00',
  tax: '90250.00',
  total: '565250.02',
};
const account: AccountWire = {
  code: 'DEMO001',
  identificationType: '06',
  document: '123',
  name: 'Cuenta ficticia',
  firstName: null,
  lastName: null,
  email: 'demo@example.test',
  personType: '1',
  cityCode: '11001',
  departmentCode: '11',
  countryCode: '057',
  address: null,
  additionalAddress: null,
  phone: null,
  customerType: '1',
  website: null,
  registeredAt: '2026-10-09T00:00:00Z',
  externalCode: null,
};

describe('Generated OpenAPI transport decoders', () => {
  it('converts supported numeric strings while keeping server amounts and cents unchanged', () => {
    const result = decodeInvoice(invoice);
    expect(result.number).toBe(42);
    expect(result.details[0].unitPrice).toBe(250000.01);
    expect(result.details[0].lineTotal).toBe(500000.02);
    expect(result.total).toBe(565250.02);
    expect(
      decodeTotals({ gross: '800000000000.01', discount: '1.01', tax: '2.02', total: '3.03' }),
    ).toEqual({ gross: 800000000000.01, discount: 1.01, tax: 2.02, total: 3.03 });
  });
  it('rejects incomplete invoice fields which the raw schema marks optional', () => {
    const { firstName: omitted, ...incomplete } = invoice;
    expect(omitted).toBeDefined();
    expect(() => decodeInvoice(incomplete)).toThrow(ApiError);
    expect(() => decodeInvoice({ ...invoice, details: undefined })).toThrow(ApiError);
    expect(() =>
      decodeInvoice({ ...invoice, details: [{ quantity: 1, unitPrice: 1, lineTotal: 1 }] }),
    ).toThrow(ApiError);
  });
  it.each(['', '   ', 'not-a-number', '1,25', 'Infinity', '1e999'])(
    'rejects malformed number %j',
    (total) => {
      expect(() => decodeTotals({ gross: 1, discount: 0, tax: 0, total })).toThrow(ApiError);
    },
  );
  it('preserves legitimate nullable account fields and normalizes numeric classifiers', () => {
    const result = decodeAccountsResult({ mode: 'simulated', accounts: [account] });
    expect(result.accounts[0].firstName).toBeNull();
    expect(result.accounts[0].phone).toBeNull();
    expect(result.accounts[0].personType).toBe(1);
  });
  it('rejects unknown response modes instead of relabelling them as simulated or real', () => {
    expect(() => decodeAccountsResult({ mode: 'unknown', accounts: [] })).toThrow(ApiError);
    expect(() =>
      decodeAcceptedAccount({ mode: 'unknown', code: 'DEMO001', accepted: true }),
    ).toThrow(ApiError);
    const session: SessionWire = {
      mode: 'unknown',
      authenticated: true,
      canEvaluate: true,
      canUseRealAccounts: false,
    };
    expect(() => decodeSession(session)).toThrow(ApiError);
    expect(
      decodeSession({ ...session, mode: 'local', csrfToken: 'test-csrf', oidc: null }).oidc,
    ).toBeNull();
  });
  it('decodes paging and rejects fractional integer fields without calculating business totals', () => {
    expect(
      decodeInvoicePage({ items: [invoice], page: '1', pageSize: '10', totalCount: '1' }).page,
    ).toBe(1);
    expect(() =>
      decodeInvoicePage({ items: [], page: '1.5', pageSize: 10, totalCount: 0 }),
    ).toThrow(ApiError);
  });
  it('keeps the backend trace when a decoder rejects a malformed successful response', () => {
    const response: Envelope<TotalsWire> = {
      isSuccess: true,
      statusCode: 200,
      traceId: 'backend-trace-test',
      data: { gross: 1, discount: 0, tax: 0, total: 'invalid' },
    };
    expect(() => decodeEnvelope(response, decodeTotals)).toThrowError(
      expect.objectContaining({
        code: 'response.invalid',
        traceId: 'backend-trace-test',
        status: 200,
      }),
    );
  });
});
