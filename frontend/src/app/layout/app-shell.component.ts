import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { ShopifyBridgeService } from '../core/shopify/shopify-bridge.service';

@Component({
  standalone: true,
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="min-h-screen" [class.shopify-embedded]="shopify.isEmbedded()">
      @if (!shopify.isEmbedded()) {
        <header class="sticky top-0 z-30 border-b border-slate-200/80 bg-white/90 backdrop-blur-xl">
          <div class="mx-auto flex h-[72px] max-w-[1500px] items-center justify-between px-5 lg:px-7">
            <a routerLink="/dashboard" class="flex items-center gap-3.5">
              <div class="flex h-10 w-10 items-center justify-center rounded-[14px] bg-slate-950 text-sm font-extrabold tracking-tight text-white shadow-lg shadow-slate-900/10">SR</div>
              <div>
                <div class="text-[15px] font-extrabold tracking-tight text-slate-950">SmartReconcile <span class="text-blue-600">AI</span></div>
                <div class="mt-0.5 text-[11px] font-medium text-slate-500">Automatisierte Abstimmung</div>
              </div>
            </a>
            <div class="hidden items-center gap-2 rounded-full border border-emerald-200 bg-emerald-50 px-3 py-1.5 text-xs font-semibold text-emerald-700 sm:flex">
              <span class="h-2 w-2 rounded-full bg-emerald-500 shadow-[0_0_0_4px_rgba(16,185,129,.12)]"></span>
              API online
            </div>
          </div>
        </header>
      }

      @if (shopify.isEmbedded()) {
        <div class="sticky top-0 z-20 border-b border-slate-200/80 bg-white/95 px-4 py-2 backdrop-blur-xl">
          <nav class="flex items-center gap-1 overflow-x-auto" aria-label="SmartReconcile Navigation">
            <a routerLink="/dashboard" routerLinkActive="nav-active" [routerLinkActiveOptions]="{exact:true}" class="embedded-nav-item">Übersicht</a>
            <a routerLink="/abweichungen" routerLinkActive="nav-active" class="embedded-nav-item">Abweichungen</a>
            <a routerLink="/bestellungen" routerLinkActive="nav-active" class="embedded-nav-item">Bestellungen</a>
            <a routerLink="/zahlungen" routerLinkActive="nav-active" class="embedded-nav-item">Zahlungen</a>
            <a routerLink="/rechnungen" routerLinkActive="nav-active" class="embedded-nav-item">Rechnungen</a>
            <a routerLink="/abgleich" routerLinkActive="nav-active" class="embedded-nav-item">Abgleich</a>
            <a routerLink="/integrationen" routerLinkActive="nav-active" class="embedded-nav-item">Integrationen</a>
            <a routerLink="/einstellungen" routerLinkActive="nav-active" class="embedded-nav-item">Einstellungen</a>
          </nav>
        </div>
      }

      <div [class]="shopify.isEmbedded()
        ? 'mx-auto grid max-w-[1500px] grid-cols-1'
        : 'mx-auto grid max-w-[1500px] grid-cols-1 lg:grid-cols-[245px_1fr]'">
        @if (!shopify.isEmbedded()) {
          <aside class="border-r border-slate-200/80 bg-white/75 p-4 lg:min-h-[calc(100vh-4.5rem)]">
            <div class="mb-5 px-3 pt-2 text-[10px] font-extrabold uppercase tracking-[.14em] text-slate-400">Arbeitsbereich</div>
            <nav class="space-y-1">
              <a routerLink="/dashboard" routerLinkActive="nav-active" [routerLinkActiveOptions]="{exact:true}" class="nav-item"><span class="nav-icon">⌂</span><span>Dashboard</span></a>
              <a routerLink="/abweichungen" routerLinkActive="nav-active" class="nav-item"><span class="nav-icon">!</span><span>Abweichungen</span></a>
              <a routerLink="/bestellungen" routerLinkActive="nav-active" class="nav-item"><span class="nav-icon">↗</span><span>Bestellungen</span></a>
              <a routerLink="/zahlungen" routerLinkActive="nav-active" class="nav-item"><span class="nav-icon">€</span><span>Zahlungen</span></a>
              <a routerLink="/rechnungen" routerLinkActive="nav-active" class="nav-item"><span class="nav-icon">▤</span><span>Rechnungen</span></a>
              <a routerLink="/abgleich" routerLinkActive="nav-active" class="nav-item"><span class="nav-icon">↻</span><span>Abgleich</span></a>
            </nav>

            <div class="my-5 border-t border-slate-200"></div>
            <div class="mb-2 px-3 text-[10px] font-extrabold uppercase tracking-[.14em] text-slate-400">System</div>
            <a routerLink="/integrationen" routerLinkActive="nav-active" class="nav-item"><span class="nav-icon">↔</span><span>Integrationen</span></a>
            <a routerLink="/einstellungen" routerLinkActive="nav-active" class="nav-item"><span class="nav-icon">⚙</span><span>Einstellungen</span></a>

            <div class="mt-8 rounded-2xl border border-blue-100 bg-gradient-to-br from-blue-50 to-white p-4">
              <div class="text-xs font-bold text-slate-900">SmartReconcile AI</div>
              <p class="mt-1 text-[11px] leading-5 text-slate-500">Automatische Prüfung von Bestellungen, Zahlungen und Rechnungen.</p>
            </div>

            <div class="mt-5 px-3 text-[10px] font-medium text-slate-400">Entwickler · Hai Linh Nguyen</div>
          </aside>
        }

        <main [class]="shopify.isEmbedded() ? 'min-w-0 p-4' : 'min-w-0 p-5 lg:p-8'">
          <router-outlet />
        </main>
      </div>
    </div>
  `,
  styles: [`
    .embedded-nav-item {
      display:inline-flex;
      align-items:center;
      white-space:nowrap;
      border-radius:10px;
      padding:.5rem .75rem;
      color:#64748b;
      font-size:.78rem;
      font-weight:700;
      transition:all .16s ease;
    }
    .embedded-nav-item:hover { background:#f8fafc; color:#0f172a; }
    .embedded-nav-item.nav-active { background:#eff6ff; color:#1d4ed8; }
    .nav-item {
      display:flex;
      align-items:center;
      gap:.75rem;
      border-radius:12px;
      padding:.7rem .8rem;
      color:#64748b;
      font-size:.84rem;
      font-weight:650;
      transition:all .16s ease;
    }
    .nav-item:hover { background:#f8fafc; color:#0f172a; }
    .nav-active {
      background:#eff6ff;
      color:#1d4ed8;
      box-shadow:inset 3px 0 0 #2563eb;
    }
    .nav-icon {
      display:flex;
      width:1.35rem;
      justify-content:center;
      font-size:.95rem;
      font-weight:800;
    }
  `]
})
export class AppShellComponent {
  readonly shopify = inject(ShopifyBridgeService);
}
