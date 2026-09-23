import { Component, inject } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { MatButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';
import { MatToolbar } from '@angular/material/toolbar';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, RouterLink, MatToolbar, MatButton, MatIcon],
  templateUrl: './shell.html',
  styleUrl: './shell.scss',
})
export class Shell {
  protected readonly auth = inject(AuthService);

  protected logout(): void {
    this.auth.logout();
  }
}