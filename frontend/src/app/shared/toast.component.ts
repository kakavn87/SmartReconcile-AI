import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ToastService } from '../core/services/toast.service';

@Component({
  selector: 'app-toast',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="pointer-events-none fixed right-4 top-4 z-[100] flex w-[min(92vw,420px)] flex-col gap-3" aria-live="polite" aria-atomic="true">
      @for (toast of toastService.toasts(); track toast.id) {
        <div class="pointer-events-auto flex items-start gap-3 rounded-xl border bg-white p-4 shadow-xl"
          [class.border-emerald-200]="toast.type === 'success'"
          [class.border-red-200]="toast.type === 'error'"
          [class.bg-emerald-50]="toast.type === 'success'"
          [class.bg-red-50]="toast.type === 'error'">
          <div class="mt-0.5 flex h-5 w-5 shrink-0 items-center justify-center rounded-full text-xs font-black"
            [class.bg-emerald-600]="toast.type === 'success'"
            [class.text-white]="true"
            [class.bg-red-600]="toast.type === 'error'">
            {{ toast.type === 'success' ? '✓' : '!' }}
          </div>
          <div class="min-w-0 flex-1 text-sm font-semibold"
            [class.text-emerald-800]="toast.type === 'success'"
            [class.text-red-800]="toast.type === 'error'">
            {{ toast.message }}
          </div>
          <button type="button" class="shrink-0 text-lg leading-none text-slate-400 hover:text-slate-700" (click)="toastService.dismiss(toast.id)" aria-label="Meldung schließen">×</button>
        </div>
      }
    </div>
  `
})
export class ToastComponent {
  readonly toastService = inject(ToastService);
}
