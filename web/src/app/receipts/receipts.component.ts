import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ReceiptService, Receipt } from '../receipt.service';
import { ClientService, Client } from '../client.service';
import { firstValueFrom } from 'rxjs';

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
  amountDisplay = '';
  sharingReceiptId: number | null = null;
  shareFeedback = '';

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
    if (this.currentReceipt.amount <= 0) {
      return;
    }

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
        this.amountDisplay = '';
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
    this.amountDisplay = this.formatCurrency(receipt.amount);
    this.editingReceipt = true;
  }

  cancelEdit(): void {
    this.currentReceipt = this.emptyReceipt();
    this.amountDisplay = '';
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

  onAmountInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    const digits = input.value.replace(/\D/g, '');
    const amount = digits ? Number(digits) / 100 : 0;

    this.currentReceipt.amount = amount;
    this.amountDisplay = digits ? this.formatCurrency(amount) : '';
    input.value = this.amountDisplay;
  }

  async sharePdf(receipt: Receipt): Promise<void> {
    if (!receipt.id || this.sharingReceiptId !== null) {
      return;
    }

    this.sharingReceiptId = receipt.id;
    this.shareFeedback = '';

    try {
      const blob = await firstValueFrom(this.receiptService.generateReceiptPdf(receipt.id));
      const receiptNumber = receipt.id.toString().padStart(6, '0');
      const fileName = `recibo-${receiptNumber}.pdf`;
      const file = new File([blob], fileName, { type: 'application/pdf' });

      if (navigator.share && navigator.canShare?.({ files: [file] })) {
        await navigator.share({
          title: `Recibo ${receiptNumber}`,
          text: `Recibo da Coopertáxi Jundiaí para ${receipt.client?.name || 'cliente'}.`,
          files: [file]
        });
        return;
      }

      this.downloadPdf(blob, fileName);
      this.shareFeedback = 'O compartilhamento de arquivos não está disponível neste navegador. O PDF foi baixado.';
    } catch (error) {
      if (error instanceof DOMException && error.name === 'AbortError') {
        return;
      }

      console.error('Erro ao compartilhar PDF', error);
      this.shareFeedback = 'Não foi possível preparar o recibo para compartilhamento. Tente novamente.';
    } finally {
      this.sharingReceiptId = null;
    }
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
      driverName: ''
    };
  }

  private toApiPayload(receipt: Receipt): Receipt {
    return {
      id: receipt.id,
      clientId: receipt.clientId,
      description: receipt.description,
      amount: receipt.amount,
      startTime: this.toDateTime(receipt.startTime),
      endTime: this.toDateTime(receipt.endTime),
      serviceDates: receipt.serviceDates,
      driverName: receipt.driverName
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

  private formatCurrency(value: number): string {
    return new Intl.NumberFormat('pt-BR', {
      style: 'currency',
      currency: 'BRL'
    }).format(value);
  }

  private downloadPdf(blob: Blob, fileName: string): void {
    const fileUrl = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = fileUrl;
    link.download = fileName;
    link.click();
    setTimeout(() => URL.revokeObjectURL(fileUrl), 1000);
  }
}
