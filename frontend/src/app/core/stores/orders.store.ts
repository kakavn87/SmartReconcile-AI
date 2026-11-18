import { inject } from '@angular/core';
import { patchState, signalStore, withMethods, withState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { exhaustMap, filter, pipe, tap } from 'rxjs';
import { tapResponse } from '@ngrx/operators';
import { ApiService } from '../api/api.service';
import { Order } from '../models/api.models';

type State={items:Order[];loading:boolean;error:string|null;loaded:boolean};

export const OrdersStore=signalStore(
  {providedIn:'root'},
  withState<State>({items:[],loading:false,error:null,loaded:false}),
  withMethods((store,api=inject(ApiService))=>({
    load:rxMethod<void>(pipe(
      filter(()=>!store.loaded() && !store.loading()),
      tap(()=>patchState(store,{loading:true,error:null})),
      exhaustMap(()=>api.orders().pipe(tapResponse({
        next:items=>patchState(store,{
          items:[...new Map(items.map(item=>[item.id,item])).values()],
          loading:false,
          loaded:true
        }),
        error:()=>patchState(store,{
          loading:false,
          error:'Bestellungen konnten nicht geladen werden.'
        })
      })))
    )),
    refresh:rxMethod<void>(pipe(
      tap(()=>patchState(store,{loaded:false})),
      tap(()=>store.load())
    ))
  }))
);
