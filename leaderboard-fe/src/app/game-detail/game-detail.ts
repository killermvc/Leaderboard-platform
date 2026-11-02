import { Component, ChangeDetectionStrategy, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { GameService, Game } from '../core/game-service';
import { ScoreService, LeaderboardEntry } from '../core/score-service';
import { Leaderboard, LeaderboardService } from '../core/leaderboard-service';
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
  private leaderboardService = inject(LeaderboardService);
  private authService = inject(AuthService);
  private moderationService = inject(ModerationService);
  private apiKeyService = inject(ApiKeyService);
  private fb = inject(FormBuilder);

  game = signal<Game | null>(null);
  leaderboard = signal<LeaderboardEntry[]>([]);
  leaderboards = signal<Leaderboard[]>([]);
  selectedLeaderboardId = signal<number | null>(null);
  loading = signal(true);
  loadingLeaderboard = signal(true);
  error = signal<string | null>(null);
  submitSuccess = signal<string | null>(null);
  submitError = signal<string | null>(null);
  submitting = signal(false);
  canModerate = signal(false);
  canManageKeys = signal(false);
  selectedLeaderboard = computed(() =>
    this.leaderboards().find(board => board.id === this.selectedLeaderboardId()) ?? null
  );
  activeLeaderboards = computed(() => this.leaderboards().filter(board => board.isActive));

  isLoggedIn = computed(() => this.authService.isAuthenticated());
  isAdmin = computed(() => this.authService.hasRole('Admin'));
  canConfigure = computed(() => {
    const currentGame = this.game();
    const currentUserId = this.authService.currentUserIdSig();
    return this.isAdmin() || (!!currentGame?.ownerId && currentGame.ownerId === currentUserId);
  });

  topLabel = signal('Top');
  unknownPlayer = signal('Unknown Player');
  titlePlaceholder = signal('e.g. Game Name - Score');

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

    this.route.queryParamMap.subscribe(params => {
      const requestedId = Number(params.get('leaderboardId'));
      if (Number.isInteger(requestedId) && requestedId > 0) {
        this.selectedLeaderboardId.set(requestedId);
        if (this.leaderboards().some(board => board.id === requestedId)) {
          this.selectLeaderboard(requestedId, false);
        }
      }
    });

    this.loadGame(gameId);
    this.loadLeaderboards(gameId);
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

  private loadLeaderboards(gameId: number) {
    this.leaderboardService.getByGame(gameId).subscribe({
      next: boards => {
        this.leaderboards.set(boards);
        const requestedId = this.selectedLeaderboardId();
        const selected = boards.find(board => board.id === requestedId);
        const initial = selected ?? boards.find(board => board.isActive) ?? boards[0];
        if (initial) {
          this.selectLeaderboard(initial.id, false);
        } else {
          this.loadingLeaderboard.set(false);
        }
      },
      error: err => {
        console.error('Failed to load leaderboards', err);
        this.loadingLeaderboard.set(false);
      }
    });
  }

  private loadLeaderboard(leaderboardId: number) {
    this.loadingLeaderboard.set(true);
    this.scoreService.getLeaderboard(leaderboardId, 20).subscribe({
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

  selectLeaderboard(leaderboardId: number, updateUrl = true) {
    const board = this.leaderboards().find(item => item.id === leaderboardId);
    if (!board) {
      return;
    }

    this.selectedLeaderboardId.set(leaderboardId);
    this.submitSuccess.set(null);
    this.submitError.set(null);
    this.loadLeaderboard(leaderboardId);

    if (updateUrl) {
      void this.router.navigate([], {
        relativeTo: this.route,
        queryParams: { leaderboardId },
        queryParamsHandling: 'merge'
      });
    }
  }

  onLeaderboardChange(event: Event) {
    const target = event.target;
    if (!(target instanceof HTMLSelectElement)) {
      return;
    }

    const leaderboardId = Number(target.value);
    if (Number.isInteger(leaderboardId) && leaderboardId > 0) {
      this.selectLeaderboard(leaderboardId);
    }
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
    const board = this.selectedLeaderboard();
    if (this.scoreForm.invalid || !this.game() || !board?.isActive) {
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

    this.scoreService.submitScore(gameId, board.id, scoreValue, title, description).subscribe({
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

  goToConfigure() {
    const gameId = this.game()?.id;
    if (gameId) {
      this.router.navigate(['/games', gameId, 'configure']);
    }
  }

  goToLeaderboards() {
    const gameId = this.game()?.id;
    if (gameId) {
      this.router.navigate(['/games', gameId, 'leaderboards']);
    }
  }

  viewScorePost(userId: number | null) {
    // A score sent by a game client belongs to a player name, there is no score post to open
    if (userId === null) return;

    const gameId = this.game()?.id;
    if (!gameId) return;

    const leaderboardId = this.selectedLeaderboardId();
    if (!leaderboardId) return;

    this.scoreService.lookupScore(gameId, leaderboardId, userId).subscribe({
      next: (result) => {
        this.router.navigate(['/scores', result.scoreId]);
      },
      error: (err) => {
        console.error('Could not find score post', err);
      }
    });
  }
}
