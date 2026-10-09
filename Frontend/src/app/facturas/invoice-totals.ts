import { CurrencyPipe } from '@angular/common';
import { Component, input } from '@angular/core';
import { Totals } from '../core/api-contract';
@Component({
  selector: 'app-invoice-totals',
  imports: [CurrencyPipe],
  template: `
    <dl class="totals" aria-live="polite" aria-atomic="true">
      <div>
        <dt>Valor bruto</dt>
        <dd>
          {{ totals() ? (totals()!.gross | currency: 'COP' : 'symbol-narrow' : '1.2-2') : '—' }}
        </dd>
      </div>
      <div>
        <dt>Descuento <span>5 % desde $500.000</span></dt>
        <dd>
          {{ totals() ? (totals()!.discount | currency: 'COP' : 'symbol-narrow' : '1.2-2') : '—' }}
        </dd>
      </div>
      <div>
        <dt>IVA <span>19 %</span></dt>
        <dd>
          {{ totals() ? (totals()!.tax | currency: 'COP' : 'symbol-narrow' : '1.2-2') : '—' }}
        </dd>
      </div>
      <div class="grand-total">
        <dt>Total <span>COP</span></dt>
        <dd>
          {{ totals() ? (totals()!.total | currency: 'COP' : 'symbol-narrow' : '1.2-2') : '—' }}
        </dd>
      </div>
    </dl>
  `,
  styles: `
    .totals {
      margin: 0;
      display: grid;
      gap: 1rem;
    }
    .totals > div {
      display: flex;
      justify-content: space-between;
      gap: 1rem;
      font-size: 0.85rem;
    }
    dt span {
      display: block;
      color: var(--muted);
      font-size: 0.68rem;
      margin-top: 0.25rem;
    }
    dd {
      margin: 0;
      font-variant-numeric: tabular-nums;
      white-space: nowrap;
      font-weight: 600;
    }
    .grand-total {
      border-top: 1px solid var(--line);
      padding-top: 1.2rem;
      margin-top: 0.2rem;
      align-items: center;
    }
    .grand-total dt {
      font-size: 1rem;
      font-weight: 700;
    }
    .grand-total dd {
      font-size: 1.65rem;
      color: var(--ink);
      letter-spacing: -0.05em;
    }
  `,
})
export class InvoiceTotals {
  readonly totals = input<Totals | null>(null);
}
