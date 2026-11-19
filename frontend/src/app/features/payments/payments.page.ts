import { ChangeDetectionStrategy, Component, effect, inject, signal } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { PaymentsStore } from '../../core/stores/payments.store';
import { OrdersStore } from '../../core/stores/orders.store';

@Component({
  standalone:true, imports:[CurrencyPipe,DatePipe], changeDetection:ChangeDetectionStrategy.OnPush,
  template:`
    <div class="mb-8"><h1 class="text-2xl font-bold tracking-tight">Zahlungen</h1><p class="mt-1 text-sm text-slate-500">Zahlungseingänge und Zahlungsstatus.</p></div>
    @if(store.error()){<div class="mb-4 rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-700">{{store.error()}}</div>}
    @if(store.loading()){<div class="app-card p-8 text-sm text-slate-500">Zahlungen werden geladen…</div>}
    @if(!store.loading()){<div class="app-card overflow-hidden"><div class="overflow-x-auto"><table class="min-w-full text-sm">
      <thead class="border-b border-slate-200 bg-slate-50 text-left text-xs uppercase tracking-wide text-slate-500"><tr><th class="px-5 py-3">Transaktion</th><th class="px-5 py-3">Bestellung</th><th class="px-5 py-3">Art</th><th class="px-5 py-3">Gateway</th><th class="px-5 py-3">Betrag</th><th class="px-5 py-3">Status</th><th class="px-5 py-3">Datum</th></tr></thead>
      <tbody class="divide-y divide-slate-100">@for(item of store.items();track item.id){<tr class="cursor-pointer hover:bg-slate-50" (click)="open(item)"><td class="px-5 py-4 font-semibold">{{displayTransactionId(item.transactionId)}}</td><td class="px-5 py-4 text-slate-500">{{orderLabel(item.orderId)}}</td><td class="px-5 py-4">{{kindLabel(item.kind)}}</td><td class="px-5 py-4 text-slate-500">{{item.gateway ?? '—'}}</td><td class="px-5 py-4 font-semibold">{{item.amount|currency:item.currency:'symbol':'1.2-2':'de-DE'}}</td><td class="px-5 py-4">{{statusLabel(item.status)}}</td><td class="px-5 py-4 text-slate-500">{{item.paidAtUtc|date:'dd.MM.yyyy HH:mm'}}</td></tr>}</tbody>
    </table></div></div>}
    @if(selected()){<div class="fixed inset-0 z-40 bg-slate-950/20" (click)="close()"></div><aside class="fixed right-0 top-0 z-50 h-full w-full max-w-lg overflow-y-auto bg-white p-6 shadow-2xl">
      @if(returnTarget()){<button type="button" (click)="goBack()" class="mb-5 text-sm font-bold text-slate-500 hover:text-slate-900">← Zurück</button>}
      <div class="flex justify-between"><div><div class="text-xs font-bold uppercase tracking-wide text-slate-400">ZAHLUNG</div><h2 class="mt-1 text-xl font-extrabold">{{displayTransactionId(selected()!.transactionId)}}</h2></div><button type="button" (click)="close()" class="text-2xl text-slate-400">×</button></div>
      <div class="mt-6 space-y-4 text-sm"><div><span class="text-slate-400">Bestellung</span><div class="font-bold">{{orderLabel(selected()!.orderId)}}</div></div><div><span class="text-slate-400">Art / Status</span><div class="font-bold">{{kindLabel(selected()!.kind)}} · {{statusLabel(selected()!.status)}}</div></div><div><span class="text-slate-400">Betrag</span><div class="font-bold">{{selected()!.amount|currency:selected()!.currency:'symbol':'1.2-2':'de-DE'}}</div></div><div><span class="text-slate-400">Gateway</span><div class="font-bold">{{selected()!.gateway||'—'}}</div></div>
        <button type="button" (click)="openOrder()" class="block w-full rounded-xl bg-slate-50 p-4 text-left font-bold hover:bg-slate-100">→ Bestellung {{orderLabel(selected()!.orderId)}}</button>
        <button type="button" (click)="openInvoice()" class="block w-full rounded-xl bg-slate-50 p-4 text-left font-bold hover:bg-slate-100">→ Rechnung öffnen</button>
      </div>
    </aside>}
  `
})
export class PaymentsPage {
  readonly store=inject(PaymentsStore);
  readonly orders=inject(OrdersStore);
  private readonly route=inject(ActivatedRoute);
  private readonly router=inject(Router);
  readonly selected=signal<import('../../core/models/api.models').Payment|null>(null);
  readonly pendingId=signal<string|null>(null);
  readonly pendingOrderId=signal<string|null>(null);
  readonly returnTarget=signal<{route:string;param:string;id:string}|null>(null);

  constructor(){
    this.store.load();
    this.orders.load();
    this.route.queryParamMap.subscribe(params=>{
      this.pendingId.set(params.get('paymentId'));
      this.pendingOrderId.set(params.get('orderId'));
      const from=params.get('from');
      const fromId=params.get('fromId');
      this.returnTarget.set(from && fromId ? this.returnTargetFor(from,fromId) : null);
    });
    effect(()=>{
      const id=this.pendingId();
      const orderId=this.pendingOrderId();
      const item=id
        ? this.store.items().find(x=>x.id===id)
        : orderId
          ? this.store.items().find(x=>x.orderId===orderId)
          : null;
      if(item) this.selected.set(item);
    });
  }

  open(x:import('../../core/models/api.models').Payment){this.selected.set(x)}
  close(){
    this.selected.set(null);
    this.router.navigate([], {relativeTo:this.route, queryParams:{paymentId:null,orderId:null,from:null,fromId:null}, queryParamsHandling:'merge'});
  }
  openOrder(){
    const item=this.selected();
    if(!item)return;
    this.router.navigate(['/bestellungen'],{queryParams:{orderId:item.orderId,from:'zahlungen',fromId:item.id}});
  }
  openInvoice(){
    const item=this.selected();
    if(!item)return;
    this.router.navigate(['/rechnungen'],{queryParams:{orderId:item.orderId,from:'zahlungen',fromId:item.id}});
  }
  goBack(){
    const target=this.returnTarget();
    if(!target)return;
    this.router.navigate([target.route],{queryParams:{[target.param]:target.id}});
  }
  orderLabel(orderId:string){
    return this.orders.items().find(x=>x.id===orderId)?.orderNumber ?? orderId;
  }
  displayTransactionId(transactionId:string){
    const match=transactionId.match(/^SHOPIFY-gid:\/\/shopify\/OrderTransaction\/(.+)$/i);
    return match ? `#${match[1]}` : transactionId;
  }
  private returnTargetFor(from:string,id:string):{route:string;param:string;id:string}|null{
    if(from==='bestellungen') return {route:'/bestellungen',param:'orderId',id};
    if(from==='rechnungen') return {route:'/rechnungen',param:'invoiceId',id};
    return null;
  }
  statusLabel(s:string){return ({Pending:'Ausstehend',Succeeded:'Erfolgreich',Failed:'Fehlgeschlagen',Refunded:'Erstattet'} as Record<string,string>)[s]??s}
  kindLabel(s:string){return ({Sale:'Verkauf',Capture:'Einzug',Refund:'Erstattung'} as Record<string,string>)[s]??s}
}
