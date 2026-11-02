import { Component, DestroyRef, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Observable, finalize } from 'rxjs';
import { AuthService } from '../core/auth.service';
import { apiError } from '../core/api-error';

@Component({
  selector: 'app-auth',
  imports: [ReactiveFormsModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.Eager,
  template: ` <section class="auth-layout">
    <div class="auth-intro">
      <span class="eyebrow">A little clarity. Every day.</span>
      <h1>Make room for<br />what matters.</h1>
      <p>A focused space for your tasks, from the first idea to the final check.</p>
      <div class="intro-note">
        <span class="check-mark">✓</span>
        <div>
          <strong>Your work, at your pace.</strong><br />Plan it. Move it forward. Get it done.
        </div>
      </div>
    </div>
    <div class="panel auth-panel">
      <span class="eyebrow">{{ registration ? 'Get started' : 'Welcome back' }}</span>
      <h2>{{ registration ? 'Create your account' : 'Sign in to TaskManager' }}</h2>
      <p class="muted">
        {{
          registration
            ? 'Your tasks will be private to your account.'
            : 'Pick up where you left off.'
        }}
      </p>
      @if (registered) {
        <p class="notice success" role="status">Account created. Sign in to get started.</p>
      }
      @if (error()) {
        <p class="notice error" role="alert">{{ error() }}</p>
      }
      <form [formGroup]="form" (ngSubmit)="submit()" novalidate>
        <label for="email">Email address</label>
        <input
          id="email"
          type="email"
          formControlName="email"
          autocomplete="email"
          maxlength="254"
          placeholder="you@example.com"
          [attr.aria-invalid]="form.controls.email.touched && form.controls.email.invalid"
          aria-describedby="email-error"
        />
        <div id="email-error" class="field-error">
          @if (form.controls.email.touched && form.controls.email.invalid) {
            Enter a valid email address.
          }
        </div>
        <label for="password">Password</label>
        <input
          id="password"
          type="password"
          formControlName="password"
          [autocomplete]="registration ? 'new-password' : 'current-password'"
          maxlength="128"
          [attr.aria-invalid]="form.controls.password.touched && form.controls.password.invalid"
          aria-describedby="password-help"
        />
        <div id="password-help" class="field-help">
          {{ registration ? 'Use 12 to 128 characters.' : '' }}
          @if (form.controls.password.touched && form.controls.password.invalid) {
            <span class="field-error">{{
              registration ? 'Enter a password of at least 12 characters.' : 'Enter your password.'
            }}</span>
          }
        </div>
        <button class="button primary full" type="submit" [disabled]="pending()">
          {{ pending() ? 'Please wait…' : registration ? 'Create account' : 'Sign in' }}
        </button>
      </form>
      <p class="auth-switch">
        {{ registration ? 'Already have an account?' : 'New here?' }}
        <a [routerLink]="registration ? '/login' : '/register'">{{
          registration ? 'Sign in' : 'Create an account'
        }}</a>
      </p>
    </div>
  </section>`,
})
export class AuthComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly destroy = inject(DestroyRef);
  readonly registration = this.route.snapshot.data['registration'] === true;
  readonly registered = this.route.snapshot.queryParamMap.has('registered');
  readonly pending = signal(false);
  readonly error = signal('');
  readonly form = inject(FormBuilder).nonNullable.group({
    email: ['', [Validators.required, Validators.email, Validators.maxLength(254)]],
    password: [
      '',
      [
        Validators.required,
        Validators.maxLength(128),
        ...(this.registration ? [Validators.minLength(12)] : []),
      ],
    ],
  });
  submit(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid || this.pending()) return;
    this.pending.set(true);
    this.error.set('');
    const credentials = this.form.getRawValue();
    const request: Observable<unknown> = this.registration
      ? this.auth.register(credentials)
      : this.auth.login(credentials);
    request
      .pipe(
        takeUntilDestroyed(this.destroy),
        finalize(() => this.pending.set(false)),
      )
      .subscribe({
        next: () => {
          void this.router.navigate(
            [this.registration ? '/login' : '/tasks'],
            this.registration ? { queryParams: { registered: true } } : {},
          );
        },
        error: (error) => this.error.set(apiError(error)),
      });
  }
}
