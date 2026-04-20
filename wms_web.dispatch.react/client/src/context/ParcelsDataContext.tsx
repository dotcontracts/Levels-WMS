import React, { createContext, useContext, useState, useCallback, useMemo } from 'react';
import { wmsApi, Parcel } from '../services/wmsApi';

export interface ParcelFilters {
  search: string;
  status: string;
  destination: string;
  paymentMethod: string;
  dateRange: { from: string; to: string };
  sender: string;
  receiver: string;
  clerk: string;
  page: number;
  pageSize: number;
}

interface CachedParcelData {
  parcels: Parcel[];
  totalCount: number;
  totalPages: number;
  currentPage: number;
  lastFetched: number;
  filters?: ParcelFilters;
  isPartial?: boolean;
  loadProgress?: {
    loaded: number;
    total: number;
    isComplete: boolean;
  };
}

interface ParcelsDataContextType {
  cachedData: CachedParcelData | null;
  loading: boolean;
  error: string;
  getCachedParcels: (filters?: Partial<ParcelFilters>) => Promise<CachedParcelData>;
  getStreamingParcels: (filters?: Partial<ParcelFilters>, onProgress?: (data: CachedParcelData) => void) => Promise<CachedParcelData>;
  loadMoreParcels: () => Promise<CachedParcelData | null>;
  invalidateCache: (filters?: Partial<ParcelFilters>) => void;
  isDataFresh: () => boolean;
  getDataAge: () => string;
  getLoadProgress: () => { loaded: number; total: number; percentage: number } | null;
  hasDataInCache: (filters?: Partial<ParcelFilters>) => boolean;
}

const ParcelsDataContext = createContext<ParcelsDataContextType | undefined>(undefined);

const CACHE_DURATION = 3 * 60 * 1000; // 3 minutes in milliseconds
const AUTO_REFRESH_INTERVAL = 5 * 60 * 1000; // 5 minutes in milliseconds
const DEFAULT_PAGE_SIZE = 200;

export const ParcelsDataProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [cachedData, setCachedData] = useState<CachedParcelData | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  const isDataFresh = useCallback((): boolean => {
    if (!cachedData) return false;
    return Date.now() - cachedData.lastFetched < CACHE_DURATION;
  }, [cachedData]);

  const getDataAge = useCallback((): string => {
    if (!cachedData) return 'No data';
    
    const ageMs = Date.now() - cachedData.lastFetched;
    const ageMinutes = Math.floor(ageMs / (60 * 1000));
    const ageSeconds = Math.floor((ageMs % (60 * 1000)) / 1000);
    
    if (ageMinutes > 0) {
      return `${ageMinutes}m ${ageSeconds}s ago`;
    }
    return `${ageSeconds}s ago`;
  }, [cachedData]);

  const getLoadProgress = useCallback(() => {
    if (!cachedData?.loadProgress) return null;
    
    const { loaded, total } = cachedData.loadProgress;
    const percentage = total > 0 ? Math.round((loaded / total) * 100) : 0;
    
    return { loaded, total, percentage };
  }, [cachedData]);

  const normalizeFilters = useCallback((filters?: Partial<ParcelFilters>): ParcelFilters => {
    return {
      search: filters?.search || '',
      status: filters?.status || '',
      destination: filters?.destination || '',
      paymentMethod: filters?.paymentMethod || '',
      dateRange: filters?.dateRange || { from: '', to: '' },
      sender: filters?.sender || '',
      receiver: filters?.receiver || '',
      clerk: filters?.clerk || '',
      page: filters?.page || 1,
      pageSize: filters?.pageSize || DEFAULT_PAGE_SIZE
    };
  }, []);

  const fetchParcelData = useCallback(async (filters?: Partial<ParcelFilters>): Promise<CachedParcelData> => {
    setLoading(true);
    setError('');

    try {
      const normalizedFilters = normalizeFilters(filters);
      console.log('ParcelsDataContext: Fetching parcel data with filters:', normalizedFilters);
      
      // Generate consistent cache namespace for this filter combination
      const filterParams = {
        search: normalizedFilters.search,
        status: normalizedFilters.status,
        destination: normalizedFilters.destination,
        paymentMethod: normalizedFilters.paymentMethod,
        dateRange: normalizedFilters.dateRange,
        sender: normalizedFilters.sender,
        receiver: normalizedFilters.receiver,
        clerk: normalizedFilters.clerk,
        page: normalizedFilters.page,
        pageSize: normalizedFilters.pageSize
      };
      
      // Use the same caching mechanism as the API layer
      console.log('ParcelsDataContext: Using smart cache namespace for data sharing');
      
      // Build search parameters for API call
      const searchParams: any = {
        page: normalizedFilters.page,
        pageSize: normalizedFilters.pageSize
      };

      // Add filter parameters
      if (normalizedFilters.search) {
        searchParams.search = normalizedFilters.search;
      }
      if (normalizedFilters.destination) {
        searchParams.destination = normalizedFilters.destination;
      }
      if (normalizedFilters.status) {
        searchParams.status = parseInt(normalizedFilters.status);
      }
      if (normalizedFilters.dateRange.from) {
        searchParams.fromDate = normalizedFilters.dateRange.from;
      }
      if (normalizedFilters.dateRange.to) {
        searchParams.toDate = normalizedFilters.dateRange.to;
      }
      if (normalizedFilters.clerk) {
        searchParams.createdByUsername = normalizedFilters.clerk;
      }

      // Build comprehensive search for additional fields (excluding payment method)
      let searchTerms = [];
      if (normalizedFilters.sender) {
        searchTerms.push(normalizedFilters.sender);
      }
      if (normalizedFilters.receiver) {
        searchTerms.push(normalizedFilters.receiver);
      }
      // Note: paymentMethod is handled separately, not as a general search term
      
      // Handle payment method filtering specifically
      if (normalizedFilters.paymentMethod) {
        // For payment method, we need to search specifically in the paymentMethods field
        const paymentMethodSearch = `paymentMethod:${normalizedFilters.paymentMethod}`;
        if (searchParams.search) {
          searchParams.search += ` ${paymentMethodSearch}`;
        } else {
          searchParams.search = paymentMethodSearch;
        }
      }

      // Combine with existing search if any
      if (searchTerms.length > 0) {
        const additionalSearch = searchTerms.join(' ');
        if (searchParams.search) {
          searchParams.search += ` ${additionalSearch}`;
        } else {
          searchParams.search = additionalSearch;
        }
      }

      // Use the namespace-aware API call
      const result = await wmsApi.getParcelsPaged(searchParams, `parcels:${JSON.stringify(filterParams)}`);

      const newCachedData: CachedParcelData = {
        parcels: result.data,
        totalCount: result.totalCount,
        totalPages: result.totalPages,
        currentPage: result.currentPage,
        lastFetched: Date.now(),
        filters: normalizedFilters,
        isPartial: false,
        loadProgress: {
          loaded: result.data.length,
          total: result.totalCount,
          isComplete: true
        }
      };

      setCachedData(newCachedData);
      console.log(`ParcelsDataContext: Cached ${result.data.length} parcels (page ${result.currentPage}/${result.totalPages})`);
      
      return newCachedData;
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to fetch parcels data';
      setError(errorMessage);
      console.error('ParcelsDataContext: Error fetching data:', err);
      throw err;
    } finally {
      setLoading(false);
    }
  }, [normalizeFilters]);

  const getStreamingParcels = useCallback(async (
    filters?: Partial<ParcelFilters>, 
    onProgress?: (data: CachedParcelData) => void
  ): Promise<CachedParcelData> => {
    setLoading(true);
    setError('');

    try {
      const normalizedFilters = normalizeFilters(filters);
      console.log('ParcelsDataContext: Starting streaming parcels fetch...');
      
      // Generate consistent cache namespace for streaming
      const filterParams = {
        search: normalizedFilters.search,
        status: normalizedFilters.status,
        destination: normalizedFilters.destination,
        paymentMethod: normalizedFilters.paymentMethod,
        dateRange: normalizedFilters.dateRange,
        sender: normalizedFilters.sender,
        receiver: normalizedFilters.receiver,
        clerk: normalizedFilters.clerk
      };
      const cacheNamespace = `parcels:streaming:${JSON.stringify(filterParams)}`;
      
      let streamingParcels: Parcel[] = [];
      
      // Use progressive loading with namespace isolation
      const parcelsData = await wmsApi.getAllParcelsProgressively({
        dateRange: normalizedFilters.dateRange.from && normalizedFilters.dateRange.to 
          ? { from: normalizedFilters.dateRange.from, to: normalizedFilters.dateRange.to }
          : undefined,
        destination: normalizedFilters.destination,
        search: normalizedFilters.search,
        chunkSize: normalizedFilters.pageSize,
        cacheNamespace: cacheNamespace,
        fields: [
          'id', 'waybillNumber', 'senderName', 'recipientName', 
          'destination', 'totalAmount', 'paymentMethods', 'createdAt', 
          'status', 'createdById', 'createdBy', 'dispatchedAt'
        ],
        onProgress: (loaded, total) => {
          console.log(`ParcelsDataContext: Streaming progress: ${loaded}/${total} parcels`);
          
          const partialData: CachedParcelData = {
            parcels: streamingParcels.slice(),
            totalCount: total,
            totalPages: Math.ceil(total / normalizedFilters.pageSize),
            currentPage: 1,
            lastFetched: Date.now(),
            filters: normalizedFilters,
            isPartial: loaded < total,
            loadProgress: {
              loaded,
              total,
              isComplete: loaded >= total
            }
          };
          
          setCachedData(partialData);
          onProgress?.(partialData);
        }
      });

      streamingParcels = parcelsData;

      const finalData: CachedParcelData = {
        parcels: parcelsData,
        totalCount: parcelsData.length,
        totalPages: Math.ceil(parcelsData.length / normalizedFilters.pageSize),
        currentPage: 1,
        lastFetched: Date.now(),
        filters: normalizedFilters,
        isPartial: false,
        loadProgress: {
          loaded: parcelsData.length,
          total: parcelsData.length,
          isComplete: true
        }
      };

      setCachedData(finalData);
      console.log(`ParcelsDataContext: Streaming complete - ${parcelsData.length} parcels`);
      
      return finalData;
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to fetch streaming parcels data';
      setError(errorMessage);
      console.error('ParcelsDataContext: Error fetching streaming data:', err);
      throw err;
    } finally {
      setLoading(false);
    }
  }, [normalizeFilters]);

  const getCachedParcels = useCallback(async (filters?: Partial<ParcelFilters>): Promise<CachedParcelData> => {
    const normalizedFilters = normalizeFilters(filters);
    
    // Check if we have fresh cached data with matching filters
    if (cachedData && isDataFresh()) {
      // Check if filters match (excluding page for cache sharing)
      const currentFiltersForCache = { ...cachedData.filters };
      const newFiltersForCache = { ...normalizedFilters };
      
      // Compare filters excluding page for cache sharing between pages
      const { page: currentPage, ...currentFiltersNoPage } = currentFiltersForCache;
      const { page: newPage, ...newFiltersNoPage } = newFiltersForCache;
      
      const filtersMatch = JSON.stringify(currentFiltersNoPage) === JSON.stringify(newFiltersNoPage);
      
      if (filtersMatch) {
        console.log('ParcelsDataContext: Using cached data');
        return cachedData;
      }
    }

    // Fetch fresh data if cache is stale or filters don't match
    console.log('ParcelsDataContext: Cache miss or stale, fetching fresh data');
    return await fetchParcelData(filters);
  }, [cachedData, isDataFresh, fetchParcelData, normalizeFilters]);

  const loadMoreParcels = useCallback(async (): Promise<CachedParcelData | null> => {
    if (!cachedData || !cachedData.filters) {
      console.warn('ParcelsDataContext: No cached data to load more from');
      return null;
    }

    if (cachedData.currentPage >= cachedData.totalPages) {
      console.log('ParcelsDataContext: No more pages to load');
      return cachedData;
    }

    try {
      setLoading(true);
      const nextPageFilters = {
        ...cachedData.filters,
        page: cachedData.currentPage + 1
      };

      const nextPageData = await fetchParcelData(nextPageFilters);
      
      // Merge with existing data
      const mergedData: CachedParcelData = {
        ...nextPageData,
        parcels: [...cachedData.parcels, ...nextPageData.parcels],
        currentPage: nextPageData.currentPage,
        loadProgress: {
          loaded: cachedData.parcels.length + nextPageData.parcels.length,
          total: nextPageData.totalCount,
          isComplete: nextPageData.currentPage >= nextPageData.totalPages
        }
      };

      setCachedData(mergedData);
      console.log(`ParcelsDataContext: Loaded more parcels, total: ${mergedData.parcels.length}`);
      
      return mergedData;
    } catch (error) {
      console.error('ParcelsDataContext: Error loading more parcels:', error);
      return null;
    } finally {
      setLoading(false);
    }
  }, [cachedData, fetchParcelData]);

  const invalidateCache = useCallback((filters?: Partial<ParcelFilters>) => {
    console.log('ParcelsDataContext: Cache invalidated');
    setCachedData(null);
    setError('');
    
    // Also clear the corresponding API cache namespace if filters are provided
    if (filters) {
      const normalizedFilters = normalizeFilters(filters);
      const filterParams = {
        search: normalizedFilters.search,
        status: normalizedFilters.status,
        destination: normalizedFilters.destination,
        paymentMethod: normalizedFilters.paymentMethod,
        dateRange: normalizedFilters.dateRange,
        sender: normalizedFilters.sender,
        receiver: normalizedFilters.receiver,
        clerk: normalizedFilters.clerk,
        page: normalizedFilters.page,
        pageSize: normalizedFilters.pageSize
      };
      const cacheNamespace = `parcels:${JSON.stringify(filterParams)}`;
      console.log(`ParcelsDataContext: Clearing API cache namespace: ${cacheNamespace}`);
      wmsApi.clearNamespacedCache(cacheNamespace);
    }
  }, [normalizeFilters]);
  
  const hasDataInCache = useCallback((filters?: Partial<ParcelFilters>): boolean => {
    if (!cachedData) return false;
    
    const normalizedFilters = normalizeFilters(filters);
    
    // Check if filters match (excluding page for cache sharing)
    const currentFiltersForCache = { ...cachedData.filters };
    const newFiltersForCache = { ...normalizedFilters };
    
    const { page: currentPage, ...currentFiltersNoPage } = currentFiltersForCache || {};
    const { page: newPage, ...newFiltersNoPage } = newFiltersForCache;
    
    const filtersMatch = JSON.stringify(currentFiltersNoPage) === JSON.stringify(newFiltersNoPage);
    
    return filtersMatch && isDataFresh();
  }, [cachedData, isDataFresh, normalizeFilters]);

  // Auto-refresh mechanism
  React.useEffect(() => {
    if (!cachedData) return;

    const interval = setInterval(() => {
      const age = Date.now() - cachedData.lastFetched;
      if (age > AUTO_REFRESH_INTERVAL) {
        console.log('ParcelsDataContext: Auto-refreshing stale cache');
        fetchParcelData(cachedData.filters).catch(err => {
          console.error('ParcelsDataContext: Auto-refresh failed:', err);
        });
      }
    }, 60000); // Check every minute

    return () => clearInterval(interval);
  }, [cachedData, fetchParcelData]);

  const contextValue = useMemo(() => ({
    cachedData,
    loading,
    error,
    getCachedParcels,
    getStreamingParcels,
    loadMoreParcels,
    invalidateCache,
    isDataFresh,
    getDataAge,
    getLoadProgress,
    hasDataInCache
  }), [
    cachedData,
    loading,
    error,
    getCachedParcels,
    getStreamingParcels,
    loadMoreParcels,
    invalidateCache,
    isDataFresh,
    getDataAge,
    getLoadProgress,
    hasDataInCache
  ]);

  return (
    <ParcelsDataContext.Provider value={contextValue}>
      {children}
    </ParcelsDataContext.Provider>
  );
};

export const useParcelsData = (): ParcelsDataContextType => {
  const context = useContext(ParcelsDataContext);
  if (context === undefined) {
    throw new Error('useParcelsData must be used within a ParcelsDataProvider');
  }
  return context;
};