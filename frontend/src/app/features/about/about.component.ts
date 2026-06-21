import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { I18nService } from '../../core/i18n/i18n.service';
import { ThemeService } from '../../core/theme/theme.service';

interface Feature { icon: string; en: [string, string]; es: [string, string]; }
interface Tier { layer: string; tech: string; }

@Component({
  selector: 'cs-about',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [LucideAngularModule],
  template: `
    <div class="min-h-screen bg-slate-100 dark:bg-coal-950 text-slate-900 dark:text-mist-100">
      <header class="flex items-center justify-between px-6 h-14 border-b border-slate-200 dark:border-coal-700 bg-white dark:bg-coal-900">
        <button (click)="back()" class="flex items-center gap-2.5">
          <span class="w-7 h-7 rounded-xl bg-volt-500 grid place-items-center"><lucide-icon name="message-circle" class="w-4 h-4 text-white"></lucide-icon></span>
          <span class="font-semibold">ChatSphere</span>
        </button>
        <div class="flex items-center gap-1.5">
          <button (click)="i18n.toggle()" class="h-9 px-2.5 rounded-lg text-xs font-semibold hover:bg-slate-100 dark:hover:bg-coal-800 transition-colors flex items-center gap-1.5">
            <lucide-icon name="languages" class="w-3.5 h-3.5"></lucide-icon>{{ i18n.lang() === 'en' ? 'EN' : 'ES' }}
          </button>
          <button (click)="theme.toggle()" class="h-9 w-9 rounded-lg grid place-items-center hover:bg-slate-100 dark:hover:bg-coal-800 transition-colors">
            @if (theme.theme() === 'dark') { <lucide-icon name="sun" class="w-4 h-4"></lucide-icon> } @else { <lucide-icon name="moon" class="w-4 h-4"></lucide-icon> }
          </button>
        </div>
      </header>

      <div class="max-w-4xl mx-auto px-4 sm:px-6 py-10">
        <div class="flex items-center gap-3 mb-4">
          <span class="w-11 h-11 rounded-2xl bg-volt-500 grid place-items-center"><lucide-icon name="message-circle" class="w-6 h-6 text-white"></lucide-icon></span>
          <div>
            <h1 class="text-2xl font-semibold tracking-tight">ChatSphere</h1>
            <p class="text-sm text-slate-500 dark:text-mist-400">{{ es() ? 'Mensajería de equipo en tiempo real' : 'Real-time team messaging' }}</p>
          </div>
        </div>
        <p class="text-base text-slate-600 dark:text-mist-300 leading-relaxed max-w-2xl">
          {{ es()
            ? 'Un chat tipo Slack/Discord con canales, mensajes directos, presencia, "escribiendo…" y reacciones — todo en tiempo real sobre WebSockets con .NET SignalR y un backplane de Redis. Construido como pieza de portfolio con auth, roles, tests y una capa de demo guiada con actividad simulada.'
            : 'A Slack/Discord-style chat with channels, direct messages, presence, typing and reactions — all real-time over WebSockets with .NET SignalR and a Redis backplane. Built as a portfolio piece with auth, roles, tests and a guided demo layer with simulated activity.' }}
        </p>

        <h2 class="text-xs font-semibold uppercase tracking-wide text-slate-400 dark:text-mist-400 mt-10 mb-4">{{ es() ? 'Lo destacado' : 'Highlights' }}</h2>
        <div class="grid sm:grid-cols-2 gap-3">
          @for (f of features; track f.icon) {
            <div class="rounded-xl border border-slate-200 dark:border-coal-700 bg-white dark:bg-coal-900 p-4 flex gap-3">
              <span class="w-9 h-9 rounded-lg bg-volt-500/15 grid place-items-center shrink-0"><lucide-icon [name]="f.icon" class="w-5 h-5 text-volt-400"></lucide-icon></span>
              <div>
                <h3 class="text-sm font-semibold">{{ es() ? f.es[0] : f.en[0] }}</h3>
                <p class="text-xs text-slate-500 dark:text-mist-400 mt-0.5 leading-snug">{{ es() ? f.es[1] : f.en[1] }}</p>
              </div>
            </div>
          }
        </div>

        <h2 class="text-xs font-semibold uppercase tracking-wide text-slate-400 dark:text-mist-400 mt-10 mb-4">{{ es() ? 'Arquitectura' : 'Architecture' }}</h2>
        <div class="rounded-xl border border-slate-200 dark:border-coal-700 bg-white dark:bg-coal-900 divide-y divide-slate-100 dark:divide-coal-700">
          @for (t of stack; track t.layer) {
            <div class="flex items-center gap-4 px-4 py-2.5">
              <span class="w-32 shrink-0 text-xs font-semibold text-slate-500 dark:text-mist-400">{{ t.layer }}</span>
              <span class="text-sm">{{ t.tech }}</span>
            </div>
          }
        </div>

        <p class="text-[13px] text-slate-500 dark:text-mist-400 mt-6 leading-relaxed">
          {{ es()
            ? 'La presencia se cuenta por conexión en Redis (correcta entre instancias) y el backplane de Redis permite escalar SignalR horizontalmente. Los mensajes y el historial viven en SQL Server; la actividad ambiental es simulada y efímera.'
            : 'Presence is connection-counted in Redis (correct across instances) and the Redis backplane lets SignalR scale out horizontally. Messages and history live in SQL Server; the ambient activity is simulated and ephemeral.' }}
        </p>

        <div class="mt-8">
          <button (click)="back()" class="rounded-lg bg-volt-500 hover:bg-volt-400 text-white text-sm font-semibold px-4 py-2 transition-colors">{{ es() ? 'Volver al chat' : 'Back to chat' }}</button>
        </div>
        <p class="text-[11px] text-slate-400 dark:text-mist-400 font-mono mt-10">ChatSphere · Luis Chiquito Vera · {{ es() ? 'demo de portfolio' : 'portfolio demo' }}</p>
      </div>
    </div>
  `,
})
export class AboutComponent {
  private readonly router = inject(Router);
  readonly i18n = inject(I18nService);
  readonly theme = inject(ThemeService);
  readonly es = () => this.i18n.lang() === 'es';

  back(): void {
    void this.router.navigateByUrl('/app');
  }

  readonly features: Feature[] = [
    { icon: 'message-square', en: ['Real-time messaging', 'Messages, edits and DMs delivered instantly over SignalR.'], es: ['Mensajería en tiempo real', 'Mensajes y DMs entregados al instante por SignalR.'] },
    { icon: 'users', en: ['Live presence', 'Online/offline tracked per connection in Redis, across instances.'], es: ['Presencia en vivo', 'Online/offline por conexión en Redis, entre instancias.'] },
    { icon: 'smile-plus', en: ['Typing & reactions', '“Typing…” indicators and emoji reactions update live.'], es: ['Typing y reacciones', 'Indicadores de "escribiendo…" y reacciones en vivo.'] },
    { icon: 'at-sign', en: ['Mentions & unreads', '@-mentions notify; unread counts update as you read.'], es: ['Menciones y no leídos', 'Las @menciones notifican; los no leídos se actualizan al leer.'] },
    { icon: 'hash', en: ['Channels & history', 'Public channels and DMs with infinite-scroll history.'], es: ['Canales e historial', 'Canales públicos y DMs con historial de scroll infinito.'] },
    { icon: 'message-circle', en: ['Lively demo', 'Simulated teammates keep the demo alive even solo.'], es: ['Demo con vida', 'Compañeros simulados mantienen viva la demo aunque entres solo.'] },
  ];

  readonly stack: Tier[] = [
    { layer: 'Frontend', tech: 'Angular 20 (standalone + signals), Tailwind v4, @microsoft/signalr' },
    { layer: 'Backend', tech: '.NET 9 Web API, SignalR hubs, Clean Architecture' },
    { layer: 'Real-time', tech: 'SignalR + Redis backplane & presence (StackExchange.Redis)' },
    { layer: 'Database', tech: 'SQL Server 2022 + EF Core 9 (messages & history)' },
    { layer: 'Auth', tech: 'JWT access + rotating refresh, lockout, per-server roles' },
    { layer: 'Testing', tech: '25 backend unit tests (xUnit) + Playwright E2E' },
  ];
}
