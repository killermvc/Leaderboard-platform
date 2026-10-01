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
    getToken: jasmine.createSpy('getToken').and.resolveTo('fresh-token'),
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
    localStorage.setItem('lb_token', 'header.eyJleHAiOjF9.signature');
  });

  afterEach(() => {
    httpTesting.verify();
    localStorage.removeItem('lb_token');
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('stays authenticated when Clerk has refreshed an expired cached token', () => {
    expect(service.isAuthenticated()).toBeTrue();
  });
});
