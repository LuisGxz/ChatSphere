import { DatePipe, NgTemplateOutlet } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, ElementRef, HostListener, OnDestroy, OnInit, computed, inject, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { CdkMenu, CdkMenuItem, CdkMenuTrigger } from '@angular/cdk/menu';
import { LucideAngularModule } from 'lucide-angular';
import { ChatApi } from '../../core/api/chat.api';
import { AuthService } from '../../core/auth/auth.service';
import { DemoService } from '../../core/demo/demo.service';
import { I18nService } from '../../core/i18n/i18n.service';
import { ThemeService } from '../../core/theme/theme.service';
import { RealtimeService } from '../../core/realtime/realtime.service';
import { TPipe } from '../../core/i18n/t.pipe';
import { ChannelDto, DmDto, MessageDto, ServerDetailDto } from '../../core/models/models';
import { AvatarComponent, Presence } from '../../shared/avatar.component';
import { TourComponent } from '../demo/tour.component';
import { HelpPanelComponent } from '../demo/help-panel.component';

interface CurrentChannel { id: string; title: string; topic: string | null; isDirect: boolean; }
/** A message decorated with a "show the author header?" flag for grouping. */
interface GroupedMessage extends MessageDto { showHeader: boolean; }

const REACTION_EMOJIS = ['👍', '❤️', '😂', '🎉', '🚀', '👀', '✅', '🔥'];
const GROUP_WINDOW_MS = 5 * 60_000;
const TYPING_TTL_MS = 4_000;

@Component({
  selector: 'cs-shell',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, NgTemplateOutlet, FormsModule, CdkMenuTrigger, CdkMenu, CdkMenuItem, LucideAngularModule, TPipe, AvatarComponent, TourComponent, HelpPanelComponent],
  templateUrl: './shell.component.html',
})
export class ShellComponent implements OnInit, OnDestroy {
  private readonly api = inject(ChatApi);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  readonly rt = inject(RealtimeService);
  readonly i18n = inject(I18nService);
  readonly theme = inject(ThemeService);
  readonly demo = inject(DemoService);

  private readonly scroller = viewChild<ElementRef<HTMLElement>>('scroller');

  readonly me = this.auth.user;
  readonly server = signal<ServerDetailDto | null>(null);
  readonly dms = signal<DmDto[]>([]);
  readonly current = signal<CurrentChannel | null>(null);
  readonly messages = signal<MessageDto[]>([]);
  readonly loadingMessages = signal(false);
  readonly loadingMore = signal(false);
  readonly hasMore = signal(false);
  readonly draft = signal('');
  readonly mobileNavOpen = signal(false);
  readonly pickerFor = signal<string | null>(null);

  readonly reactionEmojis = REACTION_EMOJIS;
  private nextBefore: string | null = null;
  private lastTypingSentAt = 0;

  readonly online = signal<Record<string, boolean>>({});
  /** Current channel typing: userId → { name, at }. Pruned after a short TTL. */
  readonly typing = signal<Record<string, { name: string; at: number }>>({});

  readonly channels = computed(() => this.server()?.channels.filter((c) => !c.isDirect) ?? []);
  readonly members = computed(() => this.server()?.members ?? []);
  readonly onlineMembers = computed(() => this.members().filter((m) => this.online()[m.user.id]));
  readonly offlineMembers = computed(() => this.members().filter((m) => !this.online()[m.user.id]));
  readonly typingNames = computed(() => Object.values(this.typing()).map((t) => t.name));

  /** Messages decorated with grouping flags (collapse consecutive messages from the same author). */
  readonly grouped = computed<GroupedMessage[]>(() => {
    const list = this.messages();
    return list.map((m, i) => {
      const prev = list[i - 1];
      const showHeader =
        !prev ||
        prev.author.id !== m.author.id ||
        new Date(m.createdAt).getTime() - new Date(prev.createdAt).getTime() > GROUP_WINDOW_MS;
      return { ...m, showHeader };
    });
  });

  async ngOnInit(): Promise<void> {
    this.wireRealtime();
    await this.rt.connect();

    const servers = await firstValue(this.api.servers());
    if (servers.length > 0) {
      const detail = await firstValue(this.api.server(servers[0].id));
      this.server.set(detail);
      this.dms.set(await firstValue(this.api.dms()));
      await this.refreshPresence();
      const first = detail.channels.find((c) => c.name === 'design-crit') ?? detail.channels.find((c) => !c.isDirect);
      if (first) this.openChannel(first);
    }
    this.demo.maybeAutoStart();
  }

  /** Global keys: "?" toggles the explore guide; Esc dismisses the guide/tour. */
  @HostListener('document:keydown', ['$event'])
  onKey(event: KeyboardEvent): void {
    if (event.key === 'Escape') {
      if (this.demo.tourActive()) { this.demo.endTour(); return; }
      if (this.demo.helpOpen()) { this.demo.closeHelp(); return; }
    }
    const el = event.target as HTMLElement;
    if (/^(INPUT|TEXTAREA|SELECT)$/.test(el.tagName) || el.isContentEditable) return;
    if (event.key === '?') {
      event.preventDefault();
      this.demo.helpOpen() ? this.demo.closeHelp() : this.demo.openHelp();
    }
  }

  ngOnDestroy(): void {
    void this.rt.disconnect();
  }

  private wireRealtime(): void {
    this.rt.message$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((m) => {
      // Author is no longer "typing" once their message lands.
      this.typing.update((t) => { const { [m.author.id]: _, ...rest } = t; return rest; });

      if (m.channelId === this.current()?.id) {
        this.messages.update((list) => (list.some((x) => x.id === m.id) ? list : [...list, m]));
        this.scrollSoon();
        if (m.author.id !== this.me()?.id) void this.rt.markRead(m.channelId, m.id).catch(() => {});
      } else if (m.author.id !== this.me()?.id) {
        this.bumpUnread(m.channelId, false);
      }
    });

    this.rt.reaction$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((state) => {
      this.messages.update((list) =>
        list.map((m) =>
          m.id === state.messageId
            ? { ...m, reactions: state.groups.map((g) => ({ emoji: g.emoji, count: g.count, mine: g.userIds.includes(this.me()?.id ?? '') })) }
            : m,
        ),
      );
    });

    this.rt.typing$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((t) => {
      if (t.channelId !== this.current()?.id || t.userId === this.me()?.id) return;
      this.typing.update((cur) => ({ ...cur, [t.userId]: { name: t.displayName, at: Date.now() } }));
      setTimeout(() => this.pruneTyping(), TYPING_TTL_MS + 200);
    });

    this.rt.presence$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((p) =>
      this.online.update((map) => ({ ...map, [p.userId]: p.online })),
    );

    this.rt.mention$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((n) => {
      if (n.channelId !== this.current()?.id) this.bumpUnread(n.channelId, true);
    });
  }

  private pruneTyping(): void {
    const now = Date.now();
    this.typing.update((cur) => {
      const next: Record<string, { name: string; at: number }> = {};
      for (const [id, v] of Object.entries(cur)) if (now - v.at < TYPING_TTL_MS) next[id] = v;
      return next;
    });
  }

  private bumpUnread(channelId: string, mention: boolean): void {
    this.server.update((s) =>
      s ? { ...s, channels: s.channels.map((c) => (c.id === channelId ? { ...c, unread: c.unread + 1, hasMention: c.hasMention || mention } : c)) } : s,
    );
    this.dms.update((list) => list.map((d) => (d.channelId === channelId ? { ...d, unread: d.unread + 1 } : d)));
  }

  private async refreshPresence(): Promise<void> {
    const ids = this.members().map((m) => m.user.id);
    if (ids.length === 0) return;
    try {
      const onlineIds = await this.rt.whoIsOnline(ids);
      const map: Record<string, boolean> = {};
      for (const id of onlineIds) map[id] = true;
      this.online.set(map);
    } catch {
      /* not connected yet */
    }
  }

  presenceOf(userId: string): Presence {
    return this.online()[userId] ? 'online' : 'offline';
  }

  openChannel(c: ChannelDto): void {
    this.openCurrent({ id: c.id, title: c.name ?? 'channel', topic: c.topic, isDirect: false });
    this.markChannelRead(c.id);
  }

  openDm(d: DmDto): void {
    this.openCurrent({ id: d.channelId, title: d.other.displayName, topic: null, isDirect: true });
    this.markChannelRead(d.channelId);
  }

  private openCurrent(c: CurrentChannel): void {
    this.current.set(c);
    this.mobileNavOpen.set(false);
    this.typing.set({});
    this.pickerFor.set(null);
    this.loadMessages(c.id);
  }

  private loadMessages(channelId: string): void {
    this.loadingMessages.set(true);
    this.api.messages(channelId).subscribe({
      next: (page) => {
        this.messages.set(page.messages);
        this.hasMore.set(page.hasMore);
        this.nextBefore = page.nextBefore;
        this.loadingMessages.set(false);
        this.scrollSoon();
        const last = page.messages.at(-1);
        if (last) void this.rt.markRead(channelId, last.id).catch(() => {});
      },
      error: () => this.loadingMessages.set(false),
    });
  }

  onScroll(): void {
    const el = this.scroller()?.nativeElement;
    if (el && el.scrollTop < 60 && this.hasMore() && !this.loadingMore()) this.loadEarlier();
  }

  private loadEarlier(): void {
    const channelId = this.current()?.id;
    if (!channelId || !this.nextBefore) return;
    this.loadingMore.set(true);
    const el = this.scroller()?.nativeElement;
    const prevHeight = el?.scrollHeight ?? 0;
    this.api.messages(channelId, this.nextBefore).subscribe({
      next: (page) => {
        this.messages.update((list) => [...page.messages, ...list]);
        this.hasMore.set(page.hasMore);
        this.nextBefore = page.nextBefore;
        this.loadingMore.set(false);
        // Keep the viewport anchored to where the user was reading.
        setTimeout(() => {
          if (el) el.scrollTop = el.scrollHeight - prevHeight;
        }, 20);
      },
      error: () => this.loadingMore.set(false),
    });
  }

  private markChannelRead(channelId: string): void {
    this.server.update((s) =>
      s ? { ...s, channels: s.channels.map((c) => (c.id === channelId ? { ...c, unread: 0, hasMention: false } : c)) } : s,
    );
    this.dms.update((list) => list.map((d) => (d.channelId === channelId ? { ...d, unread: 0 } : d)));
  }

  async send(): Promise<void> {
    const body = this.draft().trim();
    const channelId = this.current()?.id;
    if (!body || !channelId) return;
    this.draft.set('');
    try {
      await this.rt.sendMessage(channelId, body);
    } catch {
      this.draft.set(body);
    }
  }

  onComposerKey(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      void this.send();
      return;
    }
    this.maybeSendTyping();
  }

  private maybeSendTyping(): void {
    const channelId = this.current()?.id;
    const now = Date.now();
    if (channelId && now - this.lastTypingSentAt > 2000) {
      this.lastTypingSentAt = now;
      void this.rt.startTyping(channelId).catch(() => {});
    }
  }

  togglePicker(messageId: string): void {
    this.pickerFor.update((cur) => (cur === messageId ? null : messageId));
  }

  react(messageId: string, emoji: string): void {
    this.pickerFor.set(null);
    void this.rt.toggleReaction(messageId, emoji).catch(() => {});
  }

  goAbout(): void {
    void this.router.navigateByUrl('/about');
  }

  signOut(): void {
    void this.rt.disconnect();
    this.auth.logout();
    void this.router.navigateByUrl('/login');
  }

  private scrollSoon(): void {
    setTimeout(() => {
      const el = this.scroller()?.nativeElement;
      if (el) el.scrollTop = el.scrollHeight;
    }, 30);
  }
}

function firstValue<T>(obs: import('rxjs').Observable<T>): Promise<T> {
  return new Promise((resolve, reject) => {
    const sub = obs.subscribe({ next: (v) => { resolve(v); sub.unsubscribe(); }, error: reject });
  });
}
