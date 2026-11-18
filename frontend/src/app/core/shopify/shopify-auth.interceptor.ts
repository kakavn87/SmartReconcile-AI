import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, from, switchMap, throwError } from 'rxjs';
import { ShopifyBridgeService } from './shopify-bridge.service';

export const shopifyAuthInterceptor: HttpInterceptorFn = (req, next) => {
  const bridge = inject(ShopifyBridgeService);

  if (!bridge.isEmbedded()) {
    return next(req);
  }

  return from(bridge.idToken()).pipe(
    switchMap(token => {
      const authenticatedRequest = token
        ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
        : req;

      return next(authenticatedRequest).pipe(
        catchError(error => {
          if (error.status !== 401 || !token) {
            return throwError(() => error);
          }

          return from(bridge.idToken()).pipe(
            switchMap(retryToken => {
              if (!retryToken) return throwError(() => error);

              return next(req.clone({
                setHeaders: { Authorization: `Bearer ${retryToken}` }
              }));
            })
          );
        })
      );
    })
  );
};
