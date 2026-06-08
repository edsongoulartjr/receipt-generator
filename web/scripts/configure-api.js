const fs = require('node:fs');
const path = require('node:path');

const defaultApiBaseUrl = '/api';
const configuredApiBaseUrl = (process.env.API_BASE_URL || defaultApiBaseUrl)
  .trim()
  .replace(/\/+$/, '');

const isRelativeApiUrl = configuredApiBaseUrl.startsWith('/');
const isAbsoluteApiUrl = /^https?:\/\//i.test(configuredApiBaseUrl);

if (!configuredApiBaseUrl || (!isRelativeApiUrl && !isAbsoluteApiUrl)) {
  throw new Error('API_BASE_URL must be an absolute HTTP(S) URL or a root-relative path.');
}

const outputPath = path.join(__dirname, '..', 'src', 'assets', 'runtime-config.js');
const output = `window.__APP_CONFIG__ = ${JSON.stringify({
  apiBaseUrl: configuredApiBaseUrl
})};\n`;

fs.writeFileSync(outputPath, output, 'utf8');
console.log(`Frontend API configured as ${configuredApiBaseUrl}`);
