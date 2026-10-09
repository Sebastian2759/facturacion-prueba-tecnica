import { HttpBackend, HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { firstValueFrom, timeout } from 'rxjs';
import type { Envelope, Session } from './api-contract';
import { unwrap } from './api-error';
import { RuntimeConfig } from './runtime-config';

@Injectable({ providedIn: 'root' })
export class AuthSession {
  private readonly http = new HttpClient(inject(HttpBackend));
  private readonly config = inject(RuntimeConfig);
  readonly state = signal<Session | null>(null);
  readonly error = signal<string | null>(null);
  readonly busy = signal(false);
  readonly canEvaluate = computed(
    () =>
      this.state()?.mode === 'local' &&
      this.state()?.authenticated === true &&
      this.state()?.canEvaluate === true,
  );
  readonly canUseRealAccounts = computed(() => false);

  async initialize(): Promise<void> {
    if (this.config.error()) return;
    this.busy.set(true);
    this.error.set(null);
    try {
      await this.refresh();
    } catch {
      this.state.set(null);
      this.error.set(
        'No pudimos conectar con la aplicación local. Comprueba que siga iniciada y vuelve a conectar.',
      );
    } finally {
      this.busy.set(false);
    }
  }
  invalidate(): void {
    this.state.set(null);
    this.error.set(
      'La sesión local venció. Vuelve a conectar para continuar; tus campos no se han borrado.',
    );
  }
  private async refresh(): Promise<void> {
    const response = await firstValueFrom(
      this.http
        .get<Envelope<Session>>(this.config.url('/api/v1/session'), { withCredentials: true })
        .pipe(timeout(15000)),
    );
    this.state.set(unwrap(response));
  }
}
