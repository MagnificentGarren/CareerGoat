import { Component, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { ApiHealthService } from './api-health.service';
import { AuthService } from './auth.service';
import { Profile, TargetRole, TargetRoleRequest, TargetRoleService } from './target-role.service';

@Component({
  selector: 'app-root',
  imports: [FormsModule],
  styleUrl: './app.css',
  templateUrl: './app-shell.html',
})
export class App {
  private readonly apiHealth = inject(ApiHealthService);
  readonly auth = inject(AuthService);
  private readonly data = inject(TargetRoleService);

  readonly apiHealthStatus = signal('Checking…');
  readonly apiHealthUrl = this.apiHealth.healthUrl;
  readonly profile = signal<Profile | null>(null);
  readonly targetRoles = signal<TargetRole[]>([]);
  readonly selectedRole = signal<TargetRole | null>(null);
  readonly loadingAccount = signal(false);
  readonly saving = signal(false);
  readonly isSignUp = signal(false);
  readonly errorMessage = signal('');
  readonly statusMessage = signal('');
  displayName = '';
  readonly roleDraft: TargetRoleRequest = { title: '', company: '', description: '' };
  editingRoleId: string | null = null;
  email = '';
  password = '';
  editorOpen = false;
  private accountLoadId = 0;

  constructor() {
    this.apiHealth.check().subscribe({
      next: () => this.apiHealthStatus.set('Connected'),
      error: () => this.apiHealthStatus.set('Unavailable — check the API URL and CORS settings'),
    });

    effect(() => {
      if (!this.auth.ready()) return;
      if (this.auth.session()) {
        void this.loadAccountData();
      } else {
        this.accountLoadId++;
        this.loadingAccount.set(false);
        this.profile.set(null);
        this.targetRoles.set([]);
        this.selectedRole.set(null);
        this.editorOpen = false;
      }
    });
  }

  async submitAuth(): Promise<void> {
    this.errorMessage.set('');
    this.statusMessage.set('');
    this.saving.set(true);
    try {
      if (this.isSignUp()) {
        const signedIn = await this.auth.signUp(this.email, this.password);
        if (!signedIn) {
          this.statusMessage.set('Check your email to confirm your account, then sign in.');
        }
      } else {
        await this.auth.signIn(this.email, this.password);
      }
    } catch (error) {
      this.errorMessage.set(this.errorText(error));
    } finally {
      this.saving.set(false);
    }
  }

  async signInWithGoogle(): Promise<void> {
    this.errorMessage.set('');
    try {
      await this.auth.signInWithGoogle();
    } catch (error) {
      this.errorMessage.set(this.errorText(error));
    }
  }

  async signOut(): Promise<void> {
    this.errorMessage.set('');
    try {
      await this.auth.signOut();
    } catch (error) {
      this.errorMessage.set(this.errorText(error));
    }
  }

  async saveProfile(): Promise<void> {
    this.saving.set(true);
    this.errorMessage.set('');
    try {
      this.profile.set(await firstValueFrom(this.data.updateProfile(this.displayName)));
      this.statusMessage.set('Profile saved.');
    } catch (error) {
      this.errorMessage.set(this.errorText(error));
    } finally {
      this.saving.set(false);
    }
  }

  openRole(role: TargetRole): void {
    this.errorMessage.set('');
    this.selectedRole.set(null);
    firstValueFrom(this.data.get(role.id))
      .then((detail) => this.selectedRole.set(detail))
      .catch((error: unknown) => this.errorMessage.set(this.errorText(error)));
  }

  startCreateRole(): void {
    this.editingRoleId = null;
    this.resetRoleDraft();
    this.selectedRole.set(null);
    this.editorOpen = true;
  }

  startEditRole(role: TargetRole): void {
    this.editingRoleId = role.id;
    this.roleDraft.title = role.title;
    this.roleDraft.company = role.company ?? '';
    this.roleDraft.description = role.description;
    this.selectedRole.set(null);
    this.editorOpen = true;
  }

  async saveRole(): Promise<void> {
    this.saving.set(true);
    this.errorMessage.set('');
    this.statusMessage.set('');
    const request = {
      title: this.roleDraft.title.trim(),
      company: this.roleDraft.company?.trim() || null,
      description: this.roleDraft.description.trim(),
    };
    try {
      const saved = this.editingRoleId
        ? await firstValueFrom(this.data.update(this.editingRoleId, request))
        : await firstValueFrom(this.data.create(request));
      this.editorOpen = false;
      this.selectedRole.set(saved);
      this.statusMessage.set(this.editingRoleId ? 'Target role updated.' : 'Target role created.');
      await this.reloadRoles();
    } catch (error) {
      this.errorMessage.set(this.errorText(error));
    } finally {
      this.saving.set(false);
    }
  }

  async deleteRole(role: TargetRole): Promise<void> {
    if (!window.confirm(`Delete "${role.title}"? This cannot be undone.`)) return;
    this.errorMessage.set('');
    try {
      await firstValueFrom(this.data.delete(role.id));
      if (this.selectedRole()?.id === role.id) this.selectedRole.set(null);
      this.statusMessage.set('Target role deleted.');
      await this.reloadRoles();
    } catch (error) {
      this.errorMessage.set(this.errorText(error));
    }
  }

  cancelRoleEdit(): void {
    this.editorOpen = false;
    this.resetRoleDraft();
  }

  private async loadAccountData(): Promise<void> {
    const userId = this.auth.session()?.user.id;
    if (!userId) return;
    const loadId = ++this.accountLoadId;
    this.loadingAccount.set(true);
    this.errorMessage.set('');
    try {
      const [profile, roles] = await Promise.all([
        firstValueFrom(this.data.getProfile()),
        firstValueFrom(this.data.list()),
      ]);
      if (this.accountLoadId !== loadId || this.auth.session()?.user.id !== userId) return;
      this.profile.set(profile);
      this.displayName = profile.displayName ?? '';
      this.targetRoles.set(roles);
    } catch (error) {
      this.errorMessage.set(this.errorText(error));
    } finally {
      if (this.accountLoadId === loadId) this.loadingAccount.set(false);
    }
  }

  private async reloadRoles(): Promise<void> {
    this.targetRoles.set(await firstValueFrom(this.data.list()));
  }

  private resetRoleDraft(): void {
    this.roleDraft.title = '';
    this.roleDraft.company = '';
    this.roleDraft.description = '';
  }

  private errorText(error: unknown): string {
    return error instanceof Error ? error.message : 'Something went wrong. Please try again.';
  }
}
