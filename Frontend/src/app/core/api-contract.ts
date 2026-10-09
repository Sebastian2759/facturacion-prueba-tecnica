import type { components } from './openapi.generated';

type Schemas = components['schemas'];

/** Exact transport types generated from the backend export; no required/nullability rewriting. */
export type Envelope<T> = Omit<Schemas['ResponseBaseOfSessionDto'], 'data'> & {
  data?: T | null;
};
export type InvoiceLineWire = Schemas['InvoiceLineDto'];
export type InvoiceLineInputWire = Schemas['InvoiceLineInput'];
export type InvoiceWire = Schemas['InvoiceDetailDto'];
export type InvoiceSummaryWire = Schemas['InvoiceListDto'];
export type CreateInvoiceWire = Schemas['CreateInvoiceRequest'];
export type PreviewInvoiceWire = Schemas['PreviewInvoiceRequest'];
export type TotalsWire = Schemas['PreviewInvoiceResponse'];
export type CreatedInvoiceWire = Schemas['CreateInvoiceResponse'];
export type InvoiceResultWire = Schemas['GetInvoiceByNumberResponse'];
export type InvoicePageWire = Schemas['ListInvoicesResponse'];
export type SessionWire = Schemas['SessionDto'];
export type AccountWire = Schemas['AccountDto'];
export type CreateAccountWire = Schemas['CreateAccountRequest'];
export type AccountsResultWire = Schemas['GetAccountsResponse'];
export type AcceptedAccountWire = Schemas['CreateAccountResponse'];

/** UI models are produced by explicit transport decoders; monetary rules remain on the server. */
export interface Totals {
  gross: number;
  discount: number;
  tax: number;
  total: number;
}
export interface InvoiceLine {
  code: number;
  quantity: number;
  unitPrice: number;
}
export interface CreateInvoice {
  document: string;
  firstName: string;
  lastName: string;
  address: string;
  phone: string;
  details: InvoiceLine[];
}
export interface InvoiceSummary {
  number: number;
  date: string;
  document: string;
  firstName: string;
  lastName: string;
  total: number;
}
export interface Invoice extends InvoiceSummary, Totals {
  id: string;
  address: string;
  phone: string;
  details: (InvoiceLine & { lineTotal: number })[];
}
export interface Page<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}
export interface Session {
  mode: 'local' | 'online';
  authenticated: boolean;
  canEvaluate: boolean;
  canUseRealAccounts: boolean;
  csrfToken?: string | null;
  oidc?: { authority: string; clientId: string; scope: string } | null;
}
export type AccountsMode = 'simulated' | 'real';
export type Account = Omit<AccountWire, 'personType' | 'customerType'> & {
  personType: number;
  customerType: number;
};
export interface AccountsResult {
  mode: AccountsMode;
  accounts: Account[];
}
export interface AcceptedAccount {
  mode: AccountsMode;
  code: string;
  accepted: boolean;
}
