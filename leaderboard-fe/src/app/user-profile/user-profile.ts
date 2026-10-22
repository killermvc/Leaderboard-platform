import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { UserService, UserProfile, GameRanking, UserScore } from '../core/user-service';
import { AuthService } from '../core/auth-service';
import { ClerkService } from 'ngx-clerk';
import { TranslationService } from '../core/translation.service';

@Component({
  selector: 'app-user-profile',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './user-profile.html',
  styleUrl: './user-profile.scss',
})
export class UserProfileComponent implements OnInit {
  private userService = inject(UserService);
  private route = inject(ActivatedRoute);
  public router = inject(Router);
  private clerk = inject(ClerkService);
  private auth = inject(AuthService);
  private translationService = inject(TranslationService);

  userProfile = signal<UserProfile | null>(null);
  loading = signal<boolean>(true);
  error = signal<string | null>(null);

  /** Stored avatar from the backend, falling back to live Clerk image for your own profile. Capped via .avatar-img CSS. */
  avatarUrl = computed(() => {
    const profile = this.userProfile();
    if (profile === null) return null;
    if (profile.profileImageUrl) return profile.profileImageUrl;
    const ownId = this.auth.currentUserIdSig();
    if (ownId === null || profile.id !== ownId) return null;
    return this.clerk.user()?.imageUrl ?? null;
  });

  ngOnInit() {
    this.route.params.subscribe(params => {
      const userId = parseInt(params['id'], 10);
      if (!isNaN(userId)) {
        this.loadUserProfile(userId);
      } else {
        this.error.set('Invalid user ID');
        this.loading.set(false);
      }
    });
  }

  loadUserProfile(userId: number) {
    this.loading.set(true);
    this.error.set(null);

    this.userService.getUserProfile(userId).subscribe({
      next: (profile) => {
        this.userProfile.set(profile);
        this.loading.set(false);
      },
      error: (err) => {
        console.error('Failed to load user profile', err);
        this.error.set('User not found');
        this.loading.set(false);
      }
    });
  }

  viewGame(gameId: number) {
    this.router.navigate(['/games', gameId]);
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

  formatDate(dateString: string): string {
    const date = new Date(dateString);
    const locale = this.translationService.getLanguage() === 'es' ? 'es-ES' : 'en-US';
    return date.toLocaleDateString(locale, {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    });
  }

  memberSinceLabel = signal('Member since');
  scoresSubmittedLabel = signal('Scores Submitted');
  gamesPlayedLabel = signal('Games Played');
  joinedLabel = signal('Joined');
  gameRankingsLabel = signal('Game Rankings');
  bestScoreLabel = signal('Best Score');
  submissionsLabel = signal('Submissions');
  recentActivityLabel = signal('Recent Activity');
  noScoresLabel = signal('This user hasn\'t submitted any scores yet.');

  getRankDisplay(rank: number | null): string {
    if (rank === null) return 'Unranked';
    if (rank === 1) return '🥇 1st';
    if (rank === 2) return '🥈 2nd';
    if (rank === 3) return '🥉 3rd';
    return `#${rank}`;
  }

  getRankClass(rank: number | null): string {
    if (rank === null) return 'unranked';
    if (rank === 1) return 'gold';
    if (rank === 2) return 'silver';
    if (rank === 3) return 'bronze';
    return '';
  }
}
