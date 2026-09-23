export interface CreateInterviewReportRequest {
  jobDescription: string;
  selfDescription: string;
}

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

export interface InterviewReport {
  id: string;
  title: string;
  matchScore: number;
  createdAt: string;
  technicalQuestions: InterviewQuestion[];
  behavioralQuestions: InterviewQuestion[];
  skillGaps: SkillGap[];
  preparationPlan: PreparationDay[];
}

export interface InterviewReportSummary {
  id: string;
  title: string;
  matchScore: number;
  createdAt: string;
}