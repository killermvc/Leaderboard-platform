import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface Leaderboard {
  id: number;
  gameId: number;
  name: string;
  description?: string | null;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface CreateLeaderboardRequest {
  name: string;
  description?: string | null;
}

export interface UpdateLeaderboardRequest extends CreateLeaderboardRequest {
  isActive: boolean;
}

@Injectable({
  providedIn: 'root',
})
export class LeaderboardService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl.replace(/\/auth$/, '')}/games`;

  getByGame(gameId: number): Observable<Leaderboard[]> {
    return this.http.get<Leaderboard[]>(`${this.baseUrl}/${gameId}/leaderboards`);
  }

  getById(gameId: number, leaderboardId: number): Observable<Leaderboard> {
    return this.http.get<Leaderboard>(`${this.baseUrl}/${gameId}/leaderboards/${leaderboardId}`);
  }

  create(gameId: number, request: CreateLeaderboardRequest): Observable<Leaderboard> {
    return this.http.post<Leaderboard>(`${this.baseUrl}/${gameId}/leaderboards`, request);
  }

  update(gameId: number, leaderboardId: number, request: UpdateLeaderboardRequest): Observable<Leaderboard> {
    return this.http.put<Leaderboard>(
      `${this.baseUrl}/${gameId}/leaderboards/${leaderboardId}`,
      request
    );
  }

  delete(gameId: number, leaderboardId: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${gameId}/leaderboards/${leaderboardId}`);
  }
}
