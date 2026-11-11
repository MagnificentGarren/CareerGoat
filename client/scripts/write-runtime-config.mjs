import { mkdir, writeFile } from 'node:fs/promises';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const clientDirectory = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const configuredUrl = process.env.CAREERGOAT_API_BASE_URL;

if (process.env.VERCEL === '1' && !configuredUrl) {
  throw new Error('CAREERGOAT_API_BASE_URL must be set for Vercel builds.');
}

const apiBaseUrl = configuredUrl || 'http://localhost:5219';
let parsedUrl;

try {
  parsedUrl = new URL(apiBaseUrl);
} catch {
  throw new Error('CAREERGOAT_API_BASE_URL must be an absolute URL.');
}

const isLocalHttp =
  parsedUrl.protocol === 'http:' &&
  ['localhost', '127.0.0.1', '[::1]'].includes(parsedUrl.hostname);

if (
  (parsedUrl.protocol !== 'https:' && !isLocalHttp) ||
  parsedUrl.pathname !== '/' ||
  parsedUrl.username ||
  parsedUrl.password ||
  parsedUrl.search ||
  parsedUrl.hash
) {
  throw new Error('CAREERGOAT_API_BASE_URL must use HTTPS, except for a local development URL.');
}

const outputPath = resolve(clientDirectory, 'dist/client/browser/runtime-config.js');
await mkdir(dirname(outputPath), { recursive: true });
await writeFile(
  outputPath,
  `window.careerGoatConfig = ${JSON.stringify({ apiBaseUrl: apiBaseUrl.replace(/\/+$/, '') })};\n`,
  'utf8',
);
