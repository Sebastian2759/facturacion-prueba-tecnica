import type {
  Account,
  AccountWire,
  AccountsResult,
  AccountsResultWire,
  AcceptedAccount,
  AcceptedAccountWire,
  CreatedInvoiceWire,
  Invoice,
  InvoiceLine,
  InvoiceLineWire,
  InvoicePageWire,
  InvoiceResultWire,
  InvoiceSummary,
  InvoiceSummaryWire,
  InvoiceWire,
  Page,
  Session,
  SessionWire,
  Totals,
  TotalsWire,
} from './api-contract';
import { ApiError } from './api-error';

function invalid(): never {
  throw new ApiError(
    'La aplicación devolvió una respuesta incompleta o no válida.',
    0,
    {},
    undefined,
    'response.invalid',
  );
}
function object(value: unknown): Record<string, unknown> {
  if (!value || typeof value !== 'object' || Array.isArray(value)) return invalid();
  return value as Record<string, unknown>;
}
function text(value: unknown): string {
  return typeof value === 'string' ? value : invalid();
}
function nullableText(value: unknown): string | null {
  return value === null ? null : text(value);
}
function optionalText(value: unknown): string | null | undefined {
  return value === undefined ? undefined : nullableText(value);
}
function flag(value: unknown): boolean {
  return typeof value === 'boolean' ? value : invalid();
}
function numeric(value: unknown, integer = false): number {
  // OpenAPI permits JSON numbers or numeric strings; conversion never calculates invoice totals.
  if (typeof value === 'string' && /^-?(?:0|[1-9]\d*)(?:\.\d+)?(?:[eE][+-]?\d+)?$/.test(value)) {
    value = Number(value);
  }
  if (
    typeof value !== 'number' ||
    !Number.isFinite(value) ||
    (integer && !Number.isSafeInteger(value))
  ) {
    return invalid();
  }
  return value;
}
function collection<TWire, T>(value: TWire[] | undefined, decode: (item: TWire) => T): T[] {
  if (!Array.isArray(value)) return invalid();
  return value.map(decode);
}

export function decodeTotals(wire: TotalsWire): Totals {
  object(wire);
  const value = wire;
  return {
    gross: numeric(value['gross']),
    discount: numeric(value['discount']),
    tax: numeric(value['tax']),
    total: numeric(value['total']),
  };
}
export function decodeInvoiceLine(wire: InvoiceLineWire): InvoiceLine & { lineTotal: number } {
  object(wire);
  const value = wire;
  return {
    code: numeric(value['code'], true),
    quantity: numeric(value['quantity'], true),
    unitPrice: numeric(value['unitPrice']),
    lineTotal: numeric(value['lineTotal']),
  };
}
export function decodeInvoiceSummary(wire: InvoiceSummaryWire): InvoiceSummary {
  object(wire);
  const value = wire;
  return {
    number: numeric(value['number'], true),
    date: text(value['date']),
    document: text(value['document']),
    firstName: text(value['firstName']),
    lastName: text(value['lastName']),
    total: numeric(value['total']),
  };
}
export function decodeInvoice(wire: InvoiceWire): Invoice {
  object(wire);
  const value = wire;
  return {
    ...decodeInvoiceSummary(wire),
    id: text(value['id']),
    address: text(value['address']),
    phone: text(value['phone']),
    details: collection(value['details'], decodeInvoiceLine),
    gross: numeric(value['gross']),
    discount: numeric(value['discount']),
    tax: numeric(value['tax']),
    total: numeric(value['total']),
  };
}
export function decodeCreatedInvoice(wire: CreatedInvoiceWire): { invoice: Invoice } {
  object(wire);
  return { invoice: decodeInvoice(wire.invoice) };
}
export function decodeInvoiceResult(wire: InvoiceResultWire): { invoice: Invoice } {
  return decodeCreatedInvoice(wire);
}
export function decodeInvoicePage(wire: InvoicePageWire): Page<InvoiceSummary> {
  object(wire);
  const value = wire;
  return {
    items: collection(value['items'], decodeInvoiceSummary),
    page: numeric(value['page'], true),
    pageSize: numeric(value['pageSize'], true),
    totalCount: numeric(value['totalCount'], true),
  };
}
export function decodeSession(wire: SessionWire): Session {
  object(wire);
  const value = wire;
  const mode = value['mode'];
  if (mode !== 'local' && mode !== 'online') return invalid();
  const rawOidc = value['oidc'];
  const oidc = rawOidc == null ? rawOidc : object(rawOidc);
  return {
    mode,
    authenticated: flag(value['authenticated']),
    canEvaluate: flag(value['canEvaluate']),
    canUseRealAccounts: flag(value['canUseRealAccounts']),
    csrfToken: optionalText(value['csrfToken']),
    oidc:
      oidc == null
        ? oidc
        : {
            authority: text(oidc['authority']),
            clientId: text(oidc['clientId']),
            scope: text(oidc['scope']),
          },
  };
}
export function decodeAccount(wire: AccountWire): Account {
  object(wire);
  const value = wire;
  return {
    code: text(value['code']),
    identificationType: text(value['identificationType']),
    document: text(value['document']),
    name: text(value['name']),
    firstName: nullableText(value['firstName']),
    lastName: nullableText(value['lastName']),
    email: text(value['email']),
    personType: numeric(value['personType'], true),
    cityCode: text(value['cityCode']),
    departmentCode: text(value['departmentCode']),
    countryCode: text(value['countryCode']),
    address: nullableText(value['address']),
    additionalAddress: nullableText(value['additionalAddress']),
    phone: nullableText(value['phone']),
    customerType: numeric(value['customerType'], true),
    website: nullableText(value['website']),
    registeredAt: text(value['registeredAt']),
    externalCode: nullableText(value['externalCode']),
  };
}
export function decodeAccountsResult(wire: AccountsResultWire): AccountsResult {
  object(wire);
  const value = wire;
  return {
    accounts: collection(value['accounts'], decodeAccount),
  };
}
export function decodeAcceptedAccount(wire: AcceptedAccountWire): AcceptedAccount {
  object(wire);
  const value = wire;
  return {
    code: text(value['code']),
    accepted: flag(value['accepted']),
  };
}
