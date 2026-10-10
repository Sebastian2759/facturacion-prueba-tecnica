import { inject, Injectable } from '@angular/core';
import { ApiClient } from '../core/api-client';
import type {
  CreateInvoice,
  CreateInvoiceWire,
  InvoiceLine,
  PreviewInvoiceWire,
} from '../core/api-contract';
import {
  decodeCreatedInvoice,
  decodeInvoicePage,
  decodeInvoiceResult,
  decodeTotals,
} from '../core/api-decoders';
@Injectable({ providedIn: 'root' })
export class InvoiceApi {
  private readonly api = inject(ApiClient);
  preview(details: InvoiceLine[]) {
    const body: PreviewInvoiceWire = { details };
    return this.api.post('/api/v1/invoices/preview', body, undefined, decodeTotals);
  }
  create(invoice: CreateInvoice, key: string) {
    const body: CreateInvoiceWire = invoice;
    return this.api.post('/api/v1/invoices', body, key, decodeCreatedInvoice);
  }
  list(page: number) {
    return this.api.get(`/api/v1/invoices?page=${page}&pageSize=10`, decodeInvoicePage);
  }
  detail(number: string) {
    return this.api.get(`/api/v1/invoices/${encodeURIComponent(number)}`, decodeInvoiceResult);
  }
}
