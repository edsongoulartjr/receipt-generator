import { Component, DestroyRef, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ClientService, Client } from '../client.service';
import { CepService } from '../cep.service';
import { formatCpfCnpj } from '../utils/tax-id.utils';

type CepStatus = 'idle' | 'loading' | 'not-found' | 'error';

@Component({
  selector: 'app-clients',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './clients.component.html',
  styleUrl: './clients.component.css'
})
export class ClientsComponent implements OnInit {
  private readonly destroyRef = inject(DestroyRef);
  private toastTimer: ReturnType<typeof setTimeout> | null = null;
  private originalSnapshot = '';

  clients: Client[] = [];
  currentClient: Client = { name: '', address: '', taxId: '' };
  editingClient = false;
  isSaving = false;

  // Address sub-fields
  zipCode = '';
  street = '';
  number = '';
  complement = '';
  neighborhood = '';
  city = '';
  state = '';
  cepStatus: CepStatus = 'idle';
  private lastLookedUpCep = '';

  pageError = '';
  formError = '';

  toast = '';
  toastType: 'success' | 'error' = 'success';

  pendingDeleteClient: Client | null = null;
  pendingDeleteError = '';
  isDeletingClient = false;

  pendingCancelEdit = false;

  constructor(
    private clientService: ClientService,
    private cepService: CepService
  ) {
    this.destroyRef.onDestroy(() => {
      if (this.toastTimer) clearTimeout(this.toastTimer);
    });
  }

  ngOnInit(): void {
    this.loadClients();
  }

  get isDirty(): boolean {
    if (!this.editingClient) return false;
    return this.buildSnapshot() !== this.originalSnapshot;
  }

  loadClients(): void {
    this.pageError = '';
    this.clientService.getClients()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (data) => { this.clients = data; },
        error: () => { this.pageError = 'Não foi possível carregar os clientes. Verifique sua conexão e tente novamente.'; }
      });
  }

  onZipCodeChange(value: string): void {
    const digits = value.replace(/\D/g, '').slice(0, 8);
    this.zipCode = digits.length > 5 ? `${digits.slice(0, 5)}-${digits.slice(5)}` : digits;

    if (digits.length === 8 && digits !== this.lastLookedUpCep) {
      this.lookupCep(digits);
    } else if (digits.length < 8) {
      this.cepStatus = 'idle';
    }
  }

  private lookupCep(digits: string): void {
    this.lastLookedUpCep = digits;
    this.cepStatus = 'loading';

    this.cepService.lookup(digits)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          if (result.erro) { this.cepStatus = 'not-found'; return; }
          this.cepStatus = 'idle';
          this.street = result.logradouro || '';
          this.complement = result.complemento || '';
          this.neighborhood = result.bairro || '';
          this.city = result.localidade || '';
          this.state = result.uf || '';
        },
        error: () => { this.cepStatus = 'error'; }
      });
  }

  onTaxIdChange(value: string): void {
    this.currentClient.taxId = formatCpfCnpj(value);
  }

  saveClient(): void {
    this.formError = '';
    this.currentClient.address = this.buildAddress();
    this.currentClient.zipCode = this.zipCode || undefined;
    this.currentClient.street = this.street.trim() || undefined;
    this.currentClient.number = this.number.trim() || undefined;
    this.currentClient.complement = this.complement.trim() || undefined;
    this.currentClient.neighborhood = this.neighborhood.trim() || undefined;
    this.currentClient.city = this.city.trim() || undefined;
    this.currentClient.state = this.state.trim() || undefined;

    if (!this.street.trim()) {
      this.formError = 'Informe pelo menos o logradouro do cliente.';
      return;
    }

    this.isSaving = true;

    if (this.editingClient) {
      this.clientService.updateClient(this.currentClient.id!, this.currentClient)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: () => {
            this.isSaving = false;
            this.loadClients();
            this.cancelEdit();
            this.showToast('Cadastro atualizado com sucesso.');
          },
          error: () => {
            this.isSaving = false;
            this.formError = 'Não foi possível atualizar o cliente. Tente novamente.';
          }
        });
    } else {
      this.clientService.addClient(this.currentClient)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: () => {
            this.isSaving = false;
            this.loadClients();
            this.resetForm();
            this.showToast('Cliente cadastrado com sucesso.');
          },
          error: () => {
            this.isSaving = false;
            this.formError = 'Não foi possível salvar o cliente. Tente novamente.';
          }
        });
    }
  }

  editClient(client: Client): void {
    this.currentClient = { ...client, taxId: formatCpfCnpj(client.taxId ?? '') };
    this.editingClient = true;
    this.zipCode = client.zipCode ?? '';
    this.street = client.street ?? client.address;
    this.number = client.number ?? '';
    this.complement = client.complement ?? '';
    this.neighborhood = client.neighborhood ?? '';
    this.city = client.city ?? '';
    this.state = client.state ?? '';
    this.cepStatus = 'idle';
    this.lastLookedUpCep = client.zipCode?.replace(/\D/g, '') ?? '';
    this.formError = '';
    this.originalSnapshot = this.buildSnapshot();

    setTimeout(() => {
      document.querySelector('.client-form')?.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }, 50);
  }

  requestCancelEdit(): void {
    if (this.isDirty) {
      this.pendingCancelEdit = true;
    } else {
      this.cancelEdit();
    }
  }

  confirmCancelEdit(): void {
    this.pendingCancelEdit = false;
    this.cancelEdit();
  }

  dismissCancelEdit(): void {
    this.pendingCancelEdit = false;
  }

  cancelEdit(): void {
    this.currentClient = { name: '', address: '', taxId: '' };
    this.editingClient = false;
    this.formError = '';
    this.originalSnapshot = '';
    this.resetAddressFields();
  }

  openDeleteClient(client: Client): void {
    this.pendingDeleteClient = client;
    this.pendingDeleteError = '';
    this.isDeletingClient = false;
  }

  confirmDeleteClient(): void {
    const client = this.pendingDeleteClient;
    if (!client?.id) return;

    this.isDeletingClient = true;
    this.clientService.deleteClient(client.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.isDeletingClient = false;
          this.pendingDeleteClient = null;
          this.loadClients();
          this.showToast('Cliente excluído.');
        },
        error: () => {
          this.isDeletingClient = false;
          this.pendingDeleteError = 'Não foi possível excluir. Este cliente pode ter recibos vinculados.';
        }
      });
  }

  cancelDeleteClient(): void {
    this.pendingDeleteClient = null;
    this.pendingDeleteError = '';
  }

  dismissToast(): void {
    if (this.toastTimer) clearTimeout(this.toastTimer);
    this.toast = '';
  }

  trackByClientId(_index: number, client: Client): number | undefined {
    return client.id;
  }

  private buildAddress(): string {
    const streetPart = [this.street.trim(), this.number.trim()].filter(Boolean).join(', ');
    const parts = [
      streetPart,
      this.complement.trim(),
      this.neighborhood.trim(),
      [this.city.trim(), this.state.trim()].filter(Boolean).join('/'),
      this.zipCode.trim() ? `CEP ${this.zipCode.trim()}` : ''
    ].filter(Boolean);
    return parts.join(' - ');
  }

  private buildSnapshot(): string {
    return JSON.stringify({
      name: this.currentClient.name,
      taxId: this.currentClient.taxId,
      zipCode: this.zipCode,
      street: this.street,
      number: this.number,
      complement: this.complement,
      neighborhood: this.neighborhood,
      city: this.city,
      state: this.state,
    });
  }

  private showToast(message: string, type: 'success' | 'error' = 'success'): void {
    if (this.toastTimer) clearTimeout(this.toastTimer);
    this.toast = message;
    this.toastType = type;
    this.toastTimer = setTimeout(() => { this.toast = ''; }, 4000);
  }

  private resetForm(): void {
    this.currentClient = { name: '', address: '', taxId: '' };
    this.resetAddressFields();
  }

  private resetAddressFields(): void {
    this.zipCode = '';
    this.street = '';
    this.number = '';
    this.complement = '';
    this.neighborhood = '';
    this.city = '';
    this.state = '';
    this.cepStatus = 'idle';
    this.lastLookedUpCep = '';
  }
}
