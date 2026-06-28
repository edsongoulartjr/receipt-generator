import type { CapacitorConfig } from '@capacitor/cli';

const config: CapacitorConfig = {
  appId: 'br.com.coopertaxijundiaisp.receipts',
  appName: 'Coopertáxi Recibos',
  webDir: 'dist/receipt-generator.frontend/browser',
  android: {
    minWebViewVersion: 80
  },
  plugins: {
    SplashScreen: {
      launchShowDuration: 0
    }
  }
};

export default config;
