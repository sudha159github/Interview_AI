import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import {
InterviewReport,
InterviewReportSummary,
UpdateInterviewReportRequest,
} from '../models/report.models';
@Injectable({ providedIn: 'root' })
export class ReportsService {
private readonly http = inject(HttpClient);
private readonly baseUrl = '/api/interview-reports';
/**
* Generate a new report. Sent as multipart/form-data because it may include a file.
* The browser sets the Content-Type header (with the boundary) automatically.
*/
create(input: {
jobDescription: string;
selfDescription?: string | null;
resume?: File | null;
companyName?: string | null;
interviewDate?: Date | null;
}): Observable<InterviewReport> {
const form = new FormData();
form.append('jobDescription', input.jobDescription);
if (input.selfDescription) {
form.append('selfDescription', input.selfDescription);
}
if (input.resume) {
form.append('resume', input.resume, input.resume.name);
}
if (input.companyName) {
form.append('companyName', input.companyName);
}
if (input.interviewDate) {
form.append('interviewDate', input.interviewDate.toISOString());
}
return this.http.post<InterviewReport>(this.baseUrl, form);
}
list(): Observable<InterviewReportSummary[]> {
return this.http.get<InterviewReportSummary[]>(this.baseUrl);
}
getById(id: string): Observable<InterviewReport> {
return this.http.get<InterviewReport>(`${this.baseUrl}/${encodeURIComponent(id)}`);
}
update(id: string, request: UpdateInterviewReportRequest): Observable<InterviewReport> {
return this.http.patch<InterviewReport>(`${this.baseUrl}/${encodeURIComponent(id)}`, request);
}
}