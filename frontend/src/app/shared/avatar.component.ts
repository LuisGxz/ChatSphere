import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

export type Presence = 'online' | 'away' | 'offline' | null;

/** Rounded-square avatar with initials over the user's colour, plus an optional presence dot. */
@Component({
  selector: 'cs-avatar',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span class="relative inline-block shrink-0">
      <span
        class="grid place-items-center rounded-lg font-bold text-white select-none"
        [class]="sizeClass()"
        [class.opacity-60]="presence() === 'away'"
        [style.background]="color()"
        [attr.title]="name()"
      >{{ initials() }}</span>
      @if (presence(); as p) {
        @if (p !== 'offline') {
          <span
            class="absolute -bottom-0.5 -right-0.5 rounded-full border-2 border-coal-950"
            [class]="dotSize()"
            [class.bg-online]="p === 'online'"
            [class.bg-away]="p === 'away'"
          ></span>
        }
      }
    </span>
  `,
})
export class AvatarComponent {
  readonly name = input<string>('');
  readonly color = input<string>('#7C6CF0');
  readonly size = input<'sm' | 'md' | 'lg'>('md');
  readonly presence = input<Presence>(null);

  readonly initials = computed(() => {
    const parts = this.name().trim().split(/\s+/).filter(Boolean);
    if (parts.length === 0) return '?';
    if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase();
    return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
  });

  readonly sizeClass = computed(() => {
    switch (this.size()) {
      case 'sm': return 'w-6 h-6 text-[9px]';
      case 'lg': return 'w-9 h-9 text-xs';
      default: return 'w-8 h-8 text-[11px]';
    }
  });

  readonly dotSize = computed(() => (this.size() === 'sm' ? 'w-2.5 h-2.5' : 'w-3 h-3'));
}
