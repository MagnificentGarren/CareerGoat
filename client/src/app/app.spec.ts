import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { ApiHealthService } from './api-health.service';
import { AuthService } from './auth.service';
import { TargetRoleService } from './target-role.service';
import { Workspace } from './workspace';

describe('Workspace', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Workspace],
      providers: [
        provideRouter([]),
        {
          provide: ApiHealthService,
          useValue: {
            healthUrl: 'https://careergoat-api.example/health',
            check: () => of(undefined),
          },
        },
        {
          provide: AuthService,
          useValue: {
            session: signal(null),
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
            getProfile: vi.fn(),
            updateProfile: vi.fn(),
            list: vi.fn(),
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

  it('should render the staging shell and configured API status', async () => {
    const fixture = TestBed.createComponent(Workspace);
    await fixture.whenStable();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('h1')?.textContent).toContain('Build your next move.');
    expect(compiled.textContent).toContain('Fake · no external calls');
    expect(compiled.textContent).toContain('Connected');
    expect(compiled.querySelector('a[href*="/health"]')?.getAttribute('href')).toBe(
      'https://careergoat-api.example/health',
    );
  });
});
