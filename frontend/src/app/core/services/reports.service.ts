import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import {
  CreateInterviewReportRequest,
  InterviewReport,
  InterviewReportSummary,
} from '../models/report.models';

@Injectable({ providedIn: 'root' })
export class ReportsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/interview-reports';

  /** Generate a new report (the AI call happens on the server). */
  create(request: CreateInterviewReportRequest): Observable<InterviewReport> {
    return this.http.post<InterviewReport>(this.baseUrl, request);
  }

  /** The current user's reports, newest first. */
  list(): Observable<InterviewReportSummary[]> {
    return this.http.get<InterviewReportSummary[]>(this.baseUrl);
  }

  /** One full report. Fails with 404 if it doesn't exist or isn't ours. */
  getById(id: string): Observable<InterviewReport> {
    return this.http.get<InterviewReport>(`${this.baseUrl}/${encodeURIComponent(id)}`);
  }
}