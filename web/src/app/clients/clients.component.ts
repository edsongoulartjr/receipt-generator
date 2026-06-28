import { Component, DestroyRef, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ClientService, Client } from '../client.service';
import { formatCpfCnpj } from '../utils/tax-id.utils';

@Component({
  selector: 'app-clients',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './clients.component.html',
  styleUrl: './clients.component.css'
})
export class ClientsComponent implements OnInit {
  private readonly destroyRef = inject(DestroyRef);

  clients: Client[] = [];
  currentClient: Client = { name: '', address: '', taxId: '' };
  editingClient = false;

  constructor(private clientService: ClientService) { }

  ngOnInit(): void {
    this.loadClients();
  }

  loadClients(): void {
    this.clientService.getClients()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (data) => { this.clients = data; },
        error: (err) => { console.error('Error fetching clients', err); }
      });
  }

  saveClient(): void {
    if (this.editingClient) {
      this.clientService.updateClient(this.currentClient.id!, this.currentClient)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: () => { this.loadClients(); this.cancelEdit(); },
          error: (err) => { console.error('Error updating client', err); }
        });
    } else {
      this.clientService.addClient(this.currentClient)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: () => {
            this.loadClients();
            this.currentClient = { name: '', address: '', taxId: '' };
          },
          error: (err) => { console.error('Error adding client', err); }
        });
    }
  }

  editClient(client: Client): void {
    this.currentClient = { ...client, taxId: formatCpfCnpj(client.taxId ?? '') };
    this.editingClient = true;
  }

  cancelEdit(): void {
    this.currentClient = { name: '', address: '', taxId: '' };
    this.editingClient = false;
  }

  deleteClient(id: number): void {
    this.clientService.deleteClient(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => { this.loadClients(); },
        error: (err) => { console.error('Error deleting client', err); }
      });
  }

  trackByClientId(_index: number, client: Client): number | undefined {
    return client.id;
  }

  onTaxIdChange(value: string): void {
    this.currentClient.taxId = formatCpfCnpj(value);
  }
}
