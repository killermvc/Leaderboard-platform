import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import {
  ApiKeyService,
  ApiKeyInfo,
  CreatedApiKey,
  EditApiKeyRequest,
  ApiKeyPermission,
} from '../core/api-key-service';
import { GameService, Game } from '../core/game-service';
import { AuthService } from '../core/auth-service';

@Component({
  selector: 'app-api-keys',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './api-keys.html',
  styleUrl: './api-keys.scss',
})
export class ApiKeysComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private apiKeyService = inject(ApiKeyService);
  private gameService = inject(GameService);
  private authService = inject(AuthService);

  game = signal<Game | null>(null);
  keys = signal<ApiKeyInfo[]>([]);
  loading = signal(true);
  error = signal<string | null>(null);
  authorized = signal(true);

  // Generate key form
  keyName = signal('');
  expiresAt = signal<string | null>(null);
  permSubmitScores = signal(true);
  permReadScores = signal(false);
  permReadLeaderboard = signal(false);
  generating = signal(false);
  generateError = signal<string | null>(null);

  // Newly created key disclosure (shown only once)
  createdKey = signal<CreatedApiKey | null>(null);
  copied = signal(false);

  revokingId = signal<number | null>(null);

  isAuthenticated = computed(() => this.authService.isAuthenticated());

  ngOnInit() {
    if (!this.isAuthenticated()) {
      this.router.navigate(['/auth/login']);
      return;
    }

    const gameId = Number(this.route.snapshot.paramMap.get('id'));
    if (isNaN(gameId)) {
      this.error.set('Invalid game ID');
      this.loading.set(false);
      return;
    }

    this.loading.set(true);
    this.loadGame(gameId);
    this.checkPermissions(gameId);
  }

  private loadGame(gameId: number) {
    this.gameService.getGameById(gameId).subscribe({
      next: (game) => {
        this.game.set(game);
      },
      error: (err) => {
        console.error('Failed to load game', err);
        this.error.set('Failed to load game.');
      }
    });
  }

  private checkPermissions(gameId: number) {
    this.apiKeyService.canManageKeys(gameId).subscribe({
      next: (result) => {
        if (result.canManage) {
          this.authorized.set(true);
          this.loadKeys(gameId);
        } else {
          this.authorized.set(false);
          this.loading.set(false);
        }
      },
      error: (err) => {
        console.error('Failed to verify api key permissions', err);
        this.authorized.set(false);
        this.loading.set(false);
      }
    });
  }

  loadKeys(gameId: number) {
    this.loading.set(true);
    this.error.set(null);

    this.apiKeyService.getKeysForGame(gameId).subscribe({
      next: (keys) => {
        this.keys.set(keys);
        this.loading.set(false);
      },
      error: (err) => {
        console.error('Failed to load api keys', err);
        this.error.set('Failed to load api keys.');
        this.loading.set(false);
      }
    });
  }

  generateKey() {
    const gameId = this.game()?.id;
    const name = this.keyName().trim();
    if (!gameId || !name) {
      this.generateError.set('A name for the api key is required.');
      return;
    }

    let permissions = 0;
    if (this.permSubmitScores()) permissions |= ApiKeyPermission.SubmitScores;
    if (this.permReadScores()) permissions |= ApiKeyPermission.ReadScores;
    if (this.permReadLeaderboard()) permissions |= ApiKeyPermission.ReadLeaderboard;

    const request: EditApiKeyRequest = {
      Name: name,
      GameId: gameId,
      Permissions: permissions,
      ExpiresAt: this.expiresAt() || null,
    };

    this.generating.set(true);
    this.generateError.set(null);

    this.apiKeyService.generateKey(request).subscribe({
      next: (created) => {
        this.createdKey.set(created);
        this.keyName.set('');
        this.expiresAt.set(null);
        this.generating.set(false);
        this.loadKeys(gameId);
      },
      error: (err) => {
        console.error('Failed to generate api key', err);
        if (err?.status === 403) {
          this.generateError.set('You do not have permission to manage api keys for this game.');
        } else {
          this.generateError.set('Failed to generate api key. Please try again.');
        }
        this.generating.set(false);
      }
    });
  }

  revokeKey(id: number) {
    const key = this.keys().find(k => k.id === id);
    if (!key) return;

    if (!confirm(`Revoke api key "${key.name}"? This cannot be undone.`)) {
      return;
    }

    this.revokingId.set(id);

    this.apiKeyService.revokeKey(id).subscribe({
      next: () => {
        const gameId = this.game()?.id;
        if (gameId) {
          this.loadKeys(gameId);
        }
        this.revokingId.set(null);
      },
      error: (err) => {
        console.error('Failed to revoke api key', err);
        this.revokingId.set(null);
        alert('Failed to revoke api key. Please try again.');
      }
    });
  }

  async copyKey() {
    const key = this.createdKey();
    if (!key) return;

    try {
      await navigator.clipboard.writeText(key.apiKey);
    } catch {
      return;
    }

    this.copied.set(true);
    setTimeout(() => this.copied.set(false), 2000);
  }

  dismissCreatedKey() {
    this.createdKey.set(null);
    this.copied.set(false);
  }

  isRevoked(key: ApiKeyInfo): boolean {
    return !!key.revokedAt;
  }

  isExpired(key: ApiKeyInfo): boolean {
    return !!key.expiresAt && new Date(key.expiresAt) < new Date();
  }

  statusLabel(key: ApiKeyInfo): string {
    if (this.isRevoked(key)) return 'Revoked';
    if (this.isExpired(key)) return 'Expired';
    return 'Active';
  }

  statusClass(key: ApiKeyInfo): string {
    if (this.isRevoked(key)) return 'revoked';
    if (this.isExpired(key)) return 'expired';
    return 'active';
  }

  formatDate(dateString: string | null | undefined): string {
    if (!dateString) return '—';
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  }

  goBack() {
    const gameId = this.game()?.id;
    if (gameId) {
      this.router.navigate(['/games', gameId]);
    } else {
      this.router.navigate(['/games']);
    }
  }
}