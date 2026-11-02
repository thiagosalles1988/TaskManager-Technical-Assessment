import { TestBed } from '@angular/core/testing';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, Router } from '@angular/router';
import { AuthService } from './auth.service';
import { authInterceptor } from './auth.interceptor';
import { AuthResult } from './models';
import { HttpClient } from '@angular/common/http';

const session: AuthResult = {
  accessToken: 'test-token',
  expiresAtUtc: '2099-01-01T00:00:00Z',
  user: { id: 'user-1', email: 'test@example.test' },
};

describe('authentication boundary', () => {
  let http: HttpTestingController;
  it('stays usable when browser storage is blocked', () => {
    vi.spyOn(Storage.prototype, 'getItem').mockImplementation(() => {
      throw new DOMException('Blocked', 'SecurityError');
    });
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => {
      throw new DOMException('Blocked', 'SecurityError');
    });
    vi.spyOn(Storage.prototype, 'removeItem').mockImplementation(() => {
      throw new DOMException('Blocked', 'SecurityError');
    });
    try {
      const auth = TestBed.inject(AuthService);
      expect(auth.token).toBeNull();
      auth.login({ email: 'test@example.test', password: 'password' }).subscribe();
      http.expectOne('/api/auth/login').flush(session);
      expect(auth.token).toBe('test-token');
      auth.logout(false);
      expect(auth.token).toBeNull();
    } finally {
      vi.restoreAllMocks();
    }
  });
  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => {
    http.verify();
    sessionStorage.clear();
  });

  it('persists a successful session and clears it on logout', () => {
    const auth = TestBed.inject(AuthService);
    auth.login({ email: 'test@example.test', password: 'password' }).subscribe();
    http.expectOne('/api/auth/login').flush(session);
    expect(auth.token).toBe('test-token');
    expect(auth.user()?.email).toBe('test@example.test');
    expect(sessionStorage.getItem('taskmanager.session')).toContain('test-token');
    auth.logout(false);
    expect(auth.token).toBeNull();
    expect(sessionStorage.getItem('taskmanager.session')).toBeNull();
  });

  it('restores a valid session on reload', () => {
    sessionStorage.setItem('taskmanager.session', JSON.stringify(session));
    expect(TestBed.inject(AuthService).token).toBe('test-token');
  });

  it.each([
    'invalid-json',
    JSON.stringify({ ...session, expiresAtUtc: '2000-01-01T00:00:00Z' }),
    JSON.stringify({}),
  ])('discards unusable storage: %s', (value) => {
    sessionStorage.setItem('taskmanager.session', value);
    expect(TestBed.inject(AuthService).token).toBeNull();
    expect(sessionStorage.getItem('taskmanager.session')).toBeNull();
  });

  it('attaches bearer token only to the task API and never an external URL', () => {
    sessionStorage.setItem('taskmanager.session', JSON.stringify(session));
    const client = TestBed.inject(HttpClient);
    client.get('/api/tasks').subscribe();
    const api = http.expectOne('/api/tasks');
    expect(api.request.headers.get('Authorization')).toBe('Bearer test-token');
    api.flush([]);
    client.get('https://example.test/data').subscribe();
    const external = http.expectOne('https://example.test/data');
    expect(external.request.headers.has('Authorization')).toBe(false);
    external.flush({});
  });

  it('clears session and returns to login after a protected API 401', () => {
    sessionStorage.setItem('taskmanager.session', JSON.stringify(session));
    const auth = TestBed.inject(AuthService);
    const router = TestBed.inject(Router);
    const navigate = vi.spyOn(router, 'navigate').mockResolvedValue(true);
    TestBed.inject(HttpClient)
      .get('/api/tasks')
      .subscribe({ error: () => {} });
    http.expectOne('/api/tasks').flush({}, { status: 401, statusText: 'Unauthorized' });
    expect(auth.token).toBeNull();
    expect(navigate).toHaveBeenCalled();
  });
});
