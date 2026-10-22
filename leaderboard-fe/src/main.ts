import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { App } from './app/app';
import { registerLocaleData } from '@angular/common';
import localeEn from '@angular/common/locales/en';
import localeEs from '@angular/common/locales/es';
import localeEnExtra from '@angular/common/locales/extra/en';
import localeEsExtra from '@angular/common/locales/extra/es';

registerLocaleData(localeEn, 'en', localeEnExtra);
registerLocaleData(localeEs, 'es', localeEsExtra);

bootstrapApplication(App, appConfig)
  .catch((err) => console.error(err));
