import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface GameModeratorUser {
  id: number;
  username: string;
}

export interface GameModeratorInfo {
  id: number;
  gameId: number;
  userId: number;
  user: GameModeratorUser;
  assignedAt: string;
}

export interface ModeratedGame {
  id: number;
  gameId: number;
  userId: number;
  game: {
    id: number;
    name: string;
    description: string;
    imageUrl?: string;
  };
  assignedAt: string;
}

export interface PendingScore {
  id: number;
  /** Null for a score sent by a game client, which carries a playerName instead. */
  user: {
    id: number;
    username: string;
  } | null;
  playerName?: string | null;
  game: {
    id: number;
    name: string;
  };
  value: number;
  dateAchieved: string;
  title: string;
  description?: string | null;
  status: number; // 0 = Pending, 1 = Approved, 2 = Rejected
  reviewedBy?: {
    id: number;
    username: string;
  };
  reviewedAt?: string;
  rejectionReason?: string;
}

@Injectable({
  providedIn: 'root',
})
export class ModerationService {
  private readonly baseUrl = `${environment.apiUrl.replace(/\/auth$/, '')}/moderation`;

  private http = inject(HttpClient);

  // ==================== Score Moderation ====================

  /**
   * Approve a pending score
   */
  approveScore(scoreId: number): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.baseUrl}/scores/${scoreId}/approve`, {});
  }

  /**
   * Reject a pending score with an optional reason
   */
  rejectScore(scoreId: number, reason?: string): Observable<{ message: string }> {
    const body = { reason: reason ?? null };
    return this.http.post<{ message: string }>(`${this.baseUrl}/scores/${scoreId}/reject`, body);
  }

  /**
   * Get pending scores for the current moderator
   * Returns pending scores for games they moderate, or unmoderated games if global moderator
   */
  getPendingScores(limit = 20, offset = 0): Observable<PendingScore[]> {
    const params = new HttpParams()
      .set('limit', limit.toString())
      .set('offset', offset.toString());
    return this.http.get<PendingScore[]>(`${this.baseUrl}/pending-scores`, { params });
  }

  /**
   * Get pending scores for a specific game
   */
  getPendingScoresForGame(gameId: number, limit = 20, offset = 0): Observable<PendingScore[]> {
    const params = new HttpParams()
      .set('limit', limit.toString())
      .set('offset', offset.toString());
    return this.http.get<PendingScore[]>(`${this.baseUrl}/games/${gameId}/pending-scores`, { params });
  }

  // ==================== Moderator Management ====================

  /** Add a user as a moderator for a game. */
  addGameModerator(gameId: number, userId: number): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.baseUrl}/games/${gameId}/moderators/${userId}`, {});
  }

  /** Remove a user as a moderator from a game. */
  removeGameModerator(gameId: number, userId: number): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.baseUrl}/games/${gameId}/moderators/${userId}`);
  }

  /**
   * Get all moderators for a game
   */
  getGameModerators(gameId: number): Observable<GameModeratorInfo[]> {
    return this.http.get<GameModeratorInfo[]>(`${this.baseUrl}/games/${gameId}/moderators`);
  }

  /**
   * Get all games the current user moderates
   */
  getMyModeratedGames(): Observable<ModeratedGame[]> {
    return this.http.get<ModeratedGame[]>(`${this.baseUrl}/my-games`);
  }

  /**
   * Check if user can moderate (is a game mod or global moderator)
   */
  canModerate(gameId: number): Observable<{ canModerate: boolean }> {
    return this.http.get<{ canModerate: boolean }>(`${this.baseUrl}/games/${gameId}/can-moderate`);
  }
}
