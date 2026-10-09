import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthSession } from '../core/auth-session';
import { InvoiceSummary } from '../core/api-contract';
import { ApiError, normalizeError } from '../core/api-error';
import { InvoiceApi } from '../facturas/invoice-api';
import { Feedback } from '../shared/feedback';
import { Icon } from '../shared/icon';
@Component({
  selector: 'app-demo',
  imports: [RouterLink, CurrencyPipe, DatePipe, Feedback, Icon],
  template: `
    <div class="page-heading">
      <div>
        <div class="eyebrow">TU ESPACIO DE EVALUACIÓN</div>
        <h1>Todo empieza con una factura.</h1>
        <p>Un recorrido sencillo: registra, revisa y guarda.</p>
      </div>
      <span class="badge neutral">DATOS FICTICIOS</span>
    </div>
    <section class="welcome-panel">
      <div>
        <span class="eyebrow">FACTURACIÓN CLARA, DE PRINCIPIO A FIN</span>
        <h2>Los detalles en orden.<br />Los importes, bajo control.</h2>
        <p>
          Clientes, artículos y totales en un solo lugar. Explora los ejemplos o inicia tu propia
          prueba.
        </p>
        <a class="button primary" [routerLink]="auth.canEvaluate() ? '/facturas/nueva' : '/acceso'"
          >{{ auth.canEvaluate() ? 'Crear una factura' : 'Acceder a la evaluación'
          }}<app-icon name="arrow"
        /></a>
      </div>
      <div class="receipt-art" aria-hidden="true">
        <div class="receipt-top">
          <span class="mini-logo">n.</span><span>FACTURA<br /><b>DE EJEMPLO</b></span>
        </div>
        <div class="receipt-lines"><span></span><span></span><span></span></div>
        <div class="receipt-total"><span>Todo en su lugar</span><app-icon name="check" /></div>
        <span class="receipt-caption">Un registro. Todos los detalles.</span>
      </div>
    </section>
    <div class="feature-grid">
      <a [routerLink]="auth.canEvaluate() ? '/facturas/nueva' : '/acceso'" class="feature-card"
        ><span class="feature-icon"><app-icon name="invoice" /></span>
        <div>
          <h2>Facturas</h2>
          <p>Crea y consulta tus registros.</p>
        </div>
        <app-icon name="arrow" /></a
      ><a [routerLink]="auth.canEvaluate() ? '/cuentas' : '/acceso'" class="feature-card"
        ><span class="feature-icon blue"><app-icon name="accounts" /></span>
        <div>
          <h2>Cuentas</h2>
          <p>Explora la integración Novasoft.</p>
        </div>
        <app-icon name="arrow"
      /></a>
      <div class="feature-card privacy-card">
        <span class="feature-icon neutral"><app-icon name="shield" /></span>
        <div>
          <h2>Tu espacio, privado</h2>
          <p>Las facturas se consultan por identidad.</p>
        </div>
      </div>
    </div>
    <section class="panel table-panel">
      <div class="panel-title">
        <div>
          <h2>Así se ve una factura registrada</h2>
          <p>Ejemplos fijos de sólo lectura. No son facturas de otros usuarios.</p>
        </div>
        <span class="badge amber">DEMOSTRACIÓN</span>
      </div>
      <app-feedback [loading]="loading()" [error]="error()" />
      @if (error()) {
        <div class="panel-padding">
          <button class="button secondary small" (click)="load()">Reintentar ejemplos</button>
        </div>
      }
      @if (invoices().length) {
        <div class="table-scroll">
          <table>
            <caption class="sr-only">
              Facturas ficticias de demostración
            </caption>
            <thead>
              <tr>
                <th>Factura</th>
                <th>Cliente de ejemplo</th>
                <th>Fecha</th>
                <th class="number">Total</th>
                <th>Origen</th>
              </tr>
            </thead>
            <tbody>
              @for (item of invoices(); track item.number) {
                <tr>
                  <td class="invoice-number">#{{ item.number }}</td>
                  <td>
                    <strong>{{ item.firstName }} {{ item.lastName }}</strong
                    ><small>{{ item.document }}</small>
                  </td>
                  <td>{{ item.date | date: 'dd MMM yyyy' }}</td>
                  <td class="number amount">
                    {{ item.total | currency: 'COP' : 'symbol-narrow' : '1.2-2' }}
                  </td>
                  <td><span class="badge neutral">Ejemplo fijo</span></td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
      @if (!loading() && !error() && !invoices().length) {
        <div class="empty-state"><p>No hay ejemplos disponibles en este momento.</p></div>
      }
    </section>
    <p class="footnote">
      <app-icon name="shield" />El acceso público muestra únicamente ejemplos. Para guardar y
      consultar tus facturas, utiliza el acceso de evaluación.
    </p>
  `,
})
export class Demo {
  protected readonly auth = inject(AuthSession);
  private readonly api = inject(InvoiceApi);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly invoices = signal<InvoiceSummary[]>([]);
  protected readonly loading = signal(false);
  protected readonly error = signal<ApiError | null>(null);
  constructor() {
    this.load();
  }
  protected load(): void {
    if (this.loading()) return;
    this.loading.set(true);
    this.error.set(null);
    this.api
      .demo()
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.loading.set(false)),
      )
      .subscribe({
        next: (result) => this.invoices.set(result.items),
        error: (error) => this.error.set(normalizeError(error)),
      });
  }
}
