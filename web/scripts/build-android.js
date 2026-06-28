#!/usr/bin/env node
/**
 * build-android.js
 * Gera o build Angular com a URL da API de producao e sincroniza com o projeto Android.
 *
 * Uso:
 *   API_BASE_URL=https://sua-api.onrender.com/api npm run build:android   (Linux/macOS)
 *   $env:API_BASE_URL="https://sua-api.onrender.com/api"; npm run build:android  (PowerShell)
 */

const { execSync } = require('child_process');

const apiBaseUrl = process.env.API_BASE_URL;

if (!apiBaseUrl) {
  console.error('\nErro: a variavel de ambiente API_BASE_URL nao foi definida.\n');
  console.error('Linux/macOS:');
  console.error('  API_BASE_URL=https://sua-api.onrender.com/api npm run build:android\n');
  console.error('PowerShell:');
  console.error('  $env:API_BASE_URL="https://sua-api.onrender.com/api"; npm run build:android\n');
  process.exit(1);
}

if (!apiBaseUrl.startsWith('https://') && !apiBaseUrl.startsWith('http://')) {
  console.error('\nErro: API_BASE_URL deve ser uma URL absoluta (https://...).\n');
  process.exit(1);
}

console.log(`\nBuild Android — API: ${apiBaseUrl}\n`);

try {
  execSync(`node scripts/configure-api.js`, { stdio: 'inherit', env: process.env });
  execSync(`npx ng build --configuration=production`, { stdio: 'inherit' });
  execSync(`npx cap sync android`, { stdio: 'inherit' });
  console.log('\nBuild e sync concluidos. Abra o Android Studio para gerar a APK:\n');
  console.log('  npm run cap:open\n');
} catch (err) {
  console.error('\nFalha no build:', err.message);
  process.exit(1);
}
