import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { ClerkService } from 'ngx-clerk';

import { AuthService } from './auth-service';

describe('AuthService', () => {
  let service: AuthService;
  let httpTesting: HttpTestingController;
  const clerk = {
    isLoaded: signal(true),
    isSignedIn: signal(true),
    user: signal(null),
    signOut: jasmine.createSpy('signOut').and.resolveTo()
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: ClerkService, useValue: clerk }
      ]
    });
    service = TestBed.inject(AuthService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('stays authenticated when Clerk reports signed-in', () => {
    expect(service.isAuthenticated()).toBeTrue();
  });

  it('does not touch localStorage for tokens', () => {
    expect(localStorage.getItem('lb_token')).toBeNull();
  });
});
