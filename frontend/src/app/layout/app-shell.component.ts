import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

@Component({
  standalone:true,
  imports:[RouterLink,RouterLinkActive,RouterOutlet],
  changeDetection:ChangeDetectionStrategy.OnPush,
  template:`
    <div class="min-h-screen">
      <header class="sticky top-0 z-20 border-b border-slate-200 bg-white/95 backdrop-blur">
        <div class="mx-auto flex h-16 max-w-[1500px] items-center justify-between px-6">
          <a routerLink="/dashboard" class="flex items-center gap-3">
            <div class="flex h-9 w-9 items-center justify-center rounded-xl bg-slate-900 text-sm font-bold text-white">SR</div>
            <div><div class="text-sm font-bold tracking-tight">SmartReconcile AI</div><div class="text-[11px] text-slate-500">Automatisierte Abstimmung</div></div>
          </a>
          <div class="flex items-center gap-2 text-sm"><span class="h-2 w-2 rounded-full bg-emerald-500"></span><span class="text-slate-600">API online</span></div>
        </div>
      </header>
      <div class="mx-auto grid max-w-[1500px] grid-cols-1 lg:grid-cols-[230px_1fr]">
        <aside class="border-r border-slate-200 bg-white p-4 lg:min-h-[calc(100vh-4rem)]">
          <nav class="space-y-1">
            <a routerLink="/dashboard" routerLinkActive="bg-slate-100 text-slate-900" class="flex items-center gap-3 rounded-lg px-3 py-2.5 text-sm font-medium text-slate-600 hover:bg-slate-50"><span class="w-5">⌂</span>Dashboard</a>
            <a routerLink="/abweichungen" routerLinkActive="bg-slate-100 text-slate-900" class="flex items-center gap-3 rounded-lg px-3 py-2.5 text-sm font-medium text-slate-600 hover:bg-slate-50"><span class="w-5">!</span>Abweichungen</a>
            <a routerLink="/bestellungen" routerLinkActive="bg-slate-100 text-slate-900" class="flex items-center gap-3 rounded-lg px-3 py-2.5 text-sm font-medium text-slate-600 hover:bg-slate-50"><span class="w-5">↗</span>Bestellungen</a>
            <a routerLink="/zahlungen" routerLinkActive="bg-slate-100 text-slate-900" class="flex items-center gap-3 rounded-lg px-3 py-2.5 text-sm font-medium text-slate-600 hover:bg-slate-50"><span class="w-5">€</span>Zahlungen</a>
            <a routerLink="/rechnungen" routerLinkActive="bg-slate-100 text-slate-900" class="flex items-center gap-3 rounded-lg px-3 py-2.5 text-sm font-medium text-slate-600 hover:bg-slate-50"><span class="w-5">▤</span>Rechnungen</a>
            <a routerLink="/abgleich" routerLinkActive="bg-slate-100 text-slate-900" class="flex items-center gap-3 rounded-lg px-3 py-2.5 text-sm font-medium text-slate-600 hover:bg-slate-50"><span class="w-5">↻</span>Abgleich</a>
            <a routerLink="/einstellungen" routerLinkActive="bg-slate-100 text-slate-900" class="flex items-center gap-3 rounded-lg px-3 py-2.5 text-sm font-medium text-slate-600 hover:bg-slate-50"><span class="w-5">⚙</span>Einstellungen</a>
          </nav>
        </aside>
        <main class="min-w-0 p-6 lg:p-8"><router-outlet /></main>
      </div>
    </div>
  `
})
export class AppShellComponent {}
