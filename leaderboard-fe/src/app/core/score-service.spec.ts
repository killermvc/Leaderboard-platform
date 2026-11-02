import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { HttpTestingController } from '@angular/common/http/testing';
import { environment } from '../../environments/environment';

import { ScoreService } from './score-service';

describe('ScoreService', () => {
  let service: ScoreService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
    });
    service = TestBed.inject(ScoreService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('submits a score for the selected leaderboard', () => {
    service.submitScore(7, 11, 9000).subscribe();

    const request = httpMock.expectOne(`${environment.scoreApiUrl}/submit`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ gameId: 7, leaderboardId: 11, score: 9000 });
    request.flush('Score submitted successfully.');
  });

  it('gets a board-scoped leaderboard', () => {
    service.getLeaderboard(11, 20).subscribe();

    const request = httpMock.expectOne(`${environment.scoreApiUrl}/leaderboard/board/11?limit=20`);
    expect(request.request.method).toBe('GET');
    request.flush([]);
  });
});
