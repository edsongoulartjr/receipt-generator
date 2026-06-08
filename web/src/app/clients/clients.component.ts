import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ClientService, Client } from '../client.service';

@Component({
  selector: 'app-clients',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './clients.component.html',
  styleUrl: './clients.component.css'
})
export class ClientsComponent implements OnInit {
  clients: Client[] = [];
  currentClient: Client = { name: '', address: '', taxId: '' };
  editingClient = false;

  constructor(private clientService: ClientService) { }

  ngOnInit(): void {
    this.loadClients();
  }

  loadClients(): void {
    this.clientService.getClients().subscribe({
      next: (data) => {
        this.clients = data;
      },
      error: (error) => {
        console.error('Error fetching clients', error);
      }
    });
  }

  saveClient(): void {
    if (this.editingClient) {
      this.clientService.updateClient(this.currentClient.id!, this.currentClient).subscribe({
        next: () => {
          this.loadClients();
          this.cancelEdit();
        },
        error: (error) => {
          console.error('Error updating client', error);
        }
      });
    } else {
      this.clientService.addClient(this.currentClient).subscribe({
        next: () => {
          this.loadClients();
          this.currentClient = { name: '', address: '', taxId: '' };
        },
        error: (error) => {
          console.error('Error adding client', error);
        }
      });
    }
  }

  editClient(client: Client): void {
    this.currentClient = { ...client, taxId: this.formatCpfCnpj(client.taxId) };
    this.editingClient = true;
  }

  cancelEdit(): void {
    this.currentClient = { name: '', address: '', taxId: '' };
    this.editingClient = false;
  }

  deleteClient(id: number): void {
    this.clientService.deleteClient(id).subscribe({
      next: () => {
        this.loadClients();
      },
      error: (error) => {
        console.error('Error deleting client', error);
      }
    });
  }

  onTaxIdChange(value: string): void {
    this.currentClient.taxId = this.formatCpfCnpj(value);
  }

  private formatCpfCnpj(value: string): string {
    const digits = value.replace(/\D/g, '').slice(0, 14);

    if (digits.length <= 11) {
      return digits
        .replace(/^(\d{3})(\d)/, '$1.$2')
        .replace(/^(\d{3})\.(\d{3})(\d)/, '$1.$2.$3')
        .replace(/\.(\d{3})(\d)/, '.$1-$2');
    }

    return digits
      .replace(/^(\d{2})(\d)/, '$1.$2')
      .replace(/^(\d{2})\.(\d{3})(\d)/, '$1.$2.$3')
      .replace(/\.(\d{3})(\d)/, '.$1/$2')
      .replace(/(\d{4})(\d)/, '$1-$2');
  }
}
