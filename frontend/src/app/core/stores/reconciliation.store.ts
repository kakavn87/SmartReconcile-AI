import { inject } from '@angular/core';
import { patchState, signalStore, withMethods, withState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { pipe, switchMap, tap } from 'rxjs';
import { tapResponse } from '@ngrx/operators';
import { ApiService } from '../api/api.service';
import { ReconciliationResult } from '../models/api.models';

type State={result:ReconciliationResult|null;running:boolean;error:string|null};
export const ReconciliationStore=signalStore(
  {providedIn:'root'},
  withState<State>({result:null,running:false,error:null}),
  withMethods((store,api=inject(ApiService))=>({
    run:rxMethod<void>(pipe(
      tap(()=>patchState(store,{running:true,error:null})),
      switchMap(()=>api.reconciliation().pipe(tapResponse({
        next:result=>patchState(store,{result,running:false}),
        error:()=>patchState(store,{running:false,error:'Der Abgleich konnte nicht durchgeführt werden.'})
      })))
    ))
  }))
);