import { Injectable, signal } from '@angular/core';

type ShopifyGlobal = {
  idToken: () => Promise<string>;
};

declare global {
  interface Window {
    shopify?: ShopifyGlobal;
  }
}

@Injectable({ providedIn: 'root' })
export class ShopifyBridgeService {
  readonly isEmbedded = signal(this.detectEmbedded());
  readonly readyState = signal<'idle' | 'ready' | 'error'>('idle');

  private initialization?: Promise<void>;

  initialize(): Promise<void> {
    if (!this.isEmbedded()) {
      this.readyState.set('error');
      return Promise.resolve();
    }

    return this.initialization ??= this.initializeEmbedded();
  }

  async idToken(): Promise<string | null> {
    if (!this.isEmbedded()) return null;

    await this.initialize();

    if (!window.shopify?.idToken) {
      throw new Error('Shopify App Bridge is not available.');
    }

    return window.shopify.idToken();
  }

  private async initializeEmbedded(): Promise<void> {
    try {
      if (!window.shopify?.idToken) {
        throw new Error('Shopify App Bridge is not available.');
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
}
