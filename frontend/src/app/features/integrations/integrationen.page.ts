import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { ApiService } from '../../core/api/api.service';
import { IntegrationConnection, ShopifySessionInfo } from '../../core/models/api.models';
import { ShopifyBridgeService } from '../../core/shopify/shopify-bridge.service';

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
            Shopify ist im Embedded-Modus direkt mit dem aktuell geöffneten Shop verbunden.
          </p>
        </div>
        <button type="button" (click)="load()" [disabled]="loading()"
          class="rounded-xl border border-slate-200 bg-white px-4 py-2.5 text-sm font-bold text-slate-700 shadow-sm hover:bg-slate-50 disabled:opacity-50">
          Aktualisieren
        </button>
      </div>

      @if (error()) {
        <div class="mt-6 rounded-2xl border border-red-200 bg-red-50 p-4 text-sm font-semibold text-red-700">{{ error() }}</div>
      }

      @if (isEmbedded()) {
        <article class="mt-8 rounded-2xl border border-emerald-200 bg-emerald-50/70 p-5 shadow-sm">
          <div class="flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
            <div>
              <div class="flex items-center gap-2">
                <span class="h-3 w-3 rounded-full bg-emerald-500 shadow-[0_0_0_5px_rgba(16,185,129,.12)]"></span>
                <span class="text-sm font-extrabold text-emerald-800">Shopify · Verbunden</span>
              </div>
              <div class="mt-2 text-xl font-extrabold text-slate-950">
                {{ session()?.shopDomain || 'Aktueller Shopify-Shop' }}
              </div>
              <p class="mt-1 text-xs text-slate-600">
                Die Verbindung wird automatisch über die aktuelle ShopifySession hergestellt. Kein OAuth-Redirect innerhalb des Embedded Apps.
              </p>
            </div>
            @if (currentConnection(); as connection) {
              <button type="button" (click)="sync(connection)" [disabled]="syncingId() === connection.id"
                class="rounded-xl bg-slate-950 px-5 py-3 text-sm font-extrabold text-white shadow-sm transition hover:bg-slate-800 disabled:cursor-not-allowed disabled:opacity-50">
                {{ syncingId() === connection.id ? 'Synchronisiere …' : 'Jetzt synchronisieren' }}
              </button>
            }
          </div>
        </article>

        <section class="mt-8">
          <div class="flex items-end justify-between">
            <div>
              <h2 class="text-lg font-extrabold text-slate-950">Verbundener Shop</h2>
              <p class="mt-1 text-sm text-slate-500">Aktueller Verbindungs- und Synchronisationsstatus</p>
            </div>
            <span class="rounded-full bg-emerald-50 px-3 py-1 text-xs font-bold text-emerald-700">Embedded App</span>
          </div>

          <div class="mt-4 overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
            @if (loading()) {
              <div class="p-8 text-sm font-semibold text-slate-400">Shopify-Verbindung wird geladen …</div>
            } @else if (!currentConnection()) {
              <div class="p-10 text-center">
                <div class="text-sm font-extrabold text-slate-800">Shopify-Verbindung wird eingerichtet …</div>
                <div class="mt-1 text-xs text-slate-400">Die aktuelle ShopifySession wird automatisch auf Tenant und Verbindung aufgelöst.</div>
              </div>
            } @else {
              @if (currentConnection(); as connection) {
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
            }
          </div>
        </section>
      } @else {
        <article class="mt-8 rounded-2xl border border-slate-200 bg-slate-50 p-5 shadow-sm">
          <div class="flex items-center gap-2">
            <span class="rounded-full bg-slate-200 px-2.5 py-1 text-[10px] font-extrabold text-slate-600">Demo-Modus</span>
            <span class="text-sm font-extrabold text-slate-800">Shopify-Vorschau</span>
          </div>
          <p class="mt-2 max-w-2xl text-sm leading-6 text-slate-500">
            Außerhalb des Shopify Admin bleibt diese Seite eine sichere Vorschau. Es wird kein OAuth-Redirect aus der Demo ausgelöst.
          </p>

          <div class="mt-6">
            <label class="text-xs font-bold text-slate-600">Shop-Domain</label>
            <div class="mt-2 flex flex-col gap-2 sm:flex-row">
              <input
                [value]="shopDomain()"
                (input)="shopDomain.set($any($event.target).value)"
                placeholder="https://mein-shop.myshopify.com/"
                class="min-w-0 flex-1 rounded-xl border border-slate-200 bg-white px-4 py-3 text-sm outline-none transition focus:border-blue-400 focus:ring-4 focus:ring-blue-100"
              />
              <button type="button" disabled
                class="rounded-xl bg-slate-300 px-5 py-3 text-sm font-extrabold text-white">
                Demo-Modus
              </button>
            </div>
            <p class="mt-2 text-xs text-slate-400">Für eine echte Verbindung bitte die App aus dem Shopify Admin öffnen.</p>
          </div>
        </article>
      }
    </section>
  `
})
export class IntegrationenPage {
  private readonly api = inject(ApiService);
  private readonly shopify = inject(ShopifyBridgeService);

  readonly connections = signal<IntegrationConnection[]>([]);
  readonly session = signal<ShopifySessionInfo | null>(null);
  readonly shopDomain = signal('');
  readonly loading = signal(false);
  readonly syncingId = signal<string | null>(null);
  readonly error = signal<string | null>(null);

  constructor() {
    this.load();
  }

  readonly isEmbedded = () => this.shopify.isEmbedded();

  currentConnection() {
    const connectionId = this.session()?.connectionId;
    return this.connections().find(x => x.id === connectionId)
      ?? this.connections()[0]
      ?? null;
  }

  load() {
    if (!this.isEmbedded()) {
      this.loading.set(false);
      this.error.set(null);
      this.session.set(null);
      this.connections.set([]);
      return;
    }

    this.loading.set(true);
    this.error.set(null);
    this.session.set(null);
    this.connections.set([]);

    this.api.shopifySession().subscribe({
      next: session => {
        this.session.set(session);
        this.shopDomain.set(session.shopDomain);
        this.api.currentTenantIntegrationConnections().subscribe({
          next: items => {
            const shopifyConnections = items.filter(x => x.provider === 'Shopify');
            this.connections.set(shopifyConnections);
            this.loading.set(false);
          },
          error: () => {
            this.loading.set(false);
            this.error.set('Shopify-Verbindung konnte nicht geladen werden.');
          }
        });
      },
      error: () => {
        this.loading.set(false);
        this.error.set('ShopifySession konnte nicht authentifiziert werden.');
      }
    });
  }

  sync(connection: IntegrationConnection) {
    this.syncingId.set(connection.id);
    this.error.set(null);

    this.api.syncShopify(connection.id).subscribe({
      next: () => {
        this.syncingId.set(null);
        this.load();
      },
      error: () => {
        this.syncingId.set(null);
        this.error.set('Shopify-Synchronisation fehlgeschlagen.');
      }
    });
  }
}
