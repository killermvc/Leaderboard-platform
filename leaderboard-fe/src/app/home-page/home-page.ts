import { Component, OnInit } from '@angular/core';
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
        this.recentError = 'No se pudieron cargar los scores recientes.';
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
        this.myError = 'No se pudieron cargar tus scores.';
        this.loadingMine = false;
      }
    });
  }

}
