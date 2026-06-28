import { Component, DestroyRef, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CreateUserRequest, User, UserService } from '../user.service';

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
  roles: CreateUserRequest['role'][] = ['Operator', 'SuperAdmin'];
  errorMessage = '';
  successMessage = '';

  constructor(private userService: UserService) { }

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

  trackByUserId(_index: number, user: User): number {
    return user.id;
  }

  trackByRole(_index: number, role: string): string {
    return role;
  }

  private emptyUser(): CreateUserRequest {
    return {
      username: '',
      password: '',
      role: 'Operator'
    };
  }

  private getCreateErrorMessage(error: HttpErrorResponse): string {
    if (error.status === 401 || error.status === 403) {
      return 'Sua sessão não possui permissão de SuperAdmin. Saia e entre novamente no sistema.';
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
