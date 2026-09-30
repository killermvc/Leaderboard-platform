import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { of } from 'rxjs';

import { Games } from './games';
import { GameService } from '../core/game-service';
import { AuthService } from '../core/auth-service';

describe('Games', () => {
  let component: Games;
  let fixture: ComponentFixture<Games>;
  let gameService: jasmine.SpyObj<GameService>;
  let authService: jasmine.SpyObj<AuthService>;

  const games = [
    { id: 1, name: 'Space Quest', description: '', isSubmitAllowed: true },
    { id: 2, name: 'Card Forge', description: '', isSubmitAllowed: true }
  ];

  beforeEach(async () => {
    gameService = jasmine.createSpyObj('GameService', ['getAllGames', 'getGamesByPlayer']);
    gameService.getAllGames.and.returnValue(of(games));
    gameService.getGamesByPlayer.and.returnValue(of([games[0]]));

    authService = jasmine.createSpyObj('AuthService', ['getUserIdFromToken', 'isAuthenticated', 'hasRole']);
    authService.getUserIdFromToken.and.returnValue('42');
    authService.isAuthenticated.and.returnValue(true);
    authService.hasRole.and.returnValue(false);

    await TestBed.configureTestingModule({
      imports: [Games],
      providers: [
        { provide: GameService, useValue: gameService },
        { provide: AuthService, useValue: authService },
        { provide: ActivatedRoute, useValue: { queryParams: of({}) } },
        { provide: Router, useValue: jasmine.createSpyObj('Router', ['navigate']) }
      ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(Games);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('filters games by name', () => {
    component.games.set(games);
    component.searchQuery.set('forge');

    expect(component.filteredGames()).toEqual([games[1]]);
  });

  it('loads only games submitted to when the filter is enabled', () => {
    component.toggleSubmittedOnly();

    expect(gameService.getGamesByPlayer).toHaveBeenCalledWith(42);
    expect(component.games()).toEqual([games[0]]);
  });
});
