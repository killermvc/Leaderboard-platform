# Internationalization (i18n) Guide

This guide explains how to add and manage translations in the Leaderboard application.

## Overview

The application uses Angular's built-in i18n system with XLF (XML Localization Interchange File Format) translation files. Currently supported languages:
- English (en) - Default
- Spanish (es)

## Architecture

### Frontend (Angular)
- **Compile-time translation**: Angular extracts translatable strings during build
- **Runtime language switching**: Page reload approach with separate builds per locale
- **Translation files**: XLF format in `src/locale/`
- **Dynamic strings**: Handled via `TranslationService`

### Backend (ASP.NET Core)
- **Error codes**: Returns machine-readable error codes instead of localized messages
- **Error format**: `{ "code": "ERROR_CODE", "message": "English message for debugging", "details": {} }`

## File Structure

```
leaderboard-fe/src/
├── locale/
│   ├── messages.xlf          # Source translations (English)
│   └── messages.es.xlf       # Spanish translations
├── app/
│   ├── core/
│   │   ├── translation.service.ts      # Dynamic string translation
│   │   ├── error-translation.service.ts # Error code to translation mapping
│   │   └── language-initializer.ts     # Language detection & persistence
│   └── ...
```

## Adding New Translatable Strings

### 1. In Templates (HTML)

Add `i18n` attribute with a unique ID:

```html
<!-- Simple text -->
<h1 i18n="@@page.title">Welcome</h1>

<!-- With interpolation -->
<p i18n="@@user.greeting">Hello, {{ username }}!</p>

<!-- Attributes -->
<input placeholder="Search..." i18n-placeholder="@@search.placeholder" />
<img alt="Logo" i18n-alt="@@logo.alt" />
<button aria-label="Close" i18n-aria-label="@@close.aria">×</button>
```

### 2. In Components (TypeScript)

Use `TranslationService` for dynamic strings:

```typescript
constructor(private translationService: TranslationService) {}

// Get translation
const message = this.translationService.translate('errors.gameNotFound');

// Or use signal for reactive templates
errorMessage = computed(() => this.translationService.translate('errors.networkError'));
```

### 3. Add Translation Keys to XLF

After adding i18n attributes, extract strings:

```bash
cd leaderboard-fe
npm run extract-i18n
```

This updates `src/locale/messages.xlf` with new entries.

### 4. Translate to Spanish

Edit `src/locale/messages.es.xlf` and add `<target>` elements:

```xml
<trans-unit id="page.title" datatype="html">
  <source>Welcome</source>
  <target>Bienvenido</target>
  <context-group purpose="location">
    <context context-type="sourcefile">src/app/home-page/home-page.html</context>
    <context context-type="linenumber">5</context>
  </context-group>
</trans-unit>
```

## Building for Different Locales

```bash
# Build English (default)
npm run build

# Build Spanish
npm run build:es
```

Output will be in `dist/leaderboard-fe/` with locale-specific bundles.

## Language Detection & Persistence

The application automatically detects the user's preferred language:

1. **localStorage**: Checks `preferredLanguage` key
2. **Browser language**: Falls back to `navigator.language` (es-* → Spanish, otherwise English)
3. **Persistence**: User selection saved to localStorage

### Language Selector

The header component includes a language selector (EN/ES) that:
1. Updates the `TranslationService`
2. Saves preference to localStorage
3. Reloads page to load the correct locale build

## Error Handling

### Backend Error Codes

The backend returns standardized error responses:

```json
{
  "code": "GAME_NOT_FOUND",
  "message": "Game not found.",
  "details": {}
}
```

### Frontend Error Translation

Use `ErrorTranslationService` to translate error codes:

```typescript
constructor(private errorTranslation: ErrorTranslationService) {}

handleError(error: HttpErrorResponse): string {
  return this.errorTranslation.getTranslatedError(error);
}
```

## Adding a New Language

1. **Add locale data** in `main.ts`:
```typescript
import localeFr from '@angular/common/locales/fr';
import localeFrExtra from '@angular/common/locales/extra/fr';
registerLocaleData(localeFr, 'fr', localeFrExtra);
```

2. **Create translation file**:
```bash
cp src/locale/messages.xlf src/locale/messages.fr.xlf
```

3. **Translate** all `<target>` elements in `messages.fr.xlf`

3. **Update angular.json**:
```json
"i18n": {
  "sourceLocale": "en",
  "locales": {
    "es": { "translation": "src/locale/messages.es.xlf" },
    "fr": { "translation": "src/locale/messages.fr.xlf" }
  }
},
"architect": {
  "build": {
    "options": {
      "localize": true
    },
    "configurations": {
      "fr": { "localize": ["fr"] }
    }
  }
}
```

4. **Add build script** in `package.json`:
```json
"build:fr": "ng build --configuration=fr"
```

5. **Register locale** in `main.ts` and update language detection logic

## Date/Number Formatting

The application uses Angular's built-in pipes which are locale-aware:

```html
<!-- Dates - automatically uses current locale -->
{{ date | date:'short' }}
{{ date | date:'medium' }}
{{ date | date:'full' }}

<!-- Numbers - automatically uses locale decimal/thousand separators -->
{{ value | number }}
{{ value | number:'1.2-2' }}
```

For custom date formatting in components, use `TranslationService.getLanguage()`:

```typescript
formatDate(dateString: string): string {
  const locale = this.translationService.getLanguage() === 'es' ? 'es-ES' : 'en-US';
  return new Date(dateString).toLocaleDateString(locale, {
    year: 'numeric',
    month: 'short',
    day: 'numeric'
  });
}
```

## Testing

Run tests with locale support:

```bash
npm test
```

The test setup automatically loads `@angular/localize/init` via `src/test-setup.ts`.

## Best Practices

1. **Use meaningful IDs**: `@@feature.section.element` (e.g., `@@gameDetail.submitScoreBtn`)
2. **Provide context**: The extractor automatically adds file/line context
3. **Avoid concatenation**: Use interpolation instead of string concatenation
4. **Mark all user-facing text**: Including alt text, aria-labels, placeholders
5. **Test both locales**: Always verify builds work for all supported languages
6. **Keep translations in sync**: Run `extract-i18n` after adding new i18n attributes

## Troubleshooting

### Build fails with "Unsupported translation file format"
- Check XLF syntax (proper XML escaping)
- Ensure `<target>` elements are properly nested
- Verify XML entities are escaped (`<`, `>`, `"`)

### Tests fail with "$localize is not a function"
- Ensure `src/test-setup.ts` is in `tsconfig.spec.json` includes
- Verify `@angular/localize/init` is imported in test setup

### Language not switching
- Check localStorage for `preferredLanguage`
- Verify `TranslationService.setLanguage()` is called
- Ensure page reload happens after language change

### Missing translations in Spanish build
- Run `npm run extract-i18n` after adding new i18n attributes
- Check `messages.es.xlf` has `<target>` for all new IDs
- Rebuild Spanish: `npm run build:es`

## References

- [Angular i18n Guide](https://angular.dev/guide/i18n)
- [XLF Format Specification](https://docs.oasis-open.org/xliff/xliff-core/v2.1/xliff-core-v2.1.html)
- [Angular DatePipe](https://angular.dev/api/common/DatePipe)
- [Angular NumberPipe](https://angular.dev/api/common/DecimalPipe)