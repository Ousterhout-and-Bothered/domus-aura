import { Component, computed, inject, signal } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';

import { ThemeService } from './shared/services/theme.service';
import { AuthService } from './authentication/service/auth.service';
import { ChatPanel } from './chat/components/chat-panel/chat-panel';

@Component({
  selector: 'app-root',
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    ToastModule,
    ConfirmDialogModule,
    ChatPanel,
  ],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  private readonly themeService = inject(ThemeService);
  private readonly authService = inject(AuthService);

  readonly isDark = computed(() => this.themeService.theme() === 'dark');
  readonly isAuthenticated = this.authService.isAuthenticated;
  readonly userName = this.authService.userName;

  /**
   * Visibility of the floating chat panel. Owned at the root so the
   * transcript persists across route changes (Devices ↔ Scenes) for
   * the lifetime of the session.
   */
  readonly chatOpen = signal(false);

  toggleTheme(): void {
    this.themeService.toggle();
  }

  toggleChat(): void {
    this.chatOpen.update(v => !v);
  }

  closeChat(): void {
    this.chatOpen.set(false);
  }

  logout(): void {
    this.authService.logout();
  }
}
