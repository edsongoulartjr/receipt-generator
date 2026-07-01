import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../auth.service';
import { Router } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';

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
  isSubmitting = false;
  errorMessage = '';

  constructor(private authService: AuthService, private router: Router) { }

  onSubmit(): void {
    if (this.isSubmitting) {
      return;
    }

    this.isSubmitting = true;
    this.errorMessage = '';

    this.authService.login({ username: this.username, password: this.password }).subscribe({
      next: () => {
        this.isSubmitting = false;
        this.router.navigate(['/receipts']);
      },
      error: (error: HttpErrorResponse) => {
        this.isSubmitting = false;
        console.error('Falha no login', error);
        this.errorMessage = this.getLoginErrorMessage(error);
      }
    });
  }

  private getLoginErrorMessage(error: HttpErrorResponse): string {
    if (error.status === 0) {
      return 'A API ainda está iniciando. Aguarde alguns segundos e tente novamente.';
    }

    if (error.status === 401) {
      return 'Usuário ou senha inválidos.';
    }

    if (error.status === 429) {
      return 'Muitas tentativas de login. Aguarde um momento e tente novamente.';
    }

    return error.error?.message || 'Não foi possível entrar no sistema. Tente novamente.';
  }
}
