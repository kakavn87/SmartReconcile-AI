import { computed, inject } from '@angular/core';
import { patchState, signalStore, withComputed, withMethods, withState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { pipe, switchMap, tap } from 'rxjs';
import { tapResponse } from '@ngrx/operators';
import { ApiService } from '../api/api.service';
import { Discrepancy, DiscrepancyStatus, DiscrepancyType } from '../models/api.models';

type State = { items:Discrepancy[]; loading:boolean; error:string|null; filterType:DiscrepancyType|'Alle'; filterStatus:DiscrepancyStatus|'Alle'; search:string; };

export const DiscrepancyStore = signalStore(
  { providedIn:'root' },
  withState<State>({ items:[], loading:false, error:null, filterType:'Alle', filterStatus:'Open', search:'' }),
  withComputed(({ items, filterType, filterStatus, search }) => ({
    filtered:computed(() => items().filter(x =>
      (filterType()==='Alle' || x.type===filterType()) &&
      (filterStatus()==='Alle' || x.status===filterStatus()) &&
      (x.title + ' ' + x.description).toLowerCase().includes(search().toLowerCase())
    )),
    openCount:computed(() => items().filter(x => x.status==='Open').length)
  })),
  withMethods((store, api=inject(ApiService)) => ({
    load:rxMethod<void>(pipe(
      tap(() => patchState(store,{items:[],loading:true,error:null})),
      switchMap(() => api.discrepancies().pipe(
        tapResponse({
          next:items => patchState(store,{items,loading:false}),
          error:() => patchState(store,{loading:false,error:'Die Abweichungen konnten nicht geladen werden.'})
        })
      ))
    )),
    setType:(value:DiscrepancyType|'Alle') => patchState(store,{filterType:value}),
    setStatus:(value:DiscrepancyStatus|'Alle') => patchState(store,{filterStatus:value}),
    setSearch:(value:string) => patchState(store,{search:value}),
    updateStatus:rxMethod<{id:string;status:DiscrepancyStatus}>(pipe(
      switchMap(({id,status}) => api.updateDiscrepancyStatus(id,status).pipe(
        tapResponse({
          next:updated => patchState(store,{items:store.items().map(x => x.id===updated.id ? updated : x)}),
          error:() => patchState(store,{error:'Der Status konnte nicht geändert werden.'})
        })
      ))
    ))
  }))
);
