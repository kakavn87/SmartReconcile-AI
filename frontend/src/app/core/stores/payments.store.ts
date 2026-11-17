import { inject } from '@angular/core';
import { patchState, signalStore, withMethods, withState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { pipe, switchMap, tap } from 'rxjs';
import { tapResponse } from '@ngrx/operators';
import { ApiService } from '../api/api.service';
import { Payment } from '../models/api.models';

type State={items:Payment[];loading:boolean;error:string|null};
export const PaymentsStore=signalStore(
  {providedIn:'root'},
  withState<State>({items:[],loading:false,error:null}),
  withMethods((store,api=inject(ApiService))=>({
    load:rxMethod<void>(pipe(
      tap(()=>patchState(store,{loading:true,error:null})),
      switchMap(()=>api.payments().pipe(tapResponse({
        next:items=>patchState(store,{items,loading:false}),
        error:()=>patchState(store,{loading:false,error:'Zahlungen konnten nicht geladen werden.'})
      })))
    ))
  }))
);