import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { canActivateClerk, ClerkService } from 'ngx-clerk';

/** Requires a signed-in Clerk user; redirects to sign-in otherwise. */
export const requireAuth: CanActivateFn = (route, state) =>
  canActivateClerk(route, state);

/** Blocks signed-in users (e.g. login/register); redirects them home. */
export const requireGuest: CanActivateFn = () => {
  const clerk = inject(ClerkService);
  const router = inject(Router);
  if (clerk.isLoaded() && clerk.isSignedIn()) {
    return router.createUrlTree(['/']);
  }
  return true;
};
