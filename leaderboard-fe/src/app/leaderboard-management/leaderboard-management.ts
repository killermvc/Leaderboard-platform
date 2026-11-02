import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { GameService, Game } from '../core/game-service';
import { AuthService } from '../core/auth-service';
import { Leaderboard, LeaderboardService } from '../core/leaderboard-service';
import { ModerationService } from '../core/moderation-service';

@Component({
  selector: 'app-leaderboard-management',
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './leaderboard-management.html',
  styleUrl: './leaderboard-management.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LeaderboardManagementComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly formBuilder = inject(FormBuilder);
  private readonly gameService = inject(GameService);
  private readonly authService = inject(AuthService);
  private readonly moderationService = inject(ModerationService);
  private readonly leaderboardService = inject(LeaderboardService);

  readonly game = signal<Game | null>(null);
  readonly leaderboards = signal<Leaderboard[]>([]);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly success = signal<string | null>(null);
  readonly editingId = signal<number | null>(null);
  readonly canManage = signal(false);
  readonly isAdmin = computed(() => this.authService.hasRole('Admin'));

  readonly leaderboardForm = this.formBuilder.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(100)]],
    description: ['', Validators.maxLength(1000)],
    isActive: [true],
  });

  ngOnInit(): void {
    const gameId = Number(this.route.snapshot.paramMap.get('id'));
    if (!Number.isInteger(gameId) || gameId <= 0) {
      this.error.set('Invalid game ID.');
      this.loading.set(false);
      return;
    }

    this.gameService.getGameById(gameId).subscribe({
      next: game => {
        this.game.set(game);
        this.checkPermission(gameId);
        this.loadLeaderboards(gameId);
      },
      error: () => {
        this.error.set('Unable to load this game.');
        this.loading.set(false);
      },
    });
  }

  save(): void {
    const gameId = this.game()?.id;
    if (!gameId || this.leaderboardForm.invalid || this.saving()) {
      this.leaderboardForm.markAllAsTouched();
      return;
    }

    const values = this.leaderboardForm.getRawValue();
    this.saving.set(true);
    this.error.set(null);
    this.success.set(null);
    const request = {
      name: values.name.trim(),
      description: values.description.trim() || null,
      isActive: values.isActive,
    };
    const editingId = this.editingId();
    const operation = editingId === null
      ? this.leaderboardService.create(gameId, request)
      : this.leaderboardService.update(gameId, editingId, request);

    operation.subscribe({
      next: () => {
        this.saving.set(false);
        this.success.set(editingId === null ? 'Leaderboard created.' : 'Leaderboard updated.');
        this.cancelEdit();
        this.loadLeaderboards(gameId);
      },
      error: err => {
        this.saving.set(false);
        this.error.set(this.getErrorMessage(err));
      },
    });
  }

  edit(board: Leaderboard): void {
    this.editingId.set(board.id);
    this.leaderboardForm.setValue({
      name: board.name,
      description: board.description ?? '',
      isActive: board.isActive,
    });
    this.error.set(null);
    this.success.set(null);
  }

  cancelEdit(): void {
    this.editingId.set(null);
    this.leaderboardForm.reset({ name: '', description: '', isActive: true });
  }

  delete(board: Leaderboard): void {
    const gameId = this.game()?.id;
    if (!gameId || !this.isAdmin() || !confirm(`Delete "${board.name}"?`)) {
      return;
    }

    this.leaderboardService.delete(gameId, board.id).subscribe({
      next: () => this.loadLeaderboards(gameId),
      error: err => this.error.set(this.getErrorMessage(err)),
    });
  }

  goBack(): void {
    const gameId = this.game()?.id;
    void this.router.navigate(gameId ? ['/games', gameId] : ['/games']);
  }

  private checkPermission(gameId: number): void {
    if (this.isAdmin()) {
      this.canManage.set(true);
      return;
    }

    this.moderationService.canModerate(gameId).subscribe({
      next: result => this.canManage.set(result.canModerate),
      error: () => this.canManage.set(false),
    });
  }

  private loadLeaderboards(gameId: number): void {
    this.leaderboardService.getByGame(gameId).subscribe({
      next: boards => {
        this.leaderboards.set(boards);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Unable to load leaderboards.');
        this.loading.set(false);
      },
    });
  }

  private getErrorMessage(error: { error?: { message?: string; Message?: string } | string }): string {
    if (typeof error.error === 'string') {
      return error.error;
    }
    return error.error?.message ?? error.error?.Message ?? 'Unable to save leaderboard changes.';
  }
}
