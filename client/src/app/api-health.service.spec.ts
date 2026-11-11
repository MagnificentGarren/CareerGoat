import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { API_BASE_URL } from './api-config';
import { ApiHealthService } from './api-health.service';

describe('ApiHealthService', () => {
  let service: ApiHealthService;
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: API_BASE_URL, useValue: 'https://careergoat-api.example/' },
      ],
    });
    service = TestBed.inject(ApiHealthService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpTesting.verify());

  it('requests the configured health endpoint', () => {
    let completed = false;
    service.check().subscribe(() => (completed = true));

    const request = httpTesting.expectOne('https://careergoat-api.example/health');
    expect(request.request.method).toBe('GET');
    request.flush({ status: 'healthy' });

    expect(completed).toBe(true);
  });

  it('rejects a response that does not report healthy', () => {
    let receivedError: unknown;
    service.check().subscribe({ error: (error: unknown) => (receivedError = error) });
    httpTesting.expectOne('https://careergoat-api.example/health').flush({ status: 'degraded' });

    expect(receivedError).toBeInstanceOf(Error);
    if (!(receivedError instanceof Error)) {
      throw new Error('Expected the service to report an unhealthy API response.');
    }
    expect(receivedError.message).toBe('The API health endpoint returned an unhealthy status.');
  });
});
