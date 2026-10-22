import { ApplicationConfig, provideBrowserGlobalErrorListeners, provideZoneChangeDetection, APP_INITIALIZER } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideClerk } from 'ngx-clerk';
import { environment } from '../environments/environment';

import { routes } from './app.routes';
import { clerkAuthInterceptor } from './core/auth-interceptor';
import { initializeLanguage } from './core/language-initializer';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideClerk({
      publishableKey: environment.clerkPublishableKey,
      signInUrl: '/auth/login',
      signUpUrl: '/auth/register',
      afterSignOutUrl: '/',
    }),
    provideHttpClient(withInterceptors([clerkAuthInterceptor])),
    {
      provide: APP_INITIALIZER,
      useFactory: initializeLanguage,
      multi: true
    }
  ]
};
