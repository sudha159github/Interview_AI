import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { InterviewQuestion, InterviewReport } from '../../../core/models/report.models';
import { MockAnswer } from '../../../core/models/mock-interview.models';
import { MockInterviewService } from '../../../core/services/mock-interview.service';
import { ReportsService } from '../../../core/services/reports.service';
import { getErrorMessage } from '../../../core/utils/api-error';
/** One question in the practice run, with its type and position. */
interface PracticeQuestion extends InterviewQuestion {
type: 'Technical' | 'Behavioral';
order: number;
}
@Component({
selector: 'app-practice',
imports: [FormsModule, RouterLink],
templateUrl: './practice.html',
styleUrl: './practice.scss',
})
export class Practice implements OnInit {
private readonly route = inject(ActivatedRoute);
private readonly reports = inject(ReportsService);
private readonly mock = inject(MockInterviewService);
private reportId = '';
protected readonly report = signal<InterviewReport | null>(null);
protected readonly loading = signal(true);
protected readonly errorMessage = signal<string | null>(null);
protected readonly questions = signal<PracticeQuestion[]>([]);
protected readonly index = signal(0);
protected readonly answerText = signal('');
protected readonly scoring = signal(false);
protected readonly feedback = signal<MockAnswer | null>(null);
protected readonly finished = signal(false);
protected readonly showModelAnswer = signal(false);
/** Scores collected during this run, used for the summary. */
protected readonly scores = signal<number[]>([]);
protected readonly current = computed(() => this.questions()[this.index()] ?? null);
protected readonly total = computed(() => this.questions().length);
protected readonly progress = computed(() =>
this.total() === 0 ? 0 : (this.index() / this.total()) * 100,
);
protected readonly averageScore = computed(() => {
const values = this.scores();
return values.length === 0
? 0
: Math.round(values.reduce((sum, value) => sum + value, 0) / values.length);
});
ngOnInit(): void {
this.reportId = this.route.snapshot.paramMap.get('id') ?? '';
if (!this.reportId) {
this.errorMessage.set('Report not found.');
this.loading.set(false);
return;
}
this.reports.getById(this.reportId).subscribe({
next: (report) => {
this.report.set(report);
this.questions.set([
...report.technicalQuestions.map((q, order) => ({ ...q, type: 'Technical' as const, order })),
...report.behavioralQuestions.map((q, order) => ({ ...q, type: 'Behavioral' as const, order })),
]);
this.loading.set(false);
},
error: (error: unknown) => {
this.errorMessage.set(getErrorMessage(error));
this.loading.set(false);
},
});
}
protected scoreLevel(score: number): 'high' | 'mid' | 'low' {
return score >= 80 ? 'high' : score >= 60 ? 'mid' : 'low';
}
protected submit(): void {
const question = this.current();
const answer = this.answerText().trim();
if (!question || answer.length < 20 || this.scoring()) {
return;
}
this.scoring.set(true);
this.errorMessage.set(null);
this.mock
.submit(this.reportId, {
questionType: question.type,
questionOrder: question.order,
answerText: answer,
})
.subscribe({
next: (result) => {
this.feedback.set(result);
this.scores.update((values) => [...values, result.score]);
this.scoring.set(false);
},
error: (error: unknown) => {
this.errorMessage.set(getErrorMessage(error));
this.scoring.set(false);
},
});
}
protected next(): void {
this.feedback.set(null);
this.answerText.set('');
this.showModelAnswer.set(false);
if (this.index() + 1 >= this.total()) {
this.finished.set(true);
return;
}
this.index.update((value) => value + 1);
}
protected restart(): void {
this.index.set(0);
this.answerText.set('');
this.feedback.set(null);
this.finished.set(false);
this.showModelAnswer.set(false);
this.scores.set([]);
}
}