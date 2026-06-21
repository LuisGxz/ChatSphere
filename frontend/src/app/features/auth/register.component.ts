import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { Router, RouterLink } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { AuthService } from '../../core/auth/auth.service';
import { I18nService } from '../../core/i18n/i18n.service';
import { TPipe } from '../../core/i18n/t.pipe';
import { ApiError } from '../../core/models/models';
import { AuthShellComponent } from './auth-shell.component';

@Component({
  selector: 'cs-register',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, RouterLink, LucideAngularModule, TPipe, AuthShellComponent],
  template: `
    <cs-auth-shell>
      <h1 class="text-2xl font-semibold tracking-tight">{{ 'auth.createAccount' | t }}</h1>
      <p class="text-sm text-slate-500 dark:text-mist-400 mt-1 mb-7">{{ 'auth.signUpSubtitle' | t }}</p>

      <form (ngSubmit)="submit()" class="space-y-4">
        <label class="block">
          <span class="text-xs font-semibold text-slate-500 dark:text-mist-400">{{ 'auth.displayName' | t }}</span>
          <input name="name" required [(ngModel)]="displayName" [disabled]="loading()"
            class="mt-1.5 w-full rounded-lg border border-slate-200 dark:border-coal-700 bg-white dark:bg-coal-800 px-3 py-2.5 text-sm outline-none focus:border-volt-500 transition-colors"
            placeholder="Alex Rivera" />
        </label>
        <label class="block">
          <span class="text-xs font-semibold text-slate-500 dark:text-mist-400">{{ 'common.email' | t }}</span>
          <input name="email" type="email" autocomplete="email" required [(ngModel)]="email" [disabled]="loading()"
            class="mt-1.5 w-full rounded-lg border border-slate-200 dark:border-coal-700 bg-white dark:bg-coal-800 px-3 py-2.5 text-sm outline-none focus:border-volt-500 transition-colors"
            placeholder="you@studio.com" />
        </label>
        <label class="block">
          <span class="text-xs font-semibold text-slate-500 dark:text-mist-400">{{ 'common.password' | t }}</span>
          <input name="password" type="password" autocomplete="new-password" required [(ngModel)]="password" [disabled]="loading()"
            class="mt-1.5 w-full rounded-lg border border-slate-200 dark:border-coal-700 bg-white dark:bg-coal-800 px-3 py-2.5 text-sm outline-none focus:border-volt-500 transition-colors"
            placeholder="8+ chars, upper, lower, digit" />
        </label>

        @if (error()) {
          <p class="flex items-center gap-2 text-xs font-medium text-red-500 bg-red-500/10 rounded-lg px-3 py-2">
            <lucide-icon name="alert-circle" class="w-4 h-4 shrink-0"></lucide-icon>{{ error() }}
          </p>
        }

        <button type="submit" [disabled]="loading() || !valid()"
          class="w-full rounded-lg bg-volt-500 hover:bg-volt-400 disabled:opacity-50 disabled:cursor-not-allowed text-white text-sm font-semibold py-2.5 transition-colors flex items-center justify-center gap-2">
          @if (loading()) {
            <lucide-icon name="loader" class="w-4 h-4 animate-spin"></lucide-icon>{{ 'auth.creatingAccount' | t }}
          } @else {
            {{ 'auth.signUp' | t }}
          }
        </button>
      </form>

      <p class="text-sm text-slate-500 dark:text-mist-400 mt-5 text-center">
        {{ 'auth.haveAccount' | t }}
        <a routerLink="/login" class="font-semibold text-volt-500 hover:text-volt-400 transition-colors">{{ 'auth.signIn' | t }}</a>
      </p>
    </cs-auth-shell>
  `,
})
export class RegisterComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly i18n = inject(I18nService);

  readonly displayName = signal('');
  readonly email = signal('');
  readonly password = signal('');
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  valid(): boolean {
    return !!this.displayName() && !!this.email() && this.password().length >= 8;
  }

  submit(): void {
    if (this.loading() || !this.valid()) return;
    this.loading.set(true);
    this.error.set(null);
    this.auth.register(this.email(), this.password(), this.displayName()).subscribe({
      next: () => this.router.navigateByUrl('/app'),
      error: (err: HttpErrorResponse) => {
        this.loading.set(false);
        this.error.set(this.messageFor(err));
      },
    });
  }

  private messageFor(err: HttpErrorResponse): string {
    if (err.status === 0) return this.i18n.t('error.network');
    const api = err.error as ApiError | undefined;
    const key = api?.code ? `error.${api.code}` : 'error.generic';
    const translated = this.i18n.t(key);
    if (translated !== key) return translated;
    const first = api?.errors ? Object.values(api.errors)[0]?.[0] : undefined;
    return first ?? api?.message ?? this.i18n.t('error.generic');
  }
}
