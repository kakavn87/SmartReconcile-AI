import { computed, inject } from '@angular/core';
import { patchState, signalStore, withComputed, withMethods, withState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { pipe, switchMap, tap } from 'rxjs';
import { tapResponse } from '@ngrx/operators';
import { ApiService } from '../api/api.service';
import { FinancialCaseDetail } from '../models/api.models';

type State = { data:FinancialCaseDetail|null; loading:boolean; actionLoading:boolean; error:string|null; };

export const CaseDetailStore = signalStore(
  { providedIn:'root' },
  withState<State>({ data:null, loading:false, actionLoading:false, error:null }),
  withComputed(({ data }) => ({
    order:computed(() => data()?.graphNodes.find(x => x.nodeType === 'Order') ?? null),
    payment:computed(() => data()?.graphNodes.find(x => x.nodeType === 'Payment') ?? null),
    invoice:computed(() => data()?.graphNodes.find(x => x.nodeType === 'Invoice') ?? null),
    isOpen:computed(() => data()?.status === 'Open' || data()?.status === 'Investigating'),
    hasAnalysis:computed(() => data()?.rootCauseAnalysis.analyzed === true)
  })),
  withMethods((store, api=inject(ApiService)) => ({
    load:rxMethod<string>(pipe(
      tap(() => patchState(store,{loading:true,error:null})),
      switchMap(id => api.financialCase(id).pipe(
        tapResponse({ next:data => patchState(store,{data,loading:false}), error:() => patchState(store,{loading:false,error:'Der Finanzfall konnte nicht geladen werden.'}) })
      ))
    )),
    investigate:rxMethod<string>(pipe(
      tap(() => patchState(store,{actionLoading:true,error:null})),
      switchMap(id => api.investigateCase(id).pipe(
        switchMap(() => api.financialCase(id)),
        tapResponse({ next:data => patchState(store,{data,actionLoading:false}), error:() => patchState(store,{actionLoading:false,error:'Die KI-Ursachenanalyse konnte nicht durchgeführt werden.'}) })
      ))
    )),
    autoFix:rxMethod<string>(pipe(
      tap(() => patchState(store,{actionLoading:true,error:null})),
      switchMap(id => api.autoFixCase(id).pipe(
        switchMap(() => api.financialCase(id)),
        tapResponse({ next:data => patchState(store,{data,actionLoading:false}), error:() => patchState(store,{actionLoading:false,error:'Die automatische Korrektur konnte nicht ausgeführt werden.'}) })
      ))
    )),
    resolve:rxMethod<{id:string;rootCause:string|null;note:string|null}>(pipe(
      tap(() => patchState(store,{actionLoading:true,error:null})),
      switchMap(request => api.resolveCase(request.id,request.rootCause,request.note).pipe(
        switchMap(() => api.financialCase(request.id)),
        tapResponse({ next:data => patchState(store,{data,actionLoading:false}), error:() => patchState(store,{actionLoading:false,error:'Der Finanzfall konnte nicht gelöst werden.'}) })
      ))
    )),
    ignore:rxMethod<string>(pipe(
      tap(() => patchState(store,{actionLoading:true,error:null})),
      switchMap(id => api.ignoreCase(id).pipe(
        switchMap(() => api.financialCase(id)),
        tapResponse({ next:data => patchState(store,{data,actionLoading:false}), error:() => patchState(store,{actionLoading:false,error:'Der Finanzfall konnte nicht ignoriert werden.'}) })
      ))
    ))
  }))
);
