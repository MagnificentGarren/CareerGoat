import { InjectionToken } from '@angular/core';

declare global {
  interface Window {
    careerGoatConfig?: {
      apiBaseUrl?: string;
    };
  }
}

export const API_BASE_URL = new InjectionToken<string>('API_BASE_URL', {
  providedIn: 'root',
  factory: () => {
    const apiBaseUrl = window.careerGoatConfig?.apiBaseUrl?.replace(/\/+$/, '');

    if (!apiBaseUrl) {
      throw new Error('The public API base URL has not been configured.');
    }

    return apiBaseUrl;
  },
});
