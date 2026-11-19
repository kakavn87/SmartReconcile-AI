import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { OrdersStore } from '../../core/stores/orders.store';
import { ApiService } from '../../core/api/api.service';

@Component({
  standalone:true, imports:[CurrencyPipe,DatePipe,RouterLink], changeDetection:ChangeDetectionStrategy.OnPush,
  template:`
    <div class="mb-8"><h1 class="text-2xl font-bold tracking-tight">Bestellungen</h1><p class="mt-1 text-sm text-slate-500">Aktueller Auftragsbestand aus dem Backend.</p></div>
    @if(store.error()){<div class="mb-4 rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-700">{{store.error()}}</div>}
    @if(store.loading()){<div class="app-card p-8 text-sm text-slate-500">Bestellungen werden geladen…</div>}
    @if(!store.loading()){<div class="app-card overflow-hidden"><div class="overflow-x-auto"><table class="min-w-full text-sm">
      <thead class="border-b border-slate-200 bg-slate-50 text-left text-xs uppercase tracking-wide text-slate-500"><tr><th class="px-5 py-3">Bestellung</th><th class="px-5 py-3">Kunde</th><th class="px-5 py-3">Betrag</th><th class="px-5 py-3">Status</th><th class="px-5 py-3">Datum</th></tr></thead>
      <tbody class="divide-y divide-slate-100">@for(item of store.items();track item.id){<tr class="cursor-pointer hover:bg-slate-50" (click)="open(item)"><td class="px-5 py-4 font-semibold">{{item.orderNumber}}</td><td class="px-5 py-4"><div>{{item.customerName}}</div><div class="text-xs text-slate-400">{{item.customerEmail}}</div></td><td class="px-5 py-4 font-semibold">{{item.totalAmount|currency:item.currency:'symbol':'1.2-2':'de-DE'}}</td><td class="px-5 py-4">{{statusLabel(item.status)}}</td><td class="px-5 py-4 text-slate-500">{{item.createdAtUtc|date:'dd.MM.yyyy HH:mm'}}</td></tr>}</tbody>
    </table></div></div>}
    @if(selected()){<div class="fixed inset-0 z-40 bg-slate-950/20" (click)="close()"></div><aside class="fixed right-0 top-0 z-50 h-full w-full max-w-lg overflow-y-auto bg-white p-6 shadow-2xl"><div class="flex justify-between"><div><div class="text-xs font-bold uppercase tracking-wide text-slate-400">BESTELLUNG</div><h2 class="mt-1 text-xl font-extrabold">{{selected()!.orderNumber}}</h2></div><button (click)="close()" class="text-2xl text-slate-400">×</button></div><div class="mt-6 space-y-4 text-sm"><div><span class="text-slate-400">Kunde</span><div class="font-bold">{{selected()!.customerName}}</div><div>{{selected()!.customerEmail}}</div></div><div><span class="text-slate-400">Betrag</span><div class="font-bold">{{selected()!.totalAmount|currency:selected()!.currency:'symbol':'1.2-2':'de-DE'}}</div></div><div><span class="text-slate-400">Status</span><div class="font-bold">{{statusLabel(selected()!.status)}}</div></div><a routerLink="/payments" class="block rounded-xl bg-slate-50 p-4 font-bold">→ Zahlungen</a><button type="button" (click)="createInvoice(selected()!);$event.stopPropagation()" class="w-full rounded-xl bg-slate-950 p-4 text-left font-bold text-white">+ Rechnung aus Bestellung erzeugen</button><a routerLink="/invoices" class="block rounded-xl bg-slate-50 p-4 font-bold">→ Rechnungen</a></div></aside>}
  `
})
export class OrdersPage {
  readonly store=inject(OrdersStore); readonly api=inject(ApiService); readonly selected=signal<import('../../core/models/api.models').Order|null>(null);
  open(x:import('../../core/models/api.models').Order){this.selected.set(x)} close(){this.selected.set(null)}
  createInvoice(order:import('../../core/models/api.models').Order){ this.api.createInvoice({invoiceType:'Sales',invoiceNumber:'',partnerName:order.customerName,amount:order.totalAmount,currency:order.currency,invoiceDateUtc:order.createdAtUtc,rawText:'Manuell aus Bestellung erzeugt.',orderId:order.id}).subscribe(()=>this.close()); } constructor(){this.store.load()} statusLabel(s:string){return ({Pending:'Offen',Paid:'Bezahlt',OnHold:'Zurückgestellt',Cancelled:'Storniert',Refunded:'Erstattet'} as Record<string,string>)[s]??s} }