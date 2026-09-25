import { Component, inject } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
@Component({
selector: 'app-shell',
imports: [RouterOutlet, RouterLink],
templateUrl: './shell.html',
styleUrl: './shell.scss',
})
export class Shell {
protected readonly auth = inject(AuthService);
protected logout(): void {
this.auth.logout();
}
protected signInAgain(): void {
this.auth.logout();
}
}