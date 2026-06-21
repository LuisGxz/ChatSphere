import { Injectable, inject, signal } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthService } from '../auth/auth.service';
import {
  MentionNotificationDto, MessageDto, PresenceDto, ReactionStateDto, ReadReceiptDto, TypingDto,
} from '../models/models';

export type ConnectionState = 'disconnected' | 'connecting' | 'connected' | 'reconnecting';

/** Thin wrapper over the SignalR hub: connection lifecycle, event streams and invokable methods. */
@Injectable({ providedIn: 'root' })
export class RealtimeService {
  private readonly auth = inject(AuthService);
  private connection?: signalR.HubConnection;

  readonly state = signal<ConnectionState>('disconnected');

  // Event streams consumed by the chat feature.
  readonly message$ = new Subject<MessageDto>();
  readonly reaction$ = new Subject<ReactionStateDto>();
  readonly typing$ = new Subject<TypingDto>();
  readonly presence$ = new Subject<PresenceDto>();
  readonly read$ = new Subject<ReadReceiptDto>();
  readonly mention$ = new Subject<MentionNotificationDto>();

  async connect(): Promise<void> {
    if (this.connection) return;

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(environment.hubUrl, { accessTokenFactory: () => this.auth.getAccessToken() ?? '' })
      .withAutomaticReconnect()
      // Quiet: connection state is surfaced in the UI; transient negotiation/reconnect noise stays out of the console.
      .configureLogging(signalR.LogLevel.Critical)
      .build();

    connection.on('ReceiveMessage', (m: MessageDto) => this.message$.next(m));
    connection.on('ReactionUpdated', (r: ReactionStateDto) => this.reaction$.next(r));
    connection.on('UserTyping', (t: TypingDto) => this.typing$.next(t));
    connection.on('PresenceChanged', (p: PresenceDto) => this.presence$.next(p));
    connection.on('MessageRead', (r: ReadReceiptDto) => this.read$.next(r));
    connection.on('MentionReceived', (n: MentionNotificationDto) => this.mention$.next(n));

    connection.onreconnecting(() => this.state.set('reconnecting'));
    connection.onreconnected(() => this.state.set('connected'));
    connection.onclose(() => this.state.set('disconnected'));

    this.connection = connection;
    this.state.set('connecting');
    try {
      await connection.start();
      this.state.set('connected');
    } catch {
      this.state.set('disconnected');
    }
  }

  async disconnect(): Promise<void> {
    if (!this.connection) return;
    const c = this.connection;
    this.connection = undefined;
    this.state.set('disconnected');
    try {
      await c.stop();
    } catch {
      /* ignore */
    }
  }

  sendMessage(channelId: string, body: string): Promise<MessageDto> {
    return this.invoke<MessageDto>('SendMessage', channelId, body);
  }
  toggleReaction(messageId: string, emoji: string): Promise<void> {
    return this.invoke<void>('ToggleReaction', messageId, emoji);
  }
  startTyping(channelId: string): Promise<void> {
    return this.invoke<void>('StartTyping', channelId);
  }
  markRead(channelId: string, lastMessageId: string): Promise<void> {
    return this.invoke<void>('MarkRead', channelId, lastMessageId);
  }
  whoIsOnline(userIds: string[]): Promise<string[]> {
    return this.invoke<string[]>('WhoIsOnline', userIds);
  }

  private invoke<T>(method: string, ...args: unknown[]): Promise<T> {
    if (this.connection?.state !== signalR.HubConnectionState.Connected)
      return Promise.reject(new Error('Not connected'));
    return this.connection.invoke<T>(method, ...args);
  }
}
