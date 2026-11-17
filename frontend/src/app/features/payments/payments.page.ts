import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { ApiService } from '../../core/api/api.service';
import { Payment } from '../../core/models/api.models';

@Component({
  standalone:true, imports:[CurrencyPipe,DatePipe], changeDetection:ChangeDetectionStrategy.OnPush,
  template:`
    <div class="mb-8"><h1 class="text-2xl font-bold tracking-tight">Zahlungen</h1><p class="mt-1 text-sm text-slate-500">Zahlungseingänge und Zahlungsstatus.</p></div>
    @if(loading){<div class="app-card p-8 text-sm text-slate-500">Zahlungen werden geladen…</div>}
    @if(error){<div class="mb-4 rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-700">{{error}}</div>}
    @if(!loading){<div class="app-card overflow-hidden"><div class="overflow-x-auto"><table class="min-w-full text-sm">
      <thead class="border-b border-slate-200 bg-slate-50 text-left text-xs uppercase tracking-wide text-slate-500"><tr><th class="px-5 py-3">Transaktion</th><th class="px-5 py-3">Bestellung</th><th class="px-5 py-3">Betrag</th><th class="px-5 py-3">Status</th><th class="px-5 py-3">Datum</th></tr></thead>
      <tbody class="divide-y divide-slate-100">@for(item of items;track item.id){<tr><td class="px-5 py-4 font-semibold">{{item.transactionId}}</td><td class="px-5 py-4 text-slate-500">{{item.orderId}}</td><td class="px-5 py-4 font-semibold">{{item.amount|currency:item.currency:'symbol':'1.2-2':'de-DE'}}</td><td class="px-5 py-4">{{statusLabel(item.status)}}</td><td class="px-5 py-4 text-slate-500">{{item.paidAtUtc|date:'dd.MM.yyyy HH:mm'}}</td></tr>}</tbody>
    </table></div></div>}
  `
})
export class PaymentsPage {
  private readonly api=inject(ApiService); items:Payment[]=[]; loading=true; error:string|null=null;
  constructor(){this.api.payments().subscribe({next:x=>{this.items=x;this.loading=false},error:()=>{this.error='Zahlungen konnten nicht geladen werden.';this.loading=false}})}
  statusLabel(s:string){return ({Pending:'Ausstehend',Succeeded:'Erfolgreich',Failed:'Fehlgeschlagen',Refunded:'Erstattet'} as Record<string,string>)[s]??s}
}