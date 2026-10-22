import { Injectable, inject } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { TranslationService } from './translation.service';

export interface ApiErrorResponse {
  code: string;
  message: string;
  details?: unknown;
}

@Injectable({ providedIn: 'root' })
export class ErrorTranslationService {
  private translationService = inject(TranslationService);

  // Map backend error codes to translation keys
  private readonly errorCodeTranslations: Record<string, string> = {
    // Authentication & Authorization
    'UNAUTHORIZED': 'errors.unauthorized',
    'FORBIDDEN': 'errors.forbidden',
    'INVALID_TOKEN': 'errors.invalidToken',
    'TOKEN_EXPIRED': 'errors.tokenExpired',

    // Validation
    'VALIDATION_FAILED': 'errors.validationFailed',
    'INVALID_REQUEST': 'errors.invalidRequest',

    // Resource Not Found
    'NOT_FOUND': 'errors.notFound',
    'GAME_NOT_FOUND': 'errors.gameNotFound',
    'SCORE_NOT_FOUND': 'errors.scoreNotFound',
    'USER_NOT_FOUND': 'errors.userNotFound',
    'API_KEY_NOT_FOUND': 'errors.apiKeyNotFound',

    // Business Logic
    'SUBMISSIONS_NOT_ALLOWED': 'errors.submissionsNotAllowed',
    'SCORE_NOT_HIGHER': 'errors.scoreNotHigher',
    'DUPLICATE_SUBMISSION': 'errors.duplicateSubmission',
    'CANNOT_MODERATE_OWN_SCORE': 'errors.cannotModerateOwnScore',
    'ALREADY_MODERATOR': 'errors.alreadyModerator',
    'NOT_MODERATOR': 'errors.notModerator',
    'CANNOT_REMOVE_OWNER': 'errors.cannotRemoveOwner',
    'GAME_ALREADY_EXISTS': 'errors.gameAlreadyExists',

    // Rate Limiting
    'RATE_LIMIT_EXCEEDED': 'errors.rateLimitExceeded',

    // Server Errors
    'INTERNAL_ERROR': 'errors.internalError',
    'SERVICE_UNAVAILABLE': 'errors.serviceUnavailable',
    'DATABASE_ERROR': 'errors.databaseError',
    'EXTERNAL_SERVICE_ERROR': 'errors.externalServiceError',

    // Clerk/Webhook
    'CLERK_ERROR': 'errors.clerkError',
    'WEBHOOK_VERIFICATION_FAILED': 'errors.webhookVerificationFailed',
  };

  /**
   * Extract error code from HTTP error response
   */
  getErrorCode(error: HttpErrorResponse): string | null {
    // Check if response has the new error format
    if (error.error && typeof error.error === 'object' && 'code' in error.error) {
      return (error.error as ApiErrorResponse).code;
    }
    return null;
  }

  /**
   * Get translation key for an error code
   */
  getTranslationKey(errorCode: string): string {
    return this.errorCodeTranslations[errorCode] ?? 'errors.unknown';
  }

  /**
   * Get translated error message for an HTTP error
   */
  getTranslatedError(error: HttpErrorResponse): string {
    const errorCode = this.getErrorCode(error);
    if (errorCode) {
      const translationKey = this.getTranslationKey(errorCode);
      return this.translationService.translate(translationKey);
    }

    // Fallback to generic error messages based on HTTP status
    switch (error.status) {
      case 0:
        return this.translationService.translate('errors.networkError');
      case 400:
        return this.translationService.translate('errors.badRequest');
      case 401:
        return this.translationService.translate('errors.unauthorized');
      case 403:
        return this.translationService.translate('errors.forbidden');
      case 404:
        return this.translationService.translate('errors.notFound');
      case 429:
        return this.translationService.translate('errors.rateLimitExceeded');
      case 500:
        return this.translationService.translate('errors.internalError');
      case 503:
        return this.translationService.translate('errors.serviceUnavailable');
      default:
        return this.translationService.translate('errors.unknown');
    }
  }

  /**
   * Get error details if available
   */
  getErrorDetails(error: HttpErrorResponse): unknown | null {
    if (error.error && typeof error.error === 'object' && 'details' in error.error) {
      return (error.error as ApiErrorResponse).details;
    }
    return null;
  }
}