import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ReceiptService, Receipt } from '../receipt.service';
import { ClientService, Client } from '../client.service';

@Component({
  selector: 'app-receipts',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './receipts.component.html',
  styleUrl: './receipts.component.css'
})
export class ReceiptsComponent implements OnInit {
  receipts: Receipt[] = [];
  clients: Client[] = [];
  currentReceipt: Receipt = this.emptyReceipt();
  editingReceipt = false;

  constructor(
    private receiptService: ReceiptService,
    private clientService: ClientService
  ) { }

  ngOnInit(): void {
    this.loadClients();
    this.loadReceipts();
  }

  loadClients(): void {
    this.clientService.getClients().subscribe({
      next: (data) => {
        this.clients = data;
      },
      error: (error) => {
        console.error('Erro ao buscar clientes', error);
      }
    });
  }

  loadReceipts(): void {
    this.receiptService.getReceipts().subscribe({
      next: (data) => {
        this.receipts = data;
      },
      error: (error) => {
        console.error('Erro ao buscar recibos', error);
      }
    });
  }

  saveReceipt(): void {
    const payload = this.toApiPayload(this.currentReceipt);

    if (this.editingReceipt) {
      this.receiptService.updateReceipt(this.currentReceipt.id!, payload).subscribe({
        next: () => {
          this.loadReceipts();
          this.cancelEdit();
        },
        error: (error) => {
          console.error('Erro ao atualizar recibo', error);
        }
      });
      return;
    }

    this.receiptService.addReceipt(payload).subscribe({
      next: () => {
        this.loadReceipts();
        this.currentReceipt = this.emptyReceipt();
      },
      error: (error) => {
        console.error('Erro ao criar recibo', error);
      }
    });
  }

  editReceipt(receipt: Receipt): void {
    this.currentReceipt = {
      ...receipt,
      startTime: this.toTimeInput(receipt.startTime),
      endTime: this.toTimeInput(receipt.endTime)
    };
    this.editingReceipt = true;
  }

  cancelEdit(): void {
    this.currentReceipt = this.emptyReceipt();
    this.editingReceipt = false;
  }

  deleteReceipt(id: number): void {
    this.receiptService.deleteReceipt(id).subscribe({
      next: () => {
        this.loadReceipts();
      },
      error: (error) => {
        console.error('Erro ao excluir recibo', error);
      }
    });
  }

  generatePdf(id: number): void {
    this.receiptService.generateReceiptPdf(id).subscribe({
      next: (data) => {
        const fileUrl = URL.createObjectURL(data);
        window.open(fileUrl, '_blank');
      },
      error: (error) => {
        console.error('Erro ao gerar PDF', error);
      }
    });
  }

  toTimeInput(value?: string): string {
    if (!value) {
      return '';
    }

    if (value.includes('T')) {
      return value.substring(11, 16);
    }

    return value.substring(0, 5);
  }

  private emptyReceipt(): Receipt {
    return {
      clientId: 0,
      description: '',
      amount: 0,
      startTime: '',
      endTime: '',
      serviceDates: '',
      issuerName: '',
      issuerPhone: '',
      issuerEmail: '',
      driverName: ''
    };
  }

  private toApiPayload(receipt: Receipt): Receipt {
    return {
      ...receipt,
      startTime: this.toDateTime(receipt.startTime),
      endTime: this.toDateTime(receipt.endTime)
    };
  }

  private toDateTime(value?: string): string | undefined {
    if (!value) {
      return undefined;
    }

    if (value.includes('T')) {
      return value;
    }

    return `1970-01-01T${value}:00Z`;
  }
}
