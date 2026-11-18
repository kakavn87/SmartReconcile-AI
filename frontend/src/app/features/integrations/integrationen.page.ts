import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { ApiService } from '../../core/api/api.service';
import { IntegrationConnection } from '../../core/models/api.models';


@Component({
  standalone: true,
  imports: [DatePipe, DecimalPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="mx-auto max-w-6xl">
      <div class="flex flex-col gap-4 md:flex-row md:items-end md:justify-between">
        <div>
          <div class="text-xs font-bold uppercase tracking-[.14em] text-blue-600">System</div>
          <h1 class="mt-1 text-3xl font-extrabold tracking-tight text-slate-950">Integrationen</h1>
          <p class="mt-2 max-w-2xl text-sm leading-6 text-slate-500">
            Verbinde deine E-Commerce- und Zahlungssysteme mit SmartReconcile AI.
          </p>
        </div>
        <button type="button" (click)="load()" class="rounded-xl border border-slate-200 bg-white px-4 py-2.5 text-sm font-bold text-slate-700 shadow-sm hover:bg-slate-50">
          Aktualisieren
        </button>
      </div>

      @if (error()) {
        <div class="mt-6 rounded-2xl border border-red-200 bg-red-50 p-4 text-sm font-semibold text-red-700">{{ error() }}</div>
      }

      <div class="mt-8 grid gap-5 lg:grid-cols-3">
        <article class="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm lg:col-span-2">
          <div class="flex items-start justify-between gap-4">
            <div>
              <div class="flex items-center gap-2">
                <div class="flex h-11 w-11 items-center justify-center rounded-xl bg-[#95bf47] text-lg font-black text-white">S</div>
                <div>
                  <h2 class="font-extrabold text-slate-950">Shopify</h2>
                  <p class="text-xs text-slate-500">Bestellungen und Zahlungen automatisch abstimmen</p>
                </div>
              </div>
            </div>
            <span class="rounded-full bg-emerald-50 px-2.5 py-1 text-[11px] font-bold text-emerald-700">Verfügbar</span>
          </div>

          <div class="mt-6">
            <label class="text-xs font-bold text-slate-600">Shop-Domain</label>
            <div class="mt-2 flex flex-col gap-2 sm:flex-row">
              <input
                [value]="shopDomain()"
                (input)="shopDomain.set($any($event.target).value)"
                placeholder="https://mein-shop.myshopify.com/"
                class="min-w-0 flex-1 rounded-xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm outline-none transition focus:border-blue-400 focus:bg-white focus:ring-4 focus:ring-blue-100"
              />
              <button type="button" (click)="connectShopify()" [disabled]="!validShop()"
                class="rounded-xl bg-slate-950 px-5 py-3 text-sm font-extrabold text-white shadow-sm transition hover:bg-slate-800 disabled:cursor-not-allowed disabled:opacity-40">
                Mit Shopify verbinden
              </button>
            </div>
            <p class="mt-2 text-xs text-slate-400">Beispiel: https://mein-shop.myshopify.com/</p>
          </div>
        </article>

        <article class="rounded-2xl border border-slate-200 bg-slate-50 p-5">
          <div class="text-xs font-bold uppercase tracking-[.14em] text-slate-400">Weitere Systeme</div>
          <div class="mt-4 space-y-3">
            <div class="rounded-xl border border-slate-200 bg-white p-4">
              <div class="font-bold text-slate-800">WooCommerce</div>
              <div class="mt-1 text-xs font-medium text-slate-400">In Vorbereitung</div>
            </div>
            <div class="rounded-xl border border-slate-200 bg-white p-4">
              <div class="font-bold text-slate-800">Stripe</div>
              <div class="mt-1 text-xs font-medium text-slate-400">Bald verfügbar</div>
            </div>
          </div>
        </article>
      </div>

      <section class="mt-8">
        <div class="flex items-end justify-between">
          <div>
            <h2 class="text-lg font-extrabold text-slate-950">Verbundene Shops</h2>
            <p class="mt-1 text-sm text-slate-500">Aktueller Verbindungs- und Synchronisationsstatus</p>
          </div>
          <span class="rounded-full bg-slate-100 px-3 py-1 text-xs font-bold text-slate-600">{{ connections().length }} Verbindung(en)</span>
        </div>

        <div class="mt-4 overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
          @if (loading()) {
            <div class="p-8 text-sm font-semibold text-slate-400">Verbindungen werden geladen …</div>
          } @else if (connections().length === 0) {
            <div class="p-10 text-center">
              <div class="text-sm font-extrabold text-slate-800">Noch kein Shop verbunden</div>
              <div class="mt-1 text-xs text-slate-400">Verbinde oben deinen Shopify-Shop, um echte Daten zu synchronisieren.</div>
            </div>
          } @else {
            <div class="divide-y divide-slate-100">
              @for (connection of connections(); track connection.id) {
                <div class="flex flex-col gap-4 p-5 md:flex-row md:items-center md:justify-between">
                  <div class="min-w-0">
                    <div class="flex flex-wrap items-center gap-2">
                      <div class="truncate font-extrabold text-slate-900">{{ connection.displayName }}</div>
                      <span class="rounded-full bg-emerald-50 px-2.5 py-1 text-[10px] font-extrabold text-emerald-700">
                        {{ connection.status === 'Connected' || connection.status === 'Active' ? 'Verbunden' : connection.status }}
                      </span>
                    </div>
                    <div class="mt-1 text-xs text-slate-400">{{ connection.shopDomain || connection.externalStoreId }}</div>
                    <div class="mt-2 flex flex-wrap gap-x-5 gap-y-1 text-xs text-slate-500">
                      <span>Letzter Webhook: <strong class="text-slate-700">{{ connection.lastWebhookAtUtc ? (connection.lastWebhookAtUtc | date:'dd.MM.yyyy HH:mm') : 'Noch keiner' }}</strong></span>
                      <span>Letzter Sync: <strong class="text-slate-700">{{ connection.lastSyncAtUtc ? (connection.lastSyncAtUtc | date:'dd.MM.yyyy HH:mm') : 'Noch keiner' }}</strong></span>
                    </div>
                  </div>
                  <button type="button" (click)="sync(connection)" [disabled]="syncingId() === connection.id"
                    class="shrink-0 rounded-xl border border-slate-200 bg-white px-4 py-2.5 text-xs font-extrabold text-slate-700 hover:bg-slate-50 disabled:opacity-50">
                    {{ syncingId() === connection.id ? 'Synchronisiere …' : 'Jetzt synchronisieren' }}
                  </button>
                </div>
              }
            </div>
          }
        </div>
      </section>
    </section>
  `
})
export class IntegrationenPage {
  private readonly api = inject(ApiService);
  readonly connections = signal<IntegrationConnection[]>([]);
  readonly shopDomain = signal('');
  readonly loading = signal(false);
  readonly syncingId = signal<string | null>(null);
  readonly error = signal<string | null>(null);

  constructor() { this.load(); }

  normalizeShopDomain(value: string): string {
    let normalized = value.trim().toLowerCase();

    normalized = normalized.replace(/^https?:\/\//, '');
    normalized = normalized.split('/')[0];
    normalized = normalized.split('?')[0];
    normalized = normalized.split('#')[0];

    return normalized;
  }

  validShop() {
    return /^[a-z0-9-]+\.myshopify\.com$/.test(this.normalizeShopDomain(this.shopDomain()));
  }

  load() {
    this.loading.set(true);
    this.error.set(null);
    this.api.currentTenantIntegrationConnections().subscribe({
      next: items => { this.connections.set(items.filter(x => x.provider === 'Shopify')); this.loading.set(false); },
      error: () => { this.loading.set(false); this.error.set('Verbindungen konnten nicht geladen werden.'); }
    });
  }

  connectShopify() {
    if (!this.validShop()) return;
    const shop = this.normalizeShopDomain(this.shopDomain());

    window.location.href = `https://smartreconcile-ai-api.onrender.com/api/integrations/shopify/install?shop=${encodeURIComponent(shop)}`;
  }

  sync(connection: IntegrationConnection) {
    this.syncingId.set(connection.id);
    this.error.set(null);
    this.api.syncShopify(connection.id).subscribe({
      next: () => { this.syncingId.set(null); this.load(); },
      error: () => { this.syncingId.set(null); this.error.set('Shopify-Synchronisation fehlgeschlagen.'); }
    });
  }
}
