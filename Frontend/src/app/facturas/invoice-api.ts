import { inject, Injectable } from '@angular/core';
import { ApiClient } from '../core/api-client';
import type {
  CreateInvoice,
  Invoice,
  InvoiceLine,
  InvoiceSummary,
  Page,
  Totals,
} from '../core/api-contract';
@Injectable({ providedIn: 'root' })
export class InvoiceApi {
  private readonly api = inject(ApiClient);
  preview(details: InvoiceLine[]) {
    return this.api.post<Totals>('/api/v1/invoices/preview', { details });
  }
  create(invoice: CreateInvoice, key: string) {
    return this.api.post<{ invoice: Invoice }>('/api/v1/invoices', invoice, key);
  }
  list(page: number) {
    return this.api.get<Page<InvoiceSummary>>(`/api/v1/invoices?page=${page}&pageSize=10`);
  }
  detail(number: string) {
    return this.api.get<{ invoice: Invoice }>(`/api/v1/invoices/${encodeURIComponent(number)}`);
  }
  demo() {
    return this.api.get<Page<InvoiceSummary>>('/api/v1/demo/invoices');
  }
}
