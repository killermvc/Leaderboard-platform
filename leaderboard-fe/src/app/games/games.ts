import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { GameService, Game } from '../core/game-service';
import { toSignal } from '@angular/core/rxjs-interop';
import { GameCard } from '../game-card/game-card';
import { AuthService } from '../core/auth-service';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { octPlus } from '@ng-icons/octicons';

@Component({
  selector: 'app-games',
  standalone: true,
  imports: [CommonModule, GameCard, NgIcon],
  providers: [provideIcons({ octPlus })],
  templateUrl: './games.html',
  styleUrl: './games.scss',
})
export class Games implements OnInit {
  private gameService = inject(GameService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  public authService = inject(AuthService);

  games = signal<Game[]>([]);
  searchQuery = signal<string>('');
  showSubmittedOnly = signal(false);

  filteredGames = computed(() => {
    const query = this.searchQuery().trim().toLowerCase();
    const allGames = this.games();
    if (!query) return allGames;
    return allGames.filter(g => g.name.toLowerCase().includes(query));
  });

  ngOnInit() {
    this.loadGames();

    this.route.queryParams.subscribe(params => {
      const q = params['q'];
      if (q) {
        this.searchQuery.set(q);
      } else {
        this.searchQuery.set('');
      }
    });
  }

  loadGames() {
    const userId = this.authService.getUserIdFromToken();
    if (this.showSubmittedOnly() && !userId) {
      this.games.set([]);
      return;
    }

    const gamesRequest = this.showSubmittedOnly()
      ? this.gameService.getGamesByPlayer(Number(userId))
      : this.gameService.getAllGames(100, 0);

    gamesRequest.subscribe({
      next: (data) => this.games.set(data),
      error: (err) => console.error('Failed to load games', err)
    });
  }

  setSearchQuery(event: Event) {
    this.searchQuery.set((event.target as HTMLInputElement).value);
  }

  toggleSubmittedOnly() {
    this.showSubmittedOnly.update(showSubmittedOnly => !showSubmittedOnly);
    this.loadGames();
  }

  viewGame(gameId: number) {
    this.router.navigate(['/games', gameId]);
  }

  routeToNewGame() {
    this.router.navigate(['/games/new']);
  }
}
