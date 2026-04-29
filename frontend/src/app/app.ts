import { Component, computed, inject } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';

import { ThemeService } from './shared/services/theme.service';
import { AuthService } from './authentication/service/auth.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, ToastModule, ConfirmDialogModule],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  private readonly themeService = inject(ThemeService);
  private readonly authService = inject(AuthService);

  readonly isDark = computed(() => this.themeService.theme() === 'dark');
  readonly isAuthenticated = this.authService.isAuthenticated;
  readonly userName = this.authService.userName;

  toggleTheme(): void {
    this.themeService.toggle();
  }

  logout(): void {
    this.authService.logout();
  }
}
