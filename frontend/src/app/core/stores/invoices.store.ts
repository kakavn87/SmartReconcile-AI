import { inject } from '@angular/core';
import { patchState, signalStore, withMethods, withState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { pipe, switchMap, tap } from 'rxjs';
import { tapResponse } from '@ngrx/operators';
import { ApiService } from '../api/api.service';
import { ToastService } from '../services/toast.service';
import { CreateInvoiceRequest, Invoice } from '../models/api.models';

type State={items:Invoice[];loading:boolean;error:string|null};
export const InvoicesStore=signalStore(
  {providedIn:'root'},
  withState<State>({items:[],loading:false,error:null}),
  withMethods((store,api=inject(ApiService),toast=inject(ToastService))=>({
    create:rxMethod<CreateInvoiceRequest>(pipe(
      tap(()=>patchState(store,{loading:true,error:null})),
      switchMap(request=>api.createInvoice(request).pipe(tapResponse({
        next:item=>{patchState(store,{items:[...store.items(),item],loading:false});toast.success('Rechnung erfolgreich erstellt.');},
        error:(response:any)=>{const message=response?.error?.message??'Rechnung konnte nicht erfasst werden.';patchState(store,{loading:false,error:message});toast.error(message);}
      })))
    )),
    load:rxMethod<void>(pipe(
      tap(()=>patchState(store,{items:[],loading:true,error:null})),
      switchMap(()=>api.invoices().pipe(tapResponse({
        next:items=>patchState(store,{items,loading:false}),
        error:()=>patchState(store,{loading:false,error:'Rechnungen konnten nicht geladen werden.'})
      })))
    ))
  }))
);
