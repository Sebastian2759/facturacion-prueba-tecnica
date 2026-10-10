import { Routes } from '@angular/router';
import { evaluationGuard } from './core/evaluation-guard';
export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'facturas' },
  {
    path: 'acceso',
    title: 'Acceso · Nexo',
    loadComponent: () => import('./core/access').then((m) => m.Access),
  },
  {
    path: 'facturas',
    canActivate: [evaluationGuard],
    title: 'Mis facturas · Nexo',
    loadComponent: () => import('./facturas/invoice-list').then((m) => m.InvoiceList),
  },
  {
    path: 'facturas/nueva',
    canActivate: [evaluationGuard],
    title: 'Nueva factura · Nexo',
    loadComponent: () => import('./facturas/invoice-new').then((m) => m.InvoiceNew),
  },
  {
    path: 'facturas/:number',
    canActivate: [evaluationGuard],
    title: 'Detalle de factura · Nexo',
    loadComponent: () => import('./facturas/invoice-detail').then((m) => m.InvoiceDetail),
  },
  {
    path: 'cuentas',
    canActivate: [evaluationGuard],
    title: 'Cuentas · Nexo',
    loadComponent: () => import('./cuentas/accounts').then((m) => m.Accounts),
  },
  { path: '**', redirectTo: '' },
];
