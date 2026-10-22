import { Component, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { ScoreService, ScoreRecord } from '../core/score-service';
import { AuthService } from '../core/auth-service';

@Component({
  selector: 'app-home-page',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './home-page.html',
  styleUrl: './home-page.scss',
})
export class HomePage implements OnInit {

  recentScores: ScoreRecord[] = [];
  myScores: ScoreRecord[] = [];
  loadingRecent = true;
  loadingMine = true;
  recentError = '';
  myError = '';

  anonymousPlayer = signal('Anonymous player');
  gameLabel = signal('Game:');
  recordedOnLabel = signal('Recorded on');

  constructor(private scoreService: ScoreService, public authService: AuthService, private router: Router) {}

  ngOnInit(): void {
    this.fetchRecentScores();
    this.fetchMyScores();
  }

  viewScorePost(scoreId: number): void {
    this.router.navigate(['/scores', scoreId]);
  }

  private fetchRecentScores(): void {
    this.loadingRecent = true;
    this.recentError = '';
    this.scoreService.getRecentScores(6).subscribe({
      next: (scores) => {
        this.recentScores = scores;
        this.loadingRecent = false;
      },
      error: () => {
        this.recentError = 'Failed to load recent scores.';
        this.loadingRecent = false;
      }
    });
  }

  private fetchMyScores(): void {
    this.loadingMine = true;
    this.myError = '';
    const userId = this.authService.currentUserIdSig();

    if (!userId) {
      this.loadingMine = false;
      this.myScores = [];
      return;
    }

    this.scoreService.getScoresByUser(Number(userId), 6).subscribe({
      next: (scores) => {
        this.myScores = scores;
        this.loadingMine = false;
      },
      error: () => {
        this.myError = 'Failed to load your scores.';
        this.loadingMine = false;
      }
    });
  }

}
