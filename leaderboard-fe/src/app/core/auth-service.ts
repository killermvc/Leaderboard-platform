import { effect, Injectable, signal } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, of, BehaviorSubject } from 'rxjs';
import { catchError, map, tap } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import { ClerkService } from 'ngx-clerk';

@Injectable({
	providedIn: 'root',
})
export class AuthService {
	private readonly apiUrl = environment.apiUrl;
	private readonly tokenKey = 'lb_token';
	private currentUserId: number | null = null;
	private token: string | null = null;
	private readonly roles = signal<string[]>([]);

	/** Holds the current username when available */
	public username$ = new BehaviorSubject<string | null>(null);

	constructor(private http: HttpClient, private clerk: ClerkService) {
		effect(() => {
			const isLoaded = this.clerk.isLoaded();
			const isSignedIn = this.clerk.isSignedIn();
			void this.refreshToken();
			if (isLoaded && isSignedIn) {
				this.getCurrentUser().subscribe();
			}
		});
	}

	private async refreshToken(): Promise<void> {
		const token = await this.clerk.getToken();
		if (this.clerk.isSignedIn()) {
			this.token = token;
			if (token) localStorage.setItem(this.tokenKey, token);
		} else {
			this.token = null;
			localStorage.removeItem(this.tokenKey);
		}
	}

	/** Fetch the currently authenticated user's basic info */
	getCurrentUser(): Observable<{ id: number; username: string; roles: string[] } | null> {
		return this.http.get<{ id: number; username: string; roles: string[] }>(`${this.apiUrl}/me`).pipe(
			tap((u) => {
				this.currentUserId = u?.id ?? null;
				this.roles.set(u?.roles ?? []);
				if (u && u.username) {
					this.username$.next(u.username);
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
		void this.clerk.signOut();
		this.currentUserId = null;
		this.roles.set([]);
		this.username$.next(null);
		this.token = null;
		localStorage.removeItem(this.tokenKey);
	}

	changePassword(oldPassword: string, newPassword: string): Observable<boolean> {
		const body = { OldPassword: oldPassword, NewPassword: newPassword };
		return this.http.put<any>(`${this.apiUrl}`, body).pipe(
			map(() => true),
			catchError(() => of(false))
		);
	}

	updateUsername(newUserName: string): Observable<boolean> {
		const body = { NewUserName: newUserName };
		return this.http.put<any>(`${this.apiUrl}/username`, body).pipe(
			map((res) => {
				if (res) {
					// Optionally, you might want to update the token or re-login
					return true;
				}
				return false;
			}),
			catchError(() => of(false))
		);
	}

	getToken(): string | null {
		return this.token ?? localStorage.getItem(this.tokenKey);
	}

	isAuthenticated(): boolean {
		return this.clerk.isLoaded() && this.clerk.isSignedIn();
	}

	getAuthHeaders(): { headers: HttpHeaders } | {} {
		const token = this.getToken();
		if (!token) return {};
		return { headers: new HttpHeaders({ Authorization: `Bearer ${token}` }) };
	}

	getUserIdFromToken(): string | null {
		if (this.currentUserId !== null) return this.currentUserId.toString();
		const token = this.getToken();
		const payload = token && this.decodeTokenPayload(token);
		if (!payload) return null;
		// In backend the Name claim holds user.Id as string
		return payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'] || payload['name'] || payload['unique_name'] || null;
	}

	hasRole(role: string): boolean {
		return this.roles().includes(role);
	}

	hasAnyRole(rolesToCheck: string[]): boolean {
		return rolesToCheck.some(role => this.roles().includes(role));
	}

	private decodeTokenPayload(token: string): any | null {
		try {
			const parts = token.split('.');
			if (parts.length < 2) return null;
			const payload = parts[1];
			// Add padding if necessary
			const padded = payload.replace(/-/g, '+').replace(/_/g, '/');
			const pad = padded.length % 4;
			const base64 = pad === 0 ? padded : padded + '='.repeat(4 - pad);
			const json = atob(base64);
			return JSON.parse(json);
		} catch {
			return null;
		}
	}
}
