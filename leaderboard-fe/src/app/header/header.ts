import { Component, computed, inject, signal } from '@angular/core';
import { AuthService } from '../core/auth-service';
import { Router } from '@angular/router';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { octSearch, octFeedPerson } from '@ng-icons/octicons';
import { toSignal } from '@angular/core/rxjs-interop';
import { ClerkService } from 'ngx-clerk';
import { TranslationService } from '../core/translation.service';
import { DropdownElement, DropdownMenu } from '../dropdown-menu/dropdown-menu';

@Component({
  selector: 'app-header',
  imports: [NgIcon, DropdownMenu],
  providers: [provideIcons({ octSearch, octFeedPerson })],
  templateUrl: './header.html',
  styleUrls: ['./header.scss'],
})
export class Header {
  public authService = inject(AuthService);
  private clerk = inject(ClerkService);
  private router = inject(Router);
  private translationService = inject(TranslationService);

  username = toSignal(this.authService.username$);
  profileImageUrl = computed(() => this.clerk.user()?.imageUrl ?? null);
  accountFallback = signal('Account');

  // Language selector
  availableLanguages = this.translationService.getAvailableLanguages();
  availableLanguageOptions = computed<DropdownElement[]>(() =>
    this.availableLanguages.map(language => ({
      value: language.code,
      label: language.nativeName,
      icon: language.flag,
    }))
  );
  currentLanguage = computed(() => this.translationService.getLanguage());

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

  onLanguageSelect(language: string): void {
    const selectedLanguage = this.availableLanguages.find(({ code }) => code === language)?.code;
    if (!selectedLanguage) {
      throw new Error(`Unsupported language selected: ${language}`);
    }

    this.translationService.setLanguage(selectedLanguage);
    // Reload page to apply new locale build
    window.location.reload();
  }
}