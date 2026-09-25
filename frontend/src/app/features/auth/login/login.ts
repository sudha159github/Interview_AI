import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { getErrorMessage } from '../../../core/utils/api-error';
@Component({
selector: 'app-login',
imports: [ReactiveFormsModule, RouterLink],
templateUrl: './login.html',
styleUrl: './login.scss',
})
export class Login {
private readonly fb = inject(FormBuilder);
private readonly auth = inject(AuthService);
private readonly router = inject(Router);
private readonly route = inject(ActivatedRoute);
protected readonly loading = signal(false);
protected readonly errorMessage = signal<string | null>(null);
protected readonly form = this.fb.nonNullable.group({
email: ['', [Validators.required, Validators.email]],
password: ['', [Validators.required]],
});
protected submit(): void {
if (this.form.invalid) {
this.form.markAllAsTouched();
return;
}
this.loading.set(true);
this.errorMessage.set(null);
this.auth.login(this.form.getRawValue()).subscribe({
next: () => this.router.navigateByUrl(this.safeReturnUrl()),
error: (error: unknown) => {
this.errorMessage.set(getErrorMessage(error));
this.loading.set(false);
},
});
}
/**
* Where to go after sign-in. Only in-app paths like "/reports/123" are allowed,
* never external URLs (open-redirect protection).
*/
private safeReturnUrl(): string {
const url = this.route.snapshot.queryParamMap.get('returnUrl');
return url && url.startsWith('/') && !url.startsWith('//') ? url : '/';
}
}