import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { DmDto, MessagePageDto, SearchResultDto, ServerDetailDto, ServerSummaryDto } from '../models/models';

/** REST read-side of chat. Real-time writes go through the SignalR RealtimeService. */
@Injectable({ providedIn: 'root' })
export class ChatApi {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBase}/api`;

  servers(): Observable<ServerSummaryDto[]> {
    return this.http.get<ServerSummaryDto[]>(`${this.base}/servers`);
  }

  server(serverId: string): Observable<ServerDetailDto> {
    return this.http.get<ServerDetailDto>(`${this.base}/servers/${serverId}`);
  }

  messages(channelId: string, before?: string, take = 30): Observable<MessagePageDto> {
    let url = `${this.base}/channels/${channelId}/messages?take=${take}`;
    if (before) url += `&before=${encodeURIComponent(before)}`;
    return this.http.get<MessagePageDto>(url);
  }

  markRead(channelId: string, lastMessageId: string): Observable<void> {
    return this.http.post<void>(`${this.base}/channels/${channelId}/read`, { lastMessageId });
  }

  dms(): Observable<DmDto[]> {
    return this.http.get<DmDto[]>(`${this.base}/dms`);
  }

  openDm(otherUserId: string): Observable<DmDto> {
    return this.http.post<DmDto>(`${this.base}/dms`, { otherUserId });
  }

  search(serverId: string, q: string): Observable<SearchResultDto[]> {
    return this.http.get<SearchResultDto[]>(`${this.base}/servers/${serverId}/search?q=${encodeURIComponent(q)}`);
  }
}
