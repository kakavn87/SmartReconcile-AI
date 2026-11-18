import { computed, inject } from '@angular/core';
import { patchState, signalStore, withComputed, withMethods, withState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { pipe, switchMap, tap } from 'rxjs';
import { tapResponse } from '@ngrx/operators';
import { ApiService } from '../api/api.service';
import { ControlTower, FinancialCase } from '../models/api.models';

type State = { data:ControlTower|null; loading:boolean; error:string|null; };

export const ControlTowerStore = signalStore(
  { providedIn:'root' },
  withState<State>({ data:null, loading:false, error:null }),
  withComputed(({ data }) => ({
    hasData:computed(() => data() !== null),
    openCases:computed(() => data()?.cases.filter(x => x.status === 'Open' || x.status === 'Investigating') ?? []),
    highRiskCases:computed(() => data()?.cases.filter(x => x.severity === 'High' || x.severity === 'Critical') ?? [])
  })),
  withMethods((store, api=inject(ApiService)) => ({
    load:rxMethod<void>(
      pipe(
        tap(() => patchState(store,{data:null,loading:true,error:null})),
        switchMap(() => api.controlTower().pipe(
          tapResponse({
            next:data => patchState(store,{data,loading:false}),
            error:() => patchState(store,{loading:false,error:'Der Financial Control Tower konnte nicht geladen werden.'})
          })
        ))
      )
    ),
    investigate:rxMethod<string>(
      pipe(
        switchMap(id => api.investigateCase(id).pipe(
          tapResponse({
            next:updated => patchState(store,{data:store.data() ? {...store.data()!,cases:store.data()!.cases.map(x => x.id === updated.id ? updated : x)} : null}),
            error:() => patchState(store,{error:'Der Finanzfall konnte nicht geöffnet werden.'})
          })
        ))
      )
    ),
    resolve:rxMethod<{id:string;rootCause:string|null}>(
      pipe(
        switchMap(request => api.resolveCase(request.id,request.rootCause).pipe(
          tapResponse({
            next:updated => patchState(store,{data:store.data() ? {...store.data()!,cases:store.data()!.cases.filter(x => x.id !== updated.id),openCaseCount:Math.max(0,store.data()!.openCaseCount-1)} : null}),
            error:() => patchState(store,{error:'Der Finanzfall konnte nicht abgeschlossen werden.'})
          })
        ))
      )
    ),
    ignore:rxMethod<string>(
      pipe(
        switchMap(id => api.ignoreCase(id).pipe(
          tapResponse({
            next:updated => patchState(store,{data:store.data() ? {...store.data()!,cases:store.data()!.cases.filter(x => x.id !== updated.id),openCaseCount:Math.max(0,store.data()!.openCaseCount-1)} : null}),
            error:() => patchState(store,{error:'Der Finanzfall konnte nicht ignoriert werden.'})
          })
        ))
      )
    )
  }))
);
