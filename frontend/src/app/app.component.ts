import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ShopifyBridgeService } from './core/shopify/shopify-bridge.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<router-outlet />'
})
export class AppComponent {
  private readonly shopify = inject(ShopifyBridgeService);

  constructor() {
    void this.shopify.initialize();
  }
}
