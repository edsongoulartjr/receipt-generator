import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AuthService } from '../auth.service';

@Component({
  selector: 'app-navigation',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './navigation.component.html',
  styleUrl: './navigation.component.css'
})
export class NavigationComponent {
  constructor(public authService: AuthService) { }

  logout(): void {
    this.authService.logout();
  }
}
