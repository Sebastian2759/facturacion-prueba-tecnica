import { Component, DestroyRef, ElementRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize } from 'rxjs';
import { ApiClient } from '../core/api-client';
import {
  AcceptedAccount,
  AccountsMode,
  AccountsResult,
  CreateAccountWire,
} from '../core/api-contract';
import { decodeAcceptedAccount, decodeAccountsResult } from '../core/api-decoders';
import { ApiError, normalizeError } from '../core/api-error';
import { AuthSession } from '../core/auth-session';
import { Feedback } from '../shared/feedback';
import { FormField } from '../shared/form-field';
import { Icon } from '../shared/icon';
const requiredText = (max: number, pattern?: RegExp) =>
  new FormControl('', {
    nonNullable: true,
    validators: [
      Validators.required,
      Validators.maxLength(max),
      ...(pattern ? [Validators.pattern(pattern)] : []),
    ],
  });
@Component({
  selector: 'app-accounts',
  imports: [ReactiveFormsModule, Feedback, FormField, Icon],
  templateUrl: './accounts.html',
})
export class Accounts {
  protected readonly auth = inject(AuthSession);
  private readonly api = inject(ApiClient);
  private readonly destroyRef = inject(DestroyRef);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
  protected readonly mode = signal<AccountsMode>('simulated');
  protected readonly tab = signal<'list' | 'create'>('list');
  protected readonly code = new FormControl('', {
    nonNullable: true,
    validators: [Validators.maxLength(15), Validators.pattern(/^[A-Za-z0-9_-]*$/)],
  });
  protected readonly result = signal<AccountsResult | null>(null);
  protected readonly accepted = signal<AcceptedAccount | null>(null);
  protected readonly loading = signal(false);
  protected readonly saving = signal(false);
  protected readonly error = signal<ApiError | null>(null);
  protected readonly form = new FormGroup({
    code: requiredText(15, /^[A-Za-z0-9_-]+$/),
    identificationType: requiredText(2, /^[A-Za-z0-9]+$/),
    document: requiredText(15, /^[A-Za-z0-9-]+$/),
    name: requiredText(200, /\S/),
    firstName: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(50)] }),
    lastName: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(50)] }),
    email: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.email, Validators.maxLength(100)],
    }),
    personType: new FormControl(1, {
      nonNullable: true,
      validators: [Validators.required, Validators.min(1), Validators.max(2)],
    }),
    cityCode: requiredText(5, /^\d{1,5}$/),
    departmentCode: requiredText(2, /^\d{1,2}$/),
    countryCode: requiredText(3, /^\d{1,3}$/),
    customerType: new FormControl(1, {
      nonNullable: true,
      validators: [Validators.required, Validators.min(1), Validators.max(5)],
    }),
    registeredAt: new FormControl(new Date().toISOString().slice(0, 10), {
      nonNullable: true,
      validators: [Validators.required],
    }),
    address: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(100)] }),
    additionalAddress: new FormControl('', {
      nonNullable: true,
      validators: [Validators.maxLength(100)],
    }),
    phone: new FormControl('', {
      nonNullable: true,
      validators: [Validators.maxLength(15), Validators.pattern(/^[0-9+() -]*$/)],
    }),
    website: new FormControl('', {
      nonNullable: true,
      validators: [Validators.maxLength(2048), Validators.pattern(/^$|^https?:\/\/.+/)],
    }),
    externalCode: new FormControl('', {
      nonNullable: true,
      validators: [Validators.maxLength(50)],
    }),
  });
  constructor() {
    this.load();
  }
  protected setMode(mode: AccountsMode): void {
    if (this.saving() || this.loading() || (mode === 'real' && !this.auth.canUseRealAccounts()))
      return;
    this.mode.set(mode);
    this.accepted.set(null);
    this.result.set(null);
    this.load();
  }
  protected load(): void {
    if (this.loading() || this.saving()) return;
    if (this.code.invalid) {
      this.code.markAsTouched();
      return;
    }
    this.loading.set(true);
    this.error.set(null);
    this.result.set(null);
    this.api
      .get(
        `/api/v1/accounts?mode=${this.mode()}&code=${encodeURIComponent(this.code.value.trim())}`,
        decodeAccountsResult,
      )
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.loading.set(false)),
      )
      .subscribe({
        next: (result) => this.result.set(result),
        error: (error) => this.error.set(normalizeError(error)),
      });
  }
  protected field(path: string): string {
    return this.error()?.fieldErrors[path]?.join(' ') ?? '';
  }
  protected create(): void {
    if (this.saving()) return;
    this.form.markAllAsTouched();
    if (this.form.invalid) {
      this.error.set(new ApiError('Revisa los campos de la cuenta antes de enviarla.'));
      queueMicrotask(() =>
        this.host.nativeElement.querySelector<HTMLInputElement>('input.ng-invalid')?.focus(),
      );
      return;
    }
    const raw = this.form.getRawValue();
    const body: CreateAccountWire = {
      ...raw,
      code: raw.code.trim(),
      name: raw.name.trim(),
      mode: this.mode(),
      registeredAt: `${raw.registeredAt}T00:00:00Z`,
    };
    this.saving.set(true);
    this.error.set(null);
    this.accepted.set(null);
    this.api
      .post('/api/v1/accounts', body, undefined, decodeAcceptedAccount)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.saving.set(false)),
      )
      .subscribe({
        next: (result) => {
          this.accepted.set(result);
          this.code.setValue(result.code);
        },
        error: (error) => this.error.set(normalizeError(error)),
      });
  }
  protected consultAccepted(): void {
    this.tab.set('list');
    this.load();
  }
  protected startAnother(): void {
    this.accepted.set(null);
    this.error.set(null);
    this.form.reset();
  }
}
