import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../auth.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css'
})
export class LoginComponent {
  username = '';
  password = '';

  constructor(private authService: AuthService, private router: Router) { }

  onSubmit(): void {
    this.authService.login({ username: this.username, password: this.password }).subscribe({
      next: (response) => {
        console.log('Login successful', response);
        this.router.navigate(['/clients']); // Redirect to clients page on successful login
      },
      error: (error) => {
        console.error('Login failed', error);
        alert(`Login failed: ${error.message || error.statusText || 'Unknown error'}`);
      }
    });
  }
}
