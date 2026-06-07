import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../auth.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './register.component.html',
  styleUrl: './register.component.css'
})
export class RegisterComponent {
  username = '';
  password = '';

  constructor(private authService: AuthService, private router: Router) { }

  onSubmit(): void {
    this.authService.register({ username: this.username, password: this.password }).subscribe({
      next: (response) => {
        console.log('Registration successful', response);
        this.router.navigate(['/login']); // Redirect to login page after successful registration
      },
      error: (error) => {
        console.error('Registration failed', error);
        // Display error message to the user
      }
    });
  }
}
