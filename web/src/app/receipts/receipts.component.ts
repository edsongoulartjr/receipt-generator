import { Component, DestroyRef, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ReceiptService, Receipt, PagedResponse } from '../receipt.service';
import { ClientService, Client } from '../client.service';
import { AuthService } from '../auth.service';
import { UserService, User } from '../user.service';
import { ShareService } from '../share.service';
import { firstValueFrom } from 'rxjs';

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
  drivers: User[] = [];
  selectedDriverId: number | null = null;
  currentReceipt: Receipt = this.emptyReceipt();
  editingReceipt = false;
  amountDisplay = '';
  sharingReceiptId: number | null = null;
  shareFeedback = '';
  isSubmitting = false;
  isLoadingPage = false;

  clientNameInput = '';
  resolvedClientId = 0;
  showNewClientPrompt = false;

  serviceDateInput = ''; // YYYY-MM-DD (valor interno do <input type="date">)

  showExtras = false;
  lastCreatedReceipt: Receipt | null = null;

  currentPage = 1;
  readonly pageSize = 20;
  totalPages = 0;
  totalCount = 0;

  filterMonth: number | undefined = undefined;
  filterYear: number | undefined = undefined;
  readonly months = [
    { value: 1, label: 'Janeiro' }, { value: 2, label: 'Fevereiro' },
    { value: 3, label: 'Março' }, { value: 4, label: 'Abril' },
    { value: 5, label: 'Maio' }, { value: 6, label: 'Junho' },
    { value: 7, label: 'Julho' }, { value: 8, label: 'Agosto' },
    { value: 9, label: 'Setembro' }, { value: 10, label: 'Outubro' },
    { value: 11, label: 'Novembro' }, { value: 12, label: 'Dezembro' }
  ];
  readonly availableYears: number[] = (() => {
    const current = new Date().getFullYear();
    return Array.from({ length: 5 }, (_, i) => current - i);
  })();

  constructor(
    private receiptService: ReceiptService,
    private clientService: ClientService,
    public authService: AuthService,
    private userService: UserService,
    private shareService: ShareService
  ) { }

  ngOnInit(): void {
    this.loadClients();
    this.loadReceipts();
    if (this.authService.isAdminOrAbove()) {
      this.loadDrivers();
    }
  }

  loadDrivers(): void {
    this.userService.getDrivers()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (data) => { this.drivers = data; },
        error: (err) => { console.error('Erro ao buscar motoristas', err); }
      });
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
    this.isLoadingPage = true;
    this.receiptService.getReceipts(this.currentPage, this.pageSize, this.filterMonth, this.filterYear)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (data: PagedResponse<Receipt>) => {
          this.receipts = data.items;
          this.totalPages = data.totalPages;
          this.totalCount = data.totalCount;
          this.isLoadingPage = false;
        },
        error: (err) => {
          console.error('Erro ao buscar recibos', err);
          this.isLoadingPage = false;
        }
      });
  }

  onFilterChange(): void {
    this.currentPage = 1;
    this.loadReceipts();
  }

  clearFilter(): void {
    this.filterMonth = undefined;
    this.filterYear = undefined;
    this.currentPage = 1;
    this.loadReceipts();
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
      return;
    }

    const match = this.clients.find(c => c.name.toLowerCase() === trimmed);
    if (match) {
      this.resolvedClientId = match.id!;
      this.showNewClientPrompt = false;
    } else {
      this.resolvedClientId = 0;
      this.showNewClientPrompt = true;
    }
  }

  onServiceDateChange(value: string): void {
    this.serviceDateInput = value;
    if (value) {
      const [y, m, d] = value.split('-');
      this.currentReceipt.serviceDates = `${d}/${m}/${y}`;
    } else {
      this.currentReceipt.serviceDates = '';
    }
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
          this.clientService.addClient({ name, address: '', taxId: '' })
        );
        clientId = created.id!;
        this.clients.push(created);
      } catch (err) {
        console.error('Erro ao criar cliente', err);
        this.isSubmitting = false;
        return;
      }
    }

    const payload = this.toApiPayload({ ...this.currentReceipt, clientId, driverUserId: this.selectedDriverId ?? undefined });

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
    this.serviceDateInput = this.parseDateForInput(receipt.serviceDates ?? '');
    this.showExtras = !!(receipt.startTime || receipt.endTime);
    this.amountDisplay = this.formatCurrency(receipt.amount);
    this.editingReceipt = true;
  }

  cancelEdit(): void {
    this.currentReceipt = this.emptyReceipt();
    this.amountDisplay = '';
    this.serviceDateInput = '';
    this.editingReceipt = false;
    this.showExtras = false;
    this.selectedDriverId = null;
    this.resetClientState();
  }

  shareLastReceipt(): void {
    if (this.lastCreatedReceipt) {
      this.shareReceipt(this.lastCreatedReceipt);
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

  async shareReceipt(receipt: Receipt): Promise<void> {
    if (!receipt.id || this.sharingReceiptId !== null) {
      return;
    }

    this.sharingReceiptId = receipt.id;
    this.shareFeedback = '';

    try {
      const blob = await firstValueFrom(this.receiptService.generateReceiptPdf(receipt.id));
      const receiptNumber = (receipt.number ?? receipt.id ?? 0).toString().padStart(6, '0');
      const fileName = `recibo-${receiptNumber}.pdf`;
      const amountFormatted = ReceiptsComponent.currencyFormatter.format(receipt.amount);
      const title = `Recibo Nº ${receiptNumber}`;
      const text = `Recibo Nº ${receiptNumber} — ${receipt.client?.name ?? 'cliente'} — ${amountFormatted} — Coopertáxi Jundiaí.`;

      const shared = await this.shareService.sharePdf(blob, fileName, title, text);

      if (!shared) {
        this.setFeedback(`PDF "${fileName}" baixado. Para enviar pelo WhatsApp, abra o WhatsApp Web e anexe o arquivo.`);
      }
    } catch (error) {
      if (error instanceof DOMException && error.name === 'AbortError') {
        return;
      }
      console.error('Erro ao compartilhar recibo', error);
      this.setFeedback('Não foi possível preparar o recibo. Tente novamente.');
    } finally {
      this.sharingReceiptId = null;
    }
  }

  toTimeInput(value?: string): string {
    if (!value) return '';
    return value.includes('T') ? value.substring(11, 16) : value.substring(0, 5);
  }

  private resetForm(): void {
    this.currentReceipt = this.emptyReceipt();
    this.amountDisplay = '';
    this.serviceDateInput = '';
    this.showExtras = false;
    this.selectedDriverId = null;
    this.resetClientState();
  }

  private resetClientState(): void {
    this.clientNameInput = '';
    this.resolvedClientId = 0;
    this.showNewClientPrompt = false;
  }

  private emptyReceipt(): Receipt {
    return {
      clientId: 0,
      description: 'Serviço de Táxi',
      amount: 0,
      startTime: '',
      endTime: '',
      serviceDates: ''
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
      driverUserId: receipt.driverUserId
    };
  }

  private parseDateForInput(serviceDates: string): string {
    // Converte dd/MM/YYYY → YYYY-MM-DD para o <input type="date">
    const match = serviceDates?.match(/^(\d{2})\/(\d{2})\/(\d{4})$/);
    if (!match) return '';
    return `${match[3]}-${match[2]}-${match[1]}`;
  }

  private toDateTime(value?: string): string | undefined {
    if (!value) return undefined;
    if (value.includes('T')) return value;
    return `1970-01-01T${value}:00Z`;
  }

  trackByMonthValue(_index: number, m: { value: number }): number {
    return m.value;
  }

  trackByYear(_index: number, y: number): number {
    return y;
  }

  trackByReceiptId(_index: number, receipt: Receipt): number | undefined {
    return receipt.id;
  }

  trackByClientId(_index: number, client: Client): number | undefined {
    return client.id;
  }

  trackByUserId(_index: number, user: User): number {
    return user.id;
  }

  private formatCurrency(value: number): string {
    return ReceiptsComponent.currencyFormatter.format(value);
  }

  clearFeedback(): void {
    this.shareFeedback = '';
  }

  private setFeedback(message: string): void {
    this.shareFeedback = message;
    setTimeout(() => { this.shareFeedback = ''; }, 12000);
  }

}
