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
  loadError = '';

  pendingResetUser: User | null = null;
  pendingResetPassword = '';
  pendingResetError = '';

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
    this.loadError = '';
    this.userService.getUsers()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (users) => { this.users = users; },
        error: () => { this.loadError = 'Não foi possível carregar os usuários. Tente novamente.'; }
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
    this.errorMessage = '';
    this.userService.activateUser(user.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => this.loadUsers(),
        error: () => { this.errorMessage = `Não foi possível ativar "${user.fullName || user.username}". Tente novamente.`; }
      });
  }

  deactivate(user: User): void {
    this.errorMessage = '';
    this.userService.deactivateUser(user.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => this.loadUsers(),
        error: () => { this.errorMessage = `Não foi possível desativar "${user.fullName || user.username}". Tente novamente.`; }
      });
  }

  openResetPassword(user: User): void {
    this.pendingResetUser = user;
    this.pendingResetPassword = '';
    this.pendingResetError = '';
  }

  confirmResetPassword(): void {
    const user = this.pendingResetUser;
    if (!user) return;

    if (this.pendingResetPassword.length < 6) {
      this.pendingResetError = 'A senha deve ter pelo menos 6 caracteres.';
      return;
    }

    this.userService.resetPassword(user.id, this.pendingResetPassword)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.pendingResetUser = null;
          this.successMessage = `Senha de "${user.fullName || user.username}" redefinida com sucesso.`;
          this.errorMessage = '';
        },
        error: () => {
          this.pendingResetError = 'Não foi possível redefinir a senha. Tente novamente.';
        }
      });
  }

  cancelResetPassword(): void {
    this.pendingResetUser = null;
    this.pendingResetPassword = '';
    this.pendingResetError = '';
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
