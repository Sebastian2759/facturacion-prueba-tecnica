/** Contract v1; nullable transport fields confirmed against C# DTOs, pending OpenAPI export. */
export interface Envelope<T> {
  isSuccess: boolean;
  statusCode: number;
  message: string;
  data: T | null;
  errors?: string[] | null;
  code?: string;
  fieldErrors?: Record<string, string[]> | null;
  traceId?: string | null;
}
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
export interface Account {
  code: string;
  identificationType: string;
  document: string;
  name: string;
  firstName?: string | null;
  lastName?: string | null;
  email: string;
  personType: number;
  cityCode: string;
  departmentCode: string;
  countryCode: string;
  address?: string | null;
  additionalAddress?: string | null;
  phone?: string | null;
  customerType: number;
  website?: string | null;
  registeredAt: string;
  externalCode?: string | null;
}
export interface AccountsResult {
  mode: AccountsMode;
  accounts: Account[];
}
export interface AcceptedAccount {
  mode: AccountsMode;
  code: string;
  accepted: boolean;
}
