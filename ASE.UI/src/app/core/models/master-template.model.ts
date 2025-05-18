export interface MasterTemplate {
  id: number;
  name: string;
  year: number;
  isActive: boolean;
  createdDate: string;
  sheetCount?: number;
  tableCount?: number;
  cellCount?: number;
} 