import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { ApiService } from '../../core/api/api.service';
import { ReconciliationResult } from '../../core/models/api.models';

@Component({
  standalone:true, imports:[CurrencyPipe,DatePipe], changeDetection:ChangeDetectionStrategy.OnPush,
  template:`
    <div class="mb-8"><h1 class="text-2xl font-bold tracking-tight">Abgleich</h1><p class="mt-1 text-sm text-slate-500">Automatische Abstimmung jetzt ausführen.</p></div>
    <div class="app-card p-6"><div class="flex flex-col gap-4 md:flex-row md:items-center md:justify-between"><div><div class="font-semibold">Manuellen Abgleich starten</div><div class="mt-1 text-sm text-slate-500">Prüft Bestellungen, Zahlungen und Rechnungen nach den definierten Regeln.</div></div><button class="rounded-lg bg-slate-900 px-4 py-2.5 text-sm font-semibold text-white disabled:opacity-50" [disabled]="loading" (click)="run()">{{loading?'Prüfung läuft…':'Abgleich jetzt starten'}}</button></div></div>
    @if(result){<div class="mt-6 grid gap-4 md:grid-cols-2"><div class="app-card p-5"><div class="text-sm text-slate-500">Neue Fälle</div><div class="mt-2 text-2xl font-bold">{{result.createdCount}}</div></div><div class="app-card p-5"><div class="text-sm text-slate-500">Erkannte Abweichungen</div><div class="mt-2 text-2xl font-bold">{{result.discrepancies.length}}</div></div></div>
      <div class="mt-6 app-card overflow-hidden"><table class="min-w-full text-sm"><thead class="border-b border-slate-200 bg-slate-50 text-left text-xs uppercase tracking-wide text-slate-500"><tr><th class="px-5 py-3">Fall</th><th class="px-5 py-3">Betrag</th><th class="px-5 py-3">Erkannt</th></tr></thead><tbody class="divide-y divide-slate-100">@for(item of result.discrepancies;track item.id){<tr><td class="px-5 py-4">{{item.title}}</td><td class="px-5 py-4 font-semibold">{{item.affectedAmount|currency:'EUR':'symbol':'1.2-2':'de-DE'}}</td><td class="px-5 py-4 text-slate-500">{{item.detectedAtUtc|date:'dd.MM.yyyy HH:mm'}}</td></tr>}</tbody></table></div>}
    @if(error){<div class="mt-6 rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-700">{{error}}</div>}
  `
})
export class ReconciliationPage {
  private readonly api=inject(ApiService); loading=false; error:string|null=null; result:ReconciliationResult|null=null;
  run(){this.loading=true;this.error=null;this.api.reconciliation().subscribe({next:x=>{this.result=x;this.loading=false},error:()=>{this.error='Der Abgleich konnte nicht durchgeführt werden.';this.loading=false}})}
}