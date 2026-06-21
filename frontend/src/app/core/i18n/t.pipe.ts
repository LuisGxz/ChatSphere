import { Pipe, PipeTransform } from '@angular/core';
import { I18nService } from './i18n.service';

/** Usage: {{ 'auth.signIn' | t }}. Impure so it re-renders when the language switches. */
@Pipe({ name: 't', pure: false })
export class TPipe implements PipeTransform {
  constructor(private readonly i18n: I18nService) {}
  transform(key: string): string {
    return this.i18n.t(key);
  }
}
