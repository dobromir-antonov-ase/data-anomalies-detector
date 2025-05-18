export interface FinanceSubmission {
  id: number;
  title: string;
  submissionDate: string;
  status: string;
  month: number;
  year: number;
  dealerId: number;
  masterTemplateId: number;
  cellCount?: number;
  cells?: FinanceSubmissionCell[];
}

export interface FinanceSubmissionCell {
  id: number;
  globalAddress: string;
  value: number;
  aggregationType: string;
} 