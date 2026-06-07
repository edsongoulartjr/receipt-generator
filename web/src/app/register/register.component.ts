import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../auth.service';
import { Router, RouterLink } from '@angular/router';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [FormsModule, RouterLink],
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
        console.log('Cadastro realizado', response);
        this.router.navigate(['/login']);
      },
      error: (error) => {
        console.error('Falha no cadastro', error);
        alert(`Falha no cadastro: ${error.message || error.statusText || 'Erro desconhecido'}`);
      }
    });
  }
}
