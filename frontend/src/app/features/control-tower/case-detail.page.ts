import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { CaseDetailStore } from '../../core/stores/case-detail.store';

@Component({
  standalone:true,
  imports:[CurrencyPipe, DatePipe, RouterLink],
  changeDetection:ChangeDetectionStrategy.OnPush,
  providers:[CaseDetailStore],
  template: `
    <div class="mb-6">
      <a routerLink="/dashboard" class="text-sm font-bold text-slate-500 hover:text-slate-900">← Zurück zum Control Tower</a>
    </div>

    @if (store.error()) {
      <div class="mb-6 rounded-2xl border border-red-200 bg-red-50 p-4 text-sm font-semibold text-red-700">{{store.error()}}</div>
    }

    @if (store.loading()) {
      <div class="app-card p-10 text-center text-sm text-slate-500">Finanzfall wird geladen …</div>
    } @else if (store.data(); as data) {
      <div class="mb-6 app-card p-6">
        <div class="flex flex-col gap-5 lg:flex-row lg:items-center lg:justify-between">
          <div>
            <div class="text-xs font-bold uppercase tracking-[.14em] text-blue-600">Finanzfall</div>
            <div class="mt-2 flex flex-wrap items-center gap-3">
              <h1 class="text-3xl font-extrabold tracking-tight text-slate-950">{{data.caseNumber}}</h1>
              <span class="rounded-full px-3 py-1 text-xs font-extrabold" [class.bg-red-50]="data.severity==='Critical'||data.severity==='High'" [class.text-red-700]="data.severity==='Critical'||data.severity==='High'" [class.bg-amber-50]="data.severity==='Medium'" [class.text-amber-700]="data.severity==='Medium'" [class.bg-slate-100]="data.severity==='Low'" [class.text-slate-700]="data.severity==='Low'">{{data.severity}}</span>
              <span class="rounded-full bg-blue-50 px-3 py-1 text-xs font-extrabold text-blue-700">{{statusLabel(data.status)}}</span>
            </div>
            <p class="mt-2 text-sm text-slate-500">{{data.title}}</p>
          </div>
          <div class="text-left lg:text-right">
            <div class="text-xs font-bold uppercase tracking-wide text-slate-400">Betroffener Betrag</div>
            <div class="mt-1 text-3xl font-extrabold text-slate-950">{{data.affectedAmount | currency:data.currency:'symbol':'1.2-2':'de-DE'}}</div>
          </div>
        </div>
      </div>

      <div class="grid items-start gap-6 xl:grid-cols-[minmax(0,1.85fr)_minmax(320px,1fr)]">
        <main class="space-y-6">
          <section class="app-card p-6">
            <div class="mb-5">
              <div class="text-xs font-bold uppercase tracking-wide text-slate-400">Finanzielle Belegkette</div>
              <h2 class="mt-1 text-lg font-extrabold text-slate-950">Order → Payment → Invoice</h2>
            </div>
            <div class="grid items-stretch gap-3 lg:grid-cols-[1fr_auto_1fr_auto_1fr]">
              @for (node of [store.order(), store.payment(), store.invoice()]; track node?.nodeType ?? $index) {
                @if ($index > 0) { <div class="hidden items-center justify-center text-2xl font-light text-slate-300 lg:flex">→</div> }
                <div class="rounded-2xl border p-5" [class.border-red-300]="!node?.exists" [class.border-dashed]="!node?.exists" [class.bg-red-50]="!node?.exists" [class.border-amber-200]="node?.exists && node?.status !== 'Bezahlt' && node?.status !== 'Erfolgreich' && node?.status !== 'Vorhanden'" [class.bg-amber-50]="node?.exists && node?.status !== 'Bezahlt' && node?.status !== 'Erfolgreich' && node?.status !== 'Vorhanden'" [class.border-emerald-200]="node?.exists && (node?.status === 'Bezahlt' || node?.status === 'Erfolgreich' || node?.status === 'Vorhanden')" [class.bg-emerald-50]="node?.exists && (node?.status === 'Bezahlt' || node?.status === 'Erfolgreich' || node?.status === 'Vorhanden')">
                  <div class="flex items-center justify-between gap-2">
                    <span class="text-xs font-extrabold uppercase tracking-wide text-slate-500">{{node?.nodeType}}</span>
                    <span class="text-[10px] font-extrabold uppercase" [class.text-red-600]="!node?.exists" [class.text-emerald-700]="node?.exists && (node?.status === 'Bezahlt' || node?.status === 'Erfolgreich' || node?.status === 'Vorhanden')" [class.text-amber-700]="node?.exists && node?.status !== 'Bezahlt' && node?.status !== 'Erfolgreich' && node?.status !== 'Vorhanden'">{{node?.status}}</span>
                  </div>
                  <div class="mt-4 text-sm font-extrabold text-slate-950">{{node?.exists ? node?.identifier : 'Fehlt'}}</div>
                  <div class="mt-2 text-xl font-extrabold text-slate-900">@if(node?.amount !== null && node?.amount !== undefined){{{node?.amount | currency:node?.currency:'symbol':'1.2-2':'de-DE'}}} @else {—}</div>
                  <div class="mt-2 text-xs text-slate-500">{{node?.createdAtUtc ? (node?.createdAtUtc | date:'dd.MM.yyyy HH:mm':'':'de-DE') : 'Keine Daten vorhanden'}}</div>
                  <div class="mt-3 text-xs text-slate-500">{{node?.detail}}</div>
                </div>
              }
            </div>
          </section>

          <section class="app-card p-6">
            <div class="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
              <div>
                <div class="text-xs font-bold uppercase tracking-wide text-slate-400">Root Cause & KI-Untersuchung</div>
                <h2 class="mt-1 text-lg font-extrabold text-slate-950">Ursachenanalyse</h2>
              </div>
              @if (!store.hasAnalysis() && store.isOpen()) {
                <button type="button" [disabled]="store.actionLoading()" (click)="store.investigate(data.id)" class="app-button bg-slate-950 text-white hover:bg-slate-800 disabled:opacity-50">KI-Ursachenanalyse starten</button>
              }
            </div>
            @if (store.hasAnalysis()) {
              <div class="mt-5 grid gap-4 md:grid-cols-2">
                <div class="rounded-2xl border border-blue-100 bg-blue-50 p-5"><div class="text-xs font-extrabold uppercase tracking-wide text-blue-700">Root Cause</div><p class="mt-3 text-sm leading-6 text-slate-700">{{data.rootCauseAnalysis.analysis}}</p></div>
                <div class="rounded-2xl border border-emerald-100 bg-emerald-50 p-5"><div class="text-xs font-extrabold uppercase tracking-wide text-emerald-700">Empfohlene Maßnahme</div><p class="mt-3 text-sm leading-6 text-slate-700">{{data.rootCauseAnalysis.recommendedAction || 'Keine zusätzliche Maßnahme vorgeschlagen.'}}</p></div>
              </div>
              @if(data.rootCauseAnalysis.isFallback){<div class="mt-3 text-xs text-slate-400">Fallback-Analyse aktiv – für eine externe KI-Analyse muss ein OpenAI API Key konfiguriert sein.</div>}
            } @else {
              <div class="mt-5 rounded-2xl bg-slate-50 p-5 text-sm text-slate-500">Für diesen Finanzfall wurde noch keine KI-Ursachenanalyse durchgeführt.</div>
            }
          </section>

          @if (store.isOpen()) {
            <section class="app-card p-6">
              <div class="text-xs font-bold uppercase tracking-wide text-slate-400">Action & Resolution</div>
              <h2 class="mt-1 text-lg font-extrabold text-slate-950">Fallbearbeitung</h2>
              <div class="mt-5 grid gap-4 lg:grid-cols-3">
                <button type="button" [disabled]="store.actionLoading()" (click)="store.autoFix(data.id)" class="rounded-2xl border border-blue-200 bg-blue-50 p-5 text-left hover:border-blue-400 disabled:opacity-50"><div class="text-sm font-extrabold text-blue-900">Automatisch korrigieren (Action)</div><div class="mt-2 text-xs leading-5 text-blue-700">Eine unterstützte automatische Korrektur ausführen und im Audit-Protokoll dokumentieren.</div></button>
                <button type="button" [disabled]="store.actionLoading()" (click)="resolve()" class="rounded-2xl border border-emerald-200 bg-emerald-50 p-5 text-left hover:border-emerald-400 disabled:opacity-50"><div class="text-sm font-extrabold text-emerald-900">Als gelöst markieren (Resolve)</div><div class="mt-2 text-xs leading-5 text-emerald-700">Den Fall mit einer kurzen Notiz abschließen.</div></button>
                <button type="button" [disabled]="store.actionLoading()" (click)="store.ignore(data.id)" class="rounded-2xl border border-slate-200 bg-slate-50 p-5 text-left hover:border-slate-400 disabled:opacity-50"><div class="text-sm font-extrabold text-slate-900">Ignorieren</div><div class="mt-2 text-xs leading-5 text-slate-500">Diesen Fall aus der offenen Bearbeitung entfernen.</div></button>
              </div>
              <div class="mt-5"><label class="text-xs font-bold uppercase tracking-wide text-slate-400">Notiz des Buchhalters</label><textarea #note rows="3" class="mt-2 w-full rounded-xl border border-slate-200 px-4 py-3 text-sm outline-none focus:border-blue-400" placeholder="Kurze Begründung oder Bearbeitungsnotiz …"></textarea><button type="button" [disabled]="store.actionLoading()" (click)="resolve(note.value)" class="mt-3 rounded-xl bg-emerald-600 px-4 py-2.5 text-sm font-extrabold text-white hover:bg-emerald-700 disabled:opacity-50">Als gelöst markieren</button></div>
            </section>
          } @else {
            <section class="app-card border-emerald-200 bg-emerald-50 p-6"><div class="text-sm font-extrabold text-emerald-800">Dieser Finanzfall ist abgeschlossen.</div><div class="mt-1 text-sm text-emerald-700">Die weitere Bearbeitung ist deaktiviert. Die Audit-Timeline bleibt vollständig erhalten.</div></section>
          }
        </main>

        <aside class="app-card p-6 xl:sticky xl:top-6">
          <div class="text-xs font-bold uppercase tracking-wide text-slate-400">Audit Timeline</div>
          <h2 class="mt-1 text-lg font-extrabold text-slate-950">Fallhistorie</h2>
          <div class="mt-6">
            @for (event of data.timeline; track event.id; let last = $last) {
              <div class="relative flex gap-4">
                @if (!last) { <div class="absolute left-[7px] top-5 h-full w-px bg-slate-200"></div> }
                <div class="relative z-10 mt-1 h-4 w-4 shrink-0 rounded-full border-4 border-white bg-blue-600 shadow-sm"></div>
                <div class="pb-7">
                  <div class="flex flex-wrap items-center gap-2"><span class="text-xs font-extrabold text-slate-900">{{eventLabel(event.eventType)}}</span><span class="text-[10px] font-semibold text-slate-400">{{event.actor}}</span></div>
                  <div class="mt-1 text-xs leading-5 text-slate-600">{{event.description}}</div>
                  <div class="mt-1 text-[10px] font-semibold text-slate-400">{{event.createdAtUtc | date:'dd.MM.yyyy HH:mm:ss':'':'de-DE'}}</div>
                </div>
              </div>
            } @empty { <div class="rounded-xl bg-slate-50 p-4 text-sm text-slate-500">Noch keine Audit-Ereignisse vorhanden.</div> }
          </div>
        </aside>
      </div>
    } @else {
      <div class="app-card p-10 text-center text-sm text-slate-500">Finanzfall nicht gefunden.</div>
    }
  `
})
export class CaseDetailPage {
  readonly store=inject(CaseDetailStore);
  private readonly route=inject(ActivatedRoute);
  constructor(){
    const id=this.route.snapshot.paramMap.get('id');
    if(id) this.store.load(id);
  }
  statusLabel(status:string):string { return status==='Open'?'Offen':status==='Investigating'?'In Prüfung':status==='Resolved'?'Gelöst':'Ignoriert'; }
  eventLabel(type:string):string { return type==='Detected'?'Erkannt':type==='Investigated'?'KI-Untersuchung':type==='ActionExecuted'?'Aktion ausgeführt':type==='Resolved'?'Gelöst':'Ignoriert'; }
  resolve(note:string|null=null):void {
    const data=this.store.data();
    if(data) this.store.resolve({id:data.id,rootCause:data.rootCauseAnalysis.analysis,note});
  }
}
