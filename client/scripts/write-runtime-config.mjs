import { mkdir, writeFile } from 'node:fs/promises';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const clientDirectory = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const localDevelopment = process.argv.includes('--local');
const configuredUrl = process.env.CAREERGOAT_API_BASE_URL;
const supabaseUrl = process.env.CAREERGOAT_SUPABASE_URL || '';
const supabaseAnonKey = process.env.CAREERGOAT_SUPABASE_ANON_KEY || '';

if (process.env.VERCEL === '1' && !configuredUrl) {
  throw new Error('CAREERGOAT_API_BASE_URL must be set for Vercel builds.');
}
if (process.env.VERCEL === '1' && (!supabaseUrl || !supabaseAnonKey)) {
  throw new Error(
    'CAREERGOAT_SUPABASE_URL and CAREERGOAT_SUPABASE_ANON_KEY must be set for Vercel builds.',
  );
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

if (supabaseUrl) {
  let parsedSupabaseUrl;
  try {
    parsedSupabaseUrl = new URL(supabaseUrl);
  } catch {
    throw new Error('CAREERGOAT_SUPABASE_URL must be an absolute URL.');
  }
  const isLocalSupabase =
    parsedSupabaseUrl.protocol === 'http:' &&
    ['localhost', '127.0.0.1', '[::1]'].includes(parsedSupabaseUrl.hostname);
  if (
    (parsedSupabaseUrl.protocol !== 'https:' && !isLocalSupabase) ||
    (parsedSupabaseUrl.pathname !== '/' && parsedSupabaseUrl.pathname !== '') ||
    parsedSupabaseUrl.username ||
    parsedSupabaseUrl.password ||
    parsedSupabaseUrl.search ||
    parsedSupabaseUrl.hash
  ) {
    throw new Error('CAREERGOAT_SUPABASE_URL must use HTTPS, except for a local development URL.');
  }
}
if (supabaseAnonKey && !supabaseAnonKey.trim()) {
  throw new Error('CAREERGOAT_SUPABASE_ANON_KEY must not be blank.');
}

const outputPath = localDevelopment
  ? resolve(clientDirectory, 'public/runtime-config.local.js')
  : resolve(clientDirectory, 'dist/client/browser/runtime-config.js');
await mkdir(dirname(outputPath), { recursive: true });
await writeFile(
  outputPath,
  `window.careerGoatConfig = ${JSON.stringify({
    apiBaseUrl: apiBaseUrl.replace(/\/+$/, ''),
    supabaseUrl: supabaseUrl.replace(/\/+$/, ''),
    supabaseAnonKey,
  })};\n`,
  'utf8',
);
