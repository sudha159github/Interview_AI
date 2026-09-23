import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatAnchor } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';
import { MatProgressSpinner } from '@angular/material/progress-spinner';
import { MatTabsModule } from '@angular/material/tabs';
import { InterviewReport } from '../../../core/models/report.models';
import { ReportsService } from '../../../core/services/reports.service';
import { getErrorMessage } from '../../../core/utils/api-error';
import { ScoreBadge } from '../../../shared/score-badge/score-badge';
import { QuestionList } from '../question-list/question-list';

@Component({
  selector: 'app-report-detail',
  imports: [
    DatePipe,
    RouterLink,
    MatAnchor,
    MatIcon,
    MatProgressSpinner,
    MatTabsModule,
    ScoreBadge,
    QuestionList,
  ],
  templateUrl: './report-detail.html',
  styleUrl: './report-detail.scss',
})
export class ReportDetail implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly reports = inject(ReportsService);

  protected readonly report = signal<InterviewReport | null>(null);
  protected readonly loading = signal(true);
  protected readonly errorMessage = signal<string | null>(null);

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');

    if (!id) {
      this.errorMessage.set('Report not found.');
      this.loading.set(false);
      return;
    }

    this.reports.getById(id).subscribe({
      next: (report) => {
        this.report.set(report);
        this.loading.set(false);
      },
      error: (error: unknown) => {
        this.errorMessage.set(
          error instanceof HttpErrorResponse && error.status === 404
            ? "This report doesn't exist or isn't yours."
            : getErrorMessage(error),
        );
        this.loading.set(false);
      },
    });
  }
}