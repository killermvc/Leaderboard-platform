import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { GameService } from '../core/game-service';

@Component({
  selector: 'app-new-game',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './new-game.html',
  styleUrl: './new-game.scss',
})
export class NewGame {
  private readonly formBuilder = inject(FormBuilder);
  private readonly gameService = inject(GameService);
  private readonly router = inject(Router);

  submitting = signal(false);
  error = signal<string | null>(null);

  gameForm = this.formBuilder.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(100)]],
    description: ['', Validators.maxLength(1000)],
    imageUrl: ['', Validators.maxLength(500)],
  });

  submit() {
    if (this.gameForm.invalid || this.submitting()) {
      this.gameForm.markAllAsTouched();
      return;
    }

    const { name, description, imageUrl } = this.gameForm.getRawValue();
    this.submitting.set(true);
    this.error.set(null);

    this.gameService.createGame(name.trim(), description.trim(), imageUrl.trim()).subscribe({
      next: (game) => this.router.navigate(['/games', game.id]),
      error: () => {
        this.submitting.set(false);
        this.error.set('Unable to create the game. Please try again.');
      },
    });
  }

  cancel() {
    this.router.navigate(['/games']);
  }
}
