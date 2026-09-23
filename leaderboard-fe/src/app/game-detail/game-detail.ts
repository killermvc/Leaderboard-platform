import { Component, ChangeDetectionStrategy, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { GameService, Game } from '../core/game-service';
import { ScoreService, LeaderboardEntry } from '../core/score-service';
import { AuthService } from '../core/auth-service';
import { ModerationService } from '../core/moderation-service';
import { ApiKeyService } from '../core/api-key-service';

@Component({
  selector: 'app-game-detail',
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './game-detail.html',
  styleUrl: './game-detail.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class GameDetail implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private gameService = inject(GameService);
  private scoreService = inject(ScoreService);
  private authService = inject(AuthService);
  private moderationService = inject(ModerationService);
  private apiKeyService = inject(ApiKeyService);
  private fb = inject(FormBuilder);

  game = signal<Game | null>(null);
  leaderboard = signal<LeaderboardEntry[]>([]);
  loading = signal(true);
  loadingLeaderboard = signal(true);
  error = signal<string | null>(null);
  submitSuccess = signal<string | null>(null);
  submitError = signal<string | null>(null);
  submitting = signal(false);
  canModerate = signal(false);
  canManageKeys = signal(false);

  isLoggedIn = computed(() => this.authService.isAuthenticated());
  isAdmin = computed(() => this.authService.hasRole('Admin'));

  scoreForm = this.fb.group({
    score: [0, [Validators.required, Validators.min(0)]],
    title: [''],
    description: ['']
  });

  ngOnInit() {
    const gameId = Number(this.route.snapshot.paramMap.get('id'));
    if (isNaN(gameId)) {
      this.error.set('Invalid game ID');
      this.loading.set(false);
      return;
    }

    this.loadGame(gameId);
    this.loadLeaderboard(gameId);
    this.checkModerationRights(gameId);
    this.checkApiKeyRights(gameId);
  }

  private loadGame(gameId: number) {
    this.loading.set(true);
    this.gameService.getGameById(gameId).subscribe({
      next: (game) => {
        this.game.set(game);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set('Failed to load game details');
        this.loading.set(false);
        console.error('Failed to load game', err);
      }
    });
  }

  private loadLeaderboard(gameId: number) {
    this.loadingLeaderboard.set(true);
    this.scoreService.getLeaderboard(gameId, 20).subscribe({
      next: (entries) => {
        this.leaderboard.set(entries);
        this.loadingLeaderboard.set(false);
      },
      error: (err) => {
        console.error('Failed to load leaderboard', err);
        this.loadingLeaderboard.set(false);
      }
    });
  }

  private checkModerationRights(gameId: number) {
    if (!this.isLoggedIn()) {
      return;
    }

    this.moderationService.canModerate(gameId).subscribe({
      next: (result) => {
        this.canModerate.set(result.canModerate);
      },
      error: () => {
        this.canModerate.set(false);
      }
    });
  }

  private checkApiKeyRights(gameId: number) {
    if (!this.isLoggedIn()) {
      return;
    }

    this.apiKeyService.canManageKeys(gameId).subscribe({
      next: (result) => {
        this.canManageKeys.set(result.canManage);
      },
      error: () => {
        this.canManageKeys.set(false);
      }
    });
  }

  submitScore() {
    if (this.scoreForm.invalid || !this.game()) {
      return;
    }

    const scoreValue = this.scoreForm.value.score ?? 0;
    const gameId = this.game()!.id;
    const gameName = this.game()!.name;
    const title = this.scoreForm.value.title?.trim() || `${gameName} - ${scoreValue}`;
    const description = this.scoreForm.value.description?.trim() || undefined;

    this.submitting.set(true);
    this.submitSuccess.set(null);
    this.submitError.set(null);

    this.scoreService.submitScore(gameId, scoreValue, title, description).subscribe({
      next: () => {
        this.submitSuccess.set('Score submitted successfully! It will appear on the leaderboard once approved by a moderator.');
        this.submitting.set(false);
        this.scoreForm.reset({ score: 0, title: '', description: '' });
        // Note: We don't refresh the leaderboard immediately since the score is pending
      },
      error: (err) => {
        // If the backend rejected the score because it's not higher, show that message
        if (err?.status === 400 && typeof err?.error === 'string') {
          const msg: string = err.error as string;
          if (msg.toLowerCase().includes('must be higher')) {
            this.submitError.set(msg);
            this.submitting.set(false);
            // expected validation error — no console logging needed
            return;
          }
        }

        this.submitError.set('Failed to submit score. Please try again.');
        this.submitting.set(false);
        console.error('Failed to submit score', err);
      }
    });
  }

  goBack() {
    this.router.navigate(['/games']);
  }

  goToModerators() {
    const gameId = this.game()?.id;
    if (gameId) {
      this.router.navigate(['/games', gameId, 'moderators']);
    }
  }

  goToPendingScores() {
    const gameId = this.game()?.id;
    if (gameId) {
      this.router.navigate(['/games', gameId, 'pending-scores']);
    }
  }

  goToApiKeys() {
    const gameId = this.game()?.id;
    if (gameId) {
      this.router.navigate(['/games', gameId, 'api-keys']);
    }
  }

  viewScorePost(userId: number) {
    const gameId = this.game()?.id;
    if (!gameId) return;

    this.scoreService.lookupScore(gameId, userId).subscribe({
      next: (result) => {
        this.router.navigate(['/scores', result.scoreId]);
      },
      error: (err) => {
        console.error('Could not find score post', err);
      }
    });
  }
}
