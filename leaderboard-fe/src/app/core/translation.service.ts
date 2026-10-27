import { Injectable, signal, computed } from '@angular/core';
import { DOCUMENT } from '@angular/common';
import { inject } from '@angular/core';

export interface LanguageOption {
  code: 'en' | 'es';
  name: string;
  nativeName: string;
  flag: string;
}

@Injectable({ providedIn: 'root' })
export class TranslationService {
  private document = inject(DOCUMENT);

  // Current language - defaults to 'en', will be set by language initializer
  private currentLang = signal<'en' | 'es'>('en');

  // Translation map - in a real app, this would be loaded from XLF files at runtime
  // For now, we provide a simple key-value map for dynamic strings
  private translations: Record<string, Record<'en' | 'es', string>> = {
    // Home page dynamic strings
    'home.recentScores.loadError': {
      en: 'Failed to load recent scores.',
      es: 'No se pudieron cargar los scores recientes.'
    },
    'home.myScores.loadError': {
      en: 'Failed to load your scores.',
      es: 'No se pudieron cargar tus scores.'
    },
    'home.anonymousPlayer': {
      en: 'Anonymous player',
      es: 'Jugador anónimo'
    },
    'home.gameLabel': {
      en: 'Game:',
      es: 'Juego:'
    },
    'home.recordedOnLabel': {
      en: 'Recorded on',
      es: 'Registrado el'
    },
    'home.creating': {
      en: 'Creating...',
      es: 'Creando...'
    },
    'home.createGame': {
      en: 'Create game',
      es: 'Crear juego'
    },
    'home.scoresLabel': {
      en: 'scores',
      es: 'puntuaciones'
    },
    'home.gamesLabel': {
      en: 'games',
      es: 'juegos'
    },
    'home.joinedLabel': {
      en: 'Joined',
      es: 'Se unió'
    },

    // Game detail dynamic strings
    'gameDetail.top': {
      en: 'Top',
      es: 'Top'
    },
    'gameDetail.unknownPlayer': {
      en: 'Unknown Player',
      es: 'Jugador desconocido'
    },
    'gameDetail.titlePlaceholder': {
      en: 'e.g. Game Name - Score',
      es: 'ej. Nombre del Juego - Puntuación'
    },

    // Score post dynamic strings
    'scorePost.unknownUser': {
      en: 'Unknown',
      es: 'Desconocido'
    },
    'scorePost.unknownPlayer': {
      en: 'Unknown player',
      es: 'Jugador desconocido'
    },
    'scorePost.rejectionReasonLabel': {
      en: 'Reason:',
      es: 'Motivo:'
    },

    // Pending scores dynamic strings
    'pendingScores.unknownPlayer': {
      en: 'Unknown player',
      es: 'Jugador desconocido'
    },

    // Game moderators dynamic strings
    'gameModerators.assignedDateLabel': {
      en: 'Added',
      es: 'Añadido'
    },

    // API keys dynamic strings
    'apiKeys.keyNameLabel': {
      en: 'Name',
      es: 'Nombre'
    },
    'apiKeys.createdLabel': {
      en: 'Created',
      es: 'Creado'
    },
    'apiKeys.keyIdLabel': {
      en: 'ID',
      es: 'ID'
    },
    'apiKeys.lastUsedLabel': {
      en: 'Last used',
      es: 'Último uso'
    },
    'apiKeys.expiresLabel': {
      en: 'Expires',
      es: 'Expira'
    },
    'apiKeys.revokedLabel': {
      en: 'Revoked',
      es: 'Revocado'
    },

    // New game dynamic strings
    'newGame.creating': {
      en: 'Creating...',
      es: 'Creando...'
    },
    'newGame.createGame': {
      en: 'Create game',
      es: 'Crear juego'
    },

    // Game configure dynamic strings
    'gameConfigure.openLabel': {
      en: 'Open for submissions',
      es: 'Abierto para envíos'
    },
    'gameConfigure.closedLabel': {
      en: 'Submissions closed',
      es: 'Envíos cerrados'
    },
    'gameConfigure.savingLabel': {
      en: 'Saving...',
      es: 'Guardando...'
    },
    'gameConfigure.saveLabel': {
      en: 'Save changes',
      es: 'Guardar cambios'
    },
    'gameConfigure.addingLabel': {
      en: 'Adding...',
      es: 'Añadiendo...'
    },
    'gameConfigure.addLabel': {
      en: 'Add',
      es: 'Añadir'
    },
    'gameConfigure.removingLabel': {
      en: 'Removing...',
      es: 'Eliminando...'
    },
    'gameConfigure.removeLabel': {
      en: 'Remove',
      es: 'Eliminar'
    },
    'gameConfigure.assignedLabel': {
      en: 'Added',
      es: 'Añadido'
    },

    // Search results dynamic strings
    'searchResults.scoresLabel': {
      en: 'scores',
      es: 'puntuaciones'
    },
    'searchResults.gamesLabel': {
      en: 'games',
      es: 'juegos'
    },
    'searchResults.joinedLabel': {
      en: 'Joined',
      es: 'Se unió'
    },

    // User profile dynamic strings
    'userProfile.memberSinceLabel': {
      en: 'Member since',
      es: 'Miembro desde'
    },
    'userProfile.scoresSubmittedLabel': {
      en: 'Scores Submitted',
      es: 'Puntuaciones Enviadas'
    },
    'userProfile.gamesPlayedLabel': {
      en: 'Games Played',
      es: 'Juegos Jugados'
    },
    'userProfile.joinedLabel': {
      en: 'Joined',
      es: 'Se unió'
    },
    'userProfile.gameRankingsLabel': {
      en: 'Game Rankings',
      es: 'Clasificación de Juegos'
    },
    'userProfile.bestScoreLabel': {
      en: 'Best Score',
      es: 'Mejor Puntuación'
    },
    'userProfile.submissionsLabel': {
      en: 'Submissions',
      es: 'Envíos'
    },
    'userProfile.recentActivityLabel': {
      en: 'Recent Activity',
      es: 'Actividad Reciente'
    },
    'userProfile.noScoresLabel': {
      en: "This user hasn't submitted any scores yet.",
      es: 'Este usuario no ha enviado ninguna puntuación aún.'
    },

    // My submissions dynamic strings
    'mySubmissions.rejectionReasonLabel': {
      en: 'Reason:',
      es: 'Motivo:'
    },
    'mySubmissions.noReasonLabel': {
      en: 'No reason provided.',
      es: 'Sin motivo proporcionado.'
    },
    'mySubmissions.rejectedOnLabel': {
      en: 'Rejected on',
      es: 'Rechazado el'
    },
    'mySubmissions.approvedOnLabel': {
      en: 'Approved on',
      es: 'Aprobado el'
    },

    // Common
    'common.loading': {
      en: 'Loading...',
      es: 'Cargando...'
    },
    'common.error': {
      en: 'Error',
      es: 'Error'
    },
    'common.save': {
      en: 'Save',
      es: 'Guardar'
    },
    'common.cancel': {
      en: 'Cancel',
      es: 'Cancelar'
    },
    'common.delete': {
      en: 'Delete',
      es: 'Eliminar'
    },
    'common.confirm': {
      en: 'Confirm',
      es: 'Confirmar'
    },
    'common.close': {
      en: 'Close',
      es: 'Cerrar'
    },
  };

  setLanguage(lang: 'en' | 'es'): void {
    this.currentLang.set(lang);
    // Update document lang attribute
    this.document.documentElement.lang = lang;
    // Store in localStorage
    localStorage.setItem('preferredLanguage', lang);
  }

  getLanguage(): 'en' | 'es' {
    return this.currentLang();
  }

  // Get translation for a key
  translate(key: string, params?: Record<string, string | number>): string {
    const lang = this.currentLang();
    const translation = this.translations[key]?.[lang] ?? this.translations[key]?.['en'] ?? key;

    // Simple parameter interpolation
    if (params) {
      return translation.replace(/\{\{(\w+)\}\}/g, (match, paramKey) => {
        return String(params[paramKey] ?? match);
      });
    }

    return translation;
  }

  // Signal-based translation for use in templates
  translateSignal(key: string): () => string {
    return () => this.translate(key);
  }

  // Get all available languages
  getAvailableLanguages(): LanguageOption[] {
    return [
      { code: 'en', name: 'English', nativeName: 'English', flag: '🇬🇧' },
      { code: 'es', name: 'Spanish', nativeName: 'Español', flag: '🇪🇸' }
    ];
  }
}