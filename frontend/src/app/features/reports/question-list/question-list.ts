import { Component, input } from '@angular/core';
import { MatExpansionModule } from '@angular/material/expansion';
import { InterviewQuestion } from '../../../core/models/report.models';

@Component({
  selector: 'app-question-list',
  imports: [MatExpansionModule],
  templateUrl: './question-list.html',
  styleUrl: './question-list.scss',
})
export class QuestionList {
  readonly questions = input.required<InterviewQuestion[]>();
}