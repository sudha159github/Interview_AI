import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { InterviewReportSummary } from '../../../core/models/report.models';
import { ReportsService } from '../../../core/services/reports.service';
import { getErrorMessage } from '../../../core/utils/api-error';
/** Must match the API: 5 MB, PDF or DOCX only. */
const MAX_RESUME_BYTES = 5 * 1024 * 1024;
const ALLOWED_EXTENSIONS = ['.pdf', '.docx'];
@Component({
selector: 'app-home',
imports: [DatePipe, ReactiveFormsModule, RouterLink],
templateUrl: './home.html',
styleUrl: './home.scss',
})
export class Home implements OnInit {
private readonly fb = inject(FormBuilder);
private readonly reports = inject(ReportsService);
private readonly router = inject(Router);
// ---------- generate form ----------
protected readonly generating = signal(false);
protected readonly errorMessage = signal<string | null>(null);
protected readonly form = this.fb.nonNullable.group({
jobDescription: ['', [Validators.required, Validators.minLength(50), Validators.maxLength(5000)]],
selfDescription: ['', [Validators.minLength(20), Validators.maxLength(3000)]],
companyName: ['', [Validators.maxLength(200)]],
interviewDate: [''],
});
/** Today as yyyy-mm-dd, used as the minimum for the date input. */
protected readonly todayIso = new Date().toISOString().slice(0, 10);
// ---------- resume file ----------
protected readonly resumeFile = signal<File | null>(null);
protected readonly resumeError = signal<string | null>(null);
protected readonly dragging = signal(false);
/** Business rule: a resume, a self-description, or both. */
protected readonly missingProfile = computed(
() => this.resumeFile() === null && this.form.controls.selfDescription.value.trim().length === 0,
);
// ---------- my reports ----------
protected readonly myReports = signal<InterviewReportSummary[]>([]);
protected readonly loadingReports = signal(true);
protected readonly reportsError = signal<string | null>(null);
ngOnInit(): void {
this.loadReports();
}
// ---------- file handling ----------
protected onFileSelected(event: Event): void {
const input = event.target as HTMLInputElement;
this.acceptFile(input.files?.[0] ?? null);
input.value = ''; // allows re-picking the same file after removing it
}
protected onDragOver(event: DragEvent): void {
event.preventDefault();
this.dragging.set(true);
}
protected onDragLeave(event: DragEvent): void {
event.preventDefault();
this.dragging.set(false);
}
protected onDrop(event: DragEvent): void {
event.preventDefault();
this.dragging.set(false);
this.acceptFile(event.dataTransfer?.files?.[0] ?? null);
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
/** Checks type and size before accepting a file. The API checks again. */
private acceptFile(file: File | null): void {
this.resumeError.set(null);
if (!file) {
return;
}
const extension = file.name.slice(file.name.lastIndexOf('.')).toLowerCase();
if (!ALLOWED_EXTENSIONS.includes(extension)) {
this.resumeError.set('Please choose a PDF or DOCX file.');
return;
}
if (file.size > MAX_RESUME_BYTES) {
this.resumeError.set('The file must be 5 MB or smaller.');
return;
}
this.resumeFile.set(file);
}
// ---------- submit ----------
protected generate(): void {
if (this.form.invalid || this.missingProfile()) {
this.form.markAllAsTouched();
return;
}
this.generating.set(true);
this.errorMessage.set(null);
this.form.disable();
const { jobDescription, selfDescription, companyName, interviewDate } = this.form.getRawValue();
this.reports
.create({
jobDescription,
selfDescription: selfDescription.trim() || null,
resume: this.resumeFile(),
companyName: companyName.trim() || null,
interviewDate: interviewDate ? new Date(interviewDate) : null,
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