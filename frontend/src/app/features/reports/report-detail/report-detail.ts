import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatAnchor, MatButton } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIcon } from '@angular/material/icon';
import { MatProgressSpinner } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatTabsModule } from '@angular/material/tabs';
import {
APPLICATION_STATUSES,
ApplicationStatus,
InterviewReport,
} from '../../../core/models/report.models';
import { ReportsService } from '../../../core/services/reports.service';
import { getErrorMessage } from '../../../core/utils/api-error';
import { Countdown } from '../../../shared/countdown/countdown';
import { ScoreBadge } from '../../../shared/score-badge/score-badge';
import { QuestionList } from '../question-list/question-list';
@Component({
selector: 'app-report-detail',
imports: [
DatePipe,
FormsModule,
RouterLink,
MatAnchor,
MatButton,
MatFormFieldModule,
MatIcon,
MatProgressSpinner,
MatSelectModule,
MatTabsModule,
Countdown,
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
protected readonly savingStatus = signal(false);
protected readonly statuses = APPLICATION_STATUSES;
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
protected changeStatus(status: ApplicationStatus): void {
const current = this.report();
if (!current || status === current.status) {
return;
}
this.savingStatus.set(true);
this.reports
.update(current.id, {
companyName: current.companyName,
interviewDate: current.interviewDate,
status,
})
.subscribe({
next: (updated) => {
this.report.set(updated);
this.savingStatus.set(false);
},
error: (error: unknown) => {
this.errorMessage.set(getErrorMessage(error));
this.savingStatus.set(false);
},
});
}
}