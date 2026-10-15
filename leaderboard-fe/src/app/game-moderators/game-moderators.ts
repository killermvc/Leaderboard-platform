import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ModerationService, GameModeratorInfo } from '../core/moderation-service';
import { UserService, User } from '../core/user-service';
import { GameService, Game } from '../core/game-service';
import { AuthService } from '../core/auth-service';

@Component({
  selector: 'app-game-moderators',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './game-moderators.html',
  styleUrl: './game-moderators.scss',
})
export class GameModeratorsComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private moderationService = inject(ModerationService);
  private userService = inject(UserService);
  private gameService = inject(GameService);
  private authService = inject(AuthService);

  game = signal<Game | null>(null);
  moderators = signal<GameModeratorInfo[]>([]);
  searchResults = signal<User[]>([]);
  loading = signal(true);
  error = signal<string | null>(null);

  // Add moderator form
  searchQuery = signal('');
  searching = signal(false);
  addingUserId = signal<number | null>(null);
  removingUserId = signal<number | null>(null);

  isAdmin = computed(() => this.authService.hasRole('Admin'));

  ngOnInit() {
    if (!this.isAdmin()) {
      this.router.navigate(['/']);
      return;
    }

    const gameId = Number(this.route.snapshot.paramMap.get('id'));
    if (isNaN(gameId)) {
      this.error.set('Invalid game ID');
      this.loading.set(false);
      return;
    }

    this.loadGame(gameId);
    this.loadModerators(gameId);
  }

  loadGame(gameId: number) {
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

  loadModerators(gameId: number) {
    this.loading.set(true);

    this.moderationService.getGameModerators(gameId).subscribe({
      next: (moderators) => {
        this.moderators.set(moderators);
        this.loading.set(false);
      },
      error: (err) => {
        console.error('Failed to load moderators', err);
        this.error.set('Failed to load moderators.');
        this.loading.set(false);
      }
    });
  }

  searchUsers() {
    const query = this.searchQuery();
    if (!query.trim()) {
      this.searchResults.set([]);
      return;
    }

    this.searching.set(true);

    this.userService.searchUsers(query, 10).subscribe({
      next: (users) => {
        // Filter out users who are already moderators
        const modUserIds = this.moderators().map(m => m.userId);
        const filtered = users.filter(u => !modUserIds.includes(u.id));
        this.searchResults.set(filtered);
        this.searching.set(false);
      },
      error: (err) => {
        console.error('Failed to search users', err);
        this.searching.set(false);
      }
    });
  }

  addModerator(userId: number) {
    const gameId = this.game()?.id;
    if (!gameId) return;

    this.addingUserId.set(userId);

    this.moderationService.addGameModerator(gameId, userId).subscribe({
      next: () => {
        this.loadModerators(gameId);
        this.searchQuery.set('');
        this.searchResults.set([]);
        this.addingUserId.set(null);
      },
      error: (err) => {
        console.error('Failed to add moderator', err);
        this.addingUserId.set(null);
        alert('Failed to add moderator. Please try again.');
      }
    });
  }

  removeModerator(userId: number) {
    const gameId = this.game()?.id;
    if (!gameId) return;

    if (!confirm('Are you sure you want to remove this moderator?')) {
      return;
    }

    this.removingUserId.set(userId);

    this.moderationService.removeGameModerator(gameId, userId).subscribe({
      next: () => {
        this.loadModerators(gameId);
        this.removingUserId.set(null);
      },
      error: (err) => {
        console.error('Failed to remove moderator', err);
        this.removingUserId.set(null);
        alert('Failed to remove moderator. Please try again.');
      }
    });
  }

  formatDate(dateString: string): string {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
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
