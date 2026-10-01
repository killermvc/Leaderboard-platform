import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { FormsModule } from '@angular/forms';
import { GameService, Game } from '../core/game-service';
import { AuthService } from '../core/auth-service';
import { ModerationService, GameModeratorInfo } from '../core/moderation-service';
import { UserService, User } from '../core/user-service';

@Component({
  selector: 'app-game-configure',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, RouterLink],
  templateUrl: './game-configure.html',
  styleUrl: './game-configure.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GameConfigure implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly formBuilder = inject(FormBuilder);
  private readonly gameService = inject(GameService);
  private readonly authService = inject(AuthService);
  private readonly moderationService = inject(ModerationService);
  private readonly userService = inject(UserService);

  game = signal<Game | null>(null);
  moderators = signal<GameModeratorInfo[]>([]);
  searchResults = signal<User[]>([]);
  loading = signal(true);
  saving = signal(false);
  searching = signal(false);
  addingUserId = signal<number | null>(null);
  removingUserId = signal<number | null>(null);
  error = signal<string | null>(null);
  success = signal<string | null>(null);
  searchQuery = signal('');
  ownerSearch = signal('');
  ownerResults = signal<User[]>([]);
  selectedOwner = signal<User | null>(null);
  searchingOwners = signal(false);

  isAdmin = computed(() => this.authService.hasRole('Admin'));

  gameForm = this.formBuilder.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(100)]],
    description: ['', Validators.maxLength(1000)],
    imageUrl: ['', Validators.maxLength(500)],
    isSubmitAllowed: [true],
  });

  ngOnInit(): void {
    const gameId = Number(this.route.snapshot.paramMap.get('id'));
    if (!Number.isInteger(gameId) || gameId <= 0) {
      this.error.set('Invalid game ID.');
      this.loading.set(false);
      return;
    }

    this.gameService.getGameById(gameId).subscribe({
      next: (game) => {
        if (!this.canManage(game)) {
          this.router.navigate(['/games', game.id]);
          return;
        }

        this.game.set(game);
        this.setSelectedOwnerFromGame(game);
        this.gameForm.setValue({
          name: game.name,
          description: game.description ?? '',
          imageUrl: game.imageUrl ?? '',
          isSubmitAllowed: game.isSubmitAllowed,
        });
        this.loading.set(false);
        this.loadModerators(game.id);
      },
      error: () => {
        this.error.set('Unable to load this game.');
        this.loading.set(false);
      },
    });
  }

  saveGame(): void {
    if (this.gameForm.invalid || this.saving() || !this.game()) {
      this.gameForm.markAllAsTouched();
      return;
    }

    const values = this.gameForm.getRawValue();
    this.saving.set(true);
    this.error.set(null);
    this.success.set(null);

    this.gameService.updateGame(this.game()!.id, {
      name: values.name.trim(),
      description: values.description.trim(),
      imageUrl: values.imageUrl.trim(),
      isSubmitAllowed: values.isSubmitAllowed,
      ownerId: this.isAdmin() ? this.selectedOwner()?.id ?? null : this.game()!.ownerId,
    }).subscribe({
      next: (game) => {
        this.game.set(game);
        this.setSelectedOwnerFromGame(game);
        this.saving.set(false);
        this.success.set('Game settings saved.');
      },
      error: () => {
        this.saving.set(false);
        this.error.set('Unable to save game settings.');
      },
    });
  }

  searchUsers(): void {
    const query = this.searchQuery().trim();
    if (!query) {
      this.searchResults.set([]);
      return;
    }

    this.searching.set(true);
    const moderatorIds = this.moderators().map((moderator) => moderator.userId);
    this.userService.searchUsers(query, 10).subscribe({
      next: (users) => {
        this.searchResults.set(users.filter((user) => !moderatorIds.includes(user.id)));
        this.searching.set(false);
      },
      error: () => {
        this.searchResults.set([]);
        this.searching.set(false);
      },
    });
  }

  searchOwners(): void {
    const query = this.ownerSearch().trim();
    if (!query) {
      this.ownerResults.set([]);
      return;
    }

    this.searchingOwners.set(true);
    this.userService.searchUsers(query, 10).subscribe({
      next: (users) => {
        this.ownerResults.set(users.filter((user) => user.id !== this.selectedOwner()?.id));
        this.searchingOwners.set(false);
      },
      error: () => {
        this.ownerResults.set([]);
        this.searchingOwners.set(false);
      },
    });
  }

  chooseOwner(user: User): void {
    this.selectedOwner.set(user);
    this.ownerSearch.set('');
    this.ownerResults.set([]);
  }

  clearOwner(): void {
    this.selectedOwner.set(null);
  }

  addModerator(userId: number): void {
    const gameId = this.game()?.id;
    if (!gameId) return;

    this.addingUserId.set(userId);
    this.moderationService.addGameModerator(gameId, userId).subscribe({
      next: () => {
        this.addingUserId.set(null);
        this.searchQuery.set('');
        this.searchResults.set([]);
        this.loadModerators(gameId);
      },
      error: () => {
        this.addingUserId.set(null);
        this.error.set('Unable to add that moderator.');
      },
    });
  }

  removeModerator(userId: number): void {
    const gameId = this.game()?.id;
    if (!gameId || !confirm('Remove this moderator from the game?')) return;

    this.removingUserId.set(userId);
    this.moderationService.removeGameModerator(gameId, userId).subscribe({
      next: () => {
        this.removingUserId.set(null);
        this.loadModerators(gameId);
      },
      error: () => {
        this.removingUserId.set(null);
        this.error.set('Unable to remove that moderator.');
      },
    });
  }

  goBack(): void {
    const gameId = this.game()?.id;
    this.router.navigate(gameId ? ['/games', gameId] : ['/games']);
  }

  formatDate(date: string): string {
    return new Date(date).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
    });
  }

  private canManage(game: Game): boolean {
    const userId = Number(this.authService.getUserIdFromToken());
    return this.isAdmin() || (!!game.ownerId && game.ownerId === userId);
  }

  private setSelectedOwnerFromGame(game: Game): void {
    if (!game.ownerId) {
      this.selectedOwner.set(null);
      return;
    }

    if (game.ownerUsername) {
      this.selectedOwner.set({ id: game.ownerId, username: game.ownerUsername });
      return;
    }

    // Fallback for older API responses without ownerUsername: resolve via profile.
    this.selectedOwner.set({ id: game.ownerId, username: `User #${game.ownerId}` });
    this.userService.getUserProfile(game.ownerId).subscribe({
      next: (profile) => {
        if (this.game()?.ownerId === profile.id) {
          this.selectedOwner.set({ id: profile.id, username: profile.username });
        }
      },
      error: () => undefined,
    });
  }

  private loadModerators(gameId: number): void {
    this.moderationService.getGameModerators(gameId).subscribe({
      next: (moderators) => this.moderators.set(moderators),
      error: () => this.error.set('Unable to load moderators.'),
    });
  }
}