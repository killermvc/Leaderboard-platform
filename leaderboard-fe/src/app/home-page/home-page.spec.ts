import { ComponentFixture, TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { of } from 'rxjs';

import { HomePage } from './home-page';
import { ScoreService } from '../core/score-service';
import { AuthService } from '../core/auth-service';

describe('HomePage', () => {
  let component: HomePage;
  let fixture: ComponentFixture<HomePage>;

  const scoreServiceMock = {
    getRecentScores: jasmine.createSpy('getRecentScores').and.returnValue(of([])),
    getScoresByUser: jasmine.createSpy('getScoresByUser').and.returnValue(of([])),
  };

  const authServiceMock = {
    currentUserIdSig: signal(1),
    isAuthenticated: jasmine.createSpy('isAuthenticated').and.returnValue(true),
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HomePage],
    providers: [
      { provide: ScoreService, useValue: scoreServiceMock },
      { provide: AuthService, useValue: authServiceMock },
    ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HomePage);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
