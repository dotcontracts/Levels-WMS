import React, { createContext, useContext, useState, useCallback, useMemo } from 'react';
import { wmsApi, Parcel, Dispatch } from '../services/wmsApi';

export interface ReportFilters {
  startDate: string;
  endDate: string;
  branch: string;
  destination: string;
  clerk: string;
}

interface CachedData {
  parcels: Parcel[];
  dispatches: Dispatch[];
  lastFetched: number;
  filters?: ReportFilters;
  isPartial?: boolean;
  loadProgress?: {
    loaded: number;
    total: number;
    isComplete: boolean;
  };
}

interface ReportsDataContextType {
  cachedData: CachedData | null;
  loading: boolean;
  error: string;
  getCachedData: (filters?: ReportFilters) => Promise<CachedData>;
  getStreamingData: (filters?: ReportFilters, onProgress?: (data: CachedData) => void) => Promise<CachedData>;
  invalidateCache: (filters?: ReportFilters) => void;
  isDataFresh: () => boolean;
  getDataAge: () => string;
  getLoadProgress: () => { loaded: number; total: number; percentage: number } | null;
  hasDataInCache: (filters?: ReportFilters) => boolean;
}

const ReportsDataContext = createContext<ReportsDataContextType | undefined>(undefined);

const CACHE_DURATION = 5 * 60 * 1000; // 5 minutes in milliseconds
const AUTO_REFRESH_INTERVAL = 10 * 60 * 1000; // 10 minutes in milliseconds

export const ReportsDataProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [cachedData, setCachedData] = useState<CachedData | null>(null);
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

  const fetchFreshData = useCallback(async (filters?: ReportFilters): Promise<CachedData> => {
    setLoading(true);
    setError('');

    try {
      console.log('ReportsDataContext: Fetching fresh data...');
      
      // Generate consistent cache namespace for this filter combination
      const filterParams = {
        startDate: filters?.startDate,
        endDate: filters?.endDate,
        destination: filters?.destination,
        search: filters?.clerk // Use search for clerk filtering
      };
      
      // Use the same caching mechanism as the API layer for consistency
      console.log('ReportsDataContext: Using smart cache namespace for data sharing');
      
      // Fetch data in parallel with namespace caching
      const [parcelsData, dispatchesData] = await Promise.all([
        wmsApi.getAllParcelsForReports(filterParams),
        // Pass the same namespace to dispatches for cache consistency
        filters 
          ? wmsApi.getDispatches(undefined, `reports:${JSON.stringify(filterParams)}`)
          : wmsApi.getDispatches()
      ]);

      const newCachedData: CachedData = {
        parcels: parcelsData,
        dispatches: dispatchesData,
        lastFetched: Date.now(),
        filters: filters,
        isPartial: false,
        loadProgress: {
          loaded: parcelsData.length,
          total: parcelsData.length,
          isComplete: true
        }
      };

      setCachedData(newCachedData);
      console.log(`ReportsDataContext: Cached ${parcelsData.length} parcels and ${dispatchesData.length} dispatches`);
      
      return newCachedData;
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to fetch reports data';
      setError(errorMessage);
      console.error('ReportsDataContext: Error fetching data:', err);
      throw err;
    } finally {
      setLoading(false);
    }
  }, []);

  const getStreamingData = useCallback(async (
    filters?: ReportFilters, 
    onProgress?: (data: CachedData) => void
  ): Promise<CachedData> => {
    setLoading(true);
    setError('');

    try {
      console.log('ReportsDataContext: Starting streaming data fetch...');
      
      // Generate consistent cache namespace for streaming
      const filterParams = {
        startDate: filters?.startDate,
        endDate: filters?.endDate,
        destination: filters?.destination,
        search: filters?.clerk
      };
      const cacheNamespace = `reports:streaming:${JSON.stringify(filterParams)}`;
      
      let streamingParcels: Parcel[] = [];
      let dispatchesData: Dispatch[] = [];
      
      // Start both requests, but stream parcels progressively with namespace isolation
      const [parcelsPromise, dispatchesPromise] = [
        wmsApi.getAllParcelsProgressively({
          dateRange: filters?.startDate && filters?.endDate 
            ? { from: filters.startDate, to: filters.endDate }
            : undefined,
          destination: filters?.destination,
          search: filters?.clerk,
          chunkSize: 1200, // Larger chunks for better streaming performance
          cacheNamespace: cacheNamespace, // Use namespace for isolation
          fields: [
            'id', 'waybillNumber', 'senderName', 'recipientName', 
            'destination', 'totalAmount', 'paymentMethods', 'createdAt', 
            'status', 'createdById', 'createdBy', 'dispatchedAt'
          ], // Only fetch fields needed for reports
          onProgress: (loaded, total) => {
            console.log(`ReportsDataContext: Streaming progress: ${loaded}/${total} parcels`);
            
            // Update cached data with partial results
            const partialData: CachedData = {
              parcels: streamingParcels.slice(), // Copy current state
              dispatches: dispatchesData,
              lastFetched: Date.now(),
              filters: filters,
              isPartial: loaded < total,
              loadProgress: {
                loaded,
                total,
                isComplete: loaded >= total
              }
            };
            
            // Update context state and notify callback
            setCachedData(partialData);
            onProgress?.(partialData);
          }
        }).then(parcels => {
          streamingParcels = parcels;
          return parcels;
        }),
        wmsApi.getDispatches(undefined, cacheNamespace).then(dispatches => {
          dispatchesData = dispatches;
          return dispatches;
        })
      ];

      // Wait for both to complete
      const [parcelsData, finalDispatchesData] = await Promise.all([parcelsPromise, dispatchesPromise]);

      const finalData: CachedData = {
        parcels: parcelsData,
        dispatches: finalDispatchesData,
        lastFetched: Date.now(),
        filters: filters,
        isPartial: false,
        loadProgress: {
          loaded: parcelsData.length,
          total: parcelsData.length,
          isComplete: true
        }
      };

      setCachedData(finalData);
      console.log(`ReportsDataContext: Streaming complete - ${parcelsData.length} parcels, ${finalDispatchesData.length} dispatches`);
      
      return finalData;
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to fetch streaming reports data';
      setError(errorMessage);
      console.error('ReportsDataContext: Error fetching streaming data:', err);
      throw err;
    } finally {
      setLoading(false);
    }
  }, []);

  const getCachedData = useCallback(async (filters?: ReportFilters): Promise<CachedData> => {
    // Check if we have fresh cached data with matching filters
    if (cachedData && isDataFresh()) {
      // Check if filters match (simple comparison for now)
      const filtersMatch = !filters || (
        JSON.stringify(cachedData.filters) === JSON.stringify(filters)
      );
      
      if (filtersMatch) {
        console.log('ReportsDataContext: Using cached data');
        return cachedData;
      }
    }

    // Fetch fresh data if cache is stale or filters don't match
    console.log('ReportsDataContext: Cache miss or stale, fetching fresh data');
    return await fetchFreshData(filters);
  }, [cachedData, isDataFresh, fetchFreshData]);

  const invalidateCache = useCallback((filters?: ReportFilters) => {
    console.log('ReportsDataContext: Cache invalidated');
    setCachedData(null);
    setError('');
    
    // Also clear the corresponding API cache namespace if filters are provided
    if (filters) {
      const filterParams = {
        startDate: filters.startDate,
        endDate: filters.endDate,
        destination: filters.destination,
        search: filters.clerk
      };
      const cacheNamespace = `reports:${JSON.stringify(filterParams)}`;
      console.log(`ReportsDataContext: Clearing API cache namespace: ${cacheNamespace}`);
      wmsApi.clearNamespacedCache(cacheNamespace);
    }
  }, []);
  
  const hasDataInCache = useCallback((filters?: ReportFilters): boolean => {
    if (!cachedData) return false;
    
    // Check if filters match (simple comparison for now)
    const filtersMatch = !filters || (
      JSON.stringify(cachedData.filters) === JSON.stringify(filters)
    );
    
    return filtersMatch && isDataFresh();
  }, [cachedData, isDataFresh]);

  // Auto-refresh mechanism
  React.useEffect(() => {
    if (!cachedData) return;

    const interval = setInterval(() => {
      const age = Date.now() - cachedData.lastFetched;
      if (age > AUTO_REFRESH_INTERVAL) {
        console.log('ReportsDataContext: Auto-refreshing stale cache');
        fetchFreshData(cachedData.filters).catch(err => {
          console.error('ReportsDataContext: Auto-refresh failed:', err);
        });
      }
    }, 60000); // Check every minute

    return () => clearInterval(interval);
  }, [cachedData, fetchFreshData]);

  const contextValue = useMemo(() => ({
    cachedData,
    loading,
    error,
    getCachedData,
    getStreamingData,
    invalidateCache,
    isDataFresh,
    getDataAge,
    getLoadProgress,
    hasDataInCache
  }), [cachedData, loading, error, getCachedData, getStreamingData, invalidateCache, isDataFresh, getDataAge, getLoadProgress, hasDataInCache]);

  return (
    <ReportsDataContext.Provider value={contextValue}>
      {children}
    </ReportsDataContext.Provider>
  );
};

export const useReportsData = (): ReportsDataContextType => {
  const context = useContext(ReportsDataContext);
  if (context === undefined) {
    throw new Error('useReportsData must be used within a ReportsDataProvider');
  }
  return context;
};