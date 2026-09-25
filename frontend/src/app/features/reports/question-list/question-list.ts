import { Component, input, signal } from '@angular/core';
import { InterviewQuestion } from '../../../core/models/report.models';
@Component({
selector: 'app-question-list',
imports: [],
templateUrl: './question-list.html',
styleUrl: './question-list.scss',
})
export class QuestionList {
readonly questions = input.required<InterviewQuestion[]>();
/** Index of the open question, or null when all are closed. */
protected readonly openIndex = signal<number | null>(0);
protected toggle(index: number): void {
this.openIndex.update((current) => (current === index ? null : index));
}
}