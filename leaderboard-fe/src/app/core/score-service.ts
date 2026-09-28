import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { AuthService } from './auth-service';

export interface LeaderboardEntry {
  /** Null for a score sent by a game client, which is identified by its player name. */
  userId: number | null;
  userName?: string | null;
  score: number;
}

export interface RankResponse {
  gameId: number;
  userId: number;
  rank: number | null;
}

export interface TopPlayersResponse {
  gameId: number;
  limit: number;
  leaderboard: LeaderboardEntry[];
}

export interface ScoreUser {
  id: number;
  username?: string | null;
}

export interface ScoreGame {
  id: number;
  name: string;
}

export enum ScoreStatus {
  Pending = 0,
  Approved = 1,
  Rejected = 2
}

export interface ScoreRecord {
  id: number;
  /** Null for a score sent by a game client, which carries a playerName instead. */
  user: ScoreUser | null;
  playerName?: string | null;
  game: ScoreGame;
  value: number;
  dateAchieved: string;
  title: string;
  description?: string | null;
  status: ScoreStatus;
  reviewedBy?: ScoreUser | null;
  reviewedAt?: string | null;
  rejectionReason?: string | null;
}

@Injectable({
  providedIn: 'root',
})
export class ScoreService {
  // Fallback guards the rare case where scoreApiUrl is omitted in environment files.
  private readonly baseUrl = environment.scoreApiUrl ?? environment.apiUrl.replace(/\/auth$/, '/score');

  constructor(private http: HttpClient, private authService: AuthService) {}

  submitScore(gameId: number, score: number, title?: string, description?: string): Observable<string> {
    const payload: Record<string, unknown> = { gameId, score };
    if (title) payload['title'] = title;
    if (description) payload['description'] = description;
    return this.http.post<string>(`${this.baseUrl}/submit`, payload, {
      ...this.getAuthOptions(),
      responseType: 'text' as 'json',
    });
  }

  /**
   * Get all score submissions (posts) visible to everyone
   */
  getAllSubmissions(limit = 20, offset = 0): Observable<ScoreRecord[]> {
    const params = this.buildPaginationParams(limit, offset);
    return this.http.get<ScoreRecord[]>(`${this.baseUrl}/scores/submissions`, { params });
  }

  /**
   * Get a single score post by ID (publicly accessible)
   */
  getScoreById(scoreId: number): Observable<ScoreRecord> {
    return this.http.get<ScoreRecord>(`${this.baseUrl}/scores/${scoreId}`);
  }

  /**
   * Look up the top approved score for a user in a game.
   * Returns the scoreId so the client can navigate to the score post.
   */
  lookupScore(gameId: number, userId: number): Observable<{ scoreId: number }> {
    const params = new HttpParams()
      .set('gameId', gameId.toString())
      .set('userId', userId.toString());
    return this.http.get<{ scoreId: number }>(`${this.baseUrl}/scores/lookup`, { params });
  }

  getLeaderboard(gameId: number, limit = 10): Observable<LeaderboardEntry[]> {
    const params = new HttpParams().set('limit', limit.toString());
    return this.http.get<LeaderboardEntry[]>(`${this.baseUrl}/leaderboard/${gameId}`, { params });
  }

  getRank(gameId: number, userId: number): Observable<RankResponse> {
    return this.http.get<RankResponse>(`${this.baseUrl}/leaderboard/${gameId}/rank/${userId}`);
  }

  getTopPlayers(gameId: number, limit: number): Observable<TopPlayersResponse> {
    return this.http.get<TopPlayersResponse>(`${this.baseUrl}/leaderboard/${gameId}/top/${limit}`);
  }

  getScoresByUser(userId: number, limit = 10, offset = 0): Observable<ScoreRecord[]> {
    const params = this.buildPaginationParams(limit, offset);
    return this.http.get<ScoreRecord[]>(`${this.baseUrl}/scores/user/${userId}`, { params });
  }

  getRecentScores(limit = 10, offset = 0): Observable<ScoreRecord[]> {
    const params = this.buildPaginationParams(limit, offset);
    return this.http.get<ScoreRecord[]>(`${this.baseUrl}/scores/recent`, { params });
  }

  /**
   * Get all scores submitted by the current user, including pending ones
   */
  getMySubmissions(limit = 20, offset = 0): Observable<ScoreRecord[]> {
    const params = this.buildPaginationParams(limit, offset);
    return this.http.get<ScoreRecord[]>(`${this.baseUrl}/scores/my-submissions`, {
      params,
      ...this.getAuthOptions()
    });
  }

  private buildPaginationParams(limit: number, offset: number): HttpParams {
    return new HttpParams()
      .set('limit', limit.toString())
      .set('offset', offset.toString());
  }

  private getAuthOptions(): { headers?: HttpHeaders } {
    return this.authService.getAuthHeaders() as { headers?: HttpHeaders };
  }
}
