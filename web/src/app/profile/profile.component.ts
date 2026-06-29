import { Component, DestroyRef, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { UserService, User, UpdateProfileRequest } from '../user.service';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './profile.component.html',
  styleUrl: './profile.component.css'
})
export class ProfileComponent implements OnInit {
  private readonly destroyRef = inject(DestroyRef);

  profile: User | null = null;

  fullName = '';
  phone = '';
  email = '';
  currentPassword = '';
  newPassword = '';
  confirmPassword = '';

  successMessage = '';
  errorMessage = '';
  isSubmitting = false;

  constructor(private userService: UserService) { }

  ngOnInit(): void {
    this.loadProfile();
  }

  loadProfile(): void {
    this.userService.getProfile()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (user) => {
          this.profile = user;
          this.fullName = user.fullName;
          this.phone = user.phone ?? '';
          this.email = user.email ?? '';
        },
        error: (err) => { console.error('Erro ao carregar perfil', err); }
      });
  }

  saveProfile(): void {
    if (this.isSubmitting) return;

    if (this.newPassword && this.newPassword !== this.confirmPassword) {
      this.errorMessage = 'A nova senha e a confirmação não coincidem.';
      return;
    }

    if (this.newPassword && !this.currentPassword) {
      this.errorMessage = 'Informe a senha atual para definir uma nova senha.';
      return;
    }

    this.isSubmitting = true;
    this.successMessage = '';
    this.errorMessage = '';

    const request: UpdateProfileRequest = {
      fullName: this.fullName !== this.profile?.fullName ? this.fullName : undefined,
      phone: this.phone !== (this.profile?.phone ?? '') ? this.phone : undefined,
      email: this.email !== (this.profile?.email ?? '') ? this.email : undefined,
      currentPassword: this.currentPassword || undefined,
      newPassword: this.newPassword || undefined
    };

    this.userService.updateProfile(request)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.isSubmitting = false;
          if (this.newPassword) {
            this.successMessage = 'Senha alterada. Você será desconectado no próximo acesso — faça login novamente.';
          } else {
            this.successMessage = 'Perfil atualizado. O novo nome aparecerá no próximo login.';
          }
          this.currentPassword = '';
          this.newPassword = '';
          this.confirmPassword = '';
          this.loadProfile();
        },
        error: (err) => {
          this.isSubmitting = false;
          this.errorMessage = err?.error?.message ?? 'Erro ao atualizar o perfil. Tente novamente.';
        }
      });
  }
}
