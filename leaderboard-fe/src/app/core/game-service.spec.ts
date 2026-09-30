import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { GameService, Game } from './game-service';
import { environment } from '../../environments/environment';

describe('GameService', () => {
  let service: GameService;
  let httpMock: HttpTestingController;
  const baseUrl = `${environment.apiUrl.replace(/\/auth$/, '')}/game`;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [GameService]
    });
    service = TestBed.inject(GameService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should create a new game', () => {
    const mockGame: Game = { id: 1, name: 'Test Game', description: '', isSubmitAllowed: true };

    service.createGame('Test Game').subscribe(game => {
      expect(game).toEqual(mockGame);
    });

    const req = httpMock.expectOne(baseUrl);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({
      Name: 'Test Game',
      Description: '',
      ImageUrl: '',
      IsSubmitAllowed: true,
    });
    req.flush(mockGame);
  });

  it('should get a game by id', () => {
    const mockGame: Game = { id: 1, name: 'Test Game', description: '', isSubmitAllowed: true };

    service.getGameById(1).subscribe(game => {
      expect(game).toEqual(mockGame);
    });

    const req = httpMock.expectOne(`${baseUrl}/1`);
    expect(req.request.method).toBe('GET');
    req.flush(mockGame);
  });

  it('should get all games with pagination', () => {
    const mockGames: Game[] = [
      { id: 1, name: 'Game 1', description: '', isSubmitAllowed: true },
      { id: 2, name: 'Game 2', description: '', isSubmitAllowed: true }
    ];

    service.getAllGames(10, 0).subscribe(games => {
      expect(games).toEqual(mockGames);
      expect(games.length).toBe(2);
    });

    const req = httpMock.expectOne(`${baseUrl}?limit=10&offset=0`);
    expect(req.request.method).toBe('GET');
    req.flush(mockGames);
  });

  it('should get games by player', () => {
    const mockGames: Game[] = [
      { id: 1, name: 'Game 1', description: '', isSubmitAllowed: true },
      { id: 2, name: 'Game 2', description: '', isSubmitAllowed: true }
    ];
    const playerId = 123;

    service.getGamesByPlayer(playerId).subscribe(games => {
      expect(games).toEqual(mockGames);
    });

    const req = httpMock.expectOne(`${baseUrl}/player/${playerId}`);
    expect(req.request.method).toBe('GET');
    req.flush(mockGames);
  });
});
