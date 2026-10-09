import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class RuntimeConfig {
  readonly error = signal<string | null>(null);
  private base = '';
  async load(): Promise<void> {
    try {
      const response = await fetch('/config.json', {
        cache: 'no-store',
        credentials: 'same-origin',
      });
      if (!response.ok) throw new Error('config');
      const value: unknown = await response.json();
      const base = (value as { apiBaseUrl?: unknown })?.apiBaseUrl;
      if (typeof base !== 'string') throw new Error('config');
      if (base) {
        const url = new URL(base);
        if (url.protocol !== 'https:' || url.username || url.password || url.search || url.hash) {
          throw new Error('config');
        }
      }
      this.base = base.replace(/\/$/, '');
    } catch {
      this.error.set(
        'No se pudo cargar una configuración válida. Revisa config.json antes de continuar.',
      );
    }
  }
  url(path: string): string {
    return `${this.base}${path}`;
  }
}
