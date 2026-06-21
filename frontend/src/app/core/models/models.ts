/** Mirrors of the backend DTOs (ChatSphere.Application.*). */

export type ServerRole = 'Member' | 'Admin' | 'Owner';
export type MessageType = 'Text' | 'System';

export interface UserDto {
  id: string;
  email: string;
  displayName: string;
  avatarColor: string;
  title: string | null;
}

export interface UserMiniDto {
  id: string;
  displayName: string;
  avatarColor: string;
  title: string | null;
}

export interface AuthTokens {
  accessToken: string;
  accessTokenExpiresAt: string;
  refreshToken: string;
  refreshTokenExpiresAt: string;
}

export interface AuthResponse {
  user: UserDto;
  tokens: AuthTokens;
}

export interface MeResponse {
  user: UserDto;
}

export interface ServerSummaryDto {
  id: string;
  name: string;
  slug: string;
  role: ServerRole;
  unreadTotal: number;
  hasMention: boolean;
}

export interface ChannelDto {
  id: string;
  name: string | null;
  topic: string | null;
  isPrivate: boolean;
  isDirect: boolean;
  position: number;
  unread: number;
  hasMention: boolean;
  lastMessagePreview: string | null;
  lastMessageAt: string | null;
}

export interface MemberDto {
  user: UserMiniDto;
  role: ServerRole;
}

export interface ServerDetailDto {
  id: string;
  name: string;
  slug: string;
  role: ServerRole;
  channels: ChannelDto[];
  members: MemberDto[];
}

export interface ReactionGroupDto {
  emoji: string;
  count: number;
  mine: boolean;
}

export interface AttachmentDto {
  id: string;
  url: string;
  fileName: string;
  contentType: string;
  width: number | null;
  height: number | null;
}

export interface MessageDto {
  id: string;
  channelId: string;
  author: UserMiniDto;
  body: string;
  type: MessageType;
  createdAt: string;
  editedAt: string | null;
  reactions: ReactionGroupDto[];
  attachments: AttachmentDto[];
  mentionsMe: boolean;
}

export interface MessagePageDto {
  messages: MessageDto[];
  hasMore: boolean;
  nextBefore: string | null;
}

export interface DmDto {
  channelId: string;
  other: UserMiniDto;
  unread: number;
  lastMessagePreview: string | null;
  lastMessageAt: string | null;
}

export interface SearchResultDto {
  channelId: string;
  channelName: string | null;
  message: MessageDto;
}

// ── Realtime payloads (SignalR) ──
export interface ReactionUserGroup {
  emoji: string;
  count: number;
  userIds: string[];
}
export interface ReactionStateDto {
  channelId: string;
  messageId: string;
  groups: ReactionUserGroup[];
}
export interface TypingDto {
  channelId: string;
  userId: string;
  displayName: string;
}
export interface PresenceDto {
  userId: string;
  online: boolean;
}
export interface ReadReceiptDto {
  channelId: string;
  userId: string;
  lastMessageId: string;
  at: string;
}
export interface MentionNotificationDto {
  channelId: string;
  channelName: string | null;
  message: MessageDto;
}

export interface ApiError {
  code: string;
  message: string;
  errors?: Record<string, string[]> | null;
}
