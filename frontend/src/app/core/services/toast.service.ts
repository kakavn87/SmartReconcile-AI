import { Injectable, signal } from '@angular/core';

export type ToastType = 'success' | 'error';

export interface Toast {
  id: number;
  type: ToastType;
  message: string;
}

@Injectable({ providedIn: 'root' })
export class ToastService {
  private readonly nextId = signal(0);
  readonly toasts = signal<Toast[]>([]);

  success(message: string): void {
    this.show('success', message);
  }

  error(message: string): void {
    this.show('error', message);
  }

  dismiss(id: number): void {
    this.toasts.update(items => items.filter(item => item.id !== id));
  }

  private show(type: ToastType, message: string): void {
    const id = this.nextId() + 1;
    this.nextId.set(id);
    this.toasts.update(items => [...items, { id, type, message }]);
    window.setTimeout(() => this.dismiss(id), 4000);
  }
}
