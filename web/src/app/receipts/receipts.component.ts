import { Component, DestroyRef, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ReceiptService, Receipt, PagedResponse } from '../receipt.service';
import { ClientService, Client } from '../client.service';
import { firstValueFrom } from 'rxjs';
import { formatCpfCnpj } from '../utils/tax-id.utils';

@Component({
  selector: 'app-receipts',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './receipts.component.html',
  styleUrl: './receipts.component.css'
})
export class ReceiptsComponent implements OnInit {
  private static readonly currencyFormatter = new Intl.NumberFormat('pt-BR', {
    style: 'currency',
    currency: 'BRL'
  });

  private readonly destroyRef = inject(DestroyRef);

  receipts: Receipt[] = [];
  clients: Client[] = [];
  currentReceipt: Receipt = this.emptyReceipt();
  editingReceipt = false;
  amountDisplay = '';
  sharingReceiptId: number | null = null;
  shareFeedback = '';
  isSubmitting = false;

  clientNameInput = '';
  resolvedClientId = 0;
  showNewClientPrompt = false;
  showClientDetails = false;
  newClientAddress = '';
  newClientTaxId = '';

  showExtras = false;
  lastCreatedReceipt: Receipt | null = null;

  currentPage = 1;
  readonly pageSize = 20;
  totalPages = 0;
  totalCount = 0;

  constructor(
    private receiptService: ReceiptService,
    private clientService: ClientService
  ) { }

  ngOnInit(): void {
    this.loadClients();
    this.loadReceipts();
    const savedDriver = localStorage.getItem('driverName');
    if (savedDriver) {
      this.currentReceipt.driverName = savedDriver;
    }
  }

  loadClients(): void {
    this.clientService.getClients()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (data) => { this.clients = data; },
        error: (err) => { console.error('Erro ao buscar clientes', err); }
      });
  }

  loadReceipts(): void {
    this.receiptService.getReceipts(this.currentPage, this.pageSize)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (data: PagedResponse<Receipt>) => {
          this.receipts = data.items;
          this.totalPages = data.totalPages;
          this.totalCount = data.totalCount;
        },
        error: (err) => { console.error('Erro ao buscar recibos', err); }
      });
  }

  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages) return;
    this.currentPage = page;
    this.loadReceipts();
  }

  nextPage(): void { this.goToPage(this.currentPage + 1); }
  prevPage(): void { this.goToPage(this.currentPage - 1); }

  onClientNameChange(name: string): void {
    this.clientNameInput = name;
    const trimmed = name.trim().toLowerCase();

    if (!trimmed) {
      this.resolvedClientId = 0;
      this.showNewClientPrompt = false;
      this.showClientDetails = false;
      return;
    }

    const match = this.clients.find(c => c.name.toLowerCase() === trimmed);
    if (match) {
      this.resolvedClientId = match.id!;
      this.showNewClientPrompt = false;
      this.showClientDetails = false;
    } else {
      this.resolvedClientId = 0;
      this.showNewClientPrompt = true;
    }
  }

  acceptWithDetails(): void {
    this.showClientDetails = true;
  }

  onNewClientTaxIdChange(value: string): void {
    this.newClientTaxId = formatCpfCnpj(value);
  }

  acceptNameOnly(): void {
    this.showClientDetails = false;
    this.showNewClientPrompt = false;
  }

  async saveReceipt(): Promise<void> {
    if (this.isSubmitting || this.currentReceipt.amount <= 0) {
      return;
    }

    this.isSubmitting = true;
    let clientId = this.resolvedClientId;

    if (clientId === 0) {
      const name = this.clientNameInput.trim();
      if (!name) {
        this.isSubmitting = false;
        return;
      }

      try {
        const created = await firstValueFrom(
          this.clientService.addClient({
            name,
            address: this.newClientAddress.trim(),
            taxId: this.newClientTaxId.trim()
          })
        );
        clientId = created.id!;
        this.clients.push(created);
      } catch (err) {
        console.error('Erro ao criar cliente', err);
        this.isSubmitting = false;
        return;
      }
    }

    const payload = this.toApiPayload({ ...this.currentReceipt, clientId });

    if (this.editingReceipt) {
      this.receiptService.updateReceipt(this.currentReceipt.id!, payload)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: () => {
            this.isSubmitting = false;
            this.loadReceipts();
            this.cancelEdit();
          },
          error: (err) => {
            this.isSubmitting = false;
            console.error('Erro ao atualizar recibo', err);
          }
        });
      return;
    }

    const driverName = this.currentReceipt.driverName?.trim();
    if (driverName) {
      localStorage.setItem('driverName', driverName);
    }

    this.receiptService.addReceipt(payload)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (created) => {
          this.isSubmitting = false;
          this.lastCreatedReceipt = created;
          this.currentPage = 1;
          this.loadReceipts();
          this.resetForm();
        },
        error: (err) => {
          this.isSubmitting = false;
          console.error('Erro ao criar recibo', err);
        }
      });
  }

  editReceipt(receipt: Receipt): void {
    this.currentReceipt = {
      ...receipt,
      startTime: this.toTimeInput(receipt.startTime),
      endTime: this.toTimeInput(receipt.endTime)
    };
    this.clientNameInput = receipt.client?.name ?? '';
    this.resolvedClientId = receipt.clientId;
    this.showNewClientPrompt = false;
    this.showClientDetails = false;
    this.showExtras = !!(receipt.startTime || receipt.endTime || receipt.driverName);
    this.amountDisplay = this.formatCurrency(receipt.amount);
    this.editingReceipt = true;
  }

  cancelEdit(): void {
    this.currentReceipt = this.emptyReceipt();
    this.amountDisplay = '';
    this.editingReceipt = false;
    this.showExtras = false;
    this.resetClientState();
  }

  shareLastReceipt(): void {
    if (this.lastCreatedReceipt) {
      this.sharePdf(this.lastCreatedReceipt);
    }
    this.lastCreatedReceipt = null;
  }

  dismissSuccess(): void {
    this.lastCreatedReceipt = null;
  }

  deleteReceipt(id: number): void {
    this.receiptService.deleteReceipt(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => { this.currentPage = 1; this.loadReceipts(); },
        error: (err) => { console.error('Erro ao excluir recibo', err); }
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
      const receiptNumber = (receipt.number ?? receipt.id ?? 0).toString().padStart(6, '0');
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

  shareWhatsApp(receipt: Receipt): void {
    const number = (receipt.number ?? receipt.id ?? 0).toString().padStart(6, '0');
    const amount = ReceiptsComponent.currencyFormatter.format(receipt.amount);
    const date = receipt.date ? new Date(receipt.date).toLocaleDateString('pt-BR') : '';
    const client = receipt.client?.name ?? 'cliente';

    const lines = [
      `*Recibo Nº ${number}*`,
      `Coopertáxi Jundiaí`,
      ``,
      `*Cliente:* ${client}`,
      `*Valor:* ${amount}`,
      `*Data:* ${date}`,
      `*Serviço:* ${receipt.description}`,
      ``,
      `_Documento emitido eletronicamente._`
    ];

    if (receipt.driverName?.trim()) {
      lines.splice(7, 0, `*Motorista:* ${receipt.driverName.trim()}`);
    }

    const url = `https://wa.me/?text=${encodeURIComponent(lines.join('\n'))}`;
    window.open(url, '_blank', 'noopener,noreferrer');
  }

  toTimeInput(value?: string): string {
    if (!value) return '';
    return value.includes('T') ? value.substring(11, 16) : value.substring(0, 5);
  }

  private resetForm(): void {
    this.currentReceipt = this.emptyReceipt();
    this.currentReceipt.driverName = localStorage.getItem('driverName') ?? '';
    this.amountDisplay = '';
    this.showExtras = false;
    this.resetClientState();
  }

  private resetClientState(): void {
    this.clientNameInput = '';
    this.resolvedClientId = 0;
    this.showNewClientPrompt = false;
    this.showClientDetails = false;
    this.newClientAddress = '';
    this.newClientTaxId = '';
  }

  private emptyReceipt(): Receipt {
    return {
      clientId: 0,
      description: 'Serviço de Táxi',
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
    if (!value) return undefined;
    if (value.includes('T')) return value;
    return `1970-01-01T${value}:00Z`;
  }

  trackByReceiptId(_index: number, receipt: Receipt): number | undefined {
    return receipt.id;
  }

  trackByClientId(_index: number, client: Client): number | undefined {
    return client.id;
  }

  private formatCurrency(value: number): string {
    return ReceiptsComponent.currencyFormatter.format(value);
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
