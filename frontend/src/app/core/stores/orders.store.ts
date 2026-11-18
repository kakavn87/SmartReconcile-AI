import { inject } from '@angular/core';
import { patchState, signalStore, withMethods, withState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { catchError, exhaustMap, filter, map, of, pipe, switchMap, tap } from 'rxjs';
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
      exhaustMap(()=>api.currentTenantIntegrationConnections().pipe(
        switchMap(connections=>{
          const shopify=connections.find(x=>x.provider?.toLowerCase()==='shopify' && x.shopDomain);
          return shopify
            ? api.syncShopify(shopify.id).pipe(map(()=>true))
            : of(false);
        }),
        catchError(()=>of(false)),
        switchMap(()=>api.orders()),
        tapResponse({
          next:items=>patchState(store,{
            items:[...new Map(items.map(item=>[item.id,item])).values()],
            loading:false,
            loaded:true
          }),
          error:()=>patchState(store,{
            loading:false,
            error:'Bestellungen konnten nicht geladen werden.'
          })
        })
      ))
    ))
  }))
);
