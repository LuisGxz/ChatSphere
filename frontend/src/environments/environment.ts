/** Runtime config. apiBase/hubUrl are rewritten at build time for production deploys (F7). */
export const environment = {
  production: false,
  apiBase: 'http://localhost:5191',
  hubUrl: 'http://localhost:5191/hubs/chat',
};
