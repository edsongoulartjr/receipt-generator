import { Component, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../auth.service';

@Component({
  selector: 'app-navigation',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive],
  templateUrl: './navigation.component.html',
  styleUrl: './navigation.component.css'
})
export class NavigationComponent {
  menuOpen = false;
  userMenuOpen = false;

  constructor(public authService: AuthService) { }

  get displayName(): string {
    return this.authService.getFullName() ?? this.authService.getUsername() ?? 'Usuário';
  }

  get userInitials(): string {
    const name = this.authService.getFullName()?.trim();
    if (!name) return '?';
    const parts = name.split(/\s+/).filter(p => p.length > 0);
    if (parts.length >= 2) return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
    return parts[0].substring(0, 2).toUpperCase();
  }

  get roleLabel(): string {
    if (this.authService.isSystemAdmin()) return 'Administrador';
    if (this.authService.isCoopAdmin()) return 'Cooperativa';
    return 'Motorista';
  }

  get isAdmin(): boolean {
    return this.authService.isAdminOrAbove();
  }

  toggleMenu(): void {
    this.menuOpen = !this.menuOpen;
    if (this.menuOpen) this.userMenuOpen = false;
  }

  toggleUserMenu(): void {
    this.userMenuOpen = !this.userMenuOpen;
  }

  closeMenus(): void {
    this.menuOpen = false;
    this.userMenuOpen = false;
  }

  logout(): void {
    this.closeMenus();
    this.authService.logout();
  }

  @HostListener('document:click')
  onDocumentClick(): void {
    if (this.userMenuOpen) this.userMenuOpen = false;
  }

  @HostListener('document:keydown.escape')
  onEscapeKey(): void {
    this.menuOpen = false;
    this.userMenuOpen = false;
  }
}
