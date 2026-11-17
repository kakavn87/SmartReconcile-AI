import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { DashboardStore } from '../../core/stores/dashboard.store';

@Component({
  standalone:true,
  imports:[CurrencyPipe,RouterLink],
  providers:[DashboardStore],
  changeDetection:ChangeDetectionStrategy.OnPush,
  template:`
    <div class="mb-8"><h1 class="text-2xl font-bold tracking-tight">Dashboard</h1><p class="mt-1 text-sm text-slate-500">Überblick über Aufträge, Zahlungen und Abweichungen.</p></div>
    @if (store.error()) { <div class="mb-6 rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-700">{{store.error()}}</div> }
    <div class="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
      <div class="app-card p-5"><div class="text-sm text-slate-500">Umsatz</div><div class="mt-2 text-2xl font-bold">{{store.data()?.totalRevenue ?? 0 | currency:'EUR':'symbol':'1.2-2':'de-DE'}}</div><div class="mt-2 text-xs text-slate-400">{{store.data()?.orderCount ?? 0}} Bestellungen</div></div>
      <div class="app-card p-5"><div class="text-sm text-slate-500">Offene Fälle</div><div class="mt-2 text-2xl font-bold">{{store.data()?.openDiscrepancyCount ?? 0}}</div><a routerLink="/abweichungen" class="mt-2 inline-block text-xs font-semibold hover:underline">Fälle prüfen →</a></div>
      <div class="app-card p-5"><div class="text-sm text-slate-500">Betroffener Betrag</div><div class="mt-2 text-2xl font-bold">{{store.data()?.totalAffectedAmount ?? 0 | currency:'EUR':'symbol':'1.2-2':'de-DE'}}</div><div class="mt-2 text-xs text-slate-400">offene Abweichungen</div></div>
      <div class="app-card p-5"><div class="text-sm text-slate-500">Datenbestand</div><div class="mt-2 text-2xl font-bold">{{store.data()?.invoiceCount ?? 0}}</div><div class="mt-2 text-xs text-slate-400">{{store.data()?.paymentCount ?? 0}} Zahlungen</div></div>
    </div>
    <div class="mt-8 app-card p-6"><div class="flex flex-col gap-4 md:flex-row md:items-center md:justify-between"><div><h2 class="font-semibold">Automatische Abstimmung</h2><p class="mt-1 text-sm text-slate-500">Prüft Bestellungen, Zahlungen und Rechnungen nach den definierten Regeln.</p></div><a routerLink="/abgleich" class="rounded-lg bg-slate-900 px-4 py-2 text-sm font-semibold text-white">Abgleich öffnen</a></div></div>
  `
})
export class DashboardPage {
  readonly store=inject(DashboardStore);
  constructor(){ this.store.load(); }
}
