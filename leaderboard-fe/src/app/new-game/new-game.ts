import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { GameService } from '../core/game-service';
import { UserService, User } from '../core/user-service';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-new-game',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  templateUrl: './new-game.html',
  styleUrl: './new-game.scss',
})
export class NewGame {
  private readonly formBuilder = inject(FormBuilder);
  private readonly gameService = inject(GameService);
  private readonly userService = inject(UserService);
  private readonly router = inject(Router);

  submitting = signal(false);
  error = signal<string | null>(null);
  ownerSearch = signal('');
  ownerResults = signal<User[]>([]);
  selectedOwner = signal<User | null>(null);
  searchingOwners = signal(false);

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

    this.gameService.createGame(name.trim(), description.trim(), imageUrl.trim(), this.selectedOwner()?.id ?? null).subscribe({
      next: (game) => this.router.navigate(['/games', game.id]),
      error: () => {
        this.submitting.set(false);
        this.error.set('Unable to create the game. Please try again.');
      },
    });
  }

  searchOwners() {
    const query = this.ownerSearch().trim();
    if (!query) {
      this.ownerResults.set([]);
      return;
    }

    this.searchingOwners.set(true);
    this.userService.searchUsers(query, 10).subscribe({
      next: (users) => {
        this.ownerResults.set(users.filter((user) => user.id !== this.selectedOwner()?.id));
        this.searchingOwners.set(false);
      },
      error: () => {
        this.ownerResults.set([]);
        this.searchingOwners.set(false);
      },
    });
  }

  chooseOwner(user: User) {
    this.selectedOwner.set(user);
    this.ownerSearch.set('');
    this.ownerResults.set([]);
  }

  clearOwner() {
    this.selectedOwner.set(null);
  }

  cancel() {
    this.router.navigate(['/games']);
  }
}
