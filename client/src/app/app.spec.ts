import { TestBed } from '@angular/core/testing';
import { signal, WritableSignal } from '@angular/core';
import { provideRouter } from '@angular/router';
import type { Session } from '@supabase/supabase-js';
import { of } from 'rxjs';
import { ApiHealthService } from './api-health.service';
import { AuthService } from './auth.service';
import { TargetRoleService } from './target-role.service';
import { Workspace } from './workspace';

describe('Workspace', () => {
  let apiHealthCheck: ReturnType<typeof vi.fn>;
  let session: WritableSignal<Session | null>;

  beforeEach(async () => {
    apiHealthCheck = vi.fn(() => of(undefined));
    session = signal<Session | null>(null);
    await TestBed.configureTestingModule({
      imports: [Workspace],
      providers: [
        provideRouter([]),
        {
          provide: ApiHealthService,
          useValue: {
            healthUrl: 'https://careergoat-api.example/health',
            check: apiHealthCheck,
          },
        },
        {
          provide: AuthService,
          useValue: {
            session,
            ready: signal(true),
            configurationError: signal(null),
            signIn: vi.fn(),
            signUp: vi.fn(),
            signInWithGoogle: vi.fn(),
            signOut: vi.fn(),
            getAccessToken: vi.fn().mockResolvedValue(null),
          },
        },
        {
          provide: TargetRoleService,
          useValue: {
            getProfile: vi.fn(() =>
              of({
                id: 'user-id',
                email: 'user@example.com',
                displayName: null,
                updatedAt: '',
              }),
            ),
            updateProfile: vi.fn(),
            list: vi.fn(() => of([])),
            get: vi.fn(),
            create: vi.fn(),
            update: vi.fn(),
            delete: vi.fn(),
          },
        },
      ],
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(Workspace);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('renders focused sign-in UI without workspace service details', async () => {
    const fixture = TestBed.createComponent(Workspace);
    await fixture.whenStable();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('h1')?.textContent).toContain('Welcome back.');
    expect(compiled.querySelector('.back-link')?.getAttribute('href')).toBe('/');
    expect(compiled.querySelector('.google-mark path[fill="#4285F4"]')).toBeTruthy();
    expect(compiled.querySelector('.status')).toBeNull();
    expect(compiled.textContent).not.toContain('Account deletion is not available');
    expect(apiHealthCheck).not.toHaveBeenCalled();
  });

  it('shows service details in the signed-in workspace', async () => {
    session.set({
      access_token: 'access-token',
      refresh_token: 'refresh-token',
      expires_in: 3600,
      token_type: 'bearer',
      user: {
        id: 'user-id',
        app_metadata: {},
        user_metadata: {},
        aud: 'authenticated',
        created_at: '',
      },
    });

    const fixture = TestBed.createComponent(Workspace);
    await fixture.whenStable();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.status')?.textContent).toContain('Connected');
    expect(compiled.textContent).toContain('Account deletion is not available');
    expect(apiHealthCheck).toHaveBeenCalledOnce();
  });
});
