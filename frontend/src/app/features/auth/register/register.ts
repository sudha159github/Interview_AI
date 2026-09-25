import { Component, inject, signal } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { getErrorMessage } from '../../../core/utils/api-error';
/** Form-level rule: the two password fields must match. */
function passwordsMatch(group: AbstractControl): ValidationErrors | null {
const password = group.get('password')?.value;
const confirm = group.get('confirmPassword')?.value;
return password && confirm && password !== confirm ? { passwordsMismatch: true } : null;
}
@Component({
selector: 'app-register',
imports: [ReactiveFormsModule, RouterLink],
templateUrl: './register.html',
styleUrl: '../login/login.scss',
})
export class Register {
private readonly fb = inject(FormBuilder);
private readonly auth = inject(AuthService);
private readonly router = inject(Router);
protected readonly loading = signal(false);
protected readonly errorMessage = signal<string | null>(null);
protected readonly form = this.fb.nonNullable.group(
{
userName: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(50),
Validators.pattern(/^[a-zA-Z0-9._-]+$/)]],
email: ['', [Validators.required, Validators.email]],
password: ['', [Validators.required, Validators.minLength(8)]],
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
// confirmPassword is a client-side check only: it is never sent
const { userName, email, password } = this.form.getRawValue();
this.auth.register({ userName, email, password }).subscribe({
next: () => this.router.navigateByUrl('/'),
error: (error: unknown) => {
this.errorMessage.set(getErrorMessage(error));
this.loading.set(false);
},
});
}
}