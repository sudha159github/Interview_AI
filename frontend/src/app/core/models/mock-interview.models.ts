export interface SubmitMockAnswerRequest {
questionType: 'Technical' | 'Behavioral';
questionOrder: number;
answerText: string;
}
export interface MockAnswer {
id: number;
questionType: 'Technical' | 'Behavioral';
questionOrder: number;
questionText: string;
answerText: string;
score: number;
starAssessment: string;
strengths: string[];
improvements: string[];
missingKeywords: string[];
createdAt: string;
}