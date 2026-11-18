import { Injectable, signal } from '@angular/core';

const API_BASE_URL = 'https://smartreconcile-ai-api.onrender.com/api';
const APP_BRIDGE_SRC = 'https://cdn.shopify.com/shopifycloud/app-bridge.js';

type ShopifyGlobal = {
  idToken: () => Promise<string>;
  config?: {
    apiKey?: string;
    shop?: string;
  };
};

declare global {
  interface Window {
    shopify?: ShopifyGlobal;
  }
}

@Injectable({ providedIn: 'root' })
export class ShopifyBridgeService {
  readonly isEmbedded = signal(this.detectEmbedded());
  readonly readyState = signal<'idle' | 'ready' | 'demo' | 'error'>('idle');

  private initialization?: Promise<void>;

  initialize(): Promise<void> {
    if (!this.isEmbedded()) {
      this.readyState.set('demo');
      return Promise.resolve();
    }

    return this.initialization ??= this.loadAppBridge();
  }

  async idToken(): Promise<string | null> {
    if (!this.isEmbedded()) return null;

    await this.initialize();

    if (!window.shopify?.idToken) {
      throw new Error('Shopify App Bridge is not available.');
    }

    return window.shopify.idToken();
  }

  private async loadAppBridge(): Promise<void> {
    try {
      const response = await fetch(API_BASE_URL + '/auth/shopify/config', {
        headers: { Accept: 'application/json' },
        credentials: 'omit'
      });

      if (!response.ok) {
        throw new Error('Shopify frontend configuration could not be loaded.');
      }

      const config = await response.json() as { clientId?: string };
      const clientId = config.clientId?.trim();

      if (!clientId) {
        throw new Error('Shopify Client ID is not configured on the backend.');
      }

      this.setMeta('shopify-api-key', clientId);
      // Authentication is attached explicitly by our Angular interceptor.
      // Disabling App Bridge's global fetch interception prevents duplicate headers.
      this.setMeta('shopify-disabled-features', 'fetch');
      this.setMeta('shopify-app-origins', API_BASE_URL.replace(/\/api$/, ''));

      await this.loadScript(APP_BRIDGE_SRC);

      if (!window.shopify?.idToken) {
        throw new Error('Shopify App Bridge loaded without the ID Token API.');
      }

      this.readyState.set('ready');
    } catch (error) {
      this.readyState.set('error');
      throw error;
    }
  }

  private detectEmbedded(): boolean {
    if (typeof window === 'undefined') return false;

    const inIframe = window.top !== window.self;
    if (!inIframe) return false;

    const params = new URLSearchParams(window.location.search);
    return params.has('host')
      || document.referrer.includes('admin.shopify.com')
      || document.referrer.includes('.myshopify.com');
  }

  private setMeta(name: string, content: string): void {
    let meta = document.querySelector<HTMLMetaElement>(`meta[name="${name}"]`);

    if (!meta) {
      meta = document.createElement('meta');
      meta.name = name;
      document.head.appendChild(meta);
    }

    meta.content = content;
  }

  private loadScript(src: string): Promise<void> {
    const existing = document.querySelector<HTMLScriptElement>(`script[src="${src}"]`);
    if (existing) return Promise.resolve();

    return new Promise((resolve, reject) => {
      const script = document.createElement('script');
      script.src = src;
      script.async = true;
      script.onload = () => resolve();
      script.onerror = () => reject(new Error('Could not load Shopify App Bridge.'));
      document.head.appendChild(script);
    });
  }
}
