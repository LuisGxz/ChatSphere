import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';
import { DemoService } from '../../core/demo/demo.service';
import { I18nService } from '../../core/i18n/i18n.service';
import { TPipe } from '../../core/i18n/t.pipe';

@Component({
  selector: 'cs-help-panel',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [LucideAngularModule, TPipe],
  template: `
    @if (demo.helpOpen()) {
      <div class="fixed inset-0 z-[90] flex justify-end">
        <div class="absolute inset-0 bg-coal-950/50 backdrop-blur-[1px]" (click)="demo.closeHelp()"></div>

        <aside class="relative w-full sm:w-[400px] h-full bg-white dark:bg-coal-900 border-l border-slate-200 dark:border-coal-700 shadow-2xl flex flex-col">
          <header class="flex items-center gap-2 px-5 py-4 border-b border-slate-200 dark:border-coal-700">
            <span class="w-8 h-8 rounded-xl bg-volt-500 grid place-items-center"><lucide-icon name="message-circle" class="w-4 h-4 text-white"></lucide-icon></span>
            <h2 class="font-semibold">{{ 'demo.help' | t }}</h2>
            <button (click)="demo.closeHelp()" class="ml-auto w-8 h-8 rounded-lg grid place-items-center text-slate-500 dark:text-mist-400 hover:bg-slate-100 dark:hover:bg-coal-800 transition-colors"><lucide-icon name="x" class="w-4 h-4"></lucide-icon></button>
          </header>

          <div class="flex-1 overflow-y-auto p-5 space-y-6">
            <p class="text-sm text-slate-600 dark:text-mist-300 leading-relaxed">{{ 'demo.intro' | t }}</p>

            <button (click)="demo.startTour()"
              class="w-full rounded-lg bg-volt-500 hover:bg-volt-400 text-white text-sm font-semibold py-2.5 transition-colors flex items-center justify-center gap-2">
              <lucide-icon name="message-circle" class="w-4 h-4"></lucide-icon>{{ 'demo.startTour' | t }}
            </button>

            <section>
              <h3 class="text-xs font-semibold uppercase tracking-wide text-slate-400 dark:text-mist-400 mb-3">{{ 'demo.whatsReal' | t }}</h3>
              <ul class="space-y-2 text-sm">
                @for (k of realKeys; track k) {
                  <li class="flex items-start gap-2.5">
                    <lucide-icon name="message-square" class="w-4 h-4 text-volt-400 shrink-0 mt-0.5"></lucide-icon>
                    <span>{{ k | t }}</span>
                  </li>
                }
              </ul>
            </section>

            <section class="rounded-xl bg-volt-500/10 border border-volt-500/30 p-4">
              <p class="text-sm text-slate-700 dark:text-mist-100 font-medium flex items-start gap-2">
                <lucide-icon name="users" class="w-4 h-4 mt-0.5 shrink-0 text-volt-400"></lucide-icon>
                <span>{{ 'demo.twoTabs' | t }}</span>
              </p>
              <ul class="mt-3 space-y-1.5">
                @for (acc of accounts; track acc.email) {
                  <li class="flex items-center gap-2 text-xs">
                    <span class="text-[10px] font-semibold rounded-full px-1.5 py-0.5 bg-slate-200 dark:bg-coal-700 text-slate-600 dark:text-mist-300">{{ ('role.' + acc.role) | t }}</span>
                    <span class="font-mono text-slate-500 dark:text-mist-400">{{ acc.email }}</span>
                  </li>
                }
                <li class="text-[11px] text-slate-400 dark:text-mist-400 pt-1">{{ es() ? 'Contraseña' : 'Password' }}: <span class="font-mono">Password123!</span></li>
              </ul>
            </section>

            <section>
              <h3 class="text-xs font-semibold uppercase tracking-wide text-slate-400 dark:text-mist-400 mb-3">{{ 'demo.shortcuts' | t }}</h3>
              <ul class="space-y-2 text-sm">
                <li class="flex items-center gap-2"><span class="kbd">?</span><span class="text-slate-500 dark:text-mist-400">{{ 'demo.help' | t }}</span></li>
                <li class="flex items-center gap-2"><span class="kbd">Enter</span><span class="text-slate-500 dark:text-mist-400">{{ es() ? 'Enviar mensaje' : 'Send message' }}</span></li>
                <li class="flex items-center gap-2"><span class="kbd">Esc</span><span class="text-slate-500 dark:text-mist-400">{{ es() ? 'Cerrar panel' : 'Close panel' }}</span></li>
              </ul>
            </section>
          </div>
        </aside>
      </div>
    }
  `,
})
export class HelpPanelComponent {
  readonly demo = inject(DemoService);
  private readonly i18n = inject(I18nService);
  readonly es = () => this.i18n.lang() === 'es';

  readonly realKeys = ['demo.real1', 'demo.real2', 'demo.real3', 'demo.real4'];
  readonly accounts = [
    { role: 'Owner', email: 'luis@chatsphere.app' },
    { role: 'Admin', email: 'priya@chatsphere.app' },
    { role: 'Member', email: 'marcus@chatsphere.app' },
  ];
}
