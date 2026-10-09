import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { catchError, of, switchMap } from 'rxjs';
import { Invoice } from '../core/api-contract';
import { ApiError, normalizeError } from '../core/api-error';
import { Feedback } from '../shared/feedback';
import { Icon } from '../shared/icon';
import { InvoiceApi } from './invoice-api';
import { InvoiceTotals } from './invoice-totals';
@Component({
  selector: 'app-invoice-detail',
  imports: [RouterLink, CurrencyPipe, DatePipe, Feedback, InvoiceTotals, Icon],
  template: `
    <div class="page-heading">
      <div>
        <div class="eyebrow">FACTURAS / DETALLE</div>
        <h1>{{ invoice() ? 'Factura #' + invoice()!.number : 'Detalle de factura' }}</h1>
        <p>Información persistida en la base de datos.</p>
      </div>
      <a routerLink="/facturas" class="button secondary">Volver a mis facturas</a>
    </div>
    @if (created && invoice()) {
      <div class="alert success" role="status">
        <app-icon name="check" />
        <div>
          <strong>Factura #{{ invoice()!.number }} creada correctamente</strong>
          <p>El cliente, los artículos y los importes quedaron guardados.</p>
        </div>
      </div>
    }
    <app-feedback [loading]="loading()" [error]="error()" />
    @if (invoice(); as item) {
      <div class="form-layout">
        <div class="form-main">
          <section class="panel">
            <div class="panel-title">
              <h2>Cliente</h2>
              <span class="badge positive">Guardada</span>
            </div>
            <dl class="detail-grid">
              <div>
                <dt>Nombre</dt>
                <dd>{{ item.firstName }} {{ item.lastName }}</dd>
              </div>
              <div>
                <dt>Documento</dt>
                <dd>{{ item.document }}</dd>
              </div>
              <div>
                <dt>Dirección</dt>
                <dd>{{ item.address }}</dd>
              </div>
              <div>
                <dt>Teléfono</dt>
                <dd>{{ item.phone }}</dd>
              </div>
            </dl>
          </section>
          <section class="panel table-panel">
            <div class="panel-title">
              <h2>Artículos</h2>
              <span class="badge neutral">{{ item.details.length }}</span>
            </div>
            <div class="table-scroll">
              <table>
                <caption class="sr-only">
                  Artículos de la factura
                  {{
                    item.number
                  }}
                </caption>
                <thead>
                  <tr>
                    <th>Código</th>
                    <th class="number">Cantidad</th>
                    <th class="number">Precio unitario</th>
                    <th class="number">Valor</th>
                  </tr>
                </thead>
                <tbody>
                  @for (line of item.details; track $index) {
                    <tr>
                      <td>{{ line.code }}</td>
                      <td class="number">{{ line.quantity }}</td>
                      <td class="number">
                        {{ line.unitPrice | currency: 'COP' : 'symbol-narrow' : '1.2-2' }}
                      </td>
                      <td class="number amount">
                        {{ line.lineTotal | currency: 'COP' : 'symbol-narrow' : '1.2-2' }}
                      </td>
                    </tr>
                  }
                </tbody>
              </table>
            </div>
          </section>
        </div>
        <aside class="panel invoice-summary">
          <div class="panel-title">
            <h2>Resumen</h2>
            <app-icon name="invoice" />
          </div>
          <div class="invoice-meta">
            <div>
              <span>Número</span><strong>#{{ item.number }}</strong>
            </div>
            <div>
              <span>Fecha del servidor</span
              ><strong>{{ item.date | date: 'dd MMM yyyy, HH:mm' }}</strong>
            </div>
          </div>
          <app-invoice-totals [totals]="item" /><a
            class="button primary full-width margin-top"
            routerLink="/facturas/nueva"
            ><app-icon name="plus" />Nueva factura</a
          >
        </aside>
      </div>
    }
  `,
})
export class InvoiceDetail {
  private readonly api = inject(InvoiceApi);
  protected readonly created = history.state?.created === true;
  protected readonly invoice = signal<Invoice | null>(null);
  protected readonly loading = signal(true);
  protected readonly error = signal<ApiError | null>(null);
  constructor() {
    inject(ActivatedRoute)
      .paramMap.pipe(
        switchMap((params) => {
          this.loading.set(true);
          this.error.set(null);
          this.invoice.set(null);
          return this.api.detail(params.get('number') ?? '').pipe(
            catchError((error) => {
              this.error.set(normalizeError(error));
              return of(null);
            }),
          );
        }),
        takeUntilDestroyed(),
      )
      .subscribe((result) => {
        this.invoice.set(result?.invoice ?? null);
        this.loading.set(false);
      });
  }
}
