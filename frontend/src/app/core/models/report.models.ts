export interface InterviewQuestion {
question: string;
intention: string;
suggestedAnswer: string;
}
export type SkillGapSeverity = 'Low' | 'Medium' | 'High';
export interface SkillGap {
skill: string;
severity: SkillGapSeverity;
}
export interface PreparationDay {
dayNumber: number;
focus: string;
tasks: string[];
}
export type ApplicationStatus = 'Planned' | 'Applied' | 'Interviewing' | 'Offer' | 'Rejected';
export const APPLICATION_STATUSES: ApplicationStatus[] = [
'Planned',
'Applied',
'Interviewing',
'Offer',
'Rejected',
];
export interface InterviewReport {
id: string;
title: string;
companyName: string | null;
matchScore: number;
createdAt: string;
interviewDate: string | null;
status: ApplicationStatus;
daysUntilInterview: number | null;
technicalQuestions: InterviewQuestion[];
behavioralQuestions: InterviewQuestion[];
skillGaps: SkillGap[];
preparationPlan: PreparationDay[];
}
export interface InterviewReportSummary {
id: string;
title: string;
companyName: string | null;
matchScore: number;
createdAt: string;
interviewDate: string | null;
status: ApplicationStatus;
daysUntilInterview: number | null;
}
export interface UpdateInterviewReportRequest {
companyName: string | null;
interviewDate: string | null;
status: ApplicationStatus;
}