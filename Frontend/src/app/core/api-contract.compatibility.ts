/** Compiled by api:check and the application: transport aliases, endpoint binding and UI refinements. */
import type { paths } from './openapi.generated';
import type {
  Account,
  AccountWire,
  AcceptedAccount,
  AcceptedAccountWire,
  AccountsResult,
  AccountsResultWire,
  CreateInvoice,
  CreateAccountWire,
  CreateInvoiceWire,
  CreatedInvoiceWire,
  Envelope,
  Invoice,
  InvoiceLine,
  InvoiceLineInputWire,
  InvoiceLineWire,
  InvoicePageWire,
  InvoiceResultWire,
  InvoiceSummary,
  InvoiceSummaryWire,
  InvoiceWire,
  Page,
  PreviewInvoiceWire,
  Session,
  SessionWire,
  Totals,
  TotalsWire,
} from './api-contract';
import type {
  decodeAcceptedAccount,
  decodeAccountsResult,
  decodeCreatedInvoice,
  decodeInvoicePage,
  decodeInvoiceResult,
  decodeSession,
  decodeTotals,
} from './api-decoders';

type Assert<T extends true> = T;
type Assignable<From, To> = [From] extends [To] ? true : false;
type Same<A, B> = [A] extends [B] ? ([B] extends [A] ? true : false) : false;
type SameKeys<A, B> = Same<keyof A, keyof B>;

type PreviewResponse =
  paths['/api/v1/invoices/preview']['post']['responses'][200]['content']['application/json'];
type CreateResponse =
  paths['/api/v1/invoices']['post']['responses'][201]['content']['application/json'];
type RepeatCreateResponse =
  paths['/api/v1/invoices']['post']['responses'][200]['content']['application/json'];
type ReadResponse =
  paths['/api/v1/invoices/{number}']['get']['responses'][200]['content']['application/json'];
type ListResponse =
  paths['/api/v1/invoices']['get']['responses'][200]['content']['application/json'];
type DemoResponse =
  paths['/api/v1/demo/invoices']['get']['responses'][200]['content']['application/json'];
type SessionResponse =
  paths['/api/v1/session']['get']['responses'][200]['content']['application/json'];
type AccountsResponse =
  paths['/api/v1/accounts']['get']['responses'][200]['content']['application/json'];
type AcceptedResponse =
  paths['/api/v1/accounts']['post']['responses'][201]['content']['application/json'];

export type ApiContractChecks = [
  // Requests can be stricter than the server: forms always send all business-required fields.
  Assert<Assignable<CreateInvoice, CreateInvoiceWire>>,
  Assert<SameKeys<CreateInvoice, CreateInvoiceWire>>,
  Assert<Assignable<InvoiceLine, InvoiceLineInputWire>>,
  Assert<SameKeys<InvoiceLine, InvoiceLineInputWire>>,
  Assert<Assignable<{ details: InvoiceLine[] }, PreviewInvoiceWire>>,
  Assert<
    Same<
      CreateAccountWire,
      paths['/api/v1/accounts']['post']['requestBody']['content']['application/json']
    >
  >,
  Assert<SameKeys<CreateAccountWire, AccountWire & { mode: string }>>,
  Assert<
    Same<
      CreateInvoiceWire,
      paths['/api/v1/invoices']['post']['requestBody']['content']['application/json']
    >
  >,
  Assert<
    Same<
      PreviewInvoiceWire,
      paths['/api/v1/invoices/preview']['post']['requestBody']['content']['application/json']
    >
  >,

  // Each actual endpoint must return the exact transport envelope consumed by its decoder.
  Assert<Same<PreviewResponse, Envelope<TotalsWire>>>,
  Assert<Same<CreateResponse, Envelope<CreatedInvoiceWire>>>,
  Assert<Same<RepeatCreateResponse, CreateResponse>>,
  Assert<Same<ReadResponse, Envelope<InvoiceResultWire>>>,
  Assert<Same<ListResponse, Envelope<InvoicePageWire>>>,
  Assert<Same<DemoResponse, ListResponse>>,
  Assert<Same<SessionResponse, Envelope<SessionWire>>>,
  Assert<Same<AccountsResponse, Envelope<AccountsResultWire>>>,
  Assert<Same<AcceptedResponse, Envelope<AcceptedAccountWire>>>,

  // These are validated UI refinements, not claims that the raw response has required properties.
  Assert<Assignable<Totals, TotalsWire>>,
  Assert<SameKeys<Totals, TotalsWire>>,
  Assert<Assignable<Invoice, InvoiceWire>>,
  Assert<SameKeys<Invoice, InvoiceWire>>,
  Assert<Assignable<InvoiceSummary, InvoiceSummaryWire>>,
  Assert<SameKeys<InvoiceSummary, InvoiceSummaryWire>>,
  Assert<Assignable<InvoiceLine & { lineTotal: number }, InvoiceLineWire>>,
  Assert<SameKeys<InvoiceLine & { lineTotal: number }, InvoiceLineWire>>,
  Assert<Assignable<Account, AccountWire>>,
  Assert<SameKeys<Account, AccountWire>>,
  Assert<Assignable<Session, SessionWire>>,
  Assert<SameKeys<Session, SessionWire>>,
  Assert<Assignable<AccountsResult, AccountsResultWire>>,
  Assert<SameKeys<AccountsResult, AccountsResultWire>>,
  Assert<Assignable<AcceptedAccount, AcceptedAccountWire>>,
  Assert<SameKeys<AcceptedAccount, AcceptedAccountWire>>,

  // Bind the functions actually called by Angular to generated inputs and consumed outputs.
  Assert<Same<Parameters<typeof decodeTotals>[0], TotalsWire>>,
  Assert<Same<ReturnType<typeof decodeTotals>, Totals>>,
  Assert<Same<Parameters<typeof decodeCreatedInvoice>[0], CreatedInvoiceWire>>,
  Assert<Same<ReturnType<typeof decodeCreatedInvoice>, { invoice: Invoice }>>,
  Assert<Same<Parameters<typeof decodeInvoiceResult>[0], InvoiceResultWire>>,
  Assert<Same<ReturnType<typeof decodeInvoiceResult>, { invoice: Invoice }>>,
  Assert<Same<Parameters<typeof decodeInvoicePage>[0], InvoicePageWire>>,
  Assert<Same<ReturnType<typeof decodeInvoicePage>, Page<InvoiceSummary>>>,
  Assert<Same<Parameters<typeof decodeSession>[0], SessionWire>>,
  Assert<Same<ReturnType<typeof decodeSession>, Session>>,
  Assert<Same<Parameters<typeof decodeAccountsResult>[0], AccountsResultWire>>,
  Assert<Same<ReturnType<typeof decodeAccountsResult>, AccountsResult>>,
  Assert<Same<Parameters<typeof decodeAcceptedAccount>[0], AcceptedAccountWire>>,
  Assert<Same<ReturnType<typeof decodeAcceptedAccount>, AcceptedAccount>>,
];
