import { Component, DestroyRef, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ReceiptService, Receipt, PagedResponse } from '../receipt.service';
import { ClientService, Client } from '../client.service';
import { AuthService } from '../auth.service';
import { UserService, User } from '../user.service';
import { ShareService } from '../share.service';
import { formatCpfCnpj } from '../utils/tax-id.utils';
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

  pendingDeleteReceipt: Receipt | null = null;
  pendingDeleteReason = '';

  pageError = '';
  formError = '';
  driversLoadError = '';

  clientNameInput = '';
  resolvedClientId = 0;
  showNewClientPrompt = false;

  payerTaxIdDisplay = '';

  private cachedIssuerPhone = '';
  private cachedIssuerEmail = '';

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
    } else {
      this.prefillIssuerFromProfile();
    }
  }

  private prefillIssuerFromProfile(): void {
    this.userService.getProfile()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (user) => {
          this.cachedIssuerPhone = user.phone ?? '';
          this.cachedIssuerEmail = user.email ?? '';
          if (!this.currentReceipt.issuerPhone)
            this.currentReceipt.issuerPhone = this.cachedIssuerPhone;
          if (!this.currentReceipt.issuerEmail)
            this.currentReceipt.issuerEmail = this.cachedIssuerEmail;
        },
        error: () => { /* silently ignore — profile is optional context */ }
      });
  }

  loadDrivers(): void {
    this.userService.getDrivers()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (data) => { this.drivers = data; },
        error: () => { this.driversLoadError = 'Não foi possível carregar a lista de motoristas. Recarregue a página.'; }
      });
  }

  loadClients(): void {
    this.clientService.getClients()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (data) => { this.clients = data; },
        error: () => { /* autocomplete fica vazio — não bloqueia o fluxo principal */ }
      });
  }

  loadReceipts(): void {
    this.isLoadingPage = true;
    this.pageError = '';
    this.receiptService.getReceipts(this.currentPage, this.pageSize, this.filterMonth, this.filterYear)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (data: PagedResponse<Receipt>) => {
          this.receipts = data.items;
          this.totalPages = data.totalPages;
          this.totalCount = data.totalCount;
          this.isLoadingPage = false;
        },
        error: () => {
          this.pageError = 'Não foi possível carregar os recibos. Verifique sua conexão e tente novamente.';
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
      if (!this.payerTaxIdDisplay && match.taxId) {
        this.payerTaxIdDisplay = formatCpfCnpj(match.taxId);
        this.currentReceipt.payerTaxId = this.payerTaxIdDisplay;
      }
    } else {
      this.resolvedClientId = 0;
      this.showNewClientPrompt = true;
    }
  }

  onPayerTaxIdChange(value: string): void {
    this.payerTaxIdDisplay = formatCpfCnpj(value);
    this.currentReceipt.payerTaxId = this.payerTaxIdDisplay || undefined;
  }

  async saveReceipt(): Promise<void> {
    if (this.isSubmitting || this.currentReceipt.amount <= 0) {
      return;
    }

    this.isSubmitting = true;
    this.formError = '';
    let clientId: number | undefined = this.resolvedClientId || undefined;

    if (!clientId) {
      const name = this.clientNameInput.trim();
      if (name) {
        try {
          const created = await firstValueFrom(
            this.clientService.addClient({ name, address: '', taxId: this.payerTaxIdDisplay || '' })
          );
          clientId = created.id!;
          this.clients.push(created);
        } catch {
          this.setFormError('Não foi possível cadastrar o cliente. Tente novamente.');
          this.isSubmitting = false;
          return;
        }
      }
      // se nome também vazio: clientId permanece undefined → recibo sem nome
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
          error: () => {
            this.isSubmitting = false;
            this.setFormError('Não foi possível atualizar o recibo. Tente novamente.');
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
        error: () => {
          this.isSubmitting = false;
          this.setFormError('Não foi possível emitir o recibo. Verifique sua conexão e tente novamente.');
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
    this.resolvedClientId = receipt.clientId ?? 0;
    this.showNewClientPrompt = false;
    this.payerTaxIdDisplay = receipt.payerTaxId ? formatCpfCnpj(receipt.payerTaxId) : '';
    this.showExtras = !!(receipt.startTime || receipt.endTime || receipt.serviceStartDate || receipt.serviceEndDate);
    this.amountDisplay = this.formatCurrency(receipt.amount);
    this.editingReceipt = true;
  }

  cancelEdit(): void {
    this.currentReceipt = this.emptyReceipt();
    this.applyIssuerCache();
    this.amountDisplay = '';
    this.payerTaxIdDisplay = '';
    this.editingReceipt = false;
    this.showExtras = false;
    this.selectedDriverId = null;
    this.formError = '';
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

  deleteReceipt(receipt: Receipt): void {
    this.pendingDeleteReceipt = receipt;
    this.pendingDeleteReason = '';
  }

  confirmDelete(): void {
    const receipt = this.pendingDeleteReceipt;
    if (!receipt?.id) return;
    this.pendingDeleteReceipt = null;

    this.receiptService.deleteReceipt(receipt.id, this.pendingDeleteReason.trim() || undefined)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => { this.currentPage = 1; this.loadReceipts(); },
        error: () => { this.setFeedback('Não foi possível cancelar o recibo. Tente novamente.'); }
      });
  }

  cancelPendingDelete(): void {
    this.pendingDeleteReceipt = null;
    this.pendingDeleteReason = '';
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
    this.applyIssuerCache();
    this.amountDisplay = '';
    this.payerTaxIdDisplay = '';
    this.showExtras = false;
    this.selectedDriverId = null;
    this.resetClientState();
  }

  private applyIssuerCache(): void {
    if (this.cachedIssuerPhone) this.currentReceipt.issuerPhone = this.cachedIssuerPhone;
    if (this.cachedIssuerEmail) this.currentReceipt.issuerEmail = this.cachedIssuerEmail;
  }

  private resetClientState(): void {
    this.clientNameInput = '';
    this.resolvedClientId = 0;
    this.showNewClientPrompt = false;
  }

  private emptyReceipt(): Receipt {
    return {
      clientId: undefined,
      description: 'Serviço de Táxi',
      amount: 0,
      startTime: '',
      endTime: '',
      serviceDates: '',
      payerTaxId: undefined
    };
  }

  private toApiPayload(receipt: Receipt): Receipt {
    return {
      id: receipt.id,
      clientId: receipt.clientId || undefined,
      description: receipt.description,
      amount: receipt.amount,
      startTime: this.toDateTime(receipt.startTime),
      endTime: this.toDateTime(receipt.endTime),
      serviceDates: receipt.serviceDates || undefined,
      serviceStartDate: receipt.serviceStartDate || undefined,
      serviceEndDate: receipt.serviceEndDate || undefined,
      payerTaxId: receipt.payerTaxId || undefined,
      driverUserId: receipt.driverUserId
    };
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

  private setFormError(message: string): void {
    this.formError = message;
    setTimeout(() => document.getElementById('form-error')?.scrollIntoView({ behavior: 'smooth', block: 'center' }), 50);
  }

}
