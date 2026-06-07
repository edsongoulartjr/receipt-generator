import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CreateUserRequest, User, UserService } from '../user.service';

@Component({
  selector: 'app-users',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './users.component.html',
  styleUrl: './users.component.css'
})
export class UsersComponent implements OnInit {
  users: User[] = [];
  currentUser: CreateUserRequest = this.emptyUser();
  roles: CreateUserRequest['role'][] = ['Operator', 'SuperAdmin'];

  constructor(private userService: UserService) { }

  ngOnInit(): void {
    this.loadUsers();
  }

  loadUsers(): void {
    this.userService.getUsers().subscribe({
      next: (users) => {
        this.users = users;
      },
      error: (error) => {
        console.error('Erro ao buscar usuários', error);
      }
    });
  }

  createUser(): void {
    this.userService.createUser(this.currentUser).subscribe({
      next: () => {
        this.currentUser = this.emptyUser();
        this.loadUsers();
      },
      error: (error) => {
        console.error('Erro ao criar usuário', error);
        alert('Não foi possível criar o usuário. Verifique se o usuário já existe.');
      }
    });
  }

  activate(user: User): void {
    this.userService.activateUser(user.id).subscribe({
      next: () => this.loadUsers(),
      error: (error) => console.error('Erro ao ativar usuário', error)
    });
  }

  deactivate(user: User): void {
    this.userService.deactivateUser(user.id).subscribe({
      next: () => this.loadUsers(),
      error: (error) => console.error('Erro ao desativar usuário', error)
    });
  }

  private emptyUser(): CreateUserRequest {
    return {
      username: '',
      password: '',
      role: 'Operator'
    };
  }
}
