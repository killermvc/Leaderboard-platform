import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { GameService, Game } from '../core/game-service';
import { UserService, User } from '../core/user-service';
import { AuthService } from '../core/auth-service';
import { ClerkService } from 'ngx-clerk';
import { GameCard } from '../game-card/game-card';

@Component({
  selector: 'app-search-results',
  standalone: true,
  imports: [CommonModule, GameCard],
  templateUrl: './search-results.html',
  styleUrl: './search-results.scss',
})
export class SearchResults implements OnInit {
  private gameService = inject(GameService);
  private userService = inject(UserService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private clerk = inject(ClerkService);
  private auth = inject(AuthService);

  games = signal<Game[]>([]);
  users = signal<User[]>([]);
  searchQuery = signal<string>('');
  loading = signal<boolean>(false);

  ownId = this.auth.currentUserIdSig;
  ownAvatarUrl = computed(() => this.clerk.user()?.imageUrl ?? null);

  filteredGames = computed(() => {
    const query = this.searchQuery().toLowerCase();
    const allGames = this.games();
    if (!query) return allGames;
    return allGames.filter(g => g.name.toLowerCase().includes(query));
  });

  filteredUsers = computed(() => {
    const query = this.searchQuery().toLowerCase();
    const allUsers = this.users();
    if (!query) return allUsers;
    return allUsers.filter(u => u.username.toLowerCase().includes(query));
  });

  hasResults = computed(() => {
    return this.filteredGames().length > 0 || this.filteredUsers().length > 0;
  });

  ngOnInit() {
    this.route.queryParams.subscribe(params => {
      const q = params['q'];
      if (q) {
        this.searchQuery.set(q);
        this.performSearch(q);
      } else {
        this.searchQuery.set('');
        this.games.set([]);
        this.users.set([]);
      }
    });
  }

  performSearch(query: string) {
    this.loading.set(true);

    // Search both games and users in parallel
    this.gameService.getAllGames(100, 0).subscribe({
      next: (data) => {
        this.games.set(data);
        this.checkLoadingComplete();
      },
      error: (err) => {
        console.error('Failed to load games', err);
        this.checkLoadingComplete();
      }
    });

    this.userService.searchUsers(query, 20).subscribe({
      next: (data) => {
        this.users.set(data);
        this.checkLoadingComplete();
      },
      error: (err) => {
        console.error('Failed to search users', err);
        this.checkLoadingComplete();
      }
    });
  }

  private loadingChecks = 0;
  private checkLoadingComplete() {
    this.loadingChecks++;
    if (this.loadingChecks >= 2) {
      this.loading.set(false);
      this.loadingChecks = 0;
    }
  }

  viewGame(gameId: number) {
    this.router.navigate(['/games', gameId]);
  }

  viewUser(userId: number) {
    this.router.navigate(['/user', userId]);
  }

  getTimeAgo(dateString: string): string {
    const date = new Date(dateString);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24));

    if (diffDays < 1) return 'today';
    if (diffDays === 1) return 'yesterday';
    if (diffDays < 7) return `${diffDays} days ago`;
    if (diffDays < 30) {
      const weeks = Math.floor(diffDays / 7);
      return weeks === 1 ? '1 week ago' : `${weeks} weeks ago`;
    }
    if (diffDays < 365) {
      const months = Math.floor(diffDays / 30);
      return months === 1 ? '1 month ago' : `${months} months ago`;
    }
    const years = Math.floor(diffDays / 365);
    return years === 1 ? '1 year ago' : `${years} years ago`;
  }
}
