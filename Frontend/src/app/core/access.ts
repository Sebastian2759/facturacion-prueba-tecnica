import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthSession } from './auth-session';
import { Icon } from '../shared/icon';
@Component({
  selector: 'app-access',
  imports: [RouterLink, Icon],
  template: `
    <div class="access-panel panel">
      <span class="access-symbol"><app-icon name="shield" /></span>
      <div class="eyebrow">APLICACIÓN LOCAL</div>
      <h1>Tu espacio de evaluación.</h1>
      <p>
        No necesitas una cuenta cloud. Inicia la aplicación con el comando del paquete y conecta
        para crear y consultar facturas en esta instalación.
      </p>
      @if (auth.error(); as error) {
        <div class="alert error" role="alert">{{ error }}</div>
      }
      @if (auth.canEvaluate()) {
        <a routerLink="/facturas/nueva" class="button primary"
          >Crear mi primera factura<app-icon name="arrow"
        /></a>
      } @else {
        <p class="helper-text">
          La sesión local no está disponible. Revisa el arranque y vuelve a conectar.
        </p>
        <button class="button secondary" [disabled]="auth.busy()" (click)="auth.initialize()">
          Volver a conectar
        </button>
      }
      <a class="text-link" routerLink="/">Volver a los ejemplos</a>
    </div>
  `,
})
export class Access {
  protected readonly auth = inject(AuthSession);
}
