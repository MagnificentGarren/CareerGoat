import { Injectable, signal } from '@angular/core';
import type { Session, SupabaseClient } from '@supabase/supabase-js';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private client: SupabaseClient | null = null;
  readonly session = signal<Session | null>(null);
  readonly ready = signal(false);
  readonly configurationError = signal<string | null>(null);

  constructor() {
    const config = window.careerGoatConfig;
    const supabaseUrl = config?.supabaseUrl;
    const supabaseAnonKey = config?.supabaseAnonKey;
    if (!supabaseUrl || !supabaseAnonKey) {
      this.configurationError.set(
        'Supabase Auth is not configured. Set the public Supabase URL and anon key in runtime configuration.',
      );
      this.ready.set(true);
      return;
    }

    void import('@supabase/supabase-js')
      .then(({ createClient }) => {
        this.client = createClient(supabaseUrl, supabaseAnonKey);
        this.client.auth.onAuthStateChange((_event, session) => this.session.set(session));
        return this.client.auth.getSession();
      })
      .then(({ data, error }) => {
        if (error) {
          console.error('Unable to restore the Supabase session.', error);
        } else {
          this.session.set(data.session);
        }
      })
      .catch((error: unknown) => {
        console.error('Unable to initialize Supabase Auth.', error);
        this.configurationError.set(
          'Unable to initialize Supabase Auth. Check the client configuration.',
        );
      })
      .finally(() => this.ready.set(true));
  }

  async signIn(email: string, password: string): Promise<void> {
    const { error } = await this.requireClient().auth.signInWithPassword({ email, password });
    if (error) throw error;
  }

  async signUp(email: string, password: string): Promise<boolean> {
    const { data, error } = await this.requireClient().auth.signUp({
      email,
      password,
      options: { emailRedirectTo: window.location.origin },
    });
    if (error) throw error;
    return data.session !== null;
  }

  async signInWithGoogle(): Promise<void> {
    const { error } = await this.requireClient().auth.signInWithOAuth({
      provider: 'google',
      options: { redirectTo: `${window.location.origin}/app` },
    });
    if (error) throw error;
  }

  async signOut(): Promise<void> {
    const { error } = await this.requireClient().auth.signOut();
    if (error) throw error;
  }

  async getAccessToken(): Promise<string | null> {
    const { data, error } = await this.requireClient().auth.getSession();
    if (error) throw error;
    return data.session?.access_token ?? null;
  }

  private requireClient(): SupabaseClient {
    if (!this.client) {
      throw new Error(this.configurationError() ?? 'Supabase Auth is not ready.');
    }
    return this.client;
  }
}
