import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { InvoicesStore } from '../../core/stores/invoices.store';
import { OrdersStore } from '../../core/stores/orders.store';
import { Invoice, InvoiceType, Order } from '../../core/models/api.models';

@Component({
  standalone:true,
  imports:[CurrencyPipe,DatePipe],
  changeDetection:ChangeDetectionStrategy.OnPush,
  template: `
    <div class="mb-8 flex items-start justify-between gap-4">
      <div><h1 class="text-2xl font-bold tracking-tight">Rechnungen</h1><p class="mt-1 text-sm text-slate-500">Ausgangs- und Eingangsrechnungen des Shops.</p></div>
      <button type="button" (click)="openCreate()" class="app-button bg-slate-950 text-white">+ Rechnung erfassen</button>
    </div>

    @if(store.error()){<div class="mb-4 rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-700">{{store.error()}}</div>}
    @if(!store.loading() || store.items().length){<div class="app-card overflow-hidden"><div class="overflow-x-auto"><table class="min-w-full text-sm">
      <thead class="border-b border-slate-200 bg-slate-50 text-left text-xs uppercase tracking-wide text-slate-500"><tr><th class="px-5 py-3">Rechnung</th><th class="px-5 py-3">Art</th><th class="px-5 py-3">Kunde / Lieferant</th><th class="px-5 py-3">Betrag</th><th class="px-5 py-3">Rechnungsdatum</th></tr></thead>
      <tbody class="divide-y divide-slate-100">@for(item of store.items();track item.id){<tr class="cursor-pointer hover:bg-slate-50" (click)="openDetail(item)"><td class="px-5 py-4 font-semibold">{{item.invoiceNumber}}</td><td class="px-5 py-4">{{typeLabel(item.invoiceType)}}</td><td class="px-5 py-4">{{partner(item)}}</td><td class="px-5 py-4 font-semibold">{{item.amount|currency:item.currency:'symbol':'1.2-2':'de-DE'}}</td><td class="px-5 py-4 text-slate-500">{{item.invoiceDateUtc|date:'dd.MM.yyyy'}}</td></tr>}</tbody>
    </table></div></div>}

    @if(drawer()){<div class="fixed inset-0 z-40 bg-slate-950/20" (click)="closeDrawer()"></div><aside class="fixed right-0 top-0 z-50 h-full w-full max-w-lg overflow-y-auto bg-white p-6 shadow-2xl">
      <div class="flex items-center justify-between"><div><div class="text-xs font-bold uppercase tracking-wide text-slate-400">RECHNUNG</div><h2 class="mt-1 text-xl font-extrabold">{{drawer()!.invoiceNumber}}</h2></div><button type="button" (click)="closeDrawer()" class="text-2xl text-slate-400">×</button></div>
      <div class="mt-6 grid gap-3 text-sm"><div><span class="text-slate-400">Art</span><div class="font-bold">{{typeLabel(drawer()!.invoiceType)}}</div></div><div><span class="text-slate-400">Kunde / Lieferant</span><div class="font-bold">{{partner(drawer()!)}}</div></div><div><span class="text-slate-400">Betrag</span><div class="font-bold">{{drawer()!.amount|currency:drawer()!.currency:'symbol':'1.2-2':'de-DE'}}</div></div><div><span class="text-slate-400">Datum</span><div class="font-bold">{{drawer()!.invoiceDateUtc|date:'dd.MM.yyyy'}}</div></div></div>
      @if(drawer()!.orderId){<div class="mt-6 rounded-xl bg-slate-50 p-4 text-sm"><div class="font-bold">BESTELLUNG</div><div class="mt-1">{{drawer()!.orderId}}</div></div>}
    </aside>}

    @if(createOpen()){<div class="fixed inset-0 z-40 bg-slate-950/20" (click)="closeCreate()"></div><aside class="fixed right-0 top-0 z-50 h-full w-full max-w-xl overflow-y-auto bg-white p-6 shadow-2xl">
      <div class="flex items-center justify-between"><div><div class="text-xs font-bold uppercase tracking-wide text-slate-400">RECHNUNG</div><h2 class="mt-1 text-xl font-extrabold">Rechnung erfassen</h2></div><button type="button" (click)="closeCreate()" class="text-2xl text-slate-400">×</button></div>
      <div class="mt-6 space-y-4">
        <label class="block text-sm font-semibold">Art<select #type class="mt-1 w-full rounded-xl border p-3"><option value="Sales">Ausgangsrechnung</option><option value="Purchase">Eingangsrechnung</option></select></label>
        <label class="block text-sm font-semibold">{{type.value==='Sales'?'Kunde':'Lieferant'}}<input #partnerName class="mt-1 w-full rounded-xl border p-3" /></label>
        <label class="block text-sm font-semibold">Rechnungsnummer<input #number class="mt-1 w-full rounded-xl border p-3" /></label>
        <div class="grid grid-cols-2 gap-3"><label class="block text-sm font-semibold">Betrag<input #amount type="number" step="0.01" class="mt-1 w-full rounded-xl border p-3" /></label><label class="block text-sm font-semibold">Währung<input #currency value="EUR" maxlength="3" class="mt-1 w-full rounded-xl border p-3" /></label></div>
        <label class="block text-sm font-semibold">Rechnungsdatum<input #date type="date" class="mt-1 w-full rounded-xl border p-3" /></label>
        <label class="block text-sm font-semibold">Bestellung (optional)<select #order [disabled]="type.value==='Purchase'" class="mt-1 w-full rounded-xl border p-3"><option value="">Nicht verknüpft (Shopify-Direktabgleich)</option>@for(o of orders.items();track o.id){<option [value]="o.id">{{o.orderNumber}}</option>}</select></label>
        <button type="button" (click)="create(type.value,partnerName.value,number.value,+amount.value,currency.value,date.value,type.value==='Sales'?order?.value??null:null)" class="w-full rounded-xl bg-slate-950 px-4 py-3 text-sm font-extrabold text-white">Rechnung speichern &amp; abstimmen</button>
      </div>
    </aside>}
  `
})
export class InvoicesPage {
  readonly store=inject(InvoicesStore);
  readonly orders=inject(OrdersStore);
  readonly drawer=signal<Invoice|null>(null);
  readonly createOpen=signal(false);
  constructor(){this.store.load();this.orders.load();}
  openCreate(){this.createOpen.set(true)}
  closeCreate(){this.createOpen.set(false)}
  openDetail(x:Invoice){this.drawer.set(x)}
  closeDrawer(){this.drawer.set(null)}
  partner(x:Invoice){return x.invoiceType==='Sales'?(x.customerName||'—'):(x.supplierName||'—')}
  typeLabel(x:InvoiceType){return x==='Sales'?'Ausgangsrechnung':'Eingangsrechnung'}
  create(type:string,partner:string,number:string,amount:number,currency:string,date:string,orderId:string|null){
    this.store.create({invoiceType:type as InvoiceType,partnerName:partner,invoiceNumber:number,amount,currency,invoiceDateUtc:date||null,rawText:'',orderId});
    this.createOpen.set(false);
  }
}
