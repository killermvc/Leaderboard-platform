import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ScoreService, ScoreRecord, ScoreStatus } from '../core/score-service';
import { ModerationService } from '../core/moderation-service';
import { AuthService } from '../core/auth-service';

@Component({
  selector: 'app-score-post',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './score-post.html',
  styleUrl: './score-post.scss',
})
export class ScorePostComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private scoreService = inject(ScoreService);
  private moderationService = inject(ModerationService);
  private authService = inject(AuthService);

  score = signal<ScoreRecord | null>(null);
  loading = signal(true);
  error = signal<string | null>(null);
  canModerate = signal(false);
  processing = signal(false);

  // Rejection modal
  showRejectModal = signal(false);
  rejectionReason = signal('');

  isAuthenticated = computed(() => this.authService.isAuthenticated());
  currentUserId = computed(() => this.authService.currentUserIdSig());

  isPending = computed(() => this.score()?.status === ScoreStatus.Pending);
  isApproved = computed(() => this.score()?.status === ScoreStatus.Approved);
  isRejected = computed(() => this.score()?.status === ScoreStatus.Rejected);
  isOwnScore = computed(() => this.score()?.user?.id === this.currentUserId());

  ScoreStatus = ScoreStatus;

  ngOnInit() {
    const scoreId = Number(this.route.snapshot.paramMap.get('id'));
    if (isNaN(scoreId)) {
      this.error.set('Invalid score ID');
      this.loading.set(false);
      return;
    }

    this.loadScore(scoreId);
  }

  private loadScore(scoreId: number) {
    this.loading.set(true);
    this.error.set(null);

    this.scoreService.getScoreById(scoreId).subscribe({
      next: (score) => {
        this.score.set(score);
        this.loading.set(false);
        if (score.game && this.isAuthenticated()) {
          this.checkModerationRights(score.game.id);
        }
      },
      error: (err) => {
        if (err.status === 404) {
          this.error.set('Score post not found.');
        } else {
          this.error.set('Failed to load score post.');
        }
        this.loading.set(false);
        console.error('Failed to load score', err);
      }
    });
  }

  private checkModerationRights(gameId: number) {
    this.moderationService.canModerate(gameId).subscribe({
      next: (result) => this.canModerate.set(result.canModerate),
      error: () => this.canModerate.set(false),
    });
  }

  approveScore() {
    const s = this.score();
    if (!s) return;

    this.processing.set(true);
    this.moderationService.approveScore(s.id).subscribe({
      next: () => {
        this.processing.set(false);
        this.loadScore(s.id); // Reload to get updated status
      },
      error: (err) => {
        this.processing.set(false);
        console.error('Failed to approve score', err);
        alert('Failed to approve score. Please try again.');
      }
    });
  }

  openRejectModal() {
    this.rejectionReason.set('');
    this.showRejectModal.set(true);
  }

  closeRejectModal() {
    this.showRejectModal.set(false);
    this.rejectionReason.set('');
  }

  confirmReject() {
    const s = this.score();
    if (!s) return;

    const reason = this.rejectionReason() || undefined;
    this.processing.set(true);
    this.closeRejectModal();

    this.moderationService.rejectScore(s.id, reason).subscribe({
      next: () => {
        this.processing.set(false);
        this.loadScore(s.id);
      },
      error: (err) => {
        this.processing.set(false);
        console.error('Failed to reject score', err);
        alert('Failed to reject score. Please try again.');
      }
    });
  }

  getStatusLabel(status: ScoreStatus): string {
    switch (status) {
      case ScoreStatus.Pending: return 'Pending Review';
      case ScoreStatus.Approved: return 'Approved';
      case ScoreStatus.Rejected: return 'Rejected';
      default: return 'Unknown';
    }
  }

  getStatusIcon(status: ScoreStatus): string {
    switch (status) {
      case ScoreStatus.Pending: return '⏳';
      case ScoreStatus.Approved: return '✅';
      case ScoreStatus.Rejected: return '❌';
      default: return '❓';
    }
  }

  getStatusClass(status: ScoreStatus): string {
    switch (status) {
      case ScoreStatus.Pending: return 'status-pending';
      case ScoreStatus.Approved: return 'status-approved';
      case ScoreStatus.Rejected: return 'status-rejected';
      default: return '';
    }
  }

  formatDate(dateString: string): string {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  goBack() {
    window.history.back();
  }

  goToGame() {
    const gameId = this.score()?.game?.id;
    if (gameId) {
      this.router.navigate(['/games', gameId]);
    }
  }

  goToUser() {
    const userId = this.score()?.user?.id;
    if (userId) {
      this.router.navigate(['/user', userId]);
    }
  }
}
