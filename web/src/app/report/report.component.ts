import { Component, DestroyRef, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ReceiptService, MonthlyReport } from '../receipt.service';

@Component({
  selector: 'app-report',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './report.component.html',
  styleUrl: './report.component.css'
})
export class ReportComponent implements OnInit {
  private readonly destroyRef = inject(DestroyRef);

  private readonly monthNames = [
    'Janeiro', 'Fevereiro', 'Março', 'Abril', 'Maio', 'Junho',
    'Julho', 'Agosto', 'Setembro', 'Outubro', 'Novembro', 'Dezembro'
  ];

  summary: MonthlyReport[] = [];
  loading = true;
  hasError = false;
  totalAmount = 0;
  totalCount = 0;

  constructor(private receiptService: ReceiptService) {}

  ngOnInit(): void {
    this.receiptService.getMonthlySummary()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (data) => {
          this.summary = data;
          this.totalAmount = data.reduce((sum, m) => sum + m.totalAmount, 0);
          this.totalCount = data.reduce((sum, m) => sum + m.count, 0);
          this.loading = false;
        },
        error: (err) => {
          console.error('Erro ao buscar relatório mensal', err);
          this.hasError = true;
          this.loading = false;
        }
      });
  }

  monthName(month: number): string {
    return this.monthNames[month - 1] ?? month.toString();
  }

  trackByMonth(_index: number, row: MonthlyReport): string {
    return `${row.year}-${row.month}`;
  }
}
