import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatButton } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinner } from '@angular/material/progress-spinner';
import { InterviewReportSummary } from '../../../core/models/report.models';
import { ReportsService } from '../../../core/services/reports.service';
import { getErrorMessage } from '../../../core/utils/api-error';
import { ScoreBadge } from '../../../shared/score-badge/score-badge';

@Component({
  selector: 'app-home',
  imports: [
    DatePipe,
    ReactiveFormsModule,
    RouterLink,
    MatButton,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinner,
    ScoreBadge,
  ],
  templateUrl: './home.html',
  styleUrl: './home.scss',
})
export class Home implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly reports = inject(ReportsService);
  private readonly router = inject(Router);

  // Generate form
  protected readonly generating = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  protected readonly form = this.fb.nonNullable.group({
    jobDescription: ['', [Validators.required, Validators.minLength(50), Validators.maxLength(5000)]],
    selfDescription: ['', [Validators.required, Validators.minLength(20), Validators.maxLength(3000)]],
  });

  // My reports list
  protected readonly myReports = signal<InterviewReportSummary[]>([]);
  protected readonly loadingReports = signal(true);
  protected readonly reportsError = signal<string | null>(null);

  ngOnInit(): void {
    this.loadReports();
  }

  protected generate(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.generating.set(true);
    this.errorMessage.set(null);
    this.form.disable(); // prevent edits while the AI works

    this.reports.create(this.form.getRawValue()).subscribe({
      next: (report) => this.router.navigate(['/reports', report.id]),
      error: (error: unknown) => {
        this.errorMessage.set(getErrorMessage(error));
        this.generating.set(false);
        this.form.enable();
      },
    });
  }

  private loadReports(): void {
    this.reports.list().subscribe({
      next: (reports) => {
        this.myReports.set(reports);
        this.loadingReports.set(false);
      },
      error: (error: unknown) => {
        this.reportsError.set(getErrorMessage(error));
        this.loadingReports.set(false);
      },
    });
  }
}