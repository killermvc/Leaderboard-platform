import { ApplicationConfig, provideBrowserGlobalErrorListeners, provideZoneChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideClerk } from 'ngx-clerk';
import { environment } from '../environments/environment';

import { routes } from './app.routes';
import { clerkAuthInterceptor } from './core/auth-interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideClerk({
      publishableKey: environment.clerkPublishableKey,
      signInUrl: '/auth/login',
      signUpUrl: '/auth/register',
    }),
    provideHttpClient(withInterceptors([clerkAuthInterceptor]))
  ]
};
