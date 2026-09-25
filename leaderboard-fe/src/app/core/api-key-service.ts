import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface ApiKeyInfo {
  id: number;
  name: string;
  createdAt: string;
  lastUsedAt?: string | null;
  expiresAt?: string | null;
  revokedAt?: string | null;
}

export interface CreatedApiKey {
  id: number;
  name: string;
  apiKey: string;
  createdAt: string;
  expiresAt?: string | null;
}

export interface EditApiKeyRequest {
  Name: string;
  GameId: number;
  Permissions?: number;
  ExpiresAt?: string | null;
}

export const ApiKeyPermission = {
  None: 0,
  SubmitScores: 1 << 0,
  ReadScores: 1 << 1,
  ReadLeaderboard: 1 << 2,
} as const;

@Injectable({
  providedIn: 'root',
})
export class ApiKeyService {
  private readonly baseUrl = `${environment.apiUrl.replace(/\/auth$/, '')}/ApiKey`;

  private http = inject(HttpClient);

  /**
   * Check whether the current user can manage api keys for a game.
   * Permission rules are enforced centrally by the backend.
   */
  canManageKeys(gameId: number): Observable<{ canManage: boolean }> {
    return this.http.get<{ canManage: boolean }>(`${this.baseUrl}/game/${gameId}/can-manage`);
  }

  /**
   * List api keys for a game (prefix, name and dates only — never the key/hash).
   */
  getKeysForGame(gameId: number): Observable<ApiKeyInfo[]> {
    return this.http.get<ApiKeyInfo[]>(`${this.baseUrl}/game/${gameId}`);
  }

  /**
   * Generate a new api key for a game. The full key is only returned once.
   */
  generateKey(request: EditApiKeyRequest): Observable<CreatedApiKey> {
    return this.http.post<CreatedApiKey>(this.baseUrl, request);
  }

  /**
   * Regenerate an api key: revokes the existing key and creates a new one
   * with the same name and permissions. The full key is only returned once.
   */
  regenerateKey(id: number): Observable<CreatedApiKey> {
    return this.http.post<CreatedApiKey>(`${this.baseUrl}/${id}/regenerate`, {});
  }

  /**
   * Revoke an api key.
   */
  revokeKey(id: number): Observable<ApiKeyInfo> {
    return this.http.post<ApiKeyInfo>(`${this.baseUrl}/${id}/revoke`, {});
  }
}