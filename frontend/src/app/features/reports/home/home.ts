import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatButton } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIcon } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinner } from '@angular/material/progress-spinner';
import { InterviewReportSummary } from '../../../core/models/report.models';
import { ReportsService } from '../../../core/services/reports.service';
import { getErrorMessage } from '../../../core/utils/api-error';
import { ScoreBadge } from '../../../shared/score-badge/score-badge';

/** Must match the API: 5 MB, PDF or DOCX only. */
const MAX_RESUME_BYTES = 5 * 1024 * 1024;
const ALLOWED_EXTENSIONS = ['.pdf', '.docx'];

@Component({
  selector: 'app-home',
  imports: [
    DatePipe,
    ReactiveFormsModule,
    RouterLink,
    MatButton,
    MatCardModule,
    MatFormFieldModule,
    MatIcon,
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

  // Selected resume file
  protected readonly resumeFile = signal<File | null>(null);
  protected readonly resumeError = signal<string | null>(null);

  protected readonly form = this.fb.nonNullable.group({
    jobDescription: ['', [Validators.required, Validators.minLength(50), Validators.maxLength(5000)]],
    selfDescription: ['', [Validators.minLength(20), Validators.maxLength(3000)]],
  });

  /** Business rule: a resume, a self-description, or both. */
  protected readonly missingProfile = computed(
    () => this.resumeFile() === null && this.form.controls.selfDescription.value.trim().length === 0,
  );

  // My reports list
  protected readonly myReports = signal<InterviewReportSummary[]>([]);
  protected readonly loadingReports = signal(true);
  protected readonly reportsError = signal<string | null>(null);

  ngOnInit(): void {
    this.loadReports();
  }

  protected onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;

    this.resumeError.set(null);

    if (!file) {
      return;
    }

    const extension = file.name.slice(file.name.lastIndexOf('.')).toLowerCase();

    if (!ALLOWED_EXTENSIONS.includes(extension)) {
      this.resumeError.set('Please choose a PDF or DOCX file.');
      input.value = '';
      return;
    }

    if (file.size > MAX_RESUME_BYTES) {
      this.resumeError.set('The file must be 5 MB or smaller.');
      input.value = '';
      return;
    }

    this.resumeFile.set(file);
    input.value = ''; // allows re-picking the same file after removing it
  }

  protected removeFile(): void {
    this.resumeFile.set(null);
    this.resumeError.set(null);
  }

  protected formatSize(bytes: number): string {
    return bytes < 1024 * 1024
      ? `${Math.round(bytes / 1024)} KB`
      : `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  }

  protected generate(): void {
    if (this.form.invalid || this.missingProfile()) {
      this.form.markAllAsTouched();
      return;
    }

    this.generating.set(true);
    this.errorMessage.set(null);
    this.form.disable(); // prevent edits while the AI works

    const { jobDescription, selfDescription } = this.form.getRawValue();

    this.reports
      .create({
        jobDescription,
        selfDescription: selfDescription.trim() || null,
        resume: this.resumeFile(),
      })
      .subscribe({
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