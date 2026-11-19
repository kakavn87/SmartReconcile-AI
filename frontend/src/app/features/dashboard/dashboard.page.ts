import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ControlTowerStore } from '../../core/stores/control-tower.store';
import { OrdersStore } from '../../core/stores/orders.store';

@Component({
  standalone:true,
  imports:[CurrencyPipe, DatePipe, DecimalPipe, RouterLink],
  changeDetection:ChangeDetectionStrategy.OnPush,
  template:`
    <div class="mb-8">
      <div class="mb-2 text-xs font-bold uppercase tracking-[.14em] text-blue-600">Financial Control Tower</div>
      <div class="flex flex-col gap-3 md:flex-row md:items-end md:justify-between">
        <div><h1 class="text-3xl font-extrabold tracking-tight text-slate-950">Finanzübersicht</h1><p class="mt-1.5 text-sm text-slate-500">Die finanzielle Integrität Ihres Unternehmens auf einen Blick.</p></div>
        <a routerLink="/abgleich" class="app-button bg-slate-950 text-white hover:bg-slate-800">Abgleich starten <span class="ml-2">→</span></a>
      </div>
    </div>
    @if (store.error()) { <div class="mb-6 rounded-2xl border border-red-200 bg-red-50 p-4 text-sm font-medium text-red-700">{{store.error()}}</div> }
    @if (store.loading()) { <div class="app-card p-8 text-center text-sm text-slate-500">Finanzdaten werden geladen …</div> }
    @else if (store.data(); as data) {
      <div class="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <div class="app-card p-5"><div class="text-xs font-bold uppercase tracking-wide text-slate-400">Finanzielle Integrität</div><div class="mt-3 text-4xl font-extrabold text-slate-950">{{data.financialIntegrityScore | number:'1.1-1'}}<span class="text-sm text-slate-400"> / 100</span></div><div class="mt-3 h-2 overflow-hidden rounded-full bg-slate-100"><div class="h-full rounded-full bg-emerald-500" [style.width.%]="data.financialIntegrityScore"></div></div><div class="mt-3 text-xs leading-5 text-slate-500">{{data.financialIntegrityExplanation}}</div></div>
        <div class="app-card p-5"><div class="text-xs font-bold uppercase tracking-wide text-slate-400">Umsatz</div><div class="mt-4 text-2xl font-extrabold text-slate-950">{{data.totalRevenue | currency:'EUR':'symbol':'1.2-2':'de-DE'}}</div><div class="mt-2 text-xs text-slate-500">Erwarteter Zahlungseingang</div></div>
        <div class="app-card p-5"><div class="text-xs font-bold uppercase tracking-wide text-slate-400">Zahlungseingang</div><div class="mt-4 text-2xl font-extrabold text-slate-950">{{data.actualCash | currency:'EUR':'symbol':'1.2-2':'de-DE'}}</div><div class="mt-2 text-xs text-slate-500">Erfolgreich verbuchte Zahlungen</div></div>
        <div class="app-card p-5"><div class="text-xs font-bold uppercase tracking-wide text-slate-400">Cash Gap</div><div class="mt-4 text-2xl font-extrabold text-slate-950">{{data.cashGap | currency:'EUR':'symbol':'1.2-2':'de-DE'}}</div><div class="mt-2 text-xs text-slate-500">Umsatz minus Zahlungseingang</div></div>
      </div>
      <div class="mt-6 grid gap-6 xl:grid-cols-[1.4fr_1fr]">
        <div class="app-card p-6"><div class="flex items-start justify-between"><div><div class="text-xs font-bold uppercase tracking-wide text-slate-400">Finanzfälle</div><h2 class="mt-1 text-lg font-extrabold text-slate-950">Heute zu prüfen</h2></div><div class="text-right"><div class="text-sm font-bold text-orange-700">{{data.openCaseCount}} offen</div><div class="text-xs text-red-600">{{data.highRiskCaseCount}} hohes Risiko</div></div></div>
          @if (data.cases.length === 0) { <div class="mt-6 rounded-xl bg-emerald-50 p-5 text-sm font-semibold text-emerald-700">Keine offenen Finanzfälle. Das System ist aktuell sauber.</div> }
          @else {
            <div class="mt-4 divide-y divide-slate-100">
              @for (item of data.cases.slice(0,6); track item.id) {
                <a [routerLink]="'/control-tower/cases/' + item.id" class="flex items-center justify-between gap-4 py-4 hover:bg-slate-50">
                  <div class="min-w-0">
                    <div class="flex gap-2">
                      <span class="text-[10px] font-bold uppercase text-slate-500">{{item.severity}}</span>
                      <span class="text-xs text-slate-400">{{item.status === 'Investigating' ? 'In Prüfung' : 'Offen'}}</span>
                    </div>
                    <div class="mt-1 truncate text-sm font-bold text-slate-900">{{item.title}}</div>
                    <div class="mt-1 truncate text-xs text-slate-500">{{item.description}}</div>
                  </div>
                  <div class="shrink-0 text-right">
                    <div class="text-sm font-extrabold text-slate-900">{{item.affectedAmount | currency:item.currency:'symbol':'1.2-2':'de-DE'}}</div>
                    <div class="mt-1 text-xs font-bold text-blue-600">Fall öffnen →</div>
                  </div>
                </a>
              }
            </div>
          }
        </div>
        <div class="app-card p-6"><div class="text-xs font-bold uppercase tracking-wide text-slate-400">Systemstatus</div><h2 class="mt-1 text-lg font-extrabold text-slate-950">Integrationen</h2><div class="mt-5 space-y-3">@for (system of data.systemHealth; track system.connectionId ?? system.name) { <div class="flex items-center justify-between gap-4 rounded-xl bg-slate-50 px-4 py-3"><div class="min-w-0"><div class="flex items-center gap-2"><div class="text-sm font-bold text-slate-800">{{system.name}}</div><span class="text-[10px] font-bold uppercase tracking-wide text-slate-400">{{system.provider}}</span></div><div class="mt-1 text-xs text-slate-400">{{system.detail ?? 'Letzte Aktivität'}}{{system.lastSeenAtUtc ? ' · ' + (system.lastSeenAtUtc | date:'dd.MM.yyyy HH:mm') : ''}}</div></div><span class="shrink-0 rounded-full px-2.5 py-1 text-[10px] font-bold" [class.bg-emerald-50]="system.status === 'Healthy'" [class.text-emerald-700]="system.status === 'Healthy'" [class.bg-amber-50]="system.status === 'Warning'" [class.text-amber-700]="system.status === 'Warning'" [class.bg-red-50]="system.status === 'Disconnected'" [class.text-red-700]="system.status === 'Disconnected'">{{system.status === 'Healthy' ? 'Aktiv' : system.status === 'Warning' ? 'Warnung' : 'Getrennt'}}</span></div> } @empty { <div class="rounded-xl bg-slate-50 p-5 text-sm text-slate-500">Noch keine Integrationen konfiguriert.</div> }</div></div>
      </div>
      <div class="mt-6 grid gap-4 md:grid-cols-3"><a routerLink="/abweichungen" class="app-card p-5"><div class="text-xs font-bold uppercase tracking-wide text-slate-400">Abweichungen</div><div class="mt-2 text-lg font-extrabold text-slate-950">Alle Fälle →</div><div class="mt-1 text-sm text-slate-500">{{data.affectedAmount | currency:'EUR':'symbol':'1.2-2':'de-DE'}} betroffen</div></a><a routerLink="/bestellungen" class="app-card p-5"><div class="text-xs font-bold uppercase tracking-wide text-slate-400">Vertrieb</div><div class="mt-2 text-lg font-extrabold text-slate-950">Bestellungen →</div></a><a routerLink="/zahlungen" class="app-card p-5"><div class="text-xs font-bold uppercase tracking-wide text-slate-400">Liquidität</div><div class="mt-2 text-lg font-extrabold text-slate-950">Zahlungen →</div></a></div>
    } @else { <div class="app-card p-8 text-center text-sm text-slate-500">Keine Finanzdaten verfügbar.</div> }
  `
})
export class DashboardPage {
  readonly store=inject(ControlTowerStore);
  readonly ordersStore=inject(OrdersStore);

  constructor(){
    this.store.load();
    this.ordersStore.load();
  }
}
