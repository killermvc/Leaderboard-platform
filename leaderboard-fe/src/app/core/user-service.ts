import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface User {
  id: number;
  username: string;
  scoresCount?: number;
  gamesPlayedCount?: number;
  createdAt?: string;
}

export interface GameRanking {
  gameId: number;
  gameName: string;
  rank: number | null;
  bestScore: number;
  scoresSubmitted: number;
}

export interface UserScore {
  id: number;
  gameId: number;
  gameName: string;
  value: number;
  dateAchieved: string;
}

export interface UserProfile {
  id: number;
  username: string;
  createdAt: string;
  totalScoresSubmitted: number;
  gamesPlayedCount: number;
  gameRankings: GameRanking[];
  recentScores: UserScore[];
}

@Injectable({
  providedIn: 'root',
})
export class UserService {
  private readonly baseUrl = environment.apiUrl;
  private readonly usersUrl = environment.apiUrl.replace(/\/auth$/, '') + '/users';

  constructor(private http: HttpClient) {}

  searchUsers(query: string, limit = 20): Observable<User[]> {
    return this.http.get<User[]>(`${this.usersUrl}/search`, {
      params: { q: query, limit: limit.toString() }
    });
  }

  getUserProfile(userId: number): Observable<UserProfile> {
    return this.http.get<UserProfile>(`${this.baseUrl}/user/${userId}`);
  }
}
