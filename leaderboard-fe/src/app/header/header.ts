import { Component, computed, inject } from '@angular/core';
import { AuthService } from '../core/auth-service';
import { Router } from '@angular/router';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { octSearch, octFeedPerson } from '@ng-icons/octicons';
import { toSignal } from '@angular/core/rxjs-interop';
import { ClerkService } from 'ngx-clerk';

@Component({
  selector: 'app-header',
  imports: [ NgIcon],
	providers: [provideIcons({ octSearch, octFeedPerson })],
  templateUrl: './header.html',
  styleUrls: ['./header.scss'],
})
export class Header {
	public authService = inject(AuthService);
	private clerk = inject(ClerkService);
	private router = inject(Router);

	username = toSignal(this.authService.username$);
	profileImageUrl = computed(() => this.clerk.user()?.imageUrl ?? null);

	constructor() {
		if (this.authService.isAuthenticated()) {
			this.authService.getCurrentUser().subscribe();
		}
	}

	onSearch(query: string) {
		if (query.trim()) {
			this.router.navigate(['/search'], { queryParams: { q: query } });
		} else {
			this.router.navigate(['/search']);
		}
	}

	onLogout() {
		this.authService.logout();
		this.router.navigate(['']);
	}
	routeToLogin() {
		this.router.navigate(['/auth/login']);
	}
	routeToRegister() {
		this.router.navigate(['/auth/register']);
	}
	routeToProfile() {
		this.router.navigate(['/auth/account'])
	}

	routeToHome() {
		this.router.navigate([''])
	}
	routeToGames() {
		this.router.navigate(['/games'])
	}
	routeToMySubmissions() {
		this.router.navigate(['/my-submissions'])
	}
	routeToModeration() {
		this.router.navigate(['/moderation/pending'])
	}
}
