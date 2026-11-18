import { computed, inject } from '@angular/core';
import { patchState, signalStore, withComputed, withMethods, withState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { pipe, switchMap, tap } from 'rxjs';
import { tapResponse } from '@ngrx/operators';
import { ApiService } from '../api/api.service';
import { AccountingSettings, ChartOfAccounts, UpdateAccountingSettingsRequest } from '../models/api.models';

interface PreviewBooking {
  amount:string;
  side:string;
  account:string;
  counterAccount:string;
  documentDate:string;
  documentReference:string;
  text:string;
}

type State = {
  data:AccountingSettings|null;
  loading:boolean;
  saving:boolean;
  exporting:boolean;
  error:string|null;
  exportError:string|null;
  preview:PreviewBooking[];
  exportReady:boolean;
};

const defaults = (chartOfAccounts:ChartOfAccounts):Pick<UpdateAccountingSettingsRequest,'revenueAccount19'|'revenueAccount7'|'paymentClearingAccount'|'feeAccount'> =>
  chartOfAccounts === 'SKR03'
    ? { revenueAccount19:'8400', revenueAccount7:'8300', paymentClearingAccount:'1360', feeAccount:'4970' }
    : { revenueAccount19:'4400', revenueAccount7:'4300', paymentClearingAccount:'1460', feeAccount:'6855' };

export const AccountingSettingsStore = signalStore(
  { providedIn:'root' },
  withState<State>({
    data:null, loading:false, saving:false, exporting:false, error:null, exportError:null, preview:[], exportReady:false
  }),
  withComputed(({ data }) => ({
    loaded:computed(() => data() !== null)
  })),
  withMethods((store, api=inject(ApiService)) => ({
    load:rxMethod<void>(pipe(
      tap(() => patchState(store,{data:null,loading:true,error:null,preview:[],exportReady:false,exportError:null})),
      switchMap(() => api.accountingSettings().pipe(
        tapResponse({
          next:data => patchState(store,{data,loading:false}),
          error:() => patchState(store,{loading:false,error:'Die Buchhaltungseinstellungen konnten nicht geladen werden.'})
        })
      ))
    )),
    save:rxMethod<UpdateAccountingSettingsRequest>(pipe(
      tap(() => patchState(store,{saving:true,error:null})),
      switchMap(request => api.updateAccountingSettings(request).pipe(
        tapResponse({
          next:data => patchState(store,{data,saving:false}),
          error:(response:any) => patchState(store,{saving:false,error:response?.error?.message ?? 'Die Buchhaltungseinstellungen konnten nicht gespeichert werden.'})
        })
      ))
    )),
    applyChartDefaults(chartOfAccounts:ChartOfAccounts) {
      const data=store.data();
      if (!data) return;
      patchState(store,{data:{...data,chartOfAccounts,...defaults(chartOfAccounts)}});
    },
    exportDatev:rxMethod<{from:string;to:string;onlyReconciled:boolean}>(pipe(
      tap(() => patchState(store,{exporting:true,exportError:null,preview:[],exportReady:false})),
      switchMap(request => api.exportDatev(request.from,request.to,request.onlyReconciled).pipe(
        tapResponse({
          next:response => {
            const filename=response.headers.get('content-disposition')?.match(/filename="?([^"]+)"?/)?.[1] ?? 'EXTF_Buchungsstapel.csv';
            response.body?.arrayBuffer().then(buffer => {
              const text=new TextDecoder('windows-1252').decode(buffer);
              const lines=text.split(/\r?\n/).filter(line => line.trim().length > 0);
              const bookingLines=lines.slice(2).slice(0,10);
              const preview=bookingLines.map(line => parseBooking(line)).filter((row):row is PreviewBooking => row !== null);
              patchState(store,{exporting:false,preview,exportReady:true});
              downloadBlob(new Blob([buffer],{type:'text/csv;charset=windows-1252'}),filename);
            }).catch(() => patchState(store,{exporting:false,exportError:'Die DATEV-Datei konnte nicht gelesen werden.'}));
          },
          error:() => patchState(store,{exporting:false,exportError:'Der DATEV-Export konnte nicht erstellt werden.'})
        })
      ))
    ))
  }))
);

function parseBooking(line:string):PreviewBooking|null {
  const fields:string[]=[];
  let current='';
  let quoted=false;
  for(let i=0;i<line.length;i++){
    const char=line[i];
    if(char==='"'){
      if(quoted && line[i+1]==='"'){ current+='"'; i++; }
      else { quoted=!quoted; }
    } else if(char===';' && !quoted){
      fields.push(current); current='';
    } else {
      current+=char;
    }
  }
  fields.push(current);
  if(fields.length < 14) return null;
  return {
    amount:fields[0],
    side:fields[1],
    account:fields[6],
    counterAccount:fields[7],
    documentDate:fields[9],
    documentReference:fields[10],
    text:fields[13]
  };
}

function downloadBlob(blob:Blob,filename:string):void {
  const url=URL.createObjectURL(blob);
  const anchor=document.createElement('a');
  anchor.href=url;
  anchor.download=filename;
  anchor.click();
  URL.revokeObjectURL(url);
}
