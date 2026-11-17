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
