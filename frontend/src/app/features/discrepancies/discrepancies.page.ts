import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { DiscrepancyStatus, DiscrepancyType } from '../../core/models/api.models';
import { DiscrepancyStore } from '../../core/stores/discrepancy.store';

@Component({
  standalone:true,
  imports:[CurrencyPipe,DatePipe],
  changeDetection:ChangeDetectionStrategy.OnPush,
  template:`
    <div class="mb-8 flex flex-col gap-4 md:flex-row md:items-end md:justify-between">
      <div><h1 class="text-2xl font-bold tracking-tight">Abweichungen</h1><p class="mt-1 text-sm text-slate-500">Automatisch erkannte Fälle.</p></div>
      <div class="flex gap-2">
        <select class="rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm" [value]="store.filterStatus()" (change)="store.setStatus($any($event.target).value)"><option value="Open">Offen</option><option value="Resolved">Gelöst</option><option value="Ignored">Ignoriert</option><option value="Alle">Alle</option></select>
        <select class="rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm" [value]="store.filterType()" (change)="store.setType($any($event.target).value)"><option value="Alle">Alle Typen</option><option value="MissingPayment">Fehlende Zahlung</option><option value="OrphanedPayment">Zahlung ohne Abschluss</option><option value="AmountMismatch">Betragsabweichung</option><option value="DuplicatePayment">Doppelte Zahlung</option><option value="CurrencyMismatch">Währungsabweichung</option><option value="DuplicateInvoice">Doppelte Rechnung</option></select>
      </div>
    </div>
    <div class="mb-4"><input class="w-full rounded-lg border border-slate-300 bg-white px-3 py-2.5 text-sm outline-none focus:border-slate-500" placeholder="Nach Vorgang oder Beschreibung suchen…" [value]="store.search()" (input)="store.setSearch($any($event.target).value)"></div>
    @if (store.error()) { <div class="mb-4 rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-700">{{store.error()}}</div> }
    <div class="app-card overflow-hidden"><div class="overflow-x-auto"><table class="min-w-full text-sm">
      <thead class="border-b border-slate-200 bg-slate-50 text-left text-xs uppercase tracking-wide text-slate-500"><tr><th class="px-5 py-3">Typ</th><th class="px-5 py-3">Fall</th><th class="px-5 py-3">Betrag</th><th class="px-5 py-3">Status</th><th class="px-5 py-3">Erkannt</th><th class="px-5 py-3 text-right">Aktion</th></tr></thead>
      <tbody class="divide-y divide-slate-100">
        @for(item of store.filtered(); track item.id) {
          <tr class="hover:bg-slate-50"><td class="px-5 py-4 font-medium">{{typeLabel(item.type)}}</td><td class="px-5 py-4"><div class="font-medium">{{item.title}}</div><div class="mt-1 max-w-xl text-xs text-slate-500">{{item.description}}</div></td><td class="px-5 py-4 font-semibold">{{item.affectedAmount|currency:'EUR':'symbol':'1.2-2':'de-DE'}}</td><td class="px-5 py-4"><span class="badge" [class.status-open]="item.status==='Open'" [class.status-resolved]="item.status==='Resolved'" [class.status-ignored]="item.status==='Ignored'">{{statusLabel(item.status)}}</span></td><td class="px-5 py-4 whitespace-nowrap text-slate-500">{{item.detectedAtUtc|date:'dd.MM.yyyy HH:mm'}}</td><td class="px-5 py-4 text-right">@if(item.status==='Open'){<button class="mr-2 rounded-lg border border-slate-300 px-3 py-1.5 text-xs font-semibold" (click)="store.updateStatus({id:item.id,status:'Ignored'})">Ignorieren</button><button class="rounded-lg bg-slate-900 px-3 py-1.5 text-xs font-semibold text-white" (click)="store.updateStatus({id:item.id,status:'Resolved'})">Lösen</button>}@else{<span class="text-xs text-slate-400">Keine Aktion</span>}</td></tr>
        } @empty { <tr><td colspan="6" class="px-5 py-14 text-center text-sm text-slate-500">Keine Abweichungen für den aktuellen Filter.</td></tr> }
      </tbody>
    </table></div></div>
  `
})
export class DiscrepanciesPage {
  readonly store=inject(DiscrepancyStore);
  constructor(){this.store.load();}
  typeLabel(t:DiscrepancyType){return ({MissingPayment:'Fehlende Zahlung',OrphanedPayment:'Zahlung ohne Abschluss',AmountMismatch:'Betragsabweichung',DuplicatePayment:'Doppelte Zahlung',CurrencyMismatch:'Währungsabweichung',DuplicateInvoice:'Doppelte Rechnung'} as Record<DiscrepancyType,string>)[t];}
  statusLabel(s:DiscrepancyStatus){return ({Open:'Offen',Resolved:'Gelöst',Ignored:'Ignoriert'} as Record<DiscrepancyStatus,string>)[s];}
}
