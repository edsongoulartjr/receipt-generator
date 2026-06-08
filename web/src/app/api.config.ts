declare global {
  interface Window {
    __APP_CONFIG__?: {
      apiBaseUrl?: string;
    };
  }
}

export const API_BASE_URL = window.__APP_CONFIG__?.apiBaseUrl || '/api';
