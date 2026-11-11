import { Component, inject, signal } from '@angular/core';
import { ApiHealthService } from './api-health.service';

@Component({
  selector: 'app-root',
  styleUrl: './app.css',
  templateUrl: './app-shell.html',
})
export class App {
  private readonly apiHealth = inject(ApiHealthService);

  readonly apiHealthStatus = signal('Checking…');
  readonly apiHealthUrl = this.apiHealth.healthUrl;

  constructor() {
    this.apiHealth.check().subscribe({
      next: () => this.apiHealthStatus.set('Connected'),
      error: () => this.apiHealthStatus.set('Unavailable — check the API URL and CORS settings'),
    });
  }
}
