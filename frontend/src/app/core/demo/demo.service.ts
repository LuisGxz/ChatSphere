import { Injectable, signal } from '@angular/core';

export interface TourStep {
  target: string | null;
  title: { en: string; es: string };
  body: { en: string; es: string };
}

const SEEN_KEY = 'cs-tour-seen';

/** Coordinates the guided-demo layer: the "How to explore" panel and the coach-mark tour. */
@Injectable({ providedIn: 'root' })
export class DemoService {
  readonly helpOpen = signal(false);
  readonly tourActive = signal(false);
  readonly stepIndex = signal(0);

  readonly steps: TourStep[] = [
    {
      target: null,
      title: { en: 'Welcome to ChatSphere', es: 'Bienvenido a ChatSphere' },
      body: {
        en: 'A real-time team chat — channels, DMs, presence, typing and reactions, powered by .NET SignalR + Redis. Take the 30-second tour.',
        es: 'Un chat de equipo en tiempo real — canales, DMs, presencia, "escribiendo…" y reacciones, con .NET SignalR + Redis. Haz el tour de 30 segundos.',
      },
    },
    {
      target: '[data-tour="channels"]',
      title: { en: 'Channels & DMs', es: 'Canales y DMs' },
      body: {
        en: 'Jump between channels and direct messages. Unread counts and @-mention badges update live as messages arrive.',
        es: 'Cambia entre canales y mensajes directos. Los no leídos y las menciones @ se actualizan en vivo al llegar mensajes.',
      },
    },
    {
      target: '[data-tour="messages"]',
      title: { en: 'It’s genuinely live', es: 'Es de verdad en vivo' },
      body: {
        en: 'Teammates are chatting here right now — that activity is simulated so the demo feels alive even solo. For true real-time, open a second tab and sign in as another role.',
        es: 'Hay compañeros conversando ahora mismo — esa actividad está simulada para que la demo respire aunque entres solo. Para el tiempo real de verdad, abre otra pestaña con otro rol.',
      },
    },
    {
      target: '[data-tour="composer"]',
      title: { en: 'Send a message', es: 'Envía un mensaje' },
      body: {
        en: 'Type and hit Enter. Others see your “typing…” indicator instantly, and your message appears for everyone in the channel.',
        es: 'Escribe y pulsa Enter. Los demás ven tu indicador de "escribiendo…" al instante, y tu mensaje aparece para todos en el canal.',
      },
    },
    {
      target: '[data-tour="help"]',
      title: { en: 'Explore freely', es: 'Explora libremente' },
      body: {
        en: 'Reopen this guide here anytime. Try reacting to a message, or open a second tab to chat with yourself in real time!',
        es: 'Reabre esta guía aquí cuando quieras. Prueba a reaccionar a un mensaje, o abre otra pestaña para chatear contigo en tiempo real.',
      },
    },
  ];

  openHelp(): void { this.helpOpen.set(true); }
  closeHelp(): void { this.helpOpen.set(false); }

  startTour(): void {
    this.helpOpen.set(false);
    this.stepIndex.set(0);
    this.tourActive.set(true);
  }

  next(): void {
    if (this.stepIndex() >= this.steps.length - 1) this.endTour();
    else this.stepIndex.update((i) => i + 1);
  }
  prev(): void { this.stepIndex.update((i) => Math.max(0, i - 1)); }

  endTour(): void {
    this.tourActive.set(false);
    this.markSeen();
  }

  maybeAutoStart(): void {
    if (!this.hasSeen()) setTimeout(() => this.startTour(), 900);
  }

  hasSeen(): boolean {
    try { return localStorage.getItem(SEEN_KEY) === '1'; } catch { return false; }
  }
  private markSeen(): void {
    try { localStorage.setItem(SEEN_KEY, '1'); } catch { /* ignore */ }
  }
}
