import { Component, DestroyRef, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CreateUserRequest, User, UserService } from '../user.service';
import { AuthService } from '../auth.service';

@Component({
  selector: 'app-users',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './users.component.html',
  styleUrl: './users.component.css'
})
export class UsersComponent implements OnInit {
  private readonly destroyRef = inject(DestroyRef);

  users: User[] = [];
  currentUser: CreateUserRequest = this.emptyUser();
  errorMessage = '';
  successMessage = '';

  constructor(private userService: UserService, public authService: AuthService) { }

  get roles(): CreateUserRequest['role'][] {
    return this.authService.isSystemAdmin()
      ? ['Driver', 'CoopAdmin', 'SystemAdmin']
      : ['Driver'];
  }

  ngOnInit(): void {
    this.loadUsers();
  }

  loadUsers(): void {
    this.userService.getUsers()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (users) => { this.users = users; },
        error: (err) => { console.error('Erro ao buscar usuários', err); }
      });
  }

  createUser(): void {
    this.errorMessage = '';
    this.successMessage = '';

    this.userService.createUser(this.currentUser)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.currentUser = this.emptyUser();
          this.successMessage = 'Usuário criado com sucesso.';
          this.loadUsers();
        },
        error: (err: HttpErrorResponse) => {
          console.error('Erro ao criar usuário', err);
          this.errorMessage = this.getCreateErrorMessage(err);
        }
      });
  }

  activate(user: User): void {
    this.userService.activateUser(user.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => this.loadUsers(),
        error: (err) => console.error('Erro ao ativar usuário', err)
      });
  }

  deactivate(user: User): void {
    this.userService.deactivateUser(user.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => this.loadUsers(),
        error: (err) => console.error('Erro ao desativar usuário', err)
      });
  }

  resetPassword(user: User): void {
    const newPassword = window.prompt(`Nova senha para "${user.fullName || user.username}" (mínimo 6 caracteres):`);
    if (!newPassword) return;
    if (newPassword.length < 6) {
      alert('A senha deve ter pelo menos 6 caracteres.');
      return;
    }

    this.userService.resetPassword(user.id, newPassword)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.successMessage = `Senha de "${user.fullName || user.username}" redefinida com sucesso.`;
          this.errorMessage = '';
        },
        error: (err) => {
          console.error('Erro ao redefinir senha', err);
          this.errorMessage = 'Não foi possível redefinir a senha. Tente novamente.';
          this.successMessage = '';
        }
      });
  }

  trackByUserId(_index: number, user: User): number {
    return user.id;
  }

  trackByRole(_index: number, role: string): string {
    return role;
  }

  roleLabel(role: string): string {
    switch (role) {
      case 'SystemAdmin': return 'Administrador do sistema';
      case 'CoopAdmin': return 'Administrador da cooperativa';
      case 'Driver': return 'Motorista';
      default: return role;
    }
  }

  private emptyUser(): CreateUserRequest {
    return {
      username: '',
      password: '',
      role: 'Driver',
      fullName: ''
    };
  }

  private getCreateErrorMessage(error: HttpErrorResponse): string {
    if (error.status === 401 || error.status === 403) {
      return 'Sua sessão não possui permissão para realizar esta ação. Saia e entre novamente no sistema.';
    }

    if (error.status === 409) {
      return error.error?.message ?? 'Este nome de usuário já está cadastrado.';
    }

    const validationErrors = error.error?.errors;
    if (validationErrors) {
      const messages = Object.values(validationErrors).flat() as string[];
      if (messages.length > 0) {
        return messages[0];
      }
    }

    return error.error?.message ?? 'Não foi possível criar o usuário. Revise os dados informados.';
  }
}
