import { inject } from '@angular/core';
import { TranslationService } from './translation.service';

export function initializeLanguage(): () => Promise<void> {
  return () => {
    const translationService = inject(TranslationService);

    // Check localStorage for saved preference
    const saved = localStorage.getItem('preferredLanguage') as 'en' | 'es' | null;

    // Detect browser language
    const browserLang = navigator.language.startsWith('es') ? 'es' : 'en';

    // Use saved preference or browser language
    const lang = saved ?? browserLang;

    translationService.setLanguage(lang);

    return Promise.resolve();
  };
}