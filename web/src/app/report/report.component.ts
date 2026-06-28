import { Component, DestroyRef, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ReceiptService, ReportSummaryResponse, MonthlyReportRow } from '../receipt.service';
import { UserService, User } from '../user.service';
import { AuthService } from '../auth.service';

@Component({
  selector: 'app-report',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './report.component.html',
  styleUrl: './report.component.css'
})
export class ReportComponent implements OnInit {
  private readonly destroyRef = inject(DestroyRef);
  private readonly receiptService = inject(ReceiptService);
  private readonly userService = inject(UserService);
  private readonly authService = inject(AuthService);

  private readonly monthNames = [
    'Janeiro', 'Fevereiro', 'Março', 'Abril', 'Maio', 'Junho',
    'Julho', 'Agosto', 'Setembro', 'Outubro', 'Novembro', 'Dezembro'
  ];

  readonly monthOptions = this.monthNames.map((label, i) => ({ value: i + 1, label }));
  readonly years: number[] = (() => {
    const cur = new Date().getFullYear();
    return Array.from({ length: 5 }, (_, i) => cur - i);
  })();

  summary: ReportSummaryResponse | null = null;
  loading = false;
  hasError = false;
  drivers: User[] = [];

  selectedYear: number | null = null;
  selectedMonth: number | null = null;
  selectedDriverId: number | null = null;

  get isAdmin(): boolean {
    return this.authService.isAdminOrAbove();
  }

  ngOnInit(): void {
    if (this.isAdmin) {
      this.userService.getUsers()
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: (users) => { this.drivers = users.filter(u => u.role === 'Driver'); },
          error: () => {}
        });
    }
    this.load();
  }

  load(): void {
    this.loading = true;
    this.hasError = false;

    const driverId = this.isAdmin && this.selectedDriverId !== null
      ? this.selectedDriverId
      : undefined;

    this.receiptService.getMonthlySummary(
      this.selectedYear ?? undefined,
      this.selectedMonth ?? undefined,
      driverId
    ).pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (data) => {
          this.summary = data;
          this.loading = false;
        },
        error: (err) => {
          console.error('Erro ao buscar relatório mensal', err);
          this.hasError = true;
          this.loading = false;
        }
      });
  }

  clearFilters(): void {
    this.selectedYear = null;
    this.selectedMonth = null;
    this.selectedDriverId = null;
    this.load();
  }

  exportCsv(): void {
    if (!this.summary?.rows.length) return;
    const header = 'Mês/Ano,Corridas,Média,Total';
    const lines = this.summary.rows.map(r =>
      `"${this.monthName(r.month)}/${r.year}",${r.count},${r.averageAmount.toFixed(2)},${r.totalAmount.toFixed(2)}`
    );
    const csv = [header, ...lines].join('\n');
    const blob = new Blob(['﻿' + csv], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `relatorio-mensal-${new Date().toISOString().slice(0, 10)}.csv`;
    a.click();
    setTimeout(() => URL.revokeObjectURL(url), 1000);
  }

  print(): void {
    window.print();
  }

  monthName(month: number): string {
    return this.monthNames[month - 1] ?? month.toString();
  }

  trackByRow(_index: number, row: MonthlyReportRow): string {
    return `${row.year}-${row.month}`;
  }

  trackByYear(_index: number, y: number): number {
    return y;
  }

  trackByMonthValue(_index: number, m: { value: number }): number {
    return m.value;
  }

  trackByUserId(_index: number, u: User): number {
    return u.id;
  }
}
