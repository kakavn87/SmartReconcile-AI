import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ApiService } from '../../core/api/api.service';

@Component({
  standalone:true, changeDetection:ChangeDetectionStrategy.OnPush,
  template:`
    <div class="mb-8"><h1 class="text-2xl font-bold tracking-tight">Einstellungen</h1><p class="mt-1 text-sm text-slate-500">Technischer Status der SmartReconcile-Umgebung.</p></div>
    <div class="grid gap-4 md:grid-cols-2">
      <div class="app-card p-6"><div class="text-sm text-slate-500">Backend</div><div class="mt-2 flex items-center gap-2"><span class="h-2.5 w-2.5 rounded-full" [class.bg-emerald-500]="apiOnline" [class.bg-red-500]="!apiOnline"></span><span class="font-semibold">{{apiOnline?'Online':'Nicht erreichbar'}}</span></div><p class="mt-3 text-xs text-slate-500">ASP.NET Core API auf Render.</p></div>
      <div class="app-card p-6"><div class="text-sm text-slate-500">Frontend</div><div class="mt-2 font-semibold">Angular 22</div><p class="mt-3 text-xs text-slate-500">Standalone Components · Signals · NgRx SignalStore · RxJS</p></div>
    </div>
    <div class="mt-6 app-card p-6"><h2 class="font-semibold">Datensicherheit</h2><p class="mt-2 text-sm text-slate-600">API-Schlüssel werden nicht im Frontend gespeichert. Die KI-Analyse läuft ausschließlich über das Backend.</p></div>
  `
})
export class SettingsPage {
  private readonly api=inject(ApiService); apiOnline=false;
  constructor(){this.api.health().subscribe({next:()=>this.apiOnline=true,error:()=>this.apiOnline=false})}
}