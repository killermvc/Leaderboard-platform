import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { ScoreService, ScoreRecord, ScoreStatus } from '../core/score-service';
import { AuthService } from '../core/auth-service';
import { TranslationService } from '../core/translation.service';

@Component({
  selector: 'app-my-submissions',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './my-submissions.html',
  styleUrl: './my-submissions.scss',
})
export class MySubmissionsComponent implements OnInit {
  private scoreService = inject(ScoreService);
  private authService = inject(AuthService);
  private router = inject(Router);
  private translationService = inject(TranslationService);

  submissions = signal<ScoreRecord[]>([]);
  loading = signal(true);
  error = signal<string | null>(null);

  isAuthenticated = computed(() => this.authService.isAuthenticated());

  // Computed counts for stats
  pendingCount = computed(() => this.submissions().filter(s => s.status === ScoreStatus.Pending).length);
  approvedCount = computed(() => this.submissions().filter(s => s.status === ScoreStatus.Approved).length);
  rejectedCount = computed(() => this.submissions().filter(s => s.status === ScoreStatus.Rejected).length);

  // Expose enum for template
  ScoreStatus = ScoreStatus;

  rejectionReasonLabel = signal('Reason:');
  noReasonLabel = signal('No reason provided.');
  rejectedOnLabel = signal('Rejected on');
  approvedOnLabel = signal('Approved on');

  ngOnInit() {
    if (!this.isAuthenticated()) {
      this.router.navigate(['/auth/login']);
      return;
    }
    this.loadSubmissions();
  }

  loadSubmissions() {
    this.loading.set(true);
    this.error.set(null);

    this.scoreService.getMySubmissions(50, 0).subscribe({
      next: (scores) => {
        this.submissions.set(scores);
        this.loading.set(false);
      },
      error: (err) => {
        console.error('Failed to load submissions', err);
        this.error.set('Failed to load your submissions.');
        this.loading.set(false);
      }
    });
  }

  getStatusLabel(status: ScoreStatus): string {
    switch (status) {
      case ScoreStatus.Pending:
        return 'Pending';
      case ScoreStatus.Approved:
        return 'Approved';
      case ScoreStatus.Rejected:
        return 'Rejected';
      default:
        return 'Unknown';
    }
  }

  getStatusClass(status: ScoreStatus): string {
    switch (status) {
      case ScoreStatus.Pending:
        return 'status-pending';
      case ScoreStatus.Approved:
        return 'status-approved';
      case ScoreStatus.Rejected:
        return 'status-rejected';
      default:
        return '';
    }
  }

  getStatusIcon(status: ScoreStatus): string {
    switch (status) {
      case ScoreStatus.Pending:
        return '⏳';
      case ScoreStatus.Approved:
        return '✅';
      case ScoreStatus.Rejected:
        return '❌';
      default:
        return '❓';
    }
  }

  formatDate(dateString: string): string {
    const date = new Date(dateString);
    const locale = this.translationService.getLanguage() === 'es' ? 'es-ES' : 'en-US';
    return date.toLocaleDateString(locale, {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  viewGame(gameId: number) {
    this.router.navigate(['/games', gameId]);
  }

  viewScorePost(scoreId: number) {
    this.router.navigate(['/scores', scoreId]);
  }

  goToLogin() {
    this.router.navigate(['/auth/login']);
  }
}
