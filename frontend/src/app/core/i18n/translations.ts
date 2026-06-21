export type Lang = 'en' | 'es';

/** Flat key → { en, es }. Keys namespaced by feature. */
export const TRANSLATIONS: Record<string, { en: string; es: string }> = {
  // Common
  'common.email': { en: 'Email', es: 'Correo' },
  'common.password': { en: 'Password', es: 'Contraseña' },
  'common.cancel': { en: 'Cancel', es: 'Cancelar' },
  'common.loading': { en: 'Loading…', es: 'Cargando…' },
  'common.retry': { en: 'Retry', es: 'Reintentar' },
  'common.search': { en: 'Search', es: 'Buscar' },

  // Auth
  'auth.signIn': { en: 'Sign in', es: 'Iniciar sesión' },
  'auth.signUp': { en: 'Sign up', es: 'Crear cuenta' },
  'auth.signOut': { en: 'Sign out', es: 'Cerrar sesión' },
  'auth.welcomeBack': { en: 'Welcome back', es: 'Bienvenido de nuevo' },
  'auth.signInSubtitle': { en: 'Sign in to your team chat.', es: 'Entra a tu chat de equipo.' },
  'auth.createAccount': { en: 'Create your account', es: 'Crea tu cuenta' },
  'auth.signUpSubtitle': { en: 'Join the conversation in seconds.', es: 'Únete a la conversación en segundos.' },
  'auth.displayName': { en: 'Your name', es: 'Tu nombre' },
  'auth.noAccount': { en: "Don't have an account?", es: '¿No tienes cuenta?' },
  'auth.haveAccount': { en: 'Already have an account?', es: '¿Ya tienes cuenta?' },
  'auth.demoAccounts': { en: 'Demo accounts', es: 'Cuentas demo' },
  'auth.demoHint': {
    en: 'Click a role to autofill. Sign in twice (two tabs) to see real-time in action.',
    es: 'Haz clic en un rol para autocompletar. Entra en dos pestañas para ver el tiempo real.',
  },
  'auth.signingIn': { en: 'Signing in…', es: 'Entrando…' },
  'auth.creatingAccount': { en: 'Creating account…', es: 'Creando cuenta…' },

  // Shell / chat
  'chat.channels': { en: 'Channels', es: 'Canales' },
  'chat.directMessages': { en: 'Direct messages', es: 'Mensajes directos' },
  'chat.members': { en: 'Members', es: 'Miembros' },
  'chat.online': { en: 'Online', es: 'En línea' },
  'chat.away': { en: 'Away', es: 'Ausente' },
  'chat.offline': { en: 'Offline', es: 'Desconectado' },
  'chat.messagePlaceholder': { en: 'Message', es: 'Mensaje a' },
  'chat.typing': { en: 'is typing…', es: 'está escribiendo…' },
  'chat.typingMany': { en: 'are typing…', es: 'están escribiendo…' },
  'chat.noMessages': { en: 'No messages yet — say hi 👋', es: 'Aún no hay mensajes — saluda 👋' },
  'chat.pickChannel': { en: 'Pick a channel to start chatting', es: 'Elige un canal para empezar a chatear' },
  'chat.loadMore': { en: 'Load earlier messages', es: 'Cargar mensajes anteriores' },
  'chat.findMember': { en: 'Find a member…', es: 'Buscar miembro…' },
  'chat.connecting': { en: 'Connecting…', es: 'Conectando…' },
  'chat.reconnecting': { en: 'Reconnecting…', es: 'Reconectando…' },

  // Demo layer
  'demo.help': { en: 'How to explore', es: 'Cómo explorar' },
  'demo.intro': {
    en: 'ChatSphere is a portfolio demo: a real-time team chat with real auth, roles, presence and a Redis-backed SignalR backplane. Here’s how to get the most out of it.',
    es: 'ChatSphere es una demo de portfolio: un chat de equipo en tiempo real con auth, roles, presencia y backplane SignalR sobre Redis. Así le sacas el máximo.',
  },
  'demo.startTour': { en: 'Start guided tour', es: 'Iniciar tour guiado' },
  'demo.skip': { en: 'Skip', es: 'Saltar' },
  'demo.next': { en: 'Next', es: 'Siguiente' },
  'demo.back': { en: 'Back', es: 'Atrás' },
  'demo.finish': { en: 'Got it', es: 'Entendido' },
  'demo.whatsReal': { en: 'What’s real here', es: 'Qué es real aquí' },
  'demo.real1': { en: 'Live messages, typing & presence over WebSockets (SignalR).', es: 'Mensajes, "escribiendo…" y presencia en vivo por WebSockets (SignalR).' },
  'demo.real2': { en: 'JWT auth with rotating refresh tokens and per-server roles.', es: 'Auth JWT con refresh rotativo y roles por servidor.' },
  'demo.real3': { en: 'Redis backplane + presence; SQL Server message history.', es: 'Backplane Redis + presencia; historial de mensajes en SQL Server.' },
  'demo.real4': { en: 'Ambient activity is simulated so the demo feels alive solo.', es: 'La actividad ambiental está simulada para que la demo respire en solitario.' },
  'demo.twoTabs': {
    en: 'Open a second tab and sign in as another role to chat with yourself in real time.',
    es: 'Abre otra pestaña con otro rol para chatear contigo en tiempo real.',
  },
  'demo.demoAccounts': { en: 'Demo accounts', es: 'Cuentas demo' },
  'demo.shortcuts': { en: 'Keyboard shortcuts', es: 'Atajos de teclado' },

  // Roles
  'role.Member': { en: 'Member', es: 'Miembro' },
  'role.Admin': { en: 'Admin', es: 'Administrador' },
  'role.Owner': { en: 'Owner', es: 'Propietario' },

  // Errors
  'error.generic': { en: 'Something went wrong.', es: 'Algo salió mal.' },
  'error.network': { en: 'Cannot reach the server.', es: 'No se puede conectar con el servidor.' },
  'error.invalid_credentials': { en: 'Invalid email or password.', es: 'Correo o contraseña inválidos.' },
  'error.locked_out': { en: 'Account locked. Try again later.', es: 'Cuenta bloqueada. Inténtalo más tarde.' },
  'error.email_taken': { en: 'That email is already registered.', es: 'Ese correo ya está registrado.' },
};
