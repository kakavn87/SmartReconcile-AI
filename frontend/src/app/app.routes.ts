import { Routes } from '@angular/router';
export const routes:Routes=[{path:'',loadComponent:()=>import('./layout/app-shell.component').then(m=>m.AppShellComponent),children:[
{path:'',pathMatch:'full',redirectTo:'dashboard'},
{path:'dashboard',loadComponent:()=>import('./features/dashboard/dashboard.page').then(m=>m.DashboardPage)},
{path:'control-tower/cases/:id',loadComponent:()=>import('./features/control-tower/case-detail.page').then(m=>m.CaseDetailPage)},
{path:'abweichungen',loadComponent:()=>import('./features/discrepancies/discrepancies.page').then(m=>m.DiscrepanciesPage)},
{path:'bestellungen',loadComponent:()=>import('./features/orders/orders.page').then(m=>m.OrdersPage)},
{path:'zahlungen',loadComponent:()=>import('./features/payments/payments.page').then(m=>m.PaymentsPage)},
{path:'rechnungen',loadComponent:()=>import('./features/invoices/invoices.page').then(m=>m.InvoicesPage)},
{path:'abgleich',loadComponent:()=>import('./features/reconciliation/reconciliation.page').then(m=>m.ReconciliationPage)},
{path:'einstellungen',loadComponent:()=>import('./features/settings/settings.page').then(m=>m.SettingsPage)},
{path:'**',redirectTo:'dashboard'}]}];