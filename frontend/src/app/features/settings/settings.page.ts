import { ChangeDetectionStrategy, Component, effect, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { AccountingSettingsStore } from '../../core/stores/accounting-settings.store';
import { ChartOfAccounts, UpdateAccountingSettingsRequest } from '../../core/models/api.models';

@Component({
  standalone:true,
  imports:[DatePipe],
  changeDetection:ChangeDetectionStrategy.OnPush,
  providers:[AccountingSettingsStore],
  template:`
    <div class="mb-8">
      <div class="text-xs font-extrabold uppercase tracking-[.14em] text-blue-600">System</div>
      <div class="mt-2 flex flex-col gap-2 lg:flex-row lg:items-end lg:justify-between">
        <div>
          <h1 class="text-3xl font-extrabold tracking-tight text-slate-950">Einstellungen</h1>
          <p class="mt-1 text-sm text-slate-500">Buchhaltung, Kontenrahmen und DATEV-Export für dieses Unternehmen.</p>
        </div>
        @if (store.data(); as data) {
          <div class="text-xs font-semibold text-slate-400">Zuletzt aktualisiert {{data.updatedAtUtc | date:'dd.MM.yyyy HH:mm':'Europe/Berlin':'de-DE'}}</div>
        }
      </div>
    </div>

    @if (store.error()) {
      <div class="mb-6 rounded-2xl border border-red-200 bg-red-50 p-4 text-sm font-semibold text-red-700">{{store.error()}}</div>
    }

    @if (store.loading()) {
      <div class="app-card p-10 text-center text-sm text-slate-500">Buchhaltungseinstellungen werden geladen …</div>
    } @else {
      <div class="grid gap-6 xl:grid-cols-[minmax(0,1.35fr)_minmax(340px,.65fr)]">
        <main class="space-y-6">
          <section class="app-card p-6">
            <div class="mb-6">
              <div class="text-xs font-extrabold uppercase tracking-wide text-slate-400">Buchhaltung</div>
              <h2 class="mt-1 text-lg font-extrabold text-slate-950">DATEV-Grundkonfiguration</h2>
              <p class="mt-1 text-sm text-slate-500">Diese Angaben werden für den DATEV-Buchungsstapel verwendet.</p>
            </div>

            <div class="grid gap-5 md:grid-cols-2">
              <label class="block">
                <span class="text-xs font-extrabold uppercase tracking-wide text-slate-500">Beraternummer</span>
                <input [value]="draft().beraternummer" (input)="setField('beraternummer',$any($event.target).value)" inputmode="numeric" maxlength="7" class="settings-input" />
              </label>
              <label class="block">
                <span class="text-xs font-extrabold uppercase tracking-wide text-slate-500">Mandantennummer</span>
                <input [value]="draft().mandantennummer" (input)="setField('mandantennummer',$any($event.target).value)" inputmode="numeric" maxlength="5" class="settings-input" />
              </label>
              <label class="block md:col-span-2">
                <span class="text-xs font-extrabold uppercase tracking-wide text-slate-500">Kontenrahmen</span>
                <select [value]="draft().chartOfAccounts" (change)="changeChart($any($event.target).value)" class="settings-input">
                  <option value="SKR03">SKR03</option>
                  <option value="SKR04">SKR04</option>
                </select>
                <span class="mt-1 block text-xs text-slate-400">Beim Wechsel werden die Standardkonten automatisch gesetzt.</span>
              </label>
            </div>
          </section>

          <section class="app-card p-6">
            <div class="mb-6">
              <div class="text-xs font-extrabold uppercase tracking-wide text-slate-400">Kontenzuordnung</div>
              <h2 class="mt-1 text-lg font-extrabold text-slate-950">Erlöse und Zahlungsverkehr</h2>
            </div>
            <div class="grid gap-5 md:grid-cols-2">
              <label class="block">
                <span class="text-xs font-extrabold uppercase tracking-wide text-slate-500">Erlöskonto 19 %</span>
                <input [value]="draft().revenueAccount19" (input)="setField('revenueAccount19',$any($event.target).value)" inputmode="numeric" class="settings-input" />
              </label>
              <label class="block">
                <span class="text-xs font-extrabold uppercase tracking-wide text-slate-500">Erlöskonto 7 %</span>
                <input [value]="draft().revenueAccount7" (input)="setField('revenueAccount7',$any($event.target).value)" inputmode="numeric" class="settings-input" />
              </label>
              <label class="block">
                <span class="text-xs font-extrabold uppercase tracking-wide text-slate-500">Zahlungsverkehr / Clearing</span>
                <input [value]="draft().paymentClearingAccount" (input)="setField('paymentClearingAccount',$any($event.target).value)" inputmode="numeric" class="settings-input" />
              </label>
              <label class="block">
                <span class="text-xs font-extrabold uppercase tracking-wide text-slate-500">Gebührenkonto</span>
                <input [value]="draft().feeAccount" (input)="setField('feeAccount',$any($event.target).value)" inputmode="numeric" class="settings-input" />
              </label>
            </div>

            <div class="mt-6 flex flex-col gap-3 border-t border-slate-100 pt-5 sm:flex-row sm:items-center sm:justify-between">
              <div class="text-xs text-slate-400">Standard: SKR03 8400 / 8300 / 1360 / 4970 · SKR04 4400 / 4300 / 1460 / 6855</div>
              <button type="button" [disabled]="store.saving()" (click)="save()" class="app-button bg-slate-950 text-white hover:bg-slate-800 disabled:opacity-50">{{store.saving() ? 'Wird gespeichert …' : 'Einstellungen speichern'}}</button>
            </div>
          </section>
        </main>

        <aside class="space-y-6">
          <section class="app-card p-6">
            <div class="text-xs font-extrabold uppercase tracking-wide text-slate-400">DATEV</div>
            <h2 class="mt-1 text-lg font-extrabold text-slate-950">Buchungsstapel exportieren</h2>
            <p class="mt-2 text-sm leading-6 text-slate-500">Erstellt einen DATEV EXTF Buchungsstapel aus den E-Commerce-Buchungen.</p>

            <div class="mt-5 space-y-4">
              <label class="block">
                <span class="text-xs font-extrabold uppercase tracking-wide text-slate-500">Von</span>
                <input type="date" [value]="fromDate()" (input)="fromDate.set($any($event.target).value)" class="settings-input" />
              </label>
              <label class="block">
                <span class="text-xs font-extrabold uppercase tracking-wide text-slate-500">Bis</span>
                <input type="date" [value]="toDate()" (input)="toDate.set($any($event.target).value)" class="settings-input" />
              </label>
              <label class="flex cursor-pointer items-start gap-3 rounded-xl border border-slate-200 bg-slate-50 p-4">
                <input type="checkbox" [checked]="onlyReconciled()" (change)="onlyReconciled.set($any($event.target).checked)" class="mt-0.5 h-4 w-4 rounded border-slate-300" />
                <span><span class="block text-sm font-extrabold text-slate-800">Nur abgestimmte Buchungen exportieren</span><span class="mt-1 block text-xs leading-5 text-slate-500">Offene Finanzfälle werden nicht in den Stapel übernommen.</span></span>
              </label>
              <button type="button" [disabled]="store.exporting()" (click)="exportDatev()" class="w-full rounded-xl bg-blue-600 px-4 py-3 text-sm font-extrabold text-white shadow-sm hover:bg-blue-700 disabled:opacity-50">{{store.exporting() ? 'Export wird erstellt …' : 'DATEV-Export erstellen'}}</button>
              @if (store.exportError()) {
                <div class="rounded-xl border border-red-200 bg-red-50 p-3 text-xs font-semibold text-red-700">{{store.exportError()}}</div>
              }
            </div>
          </section>

          @if (store.exportReady()) {
            <section class="app-card border-emerald-200 bg-emerald-50 p-6">
              <div class="flex items-center justify-between gap-3">
                <div>
                  <div class="text-xs font-extrabold uppercase tracking-wide text-emerald-700">Export bereit</div>
                  <h2 class="mt-1 text-lg font-extrabold text-emerald-950">Buchungsvorschau</h2>
                </div>
                <span class="rounded-full bg-white px-2.5 py-1 text-[10px] font-extrabold text-emerald-700">{{store.preview().length}} Buchungen</span>
              </div>
              <div class="mt-4 overflow-x-auto rounded-xl border border-emerald-100 bg-white">
                <table class="min-w-full text-left text-xs">
                  <thead class="bg-slate-50 text-[10px] font-extrabold uppercase tracking-wide text-slate-500">
                    <tr><th class="px-3 py-2">Betrag</th><th class="px-3 py-2">S/H</th><th class="px-3 py-2">Konto</th><th class="px-3 py-2">Gegenkonto</th><th class="px-3 py-2">Beleg</th></tr>
                  </thead>
                  <tbody>
                    @for (booking of store.preview(); track $index) {
                      <tr class="border-t border-slate-100"><td class="whitespace-nowrap px-3 py-2 font-bold">{{booking.amount}}</td><td class="px-3 py-2">{{booking.side}}</td><td class="px-3 py-2">{{booking.account}}</td><td class="px-3 py-2">{{booking.counterAccount}}</td><td class="px-3 py-2">{{booking.documentReference}}</td></tr>
                    } @empty {
                      <tr><td colspan="5" class="px-3 py-4 text-center text-slate-400">Keine Buchungen im gewählten Zeitraum.</td></tr>
                    }
                  </tbody>
                </table>
              </div>
              <p class="mt-3 text-[11px] leading-5 text-emerald-800">Die Datei wurde gleichzeitig als EXTF-Buchungsstapel erzeugt. Vor dem produktiven Import sollte sie mit dem DATEV-Prüfprogramm geprüft werden.</p>
            </section>
          }
        </aside>
      </div>
    }
  `,
  styles:[`
    .settings-input {
      margin-top:.45rem;
      width:100%;
      border:1px solid #e2e8f0;
      border-radius:.75rem;
      background:#fff;
      padding:.7rem .85rem;
      font-size:.875rem;
      outline:none;
      transition:border-color .16s ease, box-shadow .16s ease;
    }
    .settings-input:focus { border-color:#60a5fa; box-shadow:0 0 0 3px rgba(59,130,246,.1); }
  `]
})
export class SettingsPage {
  readonly store=inject(AccountingSettingsStore);
  readonly draft=signal<UpdateAccountingSettingsRequest>({
    beraternummer:'1001',
    mandantennummer:'10001',
    chartOfAccounts:'SKR03',
    revenueAccount19:'8400',
    revenueAccount7:'8300',
    paymentClearingAccount:'1360',
    feeAccount:'4970'
  });
  readonly fromDate=signal(this.monthStart());
  readonly toDate=signal(this.monthEnd());
  readonly onlyReconciled=signal(false);

  constructor(){
    effect(() => {
      const data=this.store.data();
      if(data){
        this.draft.set({
          beraternummer:data.beraternummer,
          mandantennummer:data.mandantennummer,
          chartOfAccounts:data.chartOfAccounts,
          revenueAccount19:data.revenueAccount19,
          revenueAccount7:data.revenueAccount7,
          paymentClearingAccount:data.paymentClearingAccount,
          feeAccount:data.feeAccount
        });
      }
    });
    this.store.load();
  }

  setField(field:keyof UpdateAccountingSettingsRequest,value:string):void {
    this.draft.update(current => ({...current,[field]:value}));
  }

  changeChart(value:ChartOfAccounts):void {
    const accounts=value==='SKR03'
      ? {revenueAccount19:'8400',revenueAccount7:'8300',paymentClearingAccount:'1360',feeAccount:'4970'}
      : {revenueAccount19:'4400',revenueAccount7:'4300',paymentClearingAccount:'1460',feeAccount:'6855'};
    this.draft.update(current => ({...current,chartOfAccounts:value,...accounts}));
  }

  save():void { this.store.save(this.draft()); }

  exportDatev():void {
    this.store.exportDatev({from:this.fromDate(),to:this.toDate(),onlyReconciled:this.onlyReconciled()});
  }

  private monthStart():string {
    const now=new Date();
    return new Date(now.getFullYear(),now.getMonth(),1).toISOString().slice(0,10);
  }

  private monthEnd():string {
    const now=new Date();
    return new Date(now.getFullYear(),now.getMonth()+1,0).toISOString().slice(0,10);
  }
}
