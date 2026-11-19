export type OrderStatus = 'Pending' | 'Paid' | 'OnHold' | 'Cancelled' | 'Refunded';
export type PaymentStatus = 'Pending' | 'Succeeded' | 'Failed' | 'Refunded';
export type PaymentKind = 'Sale' | 'Capture' | 'Refund';
export type DiscrepancyType = 'MissingPayment' | 'OrphanedPayment' | 'AmountMismatch' | 'DuplicatePayment' | 'CurrencyMismatch' | 'DuplicateInvoice';
export type DiscrepancyStatus = 'Open' | 'Resolved' | 'Ignored';

export interface Dashboard { totalRevenue:number; openDiscrepancyCount:number; totalAffectedAmount:number; orderCount:number; paymentCount:number; invoiceCount:number; }
export interface Discrepancy { id:string; type:DiscrepancyType; status:DiscrepancyStatus; title:string; description:string; affectedAmount:number; orderId:string|null; paymentId:string|null; invoiceId:string|null; detectedAtUtc:string; aiAnalysis:string|null; }
export interface Order { id:string; orderNumber:string; customerName:string; customerEmail:string; totalAmount:number; currency:string; status:OrderStatus; createdAtUtc:string; }
export interface Payment { id:string; orderId:string; transactionId:string; amount:number; currency:string; status:PaymentStatus; kind:PaymentKind; gateway:string|null; paidAtUtc:string; }
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
  financialIntegrityExplanation:string;
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

export type CaseAuditEventType = 'Detected' | 'Investigated' | 'ActionExecuted' | 'Resolved' | 'Ignored';

export interface CaseAuditEvent {
  id:string;
  eventType:CaseAuditEventType;
  description:string;
  actor:string;
  createdAtUtc:string;
}

export interface FinancialGraphNode {
  nodeType:FinancialNodeType;
  entityId:string|null;
  exists:boolean;
  status:string;
  identifier:string|null;
  amount:number|null;
  currency:string;
  createdAtUtc:string|null;
  detail:string|null;
}

export interface RootCauseAnalysis {
  analysis:string|null;
  recommendedAction:string|null;
  isAvailable:boolean;
  isFallback:boolean;
}

export interface FinancialCaseDetail {
  id:string;
  caseNumber:string;
  discrepancyId:string|null;
  title:string;
  description:string;
  severity:FinancialCaseSeverity;
  status:FinancialCaseStatus;
  affectedAmount:number;
  currency:string;
  detectedAtUtc:string;
  resolvedAtUtc:string|null;
  timeline:CaseAuditEvent[];
  graphNodes:FinancialGraphNode[];
  rootCauseAnalysis:RootCauseAnalysis;
}


export type ChartOfAccounts = 'SKR03' | 'SKR04';

export interface AccountingSettings {
  id:string;
  tenantId:string;
  beraternummer:string;
  mandantennummer:string;
  chartOfAccounts:ChartOfAccounts;
  revenueAccount19:string;
  revenueAccount7:string;
  paymentClearingAccount:string;
  feeAccount:string;
  updatedAtUtc:string;
}

export interface UpdateAccountingSettingsRequest {
  beraternummer:string;
  mandantennummer:string;
  chartOfAccounts:ChartOfAccounts;
  revenueAccount19:string;
  revenueAccount7:string;
  paymentClearingAccount:string;
  feeAccount:string;
}


export type IntegrationProvider = 'Shopify' | 'Shopware' | 'WooCommerce' | 'Amazon' | 'Stripe' | 'PayPal' | 'Custom';
export type ConnectionStatus = 'Connected' | 'Warning' | 'Disconnected' | 'Active' | 'Revoked';

export interface IntegrationConnection {
  id:string;
  tenantId:string;
  provider:IntegrationProvider;
  externalStoreId:string;
  displayName:string;
  status:ConnectionStatus;
  createdAtUtc:string;
  lastSeenAtUtc:string|null;
  shopDomain:string|null;
  scopes:string;
  lastSyncAtUtc:string|null;
  lastWebhookAtUtc:string|null;
  apiKey:string|null;
}

export interface ShopifySessionInfo {
  mode:string;
  authenticated:boolean;
  shopDomain:string;
  userId:string;
  sessionId:string;
  expiresAtUtc:string;
  tenantId:string|null;
  connectionId:string|null;
}

export interface ShopifySyncResult {
  connectionId:string;
  ordersImported:number;
  paymentsImported:number;
  skippedOrders:number;
  completedAtUtc:string;
}
