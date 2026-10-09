import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { catchError, of, switchMap } from 'rxjs';
import { InvoiceSummary, Page } from '../core/api-contract';
import { ApiError, normalizeError } from '../core/api-error';
import { Feedback } from '../shared/feedback';
import { Icon } from '../shared/icon';
import { InvoiceApi } from './invoice-api';
@Component({
  selector: 'app-invoice-list',
  imports: [RouterLink, CurrencyPipe, DatePipe, Feedback, Icon],
  template: `
    <div class="page-heading">
      <div>
        <div class="eyebrow">FACTURAS / HISTORIAL</div>
        <h1>Mis facturas</h1>
        <p>Consulta los registros asociados a tu identidad.</p>
      </div>
      <a routerLink="nueva" class="button primary"><app-icon name="plus" />Nueva factura</a>
    </div>
    <app-feedback [loading]="loading()" [error]="error()" />
    @if (error()) {
      <button class="button secondary" (click)="reload()">Reintentar consulta</button>
    }
    @if (data(); as result) {
      <section class="panel table-panel">
        <div class="panel-title">
          <h2>Facturas registradas</h2>
          <span class="badge neutral">{{ result.totalCount }} registros</span>
        </div>
        @if (result.items.length) {
          <div class="table-scroll">
            <table>
              <caption class="sr-only">
                Tus facturas, página
                {{
                  result.page
                }}
              </caption>
              <thead>
                <tr>
                  <th>Número</th>
                  <th>Cliente</th>
                  <th>Fecha</th>
                  <th class="number">Total</th>
                  <th><span class="sr-only">Acciones</span></th>
                </tr>
              </thead>
              <tbody>
                @for (invoice of result.items; track invoice.number) {
                  <tr>
                    <td>
                      <a class="invoice-number" [routerLink]="[invoice.number]"
                        >#{{ invoice.number }}</a
                      >
                    </td>
                    <td>
                      <strong>{{ invoice.firstName }} {{ invoice.lastName }}</strong
                      ><small>{{ invoice.document }}</small>
                    </td>
                    <td>{{ invoice.date | date: 'dd MMM yyyy' }}</td>
                    <td class="number amount">
                      {{ invoice.total | currency: 'COP' : 'symbol-narrow' : '1.2-2' }}
                    </td>
                    <td>
                      <a
                        [routerLink]="[invoice.number]"
                        class="table-link"
                        [attr.aria-label]="'Ver factura ' + invoice.number"
                        >Ver <app-icon name="arrow"
                      /></a>
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
          <div class="pagination">
            <span>Página {{ result.page }}</span>
            <div>
              <button
                class="button secondary small"
                (click)="go(-1)"
                [disabled]="result.page <= 1 || loading()"
              >
                Anterior</button
              ><button
                class="button secondary small"
                (click)="go(1)"
                [disabled]="result.page * result.pageSize >= result.totalCount || loading()"
              >
                Siguiente
              </button>
            </div>
          </div>
        } @else {
          <div class="empty-state">
            <app-icon name="invoice" />
            <h3>Aún no tienes facturas</h3>
            <p>Crea la primera con los datos ficticios de un cliente.</p>
            <a routerLink="nueva" class="button primary">Crear primera factura</a>
          </div>
        }
      </section>
    }
  `,
})
export class InvoiceList {
  private readonly api = inject(InvoiceApi);
  private readonly request = signal({ page: 1, version: 0 });
  protected readonly data = signal<Page<InvoiceSummary> | null>(null);
  protected readonly loading = signal(true);
  protected readonly error = signal<ApiError | null>(null);
  constructor() {
    toObservable(this.request)
      .pipe(
        switchMap((value) => {
          this.loading.set(true);
          this.error.set(null);
          this.data.set(null);
          return this.api.list(value.page).pipe(
            catchError((error) => {
              this.error.set(normalizeError(error));
              return of(null);
            }),
          );
        }),
        takeUntilDestroyed(),
      )
      .subscribe((data) => {
        this.data.set(data);
        this.loading.set(false);
      });
  }
  protected go(offset: number): void {
    this.request.update((value) => ({ ...value, page: value.page + offset }));
  }
  protected reload(): void {
    this.request.update((value) => ({ ...value, version: value.version + 1 }));
  }
}
