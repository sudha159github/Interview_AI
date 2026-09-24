import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { MockAnswer, SubmitMockAnswerRequest } from '../models/mock-interview.models';
@Injectable({ providedIn: 'root' })
export class MockInterviewService {
private readonly http = inject(HttpClient);
private url(reportId: string): string {
return `/api/interview-reports/${encodeURIComponent(reportId)}/mock-answers`;
}
submit(reportId: string, request: SubmitMockAnswerRequest): Observable<MockAnswer> {
return this.http.post<MockAnswer>(this.url(reportId), request);
}
list(reportId: string): Observable<MockAnswer[]> {
return this.http.get<MockAnswer[]>(this.url(reportId));
}
}
