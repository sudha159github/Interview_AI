import { Component, inject, signal } from '@angular/core';
import {
  AbstractControl,
  FormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  ValidatorFn,
  Validators,
} from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatButton } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinner } from '@angular/material/progress-spinner';
import { AuthService } from '../../../core/services/auth.service';
import { getErrorMessage } from '../../../core/utils/api-error';

/** Form-level rule: "password" and "confirmPassword" must be the same. */
const passwordsMatch: ValidatorFn = (group: AbstractControl): ValidationErrors | null => {
  const password = group.get('password')?.value;
  const confirm = group.get('confirmPassword')?.value;

  return password && confirm && password !== confirm ? { passwordsMismatch: true } : null;
};

@Component({
  selector: 'app-register',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatButton,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinner,
  ],
  templateUrl: './register.html',
  styleUrl: './register.scss',
})
export class Register {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly loading = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  // The same rules as the API, so users get instant feedback
  protected readonly form = this.fb.nonNullable.group(
    {
      userName: [
        '',
        [
          Validators.required,
          Validators.minLength(3),
          Validators.maxLength(50),
          Validators.pattern(/^[a-zA-Z0-9_.-]+$/),
        ],
      ],
      email: ['', [Validators.required, Validators.email, Validators.maxLength(256)]],
      password: [
        '',
        [
          Validators.required,
          Validators.minLength(8),
          Validators.maxLength(100),
          // at least one lowercase letter, one uppercase letter and one digit
          Validators.pattern(/(?=.*[a-z])(?=.*[A-Z])(?=.*\d)/),
        ],
      ],
      confirmPassword: ['', [Validators.required]],
    },
    { validators: passwordsMatch },
  );

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    this.errorMessage.set(null);

    // Send only what the API expects: confirmPassword stays in the browser
    const { userName, email, password } = this.form.getRawValue();

    this.auth.register({ userName, email, password }).subscribe({
      next: () => this.router.navigate(['/']),
      error: (error: unknown) => {
        this.errorMessage.set(getErrorMessage(error));
        this.loading.set(false);
      },
    });
  }
}