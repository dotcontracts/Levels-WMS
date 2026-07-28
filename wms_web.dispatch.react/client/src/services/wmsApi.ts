// WMS API Service - uses the .NET Web API endpoints
import { useAuth } from '../context/AuthContext';
import { API_CONFIG, getAuthHeaders } from '../config/api';

// Use centralized API configuration
const API_BASE_URL = API_CONFIG.BASE_URL;


// Updated interfaces to match .NET API structure

export interface Parcel {
  id: string;
  createdAt: string;
  updatedAt?: string;
  waybillNumber: string;
  qrCode?: string;
  dispatchedAt?: string;
  dispatchTrackingCode?: string;
  createdById: number;
  sender: string;
  senderTelephone: string;
  receiver: string;
  receiverTelephone: string;
  destination: string;
  quantity: number;
  description: string;
  amount: number;
  rate: number;
  paymentMethods: string;
  totalAmount: number;
  totalRate: number;
  status: number; // 0=Pending, 1=Confirmed, 2=InTransit, 3=Delivered, 4=Cancelled
  createdBy?: any;
  // Payment tracking fields
  amountPaid?: number;
  transactionCode?: string;
  paymentUpdatedAt?: string;
  paymentUpdatedBy?: number;
  // History tracking fields
  confirmedById?: number;
  confirmedByName?: string;
  confirmedAt?: string;
  dispatchedById?: number;
  dispatchedByName?: string;
  deliveredById?: number;
  deliveredByName?: string;
  deliveredAt?: string;
}

export interface Dispatch {
  id: string;
  dispatchCode: string;
  destination: string;
  dispatchTime: string;
  sourceBranch: string;
  vehicleNumber: string;
  driver: string;
  driverPhone?: string;
  status: string;
  totalParcels?: number;
  totalAmount?: number;
  parcelIds: string[] | { $values: string[] };
  parcels?: Parcel[];
}

export interface CreateDispatchRequest {
  dispatchCode: string;
  sourceBranch: string;
  destination: string;
  vehicleNumber: string;
  driver: string;
  driverPhone?: string;
  parcelIds: string[];
}

export interface User {
  id: number;
  username: string;
  email?: string;
  firstName?: string;
  lastName?: string;
  roles: string[];
  role?: {
    id: number;
    name: string;
    canConfirm?: boolean;
    canDispatch?: boolean;
  };
  branch?: {
    id: number;
    name: string;
  } | string;
  createdAt?: string;
  branchId?: number;
  roleId?: number;
  isActive?: boolean;
  canConfirm?: boolean;
  canDispatch?: boolean;
}

export interface ParcelDepositDto {
  id: number;
  parcelId: string;
  waybillNumber?: string;
  depositedAmount: number;
  expenses: number;
  notes?: string;
  createdAt: string;
  updatedAt: string;
  createdById?: number;
  createdByName?: string;
  updatedById?: number;
  updatedByName?: string;
  remainingDebt: number;
  parcelTotalAmount: number;
  parcelSender?: string;
  parcelReceiver?: string;
  parcelDestination?: string;
}

export interface CreateParcelDepositDto {
  parcelId: string;
  depositedAmount: number;
  expenses: number;
  notes?: string;
  createdById?: number;
}

export interface UpdateParcelDepositDto {
  depositedAmount: number;
  expenses: number;
  notes?: string;
  updatedById?: number;
}

export interface ClerkCashInSummaryDto {
  clerkId: number;
  clerkName: string;
  clerkUsername: string;
  totalPaidAmount: number;
  totalDeposited: number;
  totalExpenses: number;
  remainingDebt: number;
  parcelCount: number;
  lastUpdateDate?: string;
}

export interface ContractCustomer {
  id: number;
  name: string;
  companyName?: string;
  email?: string;
  phone?: string;
  address?: string;
  contactPerson?: string;
  contractNumber?: string;
  paymentTerms: string;
  taxRate: number;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
  createdBy?: number;
  apiUserId?: number;
  apiUsername?: string;
}

export interface Invoice {
  id: number;
  invoiceNumber: string;
  contractCustomerId: number;
  customerId: number;
  customer?: ContractCustomer;
  billingPeriodStart: string;
  billingPeriodEnd: string;
  issueDate: string;
  dueDate: string;
  status: 'draft' | 'sent' | 'paid' | 'overdue' | 'cancelled';
  subtotal: number;
  taxAmount: number;
  totalAmount: number;
  paidAmount: number;
  notes?: string;
  createdById: number;
  createdBy?: User;
  createdAt: string;
  updatedAt: string;
  items?: InvoiceItem[];
  payments?: InvoicePayment[];
}

export interface InvoiceItem {
  id: number;
  invoiceId: number;
  parcelId: string;
  waybillNumber: string;
  description: string;
  destination: string;
  parcelCreatedAt: string;
  quantity: number;
  totalPrice: number;
  createdAt: string;
}

export interface InvoicePayment {
  id: number;
  invoiceId: number;
  paymentDate: string;
  amount: number;
  paymentMethod: 'cash' | 'cheque' | 'bank_transfer' | 'mobile_money';
  chequeNumber?: string;
  bankReference?: string;
  notes?: string;
  recordedBy?: number;
  recordedByName?: string;
  createdAt: string;
}

export interface CreateInvoiceDto {
  contractCustomerId: number;
  issueDate: string;
  dueDate: string;
  billingPeriodStart: string;
  billingPeriodEnd: string;
  notes?: string;
  createdById: number;
  parcelIds: string[];
}

export interface CreateInvoicePaymentDto {
  invoiceId: number;
  amount: number;
  paymentMethod: 'cash' | 'cheque' | 'bank_transfer' | 'mobile_money';
  paymentDate: string;
  chequeNumber?: string;
  bankReference?: string;
  notes?: string;
}

// COD Collection Management Types
export interface CODCollection {
  id: number;
  branchId: number;
  branchName: string;
  collectionDate: string;
  dispatchCode: string;
  driverName: string;
  vehicleNumber: string;
  totalCODAmount: number;
  depositedAmount: number;
  shortfall: number;
  parcelIds: string[];
  collectedBy: number;
  collectedByName?: string;
  depositedAt?: string;
  depositedBy?: number;
  depositedByName?: string;
  status: 'collected' | 'deposited' | 'reconciled' | 'shortfall';
  notes?: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateCODCollectionDto {
  branchId: number;
  dispatchCode: string;
  driverName: string;
  vehicleNumber: string;
  totalCODAmount: number;
  parcelIds: string[];
  collectionDate?: string;
  notes?: string;
}

export interface UpdateCODCollectionDto {
  depositedAmount: number;
  depositedAt: string;
  notes?: string;
}

// Daily Expense Management Types
export interface DailyExpense {
  id: number;
  date: string;
  category: 'fuel' | 'casual_labor' | 'hired_cars' | 'maintenance' | 'office_supplies' | 'utilities' | 'transport' | 'other';
  description: string;
  amount: number;
  branchId?: number;
  branchName?: string;
  clerkId?: number;
  clerkName?: string;
  approvedBy?: number;
  approvedByName?: string;
  receiptNumber?: string;
  vendor?: string;
  status: 'pending' | 'approved' | 'rejected';
  rejectionReason?: string;
  createdBy: number;
  createdByName?: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateDailyExpenseDto {
  date: string;
  category: 'fuel' | 'casual_labor' | 'hired_cars' | 'maintenance' | 'office_supplies' | 'utilities' | 'transport' | 'other';
  description: string;
  amount: number;
  branchId?: number;
  clerkId?: number;
  branchName?: string;
  clerkName?: string;
  receiptNumber?: string;
  vendor?: string;
  notes?: string;
}

export interface ApproveExpenseDto {
  expenseId: number;
  approved: boolean;
  rejectionReason?: string;
  notes?: string;
}

export interface ExpensesSummary {
  totalExpenses: number;
  pendingExpenses: number;
  approvedExpenses: number;
  rejectedExpenses: number;
  monthlyTotal: number;
  categoryBreakdown: {
    category: string;
    amount: number;
    count: number;
  }[];
}

// Enhanced Clerk Cash Management Types
export interface EnhancedClerkCashSummary {
  id: number;
  clerkId: number;
  clerkName: string;
  clerkUsername: string;
  date: string;
  totalPaidAmount: number;
  depositedAmount: number;
  totalExpenses: number;
  netDebt: number;
  parcelCount: number;
  expenseCount: number;
  lastDepositDate?: string;
  status: 'pending' | 'deposited' | 'reconciled' | 'overdue';
  createdAt: string;
  updatedAt: string;
}

export interface CreateClerkDepositDto {
  clerkId: number;
  depositAmount: number;
  depositDate: string;
  notes?: string;
}

// Cheque Management Types
export interface ChequeDeposit {
  id: number;
  chequeNumber: string;
  amount: number;
  bankName: string;
  accountNumber?: string;
  drawerName: string;
  depositDate: string;
  clearanceDate?: string;
  status: 'deposited' | 'cleared' | 'bounced' | 'cancelled';
  relatedInvoiceId?: number;
  relatedInvoiceNumber?: string;
  depositedBy: number;
  depositedByName?: string;
  bounceReason?: string;
  notes?: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateChequeDepositDto {
  chequeNumber: string;
  amount: number;
  bankName: string;
  accountNumber?: string;
  drawerName: string;
  depositDate: string;
  relatedInvoiceId?: number;
  notes?: string;
}

export interface UpdateChequeStatusDto {
  status: 'cleared' | 'bounced' | 'cancelled';
  clearanceDate?: string;
  bounceReason?: string;
  notes?: string;
}

class WMSApiService {
  private requestCache: Map<string, Promise<any>> = new Map();
  private cacheTimeout: Map<string, number> = new Map();
  private readonly CACHE_DURATION = 30000; // 30 second cache for duplicate requests
  private activeProgressiveLoads: Set<string> = new Set();
  
  // Generate cache key from filters for consistent cache sharing
  private generateCacheKey(filters: any): string {
    // Sort keys for consistent hash generation
    const sortedFilters = Object.keys(filters || {})
      .sort()
      .reduce((result: any, key: string) => {
        if (filters[key] !== undefined && filters[key] !== null && filters[key] !== '') {
          result[key] = filters[key];
        }
        return result;
      }, {});
    
    // Generate hash from sorted filters
    const filterString = JSON.stringify(sortedFilters);
    return btoa(filterString).replace(/[+/=]/g, '').substring(0, 16);
  }
  
  private getHeaders(): Record<string, string> {
    // Use centralized auth headers from config
    return getAuthHeaders();
  }

  public clearRequestCache(): void {
    this.requestCache.clear();
    this.cacheTimeout.clear();
  }

  // Request deduplication helper
  private async deduplicate<T>(key: string, requestFn: () => Promise<T>): Promise<T> {
    // Clean up expired cache entries
    const now = Date.now();
    const expiredKeys: string[] = [];
    this.cacheTimeout.forEach((expiry, cacheKey) => {
      if (now > expiry) {
        expiredKeys.push(cacheKey);
      }
    });
    expiredKeys.forEach(cacheKey => {
      this.requestCache.delete(cacheKey);
      this.cacheTimeout.delete(cacheKey);
    });

    // Check if we already have this request in flight
    if (this.requestCache.has(key)) {
      console.log(`[Deduplication] Reusing existing request for: ${key}`);
      return this.requestCache.get(key) as Promise<T>;
    }

    // Create new request and cache it
    console.log(`[Deduplication] Creating new request for: ${key}`);
    const promise = requestFn();
    this.requestCache.set(key, promise);
    this.cacheTimeout.set(key, now + this.CACHE_DURATION);

    // Clean up when request completes
    promise.finally(() => {
      setTimeout(() => {
        this.requestCache.delete(key);
        this.cacheTimeout.delete(key);
      }, this.CACHE_DURATION);
    });

    return promise;
  }

  // Parcels API methods
  async getParcels(branchName?: string, page?: number, pageSize?: number): Promise<Parcel[]> {
    try {
      let url = `${API_BASE_URL}/Parcels`;
      
      // Add query parameters
      const params = new URLSearchParams();
      if (branchName) {
        params.append('branchName', branchName);
      }
      if (page !== undefined) {
        params.append('page', page?.toString());
      }
      if (pageSize !== undefined) {
        params.append('pageSize', pageSize?.toString());
      }
      
      const queryString = params.toString();
      if (queryString) {
        url += `?${queryString}`;
      }
      
      const response = await fetch(url, {
        headers: this.getHeaders(),
      });

      if (!response.ok) {
        throw new Error(`Failed to fetch parcels: ${response.status}`);
      }

      const rawData = await response.json();

      // Handle the { "$id": ..., "$values": [...] } format
      if (typeof rawData === 'object' && rawData !== null && '$values' in rawData && Array.isArray(rawData.$values)) {
        return rawData.$values;
      } else if (Array.isArray(rawData)) {
        return rawData;
      } else {
        return [];
      }
    } catch (error) {
      throw new Error(`Failed to connect to WMS API: ${error}`);
    }
  }

  // Paginated parcels endpoint for better performance with request deduplication
  async getParcelsPaged(params: {
    page?: number;
    pageSize?: number;
    destination?: string;
    status?: number;
    statuses?: string;
    fromDate?: string;
    toDate?: string;
    createdByUsername?: string;
    search?: string;
    extendedTimeout?: boolean; // Add option for longer timeout
    instanceId?: string; // Add instance ID for progressive loading isolation
  }): Promise<{
    data: Parcel[];
    page: number;
    pageSize: number;
    totalCount: number;
    totalPages: number;
    hasNextPage: boolean;
    hasPreviousPage: boolean;
    currentPage: number;
  }> {
    try {
      const queryParams = new URLSearchParams();
      
      if (params.page !== undefined) queryParams.append('page', params.page.toString());
      if (params.pageSize !== undefined) queryParams.append('pageSize', params.pageSize.toString());
      if (params.destination) queryParams.append('destination', params.destination);
      if (params.status !== undefined) queryParams.append('status', params.status.toString());
      if (params.statuses) queryParams.append('statuses', params.statuses);
      if (params.fromDate) queryParams.append('fromDate', params.fromDate);
      if (params.toDate) queryParams.append('toDate', params.toDate);
      if (params.createdByUsername) queryParams.append('createdByUsername', params.createdByUsername);
      if (params.search) queryParams.append('search', params.search);

      const url = `${API_BASE_URL}/Parcels/paged?${queryParams.toString()}`;
      
      const requestKey = params.instanceId 
        ? `getParcelsPaged:${params.instanceId}:${queryParams.toString()}`
        : `getParcelsPaged:${queryParams.toString()}`;
      
      return await this.deduplicate(requestKey, async () => {
        return this._fetchParcelsPaged(url, params);
      });
      
    } catch (error) {
      throw new Error(`Failed to connect to WMS API: ${error}`);
    }
  }

  private async _fetchParcelsPaged(url: string, params: any): Promise<{
    data: Parcel[];
    page: number;
    pageSize: number;
    totalCount: number;
    totalPages: number;
    hasNextPage: boolean;
    hasPreviousPage: boolean;
    currentPage: number;
  }> {
    // Use longer timeout for large pages or when explicitly requested
    const timeout = params.extendedTimeout || (params.pageSize && params.pageSize > 500) 
      ? API_CONFIG.TIMEOUT * 3  // 45 seconds for large pages
      : API_CONFIG.TIMEOUT;      // 15 seconds default
    
    const response = await fetch(url, {
      headers: this.getHeaders(),
      signal: AbortSignal.timeout(timeout),
    });

    if (!response.ok) {
      throw new Error(`Failed to fetch parcels: ${response.status}`);
    }

    const result = await response.json();
    
    // Ensure we return currentPage for consistency with our context
    return {
      ...result,
      currentPage: result.page || params.page || 1
    };
  }

  // Progressive loading for complete datasets - optimized to avoid redundant count queries
  async getAllParcelsProgressively(options: {
    onProgress?: (loaded: number, total: number) => void;
    chunkSize?: number;
    dateRange?: { from: string; to: string };
    fields?: string[];
    destination?: string;
    search?: string;
  } = {}): Promise<Parcel[]> {
    const {
      onProgress,
      chunkSize = 1000,
      dateRange,
      fields,
      destination,
      search,
    } = options;

    const instanceId = `instance_${Math.random().toString(36).substr(2, 9)}`;
    const loadKey = `getAllParcelsProgressively:${instanceId}`;

    try {
      console.log(`[Performance] Starting parallel data loading with chunk size ${chunkSize} (instance: ${instanceId})`);
      
      // Register this active load
      this.activeProgressiveLoads.add(loadKey);
      
      // First, get the first page to determine total count and show immediate results
      const firstPageParams = {
        page: 1,
        pageSize: chunkSize,
        ...(dateRange && { fromDate: dateRange.from, toDate: dateRange.to }),
        ...(destination && { destination }),
        ...(search && { search }),
        extendedTimeout: true, // Use extended timeout for large pages
        instanceId: instanceId // Pass instance ID for cache isolation
      };

      const firstResponse = await this.getParcelsPaged(firstPageParams);
      const totalCount = firstResponse.totalCount;
      
      if (totalCount === 0) {
        onProgress?.(0, 0);
        return [];
      }

      // Report immediate progress with first page
      const allParcels: Parcel[] = [...firstResponse.data];
      onProgress?.(allParcels.length, totalCount);
      console.log(`[Performance] First page loaded: ${allParcels.length}/${totalCount} records`);

      // Calculate remaining pages needed
      const totalPages = Math.ceil(totalCount / chunkSize);
      if (totalPages <= 1) {
        console.log(`[Performance] Only one page needed, returning ${allParcels.length} records`);
        return allParcels;
      }

      // Define parallel batch size to avoid overwhelming the server
      const PARALLEL_BATCH_SIZE = 6; // Reduced from 8 to 6 for better stability
      const remainingPages = Array.from({ length: totalPages - 1 }, (_, i) => i + 2); // Pages 2, 3, 4, ...
      
      console.log(`[Performance] Loading ${remainingPages.length} additional pages in batches of ${PARALLEL_BATCH_SIZE}`);
      console.log(`[Performance] Total expected records: ${totalCount} across ${totalPages} pages`);

      // Process pages in parallel batches
      let consecutiveFailures = 0;
      const MAX_CONSECUTIVE_FAILURES = 3;
      
      console.log(`[Performance] v2.1 - Starting parallel loading with ${remainingPages.length} pages total (instance: ${instanceId})`);
      
      for (let i = 0; i < remainingPages.length; i += PARALLEL_BATCH_SIZE) {
        const batch = remainingPages.slice(i, i + PARALLEL_BATCH_SIZE);
        
        // Create parallel requests for this batch
        const batchPromises = batch.map(pageNum => {
          const params = {
            page: pageNum,
            pageSize: chunkSize,
            ...(dateRange && { fromDate: dateRange.from, toDate: dateRange.to }),
            ...(destination && { destination }),
            ...(search && { search }),
            extendedTimeout: true, // Use extended timeout for large pages
            instanceId: instanceId // Pass instance ID for cache isolation
          };
          return this.getParcelsPaged(params);
        });

        try {
          console.log(`[Performance] Starting batch ${Math.floor(i/PARALLEL_BATCH_SIZE) + 1}, pages ${batch.join(', ')}`);
          
          // Execute batch in parallel with individual error handling
          const batchResults = await Promise.allSettled(batchPromises);
          
          // Process results - both fulfilled and rejected
          let batchSuccessCount = 0;
          for (let i = 0; i < batchResults.length; i++) {
            const result = batchResults[i];
            const pageNum = batch[i];
            
            if (result.status === 'fulfilled') {
              allParcels.push(...result.value.data);
              batchSuccessCount++;
            } else {
              console.warn(`[Performance] Page ${pageNum} failed:`, result.reason);
              
              // Try once more with sequential approach for failed pages
              try {
                const params = {
                  page: pageNum,
                  pageSize: chunkSize,
                  ...(dateRange && { fromDate: dateRange.from, toDate: dateRange.to }),
                  ...(destination && { destination }),
                  ...(search && { search }),
                  extendedTimeout: true // Use extended timeout for retries
                };
                const response = await this.getParcelsPaged(params);
                allParcels.push(...response.data);
                batchSuccessCount++;
                console.log(`[Performance] Retry successful for page ${pageNum}`);
              } catch (retryError) {
                console.error(`[Performance] Retry also failed for page ${pageNum}:`, retryError);
                // Continue with other pages
              }
            }
          }

          // Report progress after each batch
          onProgress?.(allParcels.length, totalCount);
          console.log(`[Performance] Batch complete: ${batchSuccessCount}/${batch.length} pages successful, ${allParcels.length}/${totalCount} records (${Math.round(allParcels.length/totalCount*100)}%)`);
          
          // Check for consecutive failures
          if (batchSuccessCount === 0) {
            consecutiveFailures++;
            console.error(`[Performance] Entire batch failed! Consecutive failures: ${consecutiveFailures}/${MAX_CONSECUTIVE_FAILURES}`);
            
            if (consecutiveFailures >= MAX_CONSECUTIVE_FAILURES) {
              console.error(`[Performance] Too many consecutive failures. Stopping at ${allParcels.length} records.`);
              break; // Stop loading if we keep failing
            }
          } else {
            consecutiveFailures = 0; // Reset on success
          }

        } catch (batchError) {
          console.error(`[Performance] Unexpected batch error:`, batchError);
        }

        // Longer delay between batches to reduce server load and prevent timeouts
        if (i + PARALLEL_BATCH_SIZE < remainingPages.length) {
          console.log(`[Performance] Waiting before next batch...`);
          await new Promise(resolve => setTimeout(resolve, 500)); // Increased from 100ms to 500ms
        }
      }

      console.log(`[Performance] Parallel loading complete: ${allParcels.length} records loaded`);
      
      // Verify we loaded all expected records
      if (allParcels.length < totalCount) {
        console.warn(`[Performance] Warning: Loaded ${allParcels.length} of ${totalCount} expected records (${Math.round(allParcels.length/totalCount*100)}%)`);
      } else {
        console.log(`[Performance] Successfully loaded all ${totalCount} records`);
      }
      
      return allParcels;

    } catch (error) {
      console.error(`[Performance] Parallel loading failed, attempting fallback:`, error);
      
      // Ultimate fallback: try to get at least some data
      try {
        const fallbackResponse = await this.getParcelsPaged({
          page: 1,
          pageSize: Math.min(chunkSize * 2, 2000), // Try larger single page
          ...(dateRange && { fromDate: dateRange.from, toDate: dateRange.to }),
          ...(destination && { destination }),
          ...(search && { search }),
          instanceId: instanceId // Pass instance ID for cache isolation
        });
        
        console.log(`[Performance] Fallback successful: ${fallbackResponse.data.length} records`);
        onProgress?.(fallbackResponse.data.length, fallbackResponse.totalCount);
        return fallbackResponse.data;
        
      } catch (fallbackError) {
        throw new Error(`Failed to load parcels: ${error}. Fallback also failed: ${fallbackError}`);
      }
    } finally {
      // Always clean up active load tracking
      this.activeProgressiveLoads.delete(loadKey);
      console.log(`[Performance] Cleaned up active load tracking (instance: ${instanceId})`);
    }
  }

  // Optimized for financial calculations with larger, more efficient chunks
  async getAllParcelsForFinancial(dateRange?: { from: string; to: string }): Promise<Parcel[]> {
    try {
      return await this.getAllParcelsProgressively({
        dateRange,
        chunkSize: 1500, // Larger chunks for better parallel performance
        fields: ['id', 'waybillNumber', 'totalAmount', 'paymentMethods', 'createdAt', 'status', 'createdById']
      });
    } catch (error) {
      console.warn('Failed to load financial data progressively, falling back to direct pagination');
      // Fallback to direct pagination with larger page size
      try {
        const response = await this.getParcelsPaged({
          page: 1,
          pageSize: 2000, // Larger fallback page size
          ...(dateRange && { fromDate: dateRange.from, toDate: dateRange.to })
        });
        return response.data;
      } catch (fallbackError) {
        throw new Error(`Failed to load financial data: ${fallbackError}`);
      }
    }
  }

  // Optimized for reports with efficient parallel chunk management
  async getAllParcelsForReports(filters: {
    startDate?: string;
    endDate?: string;
    destination?: string;
    search?: string;
  } = {}): Promise<Parcel[]> {
    const cacheKey = this.generateCacheKey(filters);
    const requestKey = `reports:parcels:${cacheKey}`;

    return this.deduplicate(requestKey, async () => {
      try {
        const dateRange = filters.startDate && filters.endDate 
          ? { from: filters.startDate, to: filters.endDate }
          : undefined;

        return await this.getAllParcelsProgressively({
          dateRange,
          destination: filters.destination,
          search: filters.search,
          chunkSize: 1200,
        });
      } catch (error) {
        console.warn('Failed to load reports data progressively, falling back to direct pagination');
        try {
          const response = await this.getParcelsPaged({
            page: 1,
            pageSize: 2000,
            ...(filters.startDate && filters.endDate && { 
              fromDate: filters.startDate, 
              toDate: filters.endDate 
            }),
            ...(filters.destination && { destination: filters.destination }),
            ...(filters.search && { search: filters.search }),
          });
          return response.data;
        } catch (fallbackError) {
          throw new Error(`Failed to load reports data: ${fallbackError}`);
        }
      }
    });
  }

  async getParcelById(parcelId: string): Promise<Parcel | null> {
    try {
      const response = await fetch(`${API_BASE_URL}/Parcels/${parcelId}`, {
        headers: this.getHeaders(),
        signal: AbortSignal.timeout(API_CONFIG.TIMEOUT),
      });

      if (!response.ok) {
        if (response.status === 404) {
          return null;
        }
        throw new Error(`Failed to fetch parcel: ${response.status}`);
      }

      return await response.json();
    } catch (error) {
      throw error;
    }
  }

  async getParcelByWaybill(waybillNumber: string): Promise<Parcel | null> {
    try {
      const response = await fetch(`${API_BASE_URL}/Parcels/waybill/${waybillNumber}`, {
        headers: this.getHeaders(),
        signal: AbortSignal.timeout(API_CONFIG.TIMEOUT),
      });

      if (!response.ok) {
        if (response.status === 404) {
          return null;
        }
        throw new Error(`Failed to fetch parcel by waybill: ${response.status}`);
      }

      return await response.json();
    } catch (error) {
      throw error;
    }
  }

  async getParcelsByIds(parcelIds: string[]): Promise<Parcel[]> {
    try {
      if (parcelIds.length === 0) {
        return [];
      }

      
      // For better performance, fetch parcels in parallel chunks
      const CHUNK_SIZE = 10; // Adjust based on server capabilities
      const chunks: string[][] = [];
      
      for (let i = 0; i < parcelIds.length; i += CHUNK_SIZE) {
        chunks.push(parcelIds.slice(i, i + CHUNK_SIZE));
      }

      const allParcels: Parcel[] = [];
      
      // Process chunks in parallel
      const chunkPromises = chunks.map(async (chunk) => {
        const parcelsInChunk = await Promise.all(
          chunk.map(async (parcelId) => {
            try {
              const parcel = await this.getParcelById(parcelId);
              return parcel;
            } catch (error) {
              return null;
            }
          })
        );
        
        // Filter out null results
        return parcelsInChunk.filter((parcel): parcel is Parcel => parcel !== null);
      });

      const chunkResults = await Promise.all(chunkPromises);
      
      // Flatten all results
      chunkResults.forEach(parcels => {
        allParcels.push(...parcels);
      });

      return allParcels;
    } catch (error) {
      throw error;
    }
  }

  async getParcelsForDispatch(destination?: string, statuses?: string[]): Promise<Parcel[]> {
    try {
      const url = `${API_BASE_URL}/Parcels/dispatch`;
      const params = new URLSearchParams();
      
      if (destination) {
        params.append('destination', destination);
      }
      
      if (statuses && statuses.length > 0) {
        params.append('statuses', statuses.join(','));
      }

      const response = await fetch(`${url}?${params}`, {
        headers: this.getHeaders(),
        signal: AbortSignal.timeout(API_CONFIG.TIMEOUT),
      });

      if (!response.ok) {
        throw new Error(`Failed to fetch parcels for dispatch: ${response.status}`);
      }

      const data = await response.json();
      return data.$values || data;
    } catch (error) {
      throw error;
    }
  }

  // New optimized multi-destination dispatch endpoint
  async getParcelsForMultiDestDispatch(params?: {
    page?: number;
    pageSize?: number;
    statuses?: string[];
    fromDate?: string;
    toDate?: string;
    createdByUsername?: string;
    userBranchId?: number;
  }): Promise<any> {
    try {
      const url = `${API_BASE_URL}/Parcels/dispatch-multi-dest`;
      const urlParams = new URLSearchParams();
      
      if (params?.page) {
        urlParams.append('page', params.page.toString());
      }
      
      if (params?.pageSize) {
        urlParams.append('pageSize', params.pageSize.toString());
      }
      
      if (params?.statuses && params.statuses.length > 0) {
        urlParams.append('statuses', params.statuses.join(','));
      }
      
      if (params?.fromDate) {
        urlParams.append('fromDate', params.fromDate);
      }
      
      if (params?.toDate) {
        urlParams.append('toDate', params.toDate);
      }
      
      if (params?.createdByUsername) {
        urlParams.append('createdByUsername', params.createdByUsername);
      }
      
      if (params?.userBranchId) {
        urlParams.append('userBranchId', params.userBranchId.toString());
      }

      const response = await fetch(`${url}?${urlParams}`, {
        headers: this.getHeaders(),
        signal: AbortSignal.timeout(API_CONFIG.TIMEOUT),
      });

      if (!response.ok) {
        throw new Error(`Failed to fetch parcels for multi-destination dispatch: ${response.status}`);
      }

      const data = await response.json();
      return data;
    } catch (error) {
      throw error;
    }
  }

  // New dispatch statistics endpoint
  async getDispatchStatistics(params?: {
    fromDate?: string;
    toDate?: string;
    userBranchId?: number;
  }): Promise<any> {
    try {
      const url = `${API_BASE_URL}/Parcels/dispatch-stats`;
      const urlParams = new URLSearchParams();
      
      if (params?.fromDate) {
        urlParams.append('fromDate', params.fromDate);
      }
      
      if (params?.toDate) {
        urlParams.append('toDate', params.toDate);
      }
      
      if (params?.userBranchId) {
        urlParams.append('userBranchId', params.userBranchId.toString());
      }

      const response = await fetch(`${url}?${urlParams}`, {
        headers: this.getHeaders(),
        signal: AbortSignal.timeout(API_CONFIG.TIMEOUT),
      });

      if (!response.ok) {
        throw new Error(`Failed to fetch dispatch statistics: ${response.status}`);
      }

      const data = await response.json();
      return data;
    } catch (error) {
      throw error;
    }
  }

  async getParcelsForDispatchByBranch(userBranchId?: number): Promise<Parcel[]> {
    try {
      // Get all parcels first
      const allParcels = await this.getParcels();
      
      // Filter by status: Only Pending (0) or Confirmed (1)
      const validStatuses = [0, 1];
      let filteredParcels = allParcels.filter(parcel => validStatuses.includes(parcel.status));
      
      // If user has a branch, filter by creator's branch
      if (userBranchId) {
        // Get all users to map createdById to user branches
        const users = await this.getUsers();
        
        // Filter parcels to only show those created by users from the same branch
        filteredParcels = filteredParcels.filter(parcel => {
          if (!parcel.createdById) return false;
          
          const creator = users.find(user => user.id === parcel.createdById);
          return creator && creator.branchId === userBranchId;
        });
        
        console.log(`getParcelsForDispatchByBranch - Branch ${userBranchId}: ${filteredParcels.length} parcels found`);
        console.log(`getParcelsForDispatchByBranch - Sample creators:`, 
          filteredParcels.slice(0, 3).map(p => {
            const creator = users.find(u => u.id === p.createdById);
            return { 
              parcelId: p.id, 
              createdById: p.createdById, 
              creatorBranch: creator?.branchId,
              creatorName: creator?.username 
            };
          })
        );
      }
      
      // Populate creator information for display
      try {
        const users = await this.getUsers();
        const updatedParcels = filteredParcels.map(parcel => {
          if (!parcel.createdBy && parcel.createdById) {
            const creator = users.find(u => u.id === parcel.createdById);
            if (creator) {
              return { 
                ...parcel, 
                createdBy: { 
                  username: creator.firstName && creator.lastName 
                    ? `${creator.firstName} ${creator.lastName}` 
                    : creator.username 
                } 
              };
            }
          }
          return parcel;
        });
        
        return updatedParcels;
      } catch (userErr) {
        // If user fetching fails, return parcels without creator info
        return filteredParcels;
      }
    } catch (error) {
      console.error('getParcelsForDispatchByBranch - Error:', error);
      throw error;
    }
  }

  async getPendingParcels(dateFilter?: string, branch?: string): Promise<Parcel[]> {
    try {
      const url = `${API_BASE_URL}/parcels/pending`;
      const params = new URLSearchParams();
      if (branch) {
        params.append('branchName', branch);
      }

      const response = await fetch(`${url}?${params}`, {
        headers: this.getHeaders(),
        signal: AbortSignal.timeout(API_CONFIG.TIMEOUT),
      });

      if (!response.ok) {
        throw new Error(`Failed to fetch pending parcels: ${response.status}`);
      }

      const data = await response.json();
      let parcels = data.$values || data;

      if (dateFilter) {
        // Filter parcels by date on the client side
        parcels = parcels.filter((parcel: Parcel) => {
          if (parcel.createdAt) {
            try {
              const parcelDate = new Date(parcel.createdAt.replace('Z', '+00:00'));
              return parcelDate.toISOString().split('T')[0] === dateFilter;
            } catch (error) {
              return false;
            }
          }
          return false;
        });
      }

      return parcels;
    } catch (error) {
      throw error;
    }
  }

  async updateParcel(parcelId: string, parcelData: Partial<Parcel>): Promise<boolean> {
    try {
      // Clean up the data
      const cleanedData = Object.fromEntries(
        Object.entries(parcelData).filter(([_, value]) => value !== null)
      );

      const response = await fetch(`${API_BASE_URL}/parcels/${parcelId}`, {
        method: 'PUT',
        headers: this.getHeaders(),
        body: JSON.stringify(cleanedData),
      });

      if (response.ok) {
        return true;
      } else {
        return false;
      }
    } catch (error) {
      return false;
    }
  }

  async updateParcelPayment(parcelId: string, paymentData: {
    amountPaid: number;
    transactionCode: string;
    updatedById?: number;
  }): Promise<boolean> {
    try {
      const response = await fetch(`${API_BASE_URL}/parcels/${parcelId}/payment`, {
        method: 'PUT',
        headers: this.getHeaders(),
        body: JSON.stringify({
          amountPaid: paymentData.amountPaid,
          transactionCode: paymentData.transactionCode,
          updatedById: paymentData.updatedById
        }),
        signal: AbortSignal.timeout(API_CONFIG.TIMEOUT),
      });

      return response.ok;
    } catch (error) {
      console.error('Error updating parcel payment:', error);
      return false;
    }
  }

  async updateParcelStatus(parcelId: string, status: number): Promise<boolean> {
    try {
      const response = await fetch(`${API_BASE_URL}/parcels/${parcelId}/status`, {
        method: 'PUT',
        headers: this.getHeaders(),
        body: JSON.stringify({ Status: status }),
      });

      if (response.ok) {
        return true;
      } else {
        return false;
      }
    } catch (error) {
      return false;
    }
  }

  async deleteParcel(parcelId: string): Promise<boolean> {
    try {
      const response = await fetch(`${API_BASE_URL}/parcels/${parcelId}`, {
        method: 'DELETE',
        headers: this.getHeaders(),
      });

      if (response.ok) {
        return true;
      } else {
        return false;
      }
    } catch (error) {
      return false;
    }
  }

  // Helper method to normalize parcelIds structure
  private normalizeParcelIds(parcelIds: string[] | { $values: string[] } | undefined): string[] {
    if (!parcelIds) return [];
    if (Array.isArray(parcelIds)) return parcelIds;
    if (typeof parcelIds === 'object' && parcelIds.$values && Array.isArray(parcelIds.$values)) {
      return parcelIds.$values;
    }
    return [];
  }

  // Dispatches API methods
  async getDispatches(branchName?: string): Promise<Dispatch[]> {
    return this._fetchDispatches(branchName);
  }
  
  private async _fetchDispatches(branchName?: string): Promise<Dispatch[]> {
    try {
      let url = `${API_BASE_URL}/Dispatches`;
      
      // Add branch filter if provided - use the dedicated branch endpoint
      if (branchName) {
        url = `${API_BASE_URL}/Dispatches/branch/${encodeURIComponent(branchName)}`;
      }
      
      console.log('getDispatches API call:', { branchName, url });
      
      const response = await fetch(url, {
        headers: this.getHeaders(),
      });

      console.log('getDispatches API response:', { status: response.status, ok: response.ok });

      if (!response.ok) {
        const errorText = await response.text();
        console.error('getDispatches API error:', errorText);
        throw new Error(`Failed to fetch dispatches: ${response.status} - ${errorText}`);
      }

      const data = await response.json();
      const dispatches = data.$values || data;
      
      // Normalize parcelIds structure for all dispatches
      const normalizedDispatches = dispatches.map((dispatch: Dispatch) => ({
        ...dispatch,
        parcelIds: this.normalizeParcelIds(dispatch.parcelIds)
      }));
      
      return normalizedDispatches;
    } catch (error) {
      throw error;
    }
  }

  async getDispatchById(dispatchId: string): Promise<Dispatch | null> {
    try {
      const response = await fetch(`${API_BASE_URL}/Dispatches/${dispatchId}/note`, {
        headers: this.getHeaders(),
        signal: AbortSignal.timeout(API_CONFIG.TIMEOUT),
      });

      if (!response.ok) {
        if (response.status === 404) {
          return null;
        }
        throw new Error(`Failed to fetch dispatch: ${response.status}`);
      }

      const data = await response.json();
      const dispatch = data.$values || data;
      
      // Normalize parcelIds structure
      if (dispatch) {
        return {
          ...dispatch,
          parcelIds: this.normalizeParcelIds(dispatch.parcelIds)
        };
      }
      
      return dispatch;
    } catch (error) {
      throw error;
    }
  }

  async createDispatch(dispatchData: {
    dispatchCode: string;
    sourceBranch: string;
    destination: string | null;
    driver: string;
    driverPhone?: string;
    vehicleNumber: string;
    parcelIds: string[];
  }): Promise<Dispatch | null> {
    try {
      // Debug: Log input data
      
      // Use the .NET API structure for creating dispatch
      const createDispatchRequest = {
        dispatchCode: dispatchData.dispatchCode,
        sourceBranch: dispatchData.sourceBranch,
        destination: dispatchData.destination,
        vehicleNumber: dispatchData.vehicleNumber,
        driver: dispatchData.driver,
        driverPhone: dispatchData.driverPhone,
        parcelIds: dispatchData.parcelIds
      };

      // Debug: Log the request payload being sent to API

      const response = await fetch(`${API_BASE_URL}/Dispatches/create`, {
        method: 'POST',
        headers: this.getHeaders(),
        body: JSON.stringify(createDispatchRequest),
        signal: AbortSignal.timeout(API_CONFIG.TIMEOUT * 2), // Double timeout for create operations
      });

      if (!response.ok) {
        const errorText = await response.text();
        throw new Error(`Failed to create dispatch: ${response.status}`);
      }

      return await response.json();
    } catch (error) {
      throw error;
    }
  }

  // Users API methods
  async getUserById(userId: number): Promise<User | null> {
    try {
      const response = await fetch(`${API_BASE_URL}/Users/${userId}`, {
        headers: this.getHeaders(),
        signal: AbortSignal.timeout(API_CONFIG.TIMEOUT),
      });

      if (!response.ok) {
        if (response.status === 404) {
          return null;
        }
        throw new Error(`Failed to fetch user: ${response.status}`);
      }

      return await response.json();
    } catch (error) {
      return null;
    }
  }

  async getUsers(): Promise<User[]> {
    try {
      // Use the main /Users endpoint which we know returns complete data with branches
      const response = await fetch(`${API_BASE_URL}/Users`, {
        headers: this.getHeaders(),
        signal: AbortSignal.timeout(API_CONFIG.TIMEOUT),
      });

      if (!response.ok) {
        throw new Error(`Failed to fetch users: ${response.status}`);
      }

      const data = await response.json();
      console.log('getUsers - Raw API response:', data);
      
      // Handle the $values format
      const users = data.$values || data;
      console.log('getUsers - Users array:', users);

      // Transform API data to match our User interface
      const transformedUsers = users.map((user: any) => {
        const transformedUser = {
          id: user.id,
          username: user.username || '',
          email: user.email,
          firstName: user.firstName,
          lastName: user.lastName,
          roles: user.role?.name ? [user.role.name] : ['User'],
          role: user.role ? {
            id: user.role.id,
            name: user.role.name,
            canConfirm: user.role.canConfirm || false,
            canDispatch: user.role.canDispatch || false
          } : { id: 0, name: 'User', canConfirm: false, canDispatch: false },
          branch: user.branch ? {
            id: user.branch.id,
            name: user.branch.name
          } : undefined,
          branchId: user.branch?.id,
          roleId: user.role?.id,
          createdAt: user.createdAt,
          isActive: user.isActive !== undefined ? user.isActive : true,
          canConfirm: user.canConfirm || false,
          canDispatch: user.canDispatch || false
        };
        
        console.log(`getUsers - Transformed user ${user.id}:`, transformedUser);
        return transformedUser;
      });

      return transformedUsers;
    } catch (error) {
      console.error('getUsers - Error:', error);
      throw error;
    }
  }

  async createUser(userData: {
    username: string;
    email: string;
    firstName: string;
    lastName: string;
    password: string;
    roleId: number;
    branchId?: number;
  }): Promise<User | null> {
    try {
      const response = await fetch(`${API_BASE_URL}/Users`, {
        method: 'POST',
        headers: this.getHeaders(),
        body: JSON.stringify(userData),
        signal: AbortSignal.timeout(API_CONFIG.TIMEOUT),
      });

      if (!response.ok) {
        const errorText = await response.text();
        throw new Error(`Failed to create user: ${response.status}`);
      }

      return await response.json();
    } catch (error) {
      throw error;
    }
  }

  async updateUser(userId: number, userData: {
    id?: number;
    username?: string;
    email?: string;
    firstName?: string;
    lastName?: string;
    roleId?: number;
    branchId?: number;
    isActive?: boolean;
    canConfirm?: boolean;
    canDispatch?: boolean;
  }): Promise<void> {
    try {
      const response = await fetch(`${API_BASE_URL}/Users/${userId}`, {
        method: 'PUT',
        headers: this.getHeaders(),
        body: JSON.stringify(userData),
        signal: AbortSignal.timeout(API_CONFIG.TIMEOUT),
      });

      if (!response.ok) {
        const errorText = await response.text();
        throw new Error(`Failed to update user: ${response.status}`);
      }
    } catch (error) {
      throw error;
    }
  }

  async changeUserPassword(userId: number, newPassword: string): Promise<void> {
    try {
      const response = await fetch(`${API_BASE_URL}/Users/${userId}/change-password`, {
        method: 'PUT',
        headers: this.getHeaders(),
        body: JSON.stringify({ newPassword }),
        signal: AbortSignal.timeout(API_CONFIG.TIMEOUT),
      });

      if (!response.ok) {
        const errorText = await response.text();
        throw new Error(`Failed to change password: ${response.status}`);
      }
    } catch (error) {
      throw error;
    }
  }

  async deleteUser(userId: number): Promise<void> {
    try {
      const response = await fetch(`${API_BASE_URL}/Users/${userId}`, {
        method: 'DELETE',
        headers: this.getHeaders(),
        signal: AbortSignal.timeout(API_CONFIG.TIMEOUT),
      });

      if (!response.ok) {
        const errorText = await response.text();
        throw new Error(`Failed to delete user: ${response.status}`);
      }
    } catch (error) {
      throw error;
    }
  }

  async activateUser(userId: number): Promise<void> {
    try {
      const response = await fetch(`${API_BASE_URL}/Users/${userId}/activate`, {
        method: 'PUT',
        headers: this.getHeaders(),
        signal: AbortSignal.timeout(API_CONFIG.TIMEOUT),
      });

      if (!response.ok) {
        const errorText = await response.text();
        throw new Error(`Failed to activate user: ${response.status}`);
      }
    } catch (error) {
      throw error;
    }
  }

  async deactivateUser(userId: number): Promise<void> {
    try {
      const response = await fetch(`${API_BASE_URL}/Users/${userId}/deactivate`, {
        method: 'PUT',
        headers: this.getHeaders(),
        signal: AbortSignal.timeout(API_CONFIG.TIMEOUT),
      });

      if (!response.ok) {
        const errorText = await response.text();
        throw new Error(`Failed to deactivate user: ${response.status}`);
      }
    } catch (error) {
      throw error;
    }
  }

  // Branches API methods
  async getBranches(): Promise<{ id: number; name: string; address?: string; phone?: string; email?: string }[]> {
    try {
      const response = await fetch(`${API_BASE_URL}/Branches`, {
        headers: this.getHeaders(),
        signal: AbortSignal.timeout(API_CONFIG.TIMEOUT),
      });

      if (!response.ok) {
        throw new Error(`Failed to fetch branches: ${response.status}`);
      }

      const data = await response.json();
      return data.$values || data;
    } catch (error) {
      throw error;
    }
  }

  // Analytics methods
  async getParcelCountForDate(date: Date, branch?: string): Promise<number> {
    try {
      const params = new URLSearchParams();
      params.append('date', date.toISOString().split('T')[0]);
      if (branch) {
        params.append('branchName', branch);
      }

      const response = await fetch(`${API_BASE_URL}/Parcels/count?${params}`, {
        headers: this.getHeaders(),
        signal: AbortSignal.timeout(API_CONFIG.TIMEOUT),
      });

      if (!response.ok) {
        return 0;
      }

      const data = await response.json();
      return data.count || 0;
    } catch (error) {
      return 0;
    }
  }

  async getTotalSalesForDate(date: Date, branch?: string): Promise<number> {
    try {
      const params = new URLSearchParams();
      params.append('date', date.toISOString().split('T')[0]);
      if (branch) {
        params.append('branchName', branch);
      }

      const response = await fetch(`${API_BASE_URL}/Parcels/sales?${params}`, {
        headers: this.getHeaders(),
        signal: AbortSignal.timeout(API_CONFIG.TIMEOUT),
      });

      if (!response.ok) {
        return 0;
      }

      const data = await response.json();
      return data.totalSales || 0;
    } catch (error) {
      return 0;
    }
  }

  // Contract Customers API methods
  async getContractCustomers(): Promise<ContractCustomer[]> {
    try {
      const response = await fetch(`${API_BASE_URL}/ContractCustomers`, {
        headers: this.getHeaders(),
        signal: AbortSignal.timeout(API_CONFIG.TIMEOUT),
      });

      if (!response.ok) {
        throw new Error(`Failed to fetch contract customers: ${response.status}`);
      }

      const data = await response.json();
      return data.$values || data;
    } catch (error) {
      console.error('Error fetching contract customers:', error);
      throw error;
    }
  }


  async createContractCustomer(customer: Omit<ContractCustomer, 'id' | 'createdAt' | 'updatedAt'>): Promise<ContractCustomer> {
    try {
      console.log('Creating contract customer with data:', customer);
      
      const response = await fetch(`${API_BASE_URL}/ContractCustomers`, {
        method: 'POST',
        headers: {
          ...this.getHeaders(),
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(customer),
        signal: AbortSignal.timeout(API_CONFIG.TIMEOUT),
      });

      if (!response.ok) {
        const errorText = await response.text();
        console.error('API Error Response:', errorText);
        throw new Error(`Failed to create contract customer: ${response.status} - ${errorText}`);
      }

      return await response.json();
    } catch (error) {
      console.error('Create contract customer error:', error);
      throw error;
    }
  }

  async updateContractCustomer(id: number, customer: Partial<ContractCustomer>): Promise<void> {
    try {
      const response = await fetch(`${API_BASE_URL}/ContractCustomers/${id}`, {
        method: 'PUT',
        headers: {
          ...this.getHeaders(),
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(customer),
        signal: AbortSignal.timeout(API_CONFIG.TIMEOUT),
      });

      if (!response.ok) {
        throw new Error(`Failed to update contract customer: ${response.status}`);
      }
    } catch (error) {
      throw error;
    }
  }

  async deleteContractCustomer(id: number): Promise<void> {
    try {
      const response = await fetch(`${API_BASE_URL}/ContractCustomers/${id}`, {
        method: 'DELETE',
        headers: this.getHeaders(),
        signal: AbortSignal.timeout(API_CONFIG.TIMEOUT),
      });

      if (!response.ok) {
        throw new Error(`Failed to delete contract customer: ${response.status}`);
      }
    } catch (error) {
      throw error;
    }
  }

  // Invoice API methods
  async getInvoices(): Promise<Invoice[]> {
    try {
      const response = await fetch(`${API_BASE_URL}/Invoices`, {
        headers: this.getHeaders(),
        signal: AbortSignal.timeout(API_CONFIG.TIMEOUT),
      });

      if (!response.ok) {
        throw new Error(`Failed to fetch invoices: ${response.status}`);
      }

      const data = await response.json();
      const invoices = data.$values || data;
      
      // Transform nested items structure
      return invoices.map((invoice: any) => ({
        ...invoice,
        items: invoice.items?.$values || invoice.items || []
      }));
    } catch (error) {
      console.error('Error fetching invoices:', error);
      throw error;
    }
  }


  async getInvoiceById(invoiceId: number): Promise<Invoice | null> {
    try {
      const response = await fetch(`${API_BASE_URL}/Invoices/${invoiceId}`, {
        headers: this.getHeaders(),
        signal: AbortSignal.timeout(API_CONFIG.TIMEOUT),
      });

      if (!response.ok) {
        if (response.status === 404) return null;
        throw new Error(`Failed to fetch invoice: ${response.status}`);
      }

      const invoice = await response.json();
      
      // Transform nested items structure
      return {
        ...invoice,
        items: invoice.items?.$values || invoice.items || []
      };
    } catch (error) {
      throw error;
    }
  }

  async createInvoice(invoiceData: CreateInvoiceDto): Promise<Invoice> {
    try {
      const response = await fetch(`${API_BASE_URL}/Invoices`, {
        method: 'POST',
        headers: {
          ...this.getHeaders(),
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(invoiceData),
        signal: AbortSignal.timeout(API_CONFIG.TIMEOUT),
      });

      if (!response.ok) {
        throw new Error(`Failed to create invoice: ${response.status}`);
      }

      const invoice = await response.json();
      
      // Transform nested items structure
      return {
        ...invoice,
        items: invoice.items?.$values || invoice.items || []
      };
    } catch (error) {
      throw error;
    }
  }

  async getEligibleParcelsForInvoicing(billingPeriodStart?: string, billingPeriodEnd?: string): Promise<Parcel[]> {
    try {
      let url = `${API_BASE_URL}/Invoices/eligible-parcels`;
      const params = new URLSearchParams();
      
      if (billingPeriodStart) {
        params.append('billingPeriodStart', billingPeriodStart);
      }
      if (billingPeriodEnd) {
        params.append('billingPeriodEnd', billingPeriodEnd);
      }
      
      if (params.toString()) {
        url += `?${params.toString()}`;
      }

      const response = await fetch(url, {
        headers: this.getHeaders(),
        signal: AbortSignal.timeout(API_CONFIG.TIMEOUT),
      });

      if (!response.ok) {
        throw new Error(`Failed to get eligible parcels: ${response.status}`);
      }

      const parcels = await response.json();
      
      // Handle potential .NET $values wrapper structure
      return parcels?.$values || parcels || [];
    } catch (error) {
      throw error;
    }
  }

  async addInvoicePayment(paymentData: CreateInvoicePaymentDto): Promise<InvoicePayment> {
    try {
      const response = await fetch(`${API_BASE_URL}/Invoices/payments`, {
        method: 'POST',
        headers: {
          ...this.getHeaders(),
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(paymentData),
        signal: AbortSignal.timeout(API_CONFIG.TIMEOUT),
      });

      if (!response.ok) {
        throw new Error(`Failed to add invoice payment: ${response.status}`);
      }

      return await response.json();
    } catch (error) {
      throw error;
    }
  }

  async recordInvoicePayment(invoiceId: number, paymentData: {
    amount: number;
    paymentMethod: 'cash' | 'cheque' | 'bank_transfer' | 'mobile_money';
    paymentDate: string;
    paymentReference?: string;
    notes?: string;
  }): Promise<void> {
    try {
      const response = await fetch(`${API_BASE_URL}/Invoices/${invoiceId}/payment`, {
        method: 'POST',
        headers: {
          ...this.getHeaders(),
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          amount: paymentData.amount,
          paymentDate: paymentData.paymentDate,
          paymentMethod: paymentData.paymentMethod,
          paymentReference: paymentData.paymentReference,
          notes: paymentData.notes
        }),
        signal: AbortSignal.timeout(API_CONFIG.TIMEOUT),
      });

      if (!response.ok) {
        throw new Error(`Failed to record payment: ${response.status}`);
      }
    } catch (error) {
      throw error;
    }
  }

  async getUnbilledParcelsForCustomer(contractCustomerId: number, startDate?: string, endDate?: string): Promise<Parcel[]> {
    try {
      const params = new URLSearchParams();
      params.append('contractCustomerId', contractCustomerId.toString());
      if (startDate) params.append('startDate', startDate);
      if (endDate) params.append('endDate', endDate);

      const response = await fetch(`${API_BASE_URL}/Invoices/unbilled-parcels?${params}`, {
        headers: this.getHeaders(),
        signal: AbortSignal.timeout(API_CONFIG.TIMEOUT),
      });

      if (!response.ok) {
        throw new Error(`Failed to fetch unbilled parcels: ${response.status}`);
      }

      const data = await response.json();
      return data.$values || data;
    } catch (error) {
      throw error;
    }
  }

  // COD Collection API methods
  async getCODCollections(branchId?: number, startDate?: string, endDate?: string): Promise<CODCollection[]> {
    try {
      const params = new URLSearchParams();
      if (branchId) params.append('branchId', branchId.toString());
      if (startDate) params.append('startDate', startDate);
      if (endDate) params.append('endDate', endDate);

      const response = await fetch(`${API_BASE_URL}/Payments/cod-collections?${params}`, {
        headers: this.getHeaders(),
        signal: AbortSignal.timeout(API_CONFIG.TIMEOUT),
      });

      if (!response.ok) {
        throw new Error(`Failed to fetch COD collections: ${response.status}`);
      }

      const data = await response.json();
      return data.$values || data;
    } catch (error) {
      console.error('Error fetching COD collections:', error);
      throw error;
    }
  }


  async createCODCollection(collectionData: CreateCODCollectionDto): Promise<CODCollection> {
    try {
      const response = await fetch(`${API_BASE_URL}/Payments/cod-collections`, {
        method: 'POST',
        headers: {
          ...this.getHeaders(),
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(collectionData),
        signal: AbortSignal.timeout(API_CONFIG.TIMEOUT),
      });

      if (!response.ok) {
        throw new Error(`Failed to create COD collection: ${response.status}`);
      }

      return await response.json();
    } catch (error) {
      throw error;
    }
  }

  async updateCODCollection(collectionId: number, updateData: UpdateCODCollectionDto): Promise<CODCollection> {
    try {
      const response = await fetch(`${API_BASE_URL}/Payments/cod-collections/${collectionId}`, {
        method: 'PUT',
        headers: {
          ...this.getHeaders(),
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(updateData),
        signal: AbortSignal.timeout(API_CONFIG.TIMEOUT),
      });

      if (!response.ok) {
        throw new Error(`Failed to update COD collection: ${response.status}`);
      }

      return await response.json();
    } catch (error) {
      throw error;
    }
  }

  // Daily Expenses API methods
  async getDailyExpenses(branchId?: number, startDate?: string, endDate?: string, status?: string): Promise<DailyExpense[]> {
    try {
      const params = new URLSearchParams();
      if (branchId) params.append('branchId', branchId.toString());
      if (startDate) params.append('startDate', startDate);
      if (endDate) params.append('endDate', endDate);
      if (status) params.append('status', status);

      const response = await fetch(`${API_BASE_URL}/Expenses?${params}`, {
        headers: this.getHeaders(),
        signal: AbortSignal.timeout(API_CONFIG.TIMEOUT),
      });

      if (!response.ok) {
        throw new Error(`Failed to fetch daily expenses: ${response.status}`);
      }

      const data = await response.json();
      return data.$values || data;
    } catch (error) {
      console.error('Error fetching daily expenses:', error);
      throw error;
    }
  }


  async createDailyExpense(expenseData: CreateDailyExpenseDto): Promise<DailyExpense> {
    try {
      console.log('API: Creating expense with data:', expenseData);
      
      // Clean the expense data - include all required fields
      const cleanedData: any = {
        date: new Date(expenseData.date).toISOString(),
        category: expenseData.category,
        description: expenseData.description,
        amount: Number(expenseData.amount),
        vendor: expenseData.vendor || '',
        receiptNumber: expenseData.receiptNumber || '',
        branchName: expenseData.branchName || '',
        clerkName: expenseData.clerkName || ''
      };

      // Add optional fields if they have values (don't send undefined)
      if (expenseData.branchId !== undefined && expenseData.branchId !== null) {
        cleanedData.branchId = expenseData.branchId;
      }
      
      if (expenseData.clerkId !== undefined && expenseData.clerkId !== null) {
        cleanedData.clerkId = expenseData.clerkId;
      }
      
      if (expenseData.notes) {
        cleanedData.notes = expenseData.notes;
      }
      
      console.log('API: Sending cleaned expense data:', cleanedData);
      console.log('API: JSON payload:', JSON.stringify(cleanedData, null, 2));
      
      const response = await fetch(`${API_BASE_URL}/Expenses`, {
        method: 'POST',
        headers: {
          ...this.getHeaders(),
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(cleanedData),
        signal: AbortSignal.timeout(API_CONFIG.TIMEOUT),
      });

      if (!response.ok) {
        const errorText = await response.text();
        console.error('API Error Response:', errorText);
        throw new Error(`Failed to create daily expense: ${response.status} - ${errorText}`);
      }

      return await response.json();
    } catch (error) {
      console.error('API Error:', error);
      throw error;
    }
  }

  async approveExpense(approvalData: ApproveExpenseDto): Promise<DailyExpense> {
    try {
      console.log('API: Approving expense with data:', approvalData);
      console.log('API: JSON payload:', JSON.stringify(approvalData, null, 2));
      
      const response = await fetch(`${API_BASE_URL}/Expenses/approve`, {
        method: 'POST',
        headers: {
          ...this.getHeaders(),
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(approvalData),
        signal: AbortSignal.timeout(API_CONFIG.TIMEOUT),
      });

      if (!response.ok) {
        throw new Error(`Failed to approve expense: ${response.status}`);
      }

      return await response.json();
    } catch (error) {
      throw error;
    }
  }

  async getExpensesSummary(): Promise<ExpensesSummary> {
    try {
      const response = await fetch(`${API_BASE_URL}/Expenses/summary`, {
        headers: this.getHeaders(),
        signal: AbortSignal.timeout(API_CONFIG.TIMEOUT),
      });

      if (!response.ok) {
        throw new Error(`Failed to fetch expenses summary: ${response.status}`);
      }

      return await response.json();
    } catch (error) {
      console.error('Error fetching expenses summary:', error);
      throw error;
    }
  }

  // Enhanced Clerk Cash Management API methods
  async getEnhancedClerkCashSummaries(clerkId?: number, startDate?: string, endDate?: string): Promise<EnhancedClerkCashSummary[]> {
    try {
      const params = new URLSearchParams();
      if (clerkId) params.append('clerkId', clerkId.toString());
      if (startDate) params.append('startDate', startDate);
      if (endDate) params.append('endDate', endDate);

      const response = await fetch(`${API_BASE_URL}/ClerkCashSummaries/enhanced?${params}`, {
        headers: this.getHeaders(),
        signal: AbortSignal.timeout(API_CONFIG.TIMEOUT),
      });

      if (!response.ok) {
        throw new Error(`Failed to fetch enhanced clerk cash summaries: ${response.status}`);
      }

      const data = await response.json();
      return data.$values || data;
    } catch (error) {
      throw error;
    }
  }

  async createClerkDeposit(depositData: CreateClerkDepositDto): Promise<EnhancedClerkCashSummary> {
    try {
      const response = await fetch(`${API_BASE_URL}/ClerkCashSummaries/deposit`, {
        method: 'POST',
        headers: {
          ...this.getHeaders(),
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(depositData),
        signal: AbortSignal.timeout(API_CONFIG.TIMEOUT),
      });

      if (!response.ok) {
        throw new Error(`Failed to create clerk deposit: ${response.status}`);
      }

      return await response.json();
    } catch (error) {
      throw error;
    }
  }

  // Cheque Management API methods
  async getChequeDeposits(status?: string, startDate?: string, endDate?: string): Promise<ChequeDeposit[]> {
    try {
      const params = new URLSearchParams();
      if (status) params.append('status', status);
      if (startDate) params.append('startDate', startDate);
      if (endDate) params.append('endDate', endDate);

      const response = await fetch(`${API_BASE_URL}/Payments/cheque-deposits?${params}`, {
        headers: this.getHeaders(),
        signal: AbortSignal.timeout(API_CONFIG.TIMEOUT),
      });

      if (!response.ok) {
        console.warn('ChequeDeposits endpoint not implemented, returning mock data');
        return this.getMockChequeDeposits();
      }

      const data = await response.json();
      return data.$values || data;
    } catch (error) {
      console.warn('ChequeDeposits endpoint error, returning mock data:', error);
      return this.getMockChequeDeposits();
    }
  }

  private getMockChequeDeposits(): ChequeDeposit[] {
    return [
      {
        id: 1,
        chequeNumber: 'CHQ-001-2024',
        amount: 500000,
        bankName: 'Stanbic Bank',
        accountNumber: '9040012345678',
        drawerName: 'ABC Trading Ltd',
        depositDate: '2024-11-20',
        clearanceDate: '2024-11-22',
        status: 'cleared' as const,
        relatedInvoiceId: 1,
        relatedInvoiceNumber: 'INV-2024-001',
        depositedBy: 1,
        depositedByName: 'Finance Officer',
        createdAt: '2024-11-20T10:00:00Z',
        updatedAt: '2024-11-22T14:00:00Z'
      },
      {
        id: 2,
        chequeNumber: 'CHQ-002-2024',
        amount: 350000,
        bankName: 'Centenary Bank',
        accountNumber: '3100045678901',
        drawerName: 'XYZ Logistics',
        depositDate: '2024-11-23',
        status: 'deposited' as const,
        relatedInvoiceId: 2,
        relatedInvoiceNumber: 'INV-2024-002',
        depositedBy: 1,
        depositedByName: 'Finance Officer',
        notes: 'Awaiting clearance',
        createdAt: '2024-11-23T11:00:00Z',
        updatedAt: '2024-11-23T11:00:00Z'
      },
      {
        id: 3,
        chequeNumber: 'CHQ-003-2024',
        amount: 200000,
        bankName: 'DFCU Bank',
        accountNumber: '1234567890123',
        drawerName: 'LMN Enterprises',
        depositDate: '2024-11-18',
        status: 'bounced' as const,
        depositedBy: 1,
        depositedByName: 'Finance Officer',
        bounceReason: 'Insufficient funds',
        notes: 'Customer contacted for payment',
        createdAt: '2024-11-18T09:00:00Z',
        updatedAt: '2024-11-20T15:00:00Z'
      }
    ];
  }

  async createChequeDeposit(chequeData: CreateChequeDepositDto): Promise<ChequeDeposit> {
    try {
      const response = await fetch(`${API_BASE_URL}/Payments/cheque-deposits`, {
        method: 'POST',
        headers: {
          ...this.getHeaders(),
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(chequeData),
        signal: AbortSignal.timeout(API_CONFIG.TIMEOUT),
      });

      if (!response.ok) {
        throw new Error(`Failed to create cheque deposit: ${response.status}`);
      }

      return await response.json();
    } catch (error) {
      throw error;
    }
  }

  async updateChequeStatus(chequeId: number, statusData: UpdateChequeStatusDto): Promise<ChequeDeposit> {
    try {
      const response = await fetch(`${API_BASE_URL}/Payments/cheque-deposits/${chequeId}/status`, {
        method: 'PUT',
        headers: {
          ...this.getHeaders(),
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(statusData),
        signal: AbortSignal.timeout(API_CONFIG.TIMEOUT),
      });

      if (!response.ok) {
        throw new Error(`Failed to update cheque status: ${response.status}`);
      }

      return await response.json();
    } catch (error) {
      throw error;
    }
  }

  // Helper method to get branch-specific data based on user role
  async getBranchSpecificData<T>(
    fetchFunction: (branchName?: string) => Promise<T>,
    userBranch?: string
  ): Promise<T> {
    // If user has a branch (branch manager), filter by branch
    // If user is admin, get all data
    return await fetchFunction(userBranch);
  }

  // Branch Sales Analytics Methods
  async getBranchSalesData(startDate?: string, endDate?: string): Promise<{
    branchId: number;
    branchName: string;
    totalSales: number;
    totalParcels: number;
    avgSaleValue: number;
  }[]> {
    try {
      const [branches, parcels] = await Promise.all([
        this.getBranches(),
        this.getParcels()
      ]);

      // Filter parcels by date range if provided
      const filteredParcels = parcels.filter(parcel => {
        if (!parcel.createdAt) return false;
        
        if (startDate && endDate) {
          const parcelDate = new Date(parcel.createdAt);
          const start = new Date(startDate);
          const end = new Date(endDate);
          return parcelDate >= start && parcelDate <= end;
        }
        return true;
      });

      return branches.map(branch => {
        // Get parcels that are being sent TO this branch (destination-based)
        // Improved matching logic for branch names and destinations
        const branchParcels = filteredParcels.filter(parcel => {
          if (!parcel.destination || !branch.name) return false;
          
          const destination = parcel.destination.toLowerCase().trim();
          const branchName = branch.name.toLowerCase().trim();
          
          // Extract key location words from branch name (remove common words like 'branch', 'office', etc.)
          const branchLocation = branchName
            .replace(/\b(branch|office|depot|warehouse|hub|center|centre)\b/g, '')
            .trim();
          
          // Check multiple matching criteria:
          // 1. Exact destination match with branch location
          // 2. Destination contains branch location
          // 3. Branch location contains destination (for shorter destination names)
          return destination === branchLocation ||
                 destination.includes(branchLocation) ||
                 (destination.length >= 3 && branchLocation.includes(destination));
        });

        const totalSales = branchParcels.reduce((sum, parcel) => sum + (parcel.totalAmount || 0), 0);
        const totalParcels = branchParcels.length;

        return {
          branchId: branch.id,
          branchName: branch.name,
          totalSales,
          totalParcels,
          avgSaleValue: totalParcels > 0 ? totalSales / totalParcels : 0
        };
      });
    } catch (error) {
      console.error('Error getting branch sales data:', error);
      throw error;
    }
  }

  async getDailySalesByBranch(date?: string): Promise<{
    branchId: number;
    branchName: string;
    dailySales: number;
    dailyParcels: number;
  }[]> {
    const targetDate = date || new Date().toISOString().split('T')[0];
    const salesData = await this.getBranchSalesData(targetDate, targetDate);
    
    return salesData.map(branch => ({
      branchId: branch.branchId,
      branchName: branch.branchName,
      dailySales: branch.totalSales,
      dailyParcels: branch.totalParcels
    }));
  }

  async getMonthlySalesByBranch(year?: number, month?: number): Promise<{
    branchId: number;
    branchName: string;
    monthlySales: number;
    monthlyParcels: number;
  }[]> {
    const now = new Date();
    const targetYear = year || now.getFullYear();
    const targetMonth = month || now.getMonth();

    const startDate = new Date(targetYear, targetMonth, 1).toISOString().split('T')[0];
    const endDate = new Date(targetYear, targetMonth + 1, 0).toISOString().split('T')[0];

    const salesData = await this.getBranchSalesData(startDate, endDate);
    
    return salesData.map(branch => ({
      branchId: branch.branchId,
      branchName: branch.branchName,
      monthlySales: branch.totalSales,
      monthlyParcels: branch.totalParcels
    }));
  }

  // Branch Deposits API methods
  async getBranchDeposits(branch?: string, startDate?: string, endDate?: string): Promise<any[]> {
    try {
      const params = new URLSearchParams();
      if (branch) params.append('branch', branch);
      if (startDate) params.append('startDate', startDate);
      if (endDate) params.append('endDate', endDate);
      // Add cache busting parameter
      params.append('_t', Date.now().toString());

      const url = `${API_BASE_URL}/BranchDeposits${params.toString() ? '?' + params.toString() : ''}`;
      const response = await fetch(url, {
        headers: {
          ...this.getHeaders(),
          'Cache-Control': 'no-cache',
          'Pragma': 'no-cache'
        },
        signal: AbortSignal.timeout(API_CONFIG.TIMEOUT),
      });

      if (!response.ok) {
        throw new Error(`Failed to fetch branch deposits: ${response.status}`);
      }

      const data = await response.json();
      return data.$values || data;
    } catch (error) {
      throw error;
    }
  }

  async createBranchDeposit(depositData: {
    branch: string;
    date: string;
    codTotal: number;
    depositAmount: number;
  }): Promise<any> {
    try {
      const response = await fetch(`${API_BASE_URL}/BranchDeposits`, {
        method: 'POST',
        headers: this.getHeaders(),
        body: JSON.stringify(depositData),
        signal: AbortSignal.timeout(API_CONFIG.TIMEOUT),
      });

      if (!response.ok) {
        const errorText = await response.text();
        throw new Error(`Failed to create branch deposit: ${response.status} - ${errorText}`);
      }

      return await response.json();
    } catch (error) {
      throw error;
    }
  }

  async updateBranchDeposit(depositId: number, depositAmount: number): Promise<void> {
    try {
      const response = await fetch(`${API_BASE_URL}/BranchDeposits/${depositId}`, {
        method: 'PUT',
        headers: this.getHeaders(),
        body: JSON.stringify({ depositAmount }),
        signal: AbortSignal.timeout(API_CONFIG.TIMEOUT),
      });

      if (!response.ok) {
        const errorText = await response.text();
        throw new Error(`Failed to update branch deposit: ${response.status} - ${errorText}`);
      }
    } catch (error) {
      throw error;
    }
  }

  async deleteBranchDeposit(depositId: number): Promise<void> {
    try {
      const response = await fetch(`${API_BASE_URL}/BranchDeposits/${depositId}`, {
        method: 'DELETE',
        headers: this.getHeaders(),
        signal: AbortSignal.timeout(API_CONFIG.TIMEOUT),
      });

      if (!response.ok) {
        const errorText = await response.text();
        throw new Error(`Failed to delete branch deposit: ${response.status} - ${errorText}`);
      }
    } catch (error) {
      throw error;
    }
  }

  async approveBranchDeposit(depositId: number): Promise<void> {
    try {
      const response = await fetch(`${API_BASE_URL}/BranchDeposits/${depositId}/approve`, {
        method: 'PUT',
        headers: this.getHeaders(),
        signal: AbortSignal.timeout(API_CONFIG.TIMEOUT),
      });
      if (!response.ok) {
        const errorText = await response.text();
        throw new Error(`Failed to approve branch deposit: ${response.status} - ${errorText}`);
      }
    } catch (error) {
      throw error;
    }
  }

  async rejectBranchDeposit(depositId: number, rejectionReason: string): Promise<void> {
    try {
      const response = await fetch(`${API_BASE_URL}/BranchDeposits/${depositId}/reject`, {
        method: 'PUT',
        headers: this.getHeaders(),
        body: JSON.stringify({ rejectionReason }),
        signal: AbortSignal.timeout(API_CONFIG.TIMEOUT),
      });
      if (!response.ok) {
        const errorText = await response.text();
        throw new Error(`Failed to reject branch deposit: ${response.status} - ${errorText}`);
      }
    } catch (error) {
      throw error;
    }
  }

  async getBranchCodLockStatus(branchName: string): Promise<{
    isLocked: boolean;
    reason: string;
    previousDayCodTotal: number;
    previousDate: string;
    depositRecordId?: number;
    depositStatus?: string;
    depositAmount?: number;
    depositRejectionReason?: string;
  }> {
    try {
      const params = new URLSearchParams({ branchName });
      const response = await fetch(`${API_BASE_URL}/Parcels/branch-cod-lock-status?${params}`, {
        headers: this.getHeaders(),
        signal: AbortSignal.timeout(API_CONFIG.TIMEOUT),
      });
      if (!response.ok) {
        const errorText = await response.text();
        throw new Error(`Failed to get COD lock status: ${response.status} - ${errorText}`);
      }
      return await response.json();
    } catch (error) {
      throw error;
    }
  }

  // ParcelDeposit API methods
  async getParcelDeposits(date?: string, clerkId?: number, destination?: string): Promise<ParcelDepositDto[]> {
    try {
      const params = new URLSearchParams();
      if (date) params.append('date', date);
      if (clerkId) params.append('clerkId', clerkId.toString());
      if (destination) params.append('destination', destination);

      const url = `${API_BASE_URL}/ParcelDeposits${params.toString() ? '?' + params.toString() : ''}`;
      const response = await fetch(url, {
        headers: this.getHeaders(),
        signal: AbortSignal.timeout(API_CONFIG.TIMEOUT),
      });

      if (!response.ok) {
        throw new Error(`Failed to fetch parcel deposits: ${response.status}`);
      }

      const data = await response.json();
      return data.$values || data;
    } catch (error) {
      throw error;
    }
  }

  async getParcelDepositById(id: number): Promise<ParcelDepositDto | null> {
    try {
      const response = await fetch(`${API_BASE_URL}/ParcelDeposits/${id}`, {
        headers: this.getHeaders(),
        signal: AbortSignal.timeout(API_CONFIG.TIMEOUT),
      });

      if (!response.ok) {
        if (response.status === 404) {
          return null;
        }
        throw new Error(`Failed to fetch parcel deposit: ${response.status}`);
      }

      return await response.json();
    } catch (error) {
      throw error;
    }
  }

  async getParcelDepositByParcelId(parcelId: string): Promise<ParcelDepositDto | null> {
    try {
      const response = await fetch(`${API_BASE_URL}/ParcelDeposits/parcel/${parcelId}`, {
        headers: this.getHeaders(),
        signal: AbortSignal.timeout(API_CONFIG.TIMEOUT),
      });

      if (!response.ok) {
        if (response.status === 404) {
          return null;
        }
        throw new Error(`Failed to fetch parcel deposit: ${response.status}`);
      }

      return await response.json();
    } catch (error) {
      throw error;
    }
  }

  async getClerkCashInSummary(date?: string, destination?: string): Promise<ClerkCashInSummaryDto[]> {
    try {
      const params = new URLSearchParams();
      if (date) params.append('date', date);
      if (destination) params.append('destination', destination);

      const url = `${API_BASE_URL}/ParcelDeposits/clerk-summary${params.toString() ? '?' + params.toString() : ''}`;
      const response = await fetch(url, {
        headers: this.getHeaders(),
        signal: AbortSignal.timeout(API_CONFIG.TIMEOUT),
      });

      if (!response.ok) {
        throw new Error(`Failed to fetch clerk cash-in summary: ${response.status}`);
      }

      const data = await response.json();
      return data.$values || data;
    } catch (error) {
      throw error;
    }
  }

  async createParcelDeposit(depositData: CreateParcelDepositDto): Promise<ParcelDepositDto> {
    try {
      const response = await fetch(`${API_BASE_URL}/ParcelDeposits`, {
        method: 'POST',
        headers: this.getHeaders(),
        body: JSON.stringify(depositData),
        signal: AbortSignal.timeout(API_CONFIG.TIMEOUT),
      });

      if (!response.ok) {
        const errorText = await response.text();
        throw new Error(`Failed to create parcel deposit: ${response.status} - ${errorText}`);
      }

      return await response.json();
    } catch (error) {
      throw error;
    }
  }

  async updateParcelDeposit(id: number, depositData: UpdateParcelDepositDto): Promise<void> {
    try {
      const response = await fetch(`${API_BASE_URL}/ParcelDeposits/${id}`, {
        method: 'PUT',
        headers: this.getHeaders(),
        body: JSON.stringify(depositData),
        signal: AbortSignal.timeout(API_CONFIG.TIMEOUT),
      });

      if (!response.ok) {
        const errorText = await response.text();
        throw new Error(`Failed to update parcel deposit: ${response.status} - ${errorText}`);
      }
    } catch (error) {
      throw error;
    }
  }

  async updateOrCreateParcelDeposit(parcelId: string, depositData: UpdateParcelDepositDto): Promise<ParcelDepositDto> {
    try {
      const response = await fetch(`${API_BASE_URL}/ParcelDeposits/parcel/${parcelId}`, {
        method: 'PUT',
        headers: this.getHeaders(),
        body: JSON.stringify(depositData),
        signal: AbortSignal.timeout(API_CONFIG.TIMEOUT),
      });

      if (!response.ok) {
        const errorText = await response.text();
        throw new Error(`Failed to update or create parcel deposit: ${response.status} - ${errorText}`);
      }

      return await response.json();
    } catch (error) {
      throw error;
    }
  }

  async deleteParcelDeposit(id: number): Promise<void> {
    try {
      const response = await fetch(`${API_BASE_URL}/ParcelDeposits/${id}`, {
        method: 'DELETE',
        headers: this.getHeaders(),
        signal: AbortSignal.timeout(API_CONFIG.TIMEOUT),
      });

      if (!response.ok) {
        const errorText = await response.text();
        throw new Error(`Failed to delete parcel deposit: ${response.status} - ${errorText}`);
      }
    } catch (error) {
      throw error;
    }
  }
}

// Export a singleton instance
export const wmsApi = new WMSApiService();

// Hook for using the WMS API service with authentication context
export const useWMSApi = () => {
  const { user, token } = useAuth();

  return {
    api: wmsApi,
    user,
    token,
    isAuthenticated: !!user && !!token,
  };
};
