import { inject } from '@angular/core';
import { patchState, signalStore, withMethods, withState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { pipe, switchMap, tap } from 'rxjs';
import { tapResponse } from '@ngrx/operators';
import { ApiService } from '../api/api.service';
import { Order } from '../models/api.models';

type State={items:Order[];loading:boolean;error:string|null;loaded:boolean};

export const OrdersStore=signalStore(
  {providedIn:'root'},
  withState<State>({items:[],loading:false,error:null,loaded:false}),
  withMethods((store,api=inject(ApiService))=>({
    load:rxMethod<void>(pipe(
      tap(()=>patchState(store,{items:[],loading:true,error:null,loaded:false})),
      switchMap(()=>api.orders().pipe(tapResponse({
        next:items=>patchState(store,{
          items:items.map(item=>({
            ...item,
            totalAmount:Number(item.totalAmount),
            currency:item.currency || 'EUR',
            status:item.status || 'Pending'
          })),
          loading:false,
          loaded:true
        }),
        error:()=>patchState(store,{
          loading:false,
          error:'Bestellungen konnten nicht geladen werden.'
        })
      })))
    ))
  }))
);
