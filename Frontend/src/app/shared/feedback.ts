import { Component, input } from '@angular/core';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { ApiError } from '../core/api-error';
@Component({
  selector: 'app-feedback',
  imports: [MatProgressBarModule],
  template: `
    @if (loading()) {
      <div role="status" class="loading-state">
        <mat-progress-bar mode="indeterminate" /><span>{{ loadingText() }}</span>
      </div>
    }
    @if (error(); as failure) {
      <div class="alert error" role="alert">
        <strong>No pudimos completar la operación</strong>
        <p>{{ failure.message }}</p>
        @if (failure.traceId) {
          <small>Referencia: {{ failure.traceId }}</small>
        }
      </div>
    }
  `,
})
export class Feedback {
  readonly loading = input(false);
  readonly loadingText = input('Cargando…');
  readonly error = input<ApiError | null>(null);
}
