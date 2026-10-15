import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ModerationService, PendingScore } from '../core/moderation-service';
import { AuthService } from '../core/auth-service';
import { GameService, Game } from '../core/game-service';

@Component({
  selector: 'app-pending-scores',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './pending-scores.html',
  styleUrl: './pending-scores.scss',
})
export class PendingScoresComponent implements OnInit {
  private moderationService = inject(ModerationService);
  private authService = inject(AuthService);
  private gameService = inject(GameService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  gameId = signal<number | null>(null);
  game = signal<Game | null>(null);
  pendingScores = signal<PendingScore[]>([]);
  loading = signal(true);
  error = signal<string | null>(null);
  processingScoreId = signal<number | null>(null);

  // For rejection modal
  showRejectModal = signal(false);
  rejectingScoreId = signal<number | null>(null);
  rejectionReason = signal('');

  isAuthenticated = computed(() => this.authService.isAuthenticated());
  isModerator = computed(() =>
    this.authService.hasRole('Moderator') || this.authService.hasRole('Admin')
  );
  currentUserId = computed(() => this.authService.currentUserIdSig());

  ngOnInit() {
    if (!this.isAuthenticated()) {
      this.router.navigate(['/auth/login']);
      return;
    }

    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (isNaN(id)) {
      this.error.set('Invalid game ID');
      this.loading.set(false);
      return;
    }

    this.gameId.set(id);
    this.loadGame(id);
    this.loadPendingScores(id);
  }

  private loadGame(gameId: number) {
    this.gameService.getGameById(gameId).subscribe({
      next: (game) => {
        this.game.set(game);
      },
      error: (err) => {
        console.error('Failed to load game', err);
      }
    });
  }

  loadPendingScores(gameId?: number) {
    const id = gameId ?? this.gameId();
    if (!id) return;

    this.loading.set(true);
    this.error.set(null);

    this.moderationService.getPendingScoresForGame(id, 50, 0).subscribe({
      next: (scores) => {
        this.pendingScores.set(scores);
        this.loading.set(false);
      },
      error: (err) => {
        console.error('Failed to load pending scores', err);
        if (err.status === 403) {
          this.error.set('You do not have permission to view pending scores for this game.');
        } else {
          this.error.set('Failed to load pending scores.');
        }
        this.loading.set(false);
      }
    });
  }

  approveScore(scoreId: number) {
    this.processingScoreId.set(scoreId);

    this.moderationService.approveScore(scoreId).subscribe({
      next: () => {
        // Remove from list
        this.pendingScores.update(scores => scores.filter(s => s.id !== scoreId));
        this.processingScoreId.set(null);
      },
      error: (err) => {
        console.error('Failed to approve score', err);
        this.processingScoreId.set(null);
        alert('Failed to approve score. Please try again.');
      }
    });
  }

  openRejectModal(scoreId: number) {
    this.rejectingScoreId.set(scoreId);
    this.rejectionReason.set('');
    this.showRejectModal.set(true);
  }

  closeRejectModal() {
    this.showRejectModal.set(false);
    this.rejectingScoreId.set(null);
    this.rejectionReason.set('');
  }

  confirmReject() {
    const scoreId = this.rejectingScoreId();
    if (!scoreId) return;

    // Capture the reason BEFORE closing the modal (which clears it)
    const reason = this.rejectionReason() || undefined;

    this.processingScoreId.set(scoreId);
    this.closeRejectModal();

    this.moderationService.rejectScore(scoreId, reason).subscribe({
      next: () => {
        this.pendingScores.update(scores => scores.filter(s => s.id !== scoreId));
        this.processingScoreId.set(null);
      },
      error: (err) => {
        console.error('Failed to reject score', err);
        this.processingScoreId.set(null);
        alert('Failed to reject score. Please try again.');
      }
    });
  }

  formatDate(dateString: string): string {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  goBackToGame() {
    const id = this.gameId();
    if (id) {
      this.router.navigate(['/games', id]);
    } else {
      this.router.navigate(['/games']);
    }
  }

  viewUser(userId: number) {
    this.router.navigate(['/user', userId]);
  }

  viewScorePost(scoreId: number) {
    this.router.navigate(['/scores', scoreId]);
  }
}
