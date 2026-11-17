import { computed, inject } from '@angular/core';
import { patchState, signalStore, withComputed, withMethods, withState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { pipe, switchMap, tap } from 'rxjs';
import { tapResponse } from '@ngrx/operators';
import { ApiService } from '../api/api.service';
import { Dashboard } from '../models/api.models';

type State = { data:Dashboard|null; loading:boolean; error:string|null; };

export const DashboardStore = signalStore(
  { providedIn:'root' },
  withState<State>({ data:null, loading:false, error:null }),
  withComputed(({ data }) => ({ hasData:computed(() => data() !== null) })),
  withMethods((store, api=inject(ApiService)) => ({
    load: rxMethod<void>(
      pipe(
        tap(() => patchState(store, { loading:true, error:null })),
        switchMap(() => api.dashboard().pipe(
          tapResponse({
            next: data => patchState(store, { data, loading:false }),
            error: () => patchState(store, { loading:false, error:'Das Dashboard konnte nicht geladen werden.' })
          })
        ))
      )
    )
  }))
);
