import { effect, Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of, BehaviorSubject } from 'rxjs';
import { catchError, map, tap } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import { ClerkService } from 'ngx-clerk';

@Injectable({
	providedIn: 'root',
})
export class AuthService {
	private readonly apiUrl = environment.apiUrl;
	private readonly roles = signal<string[]>([]);
	readonly currentUserIdSig = signal<number | null>(null);

	/** Holds the current username when available */
	public username$ = new BehaviorSubject<string | null>(null);

	constructor(private http: HttpClient, private clerk: ClerkService) {
		effect(() => {
			const isLoaded = this.clerk.isLoaded();
			const isSignedIn = this.clerk.isSignedIn();
			if (isLoaded && isSignedIn) {
				this.getCurrentUser().subscribe();
			} else if (isLoaded && !isSignedIn) {
				this.clearState();
			}
		});
	}

	/** Fetch the currently authenticated user's basic info */
	getCurrentUser(): Observable<{ id: number; clerkUserId: string | null; roles: string[] } | null> {
		return this.http.get<{ id: number; clerkUserId: string | null; roles: string[] }>(`${this.apiUrl}/me`).pipe(
			tap((u) => {
				this.currentUserIdSig.set(u?.id ?? null);
				this.roles.set(u?.roles ?? []);
				const clerkUser = this.clerk.user();
				const displayName = clerkUser?.username ?? clerkUser?.fullName ?? clerkUser?.primaryEmailAddress?.emailAddress;
				if (displayName) {
					this.username$.next(displayName);
				}
			}),
			map((u) => u),
			catchError(() => of(null))
		);
	}

	getCachedUsername(): string | null {
		return this.username$.value;
	}

	logout(): void {
		void this.clerk.signOut({ redirectUrl: '/' });
		this.clearState();
	}

	isAuthenticated(): boolean {
		return this.clerk.isLoaded() && this.clerk.isSignedIn();
	}

	hasRole(role: string): boolean {
		return this.roles().includes(role);
	}

	hasAnyRole(rolesToCheck: string[]): boolean {
		return rolesToCheck.some(role => this.roles().includes(role));
	}

	private clearState(): void {
		this.currentUserIdSig.set(null);
		this.roles.set([]);
		this.username$.next(null);
	}
}
