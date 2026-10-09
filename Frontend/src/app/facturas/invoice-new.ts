import { Component, DestroyRef, ElementRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { catchError, finalize, map, of, startWith, switchMap, timer } from 'rxjs';
import { CreateInvoice, InvoiceLine, Totals } from '../core/api-contract';
import { ApiError, normalizeError } from '../core/api-error';
import { Idempotency } from '../core/idempotency';
import { FormField } from '../shared/form-field';
import { Feedback } from '../shared/feedback';
import { Icon } from '../shared/icon';
import { InvoiceApi } from './invoice-api';
import { invoiceForm, invoiceLineForm } from './invoice-form-model';
import { InvoiceTotals } from './invoice-totals';

@Component({
  selector: 'app-invoice-new',
  imports: [ReactiveFormsModule, RouterLink, FormField, Feedback, Icon, InvoiceTotals],
  templateUrl: './invoice-new.html',
})
export class InvoiceNew {
  private readonly api = inject(InvoiceApi);
  private readonly router = inject(Router);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly destroyRef = inject(DestroyRef);
  private readonly idempotency = new Idempotency();
  protected readonly form = invoiceForm();
  protected readonly saving = signal(false);
  protected readonly previewing = signal(false);
  protected readonly totals = signal<Totals | null>(null);
  protected readonly error = signal<ApiError | null>(null);
  protected readonly previewError = signal<ApiError | null>(null);
  protected readonly today = new Intl.DateTimeFormat('es-CO', {
    dateStyle: 'long',
    timeZone: 'America/Bogota',
  }).format(new Date());
  constructor() {
    this.form.controls.details.valueChanges
      .pipe(
        startWith(null),
        switchMap(() => {
          this.totals.set(null);
          this.previewError.set(null);
          this.previewing.set(false);
          if (this.form.controls.details.invalid) return of(null);
          this.previewing.set(true);
          return timer(350).pipe(
            switchMap(() => this.api.preview(this.lines())),
            map((totals) => ({ totals, error: null })),
            catchError((error) => of({ totals: null, error: normalizeError(error) })),
          );
        }),
        takeUntilDestroyed(),
      )
      .subscribe((result) => {
        if (result) {
          this.totals.set(result.totals);
          this.previewError.set(result.error);
        }
        this.previewing.set(false);
      });
    this.form.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => this.error.set(null));
  }
  protected addLine(): void {
    if (this.form.controls.details.length < 100) this.form.controls.details.push(invoiceLineForm());
  }
  protected removeLine(index: number): void {
    if (this.form.controls.details.length > 1) this.form.controls.details.removeAt(index);
  }
  protected retryPreview(): void {
    this.form.controls.details.updateValueAndValidity();
  }
  protected field(path: string): string {
    return (
      (this.error()?.fieldErrors[path] ?? this.previewError()?.fieldErrors[path])?.join(' ') ?? ''
    );
  }
  private lines(): InvoiceLine[] {
    return this.form.controls.details.getRawValue() as InvoiceLine[];
  }
  protected save(): void {
    if (this.saving()) return;
    this.form.markAllAsTouched();
    if (this.form.invalid) {
      this.error.set(
        new ApiError('Revisa los campos obligatorios y los artículos antes de guardar.'),
      );
      queueMicrotask(() =>
        this.host.nativeElement.querySelector<HTMLInputElement>('input.ng-invalid')?.focus(),
      );
      return;
    }
    const raw = this.form.getRawValue();
    const body: CreateInvoice = {
      document: raw.document.trim(),
      firstName: raw.firstName.trim(),
      lastName: raw.lastName.trim(),
      address: raw.address.trim(),
      phone: raw.phone.trim(),
      details: this.lines(),
    };
    this.saving.set(true);
    this.error.set(null);
    this.api
      .create(body, this.idempotency.forBody(body))
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.saving.set(false)),
      )
      .subscribe({
        next: (result) => {
          void this.router.navigate(['/facturas', result.invoice.number], {
            state: { created: true },
          });
        },
        error: (error) => {
          this.error.set(normalizeError(error));
          queueMicrotask(() =>
            this.host.nativeElement
              .querySelector<HTMLElement>('[role="alert"]')
              ?.scrollIntoView({ block: 'center' }),
          );
        },
      });
  }
}
