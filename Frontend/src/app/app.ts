import { Component, inject, signal } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthSession } from './core/auth-session';
import { RuntimeConfig } from './core/runtime-config';
import { Icon } from './shared/icon';
import { BreakpointObserver } from '@angular/cdk/layout';
import { CdkTrapFocus } from '@angular/cdk/a11y';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  imports: [RouterOutlet, RouterLink, RouterLinkActive, Icon, CdkTrapFocus],
  host: { '(document:keydown.escape)': 'mobileMenu.set(false)' },
  selector: 'app-root',
  styleUrl: './app.scss',
  templateUrl: './app.html',
})
export class App {
  protected readonly auth = inject(AuthSession);
  protected readonly config = inject(RuntimeConfig);
  protected readonly mobileMenu = signal(false);
  protected readonly isMobile = signal(false);
  constructor() {
    inject(BreakpointObserver)
      .observe('(max-width: 720px)')
      .pipe(takeUntilDestroyed())
      .subscribe((result) => {
        this.isMobile.set(result.matches);
        if (!result.matches) this.mobileMenu.set(false);
      });
  }
  protected onRouteChange(): void {
    this.mobileMenu.set(false);
    requestAnimationFrame(() => document.getElementById('main')?.focus({ preventScroll: true }));
  }
}
