export type OrderStatus = 'Pending' | 'Paid' | 'OnHold' | 'Cancelled' | 'Refunded';
export type PaymentStatus = 'Pending' | 'Succeeded' | 'Failed' | 'Refunded';
export type DiscrepancyType = 'MissingPayment' | 'OrphanedPayment' | 'AmountMismatch' | 'DuplicateInvoice';
export type DiscrepancyStatus = 'Open' | 'Resolved' | 'Ignored';

export interface Dashboard { totalRevenue:number; openDiscrepancyCount:number; totalAffectedAmount:number; orderCount:number; paymentCount:number; invoiceCount:number; }
export interface Discrepancy { id:string; type:DiscrepancyType; status:DiscrepancyStatus; title:string; description:string; affectedAmount:number; orderId:string|null; paymentId:string|null; invoiceId:string|null; detectedAtUtc:string; aiAnalysis:string|null; }
export interface Order { id:string; orderNumber:string; customerName:string; customerEmail:string; totalAmount:number; currency:string; status:OrderStatus; createdAtUtc:string; }
export interface Payment { id:string; orderId:string; transactionId:string; amount:number; currency:string; status:PaymentStatus; paidAtUtc:string; }
export interface Invoice { id:string; invoiceNumber:string; supplierName:string; amount:number; currency:string; invoiceDateUtc:string; rawText:string; }
export interface ReconciliationResult { createdCount:number; discrepancies:Discrepancy[]; }
export interface AiAnalysisRequest { invoiceId?:string; rawText:string; }
export interface AiAnalysisResponse { analysis:string; isFallback:boolean; }

export type FinancialCaseStatus = 'Open' | 'Investigating' | 'Resolved' | 'Ignored';
export type FinancialCaseSeverity = 'Low' | 'Medium' | 'High' | 'Critical';
export type FinancialNodeType = 'Order' | 'Payment' | 'Invoice' | 'Payout' | 'BankTransaction' | 'Discrepancy';
export type FinancialRelationType = 'PaymentFor' | 'InvoiceFor' | 'SettlementFor' | 'BankDepositFor' | 'RelatedTo';

export interface FinancialCaseLink {
  nodeType:FinancialNodeType;
  entityId:string;
  role:string;
  label:string;
  amount:number|null;
  currency:string;
}

export interface FinancialCase {
  id:string;
  discrepancyId:string|null;
  status:FinancialCaseStatus;
  severity:FinancialCaseSeverity;
  title:string;
  description:string;
  rootCause:string|null;
  affectedAmount:number;
  currency:string;
  detectedAtUtc:string;
  resolvedAtUtc:string|null;
  links:FinancialCaseLink[];
}

export interface FinancialGraphRelation {
  id:string;
  fromNodeType:FinancialNodeType;
  fromEntityId:string;
  toNodeType:FinancialNodeType;
  toEntityId:string;
  relationType:FinancialRelationType;
}

export interface SystemHealth {
  connectionId:string|null;
  name:string;
  provider:string;
  status:'Healthy'|'Warning'|'Disconnected';
  detail:string|null;
  lastSeenAtUtc:string|null;
}

export interface ControlTower {
  financialIntegrityScore:number;
  totalRevenue:number;
  expectedCash:number;
  actualCash:number;
  cashGap:number;
  openCaseCount:number;
  highRiskCaseCount:number;
  affectedAmount:number;
  systemHealth:SystemHealth[];
  cases:FinancialCase[];
}
