import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthSession } from './auth-session';

export const evaluationGuard: CanActivateFn = () =>
  inject(AuthSession).canEvaluate() || inject(Router).createUrlTree(['/acceso']);
