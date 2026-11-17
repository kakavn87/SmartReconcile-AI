import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { DashboardStore } from '../../core/stores/dashboard.store';

@Component({
  standalone: true,
  imports: [CurrencyPipe, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="mb-8">
      <div class="mb-2 text-xs font-bold uppercase tracking-[.14em] text-blue-600">Finance Operations</div>
      <div class="flex flex-col gap-3 md:flex-row md:items-end md:justify-between">
        <div>
          <h1 class="text-3xl font-extrabold tracking-tight text-slate-950">Dashboard</h1>
          <p class="mt-1.5 text-sm text-slate-500">Aktueller Überblick über Umsatz, Zahlungen und erkannte Abweichungen.</p>
        </div>
        <a routerLink="/abgleich" class="app-button inline-flex items-center justify-center bg-slate-950 text-white shadow-lg shadow-slate-900/10 hover:bg-slate-800">Abgleich starten <span class="ml-2">→</span></a>
      </div>
    </div>

    @if (store.error()) {
      <div class="mb-6 rounded-2xl border border-red-200 bg-red-50 p-4 text-sm font-medium text-red-700">{{store.error()}}</div>
    }

    <div class="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
      <div class="app-card app-card-hover p-5">
        <div class="flex items-start justify-between"><div class="text-xs font-bold uppercase tracking-wide text-slate-400">Umsatz</div><span class="rounded-lg bg-blue-50 px-2 py-1 text-xs font-bold text-blue-600">EUR</span></div>
        <div class="mt-4 text-2xl font-extrabold tracking-tight text-slate-950">{{store.data()?.totalRevenue ?? 0 | currency:'EUR':'symbol':'1.2-2':'de-DE'}}</div>
        <div class="mt-2 text-xs text-slate-500">{{store.data()?.orderCount ?? 0}} Bestellungen im Datenbestand</div>
      </div>

      <div class="app-card app-card-hover p-5">
        <div class="flex items-start justify-between"><div class="text-xs font-bold uppercase tracking-wide text-slate-400">Offene Fälle</div><span class="rounded-lg bg-orange-50 px-2 py-1 text-xs font-bold text-orange-600">Prüfen</span></div>
        <div class="mt-4 text-2xl font-extrabold tracking-tight text-slate-950">{{store.data()?.openDiscrepancyCount ?? 0}}</div>
        <a routerLink="/abweichungen" class="mt-2 inline-block text-xs font-bold text-blue-600 hover:text-blue-700">Fälle öffnen →</a>
      </div>

      <div class="app-card app-card-hover p-5">
        <div class="flex items-start justify-between"><div class="text-xs font-bold uppercase tracking-wide text-slate-400">Betroffener Betrag</div><span class="rounded-lg bg-amber-50 px-2 py-1 text-xs font-bold text-amber-700">Offen</span></div>
        <div class="mt-4 text-2xl font-extrabold tracking-tight text-slate-950">{{store.data()?.totalAffectedAmount ?? 0 | currency:'EUR':'symbol':'1.2-2':'de-DE'}}</div>
        <div class="mt-2 text-xs text-slate-500">Summe der offenen Abweichungen</div>
      </div>

      <div class="app-card app-card-hover p-5">
        <div class="flex items-start justify-between"><div class="text-xs font-bold uppercase tracking-wide text-slate-400">Datenbestand</div><span class="rounded-lg bg-emerald-50 px-2 py-1 text-xs font-bold text-emerald-700">Live</span></div>
        <div class="mt-4 text-2xl font-extrabold tracking-tight text-slate-950">{{store.data()?.invoiceCount ?? 0}}</div>
        <div class="mt-2 text-xs text-slate-500">{{store.data()?.paymentCount ?? 0}} Zahlungen · {{store.data()?.orderCount ?? 0}} Bestellungen</div>
      </div>
    </div>

    <div class="mt-6 grid gap-6 xl:grid-cols-[1.5fr_1fr]">
      <div class="app-card p-6">
        <div class="flex items-start justify-between">
          <div><div class="text-xs font-bold uppercase tracking-wide text-slate-400">Automatisierung</div><h2 class="mt-1 text-lg font-extrabold text-slate-950">Intelligente Abstimmung</h2></div>
          <div class="flex h-10 w-10 items-center justify-center rounded-xl bg-blue-50 text-blue-600">↻</div>
        </div>
        <p class="mt-3 max-w-2xl text-sm leading-6 text-slate-500">SmartReconcile prüft Bestellungen, Zahlungen und Rechnungen nach definierten Geschäftsregeln und legt erkannte Fälle automatisch als Abweichungen an.</p>
        <div class="mt-5 flex flex-wrap gap-2 text-xs font-semibold text-slate-600">
          <span class="rounded-full bg-slate-100 px-3 py-1.5">Fehlende Zahlungen</span>
          <span class="rounded-full bg-slate-100 px-3 py-1.5">Betragsabweichungen</span>
          <span class="rounded-full bg-slate-100 px-3 py-1.5">Doppelte Rechnungen</span>
        </div>
      </div>
      <div class="app-card bg-slate-950 p-6 text-white">
        <div class="text-xs font-bold uppercase tracking-wide text-slate-400">Systemstatus</div>
        <div class="mt-4 flex items-center gap-3"><span class="h-3 w-3 rounded-full bg-emerald-400 shadow-[0_0_0_5px_rgba(52,211,153,.12)]"></span><span class="font-bold">Backend erreichbar</span></div>
        <p class="mt-3 text-sm leading-6 text-slate-400">Die Kennzahlen werden direkt aus dem aktuellen Backend-State geladen.</p>
      </div>
    </div>
  `
})
export class DashboardPage {
  readonly store = inject(DashboardStore);
  constructor() { this.store.load(); }
}
