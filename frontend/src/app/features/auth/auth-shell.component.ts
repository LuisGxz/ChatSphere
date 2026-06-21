import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';
import { I18nService } from '../../core/i18n/i18n.service';
import { ThemeService } from '../../core/theme/theme.service';

/** Centered auth layout: brand mark, theme/language toggles, projected card body. */
@Component({
  selector: 'cs-auth-shell',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [LucideAngularModule],
  template: `
    <div class="min-h-screen flex flex-col bg-slate-100 dark:bg-coal-950">
      <header class="flex items-center justify-between px-6 py-4">
        <div class="flex items-center gap-2.5">
          <span class="w-8 h-8 rounded-xl bg-volt-500 grid place-items-center">
            <lucide-icon name="message-circle" class="w-4 h-4 text-white"></lucide-icon>
          </span>
          <span class="font-semibold">ChatSphere</span>
        </div>
        <div class="flex items-center gap-1.5">
          <button (click)="i18n.toggle()" title="Language"
            class="h-9 px-3 rounded-lg border border-slate-200 dark:border-coal-700 bg-white dark:bg-coal-800 text-xs font-semibold hover:border-slate-300 dark:hover:border-coal-500 transition-colors flex items-center gap-1.5">
            <lucide-icon name="languages" class="w-3.5 h-3.5"></lucide-icon>{{ i18n.lang() === 'en' ? 'EN' : 'ES' }}
          </button>
          <button (click)="theme.toggle()" title="Theme"
            class="h-9 w-9 rounded-lg border border-slate-200 dark:border-coal-700 bg-white dark:bg-coal-800 grid place-items-center hover:border-slate-300 dark:hover:border-coal-500 transition-colors">
            @if (theme.theme() === 'dark') {
              <lucide-icon name="sun" class="w-4 h-4"></lucide-icon>
            } @else {
              <lucide-icon name="moon" class="w-4 h-4"></lucide-icon>
            }
          </button>
        </div>
      </header>

      <main class="flex-1 grid place-items-center px-4 py-8">
        <div class="w-full max-w-md">
          <div class="rounded-2xl border border-slate-200 dark:border-coal-700 bg-white dark:bg-coal-900 p-7 shadow-sm">
            <ng-content></ng-content>
          </div>
          <p class="text-center text-[11px] text-slate-400 dark:text-mist-400 mt-5 font-mono">
            ChatSphere · real-time team messaging · Luis Chiquito Vera
          </p>
        </div>
      </main>
    </div>
  `,
})
export class AuthShellComponent {
  readonly i18n = inject(I18nService);
  readonly theme = inject(ThemeService);
}
