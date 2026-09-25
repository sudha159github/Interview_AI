import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ActivatedRoute, RouterLink } from '@angular/router';
import {
APPLICATION_STATUSES,
ApplicationStatus,
InterviewReport,
} from '../../../core/models/report.models';
import { ReportsService } from '../../../core/services/reports.service';
import { getErrorMessage } from '../../../core/utils/api-error';
import { QuestionList } from '../question-list/question-list';
/** Which section of the report is on screen. */
type ReportTab = 'technical' | 'behavioral' | 'plan';
@Component({
selector: 'app-report-detail',
imports: [DatePipe, RouterLink, QuestionList],
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
protected readonly activeTab = signal<ReportTab>('technical');
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
protected showTab(tab: ReportTab): void {
this.activeTab.set(tab);
}
/** Colour band for the match score: high, mid or low. */
protected scoreLevel(score: number): 'high' | 'mid' | 'low' {
return score >= 80 ? 'high' : score >= 60 ? 'mid' : 'low';
}
/** Human countdown text for the interview date. */
protected countdown(days: number | null): string {
if (days === null) {
return '';
}
if (days < 0) {
return 'Interview passed';
}
return days === 0 ? 'Interview today' : days === 1 ? 'Tomorrow' : `In ${days} days`;
}
protected onStatusChange(event: Event): void {
const value = (event.target as HTMLSelectElement).value as ApplicationStatus;
this.changeStatus(value);
}
private changeStatus(status: ApplicationStatus): void {
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