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
}

interface ReportsDataContextType {
  cachedData: CachedData | null;
  loading: boolean;
  error: string;
  getCachedData: (filters?: ReportFilters) => Promise<CachedData>;
  invalidateCache: () => void;
  isDataFresh: () => boolean;
  getDataAge: () => string;
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

  const fetchFreshData = useCallback(async (filters?: ReportFilters): Promise<CachedData> => {
    setLoading(true);
    setError('');

    try {
      console.log('ReportsDataContext: Fetching fresh data...');
      
      // Fetch data in parallel
      const [parcelsData, dispatchesData] = await Promise.all([
        filters 
          ? wmsApi.getAllParcelsForReports({
              startDate: filters.startDate,
              endDate: filters.endDate,
              destination: filters.destination,
              search: filters.clerk // Use search for clerk filtering
            })
          : wmsApi.getAllParcelsForReports({}),
        wmsApi.getDispatches()
      ]);

      const newCachedData: CachedData = {
        parcels: parcelsData,
        dispatches: dispatchesData,
        lastFetched: Date.now(),
        filters: filters
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

  const invalidateCache = useCallback(() => {
    console.log('ReportsDataContext: Cache invalidated');
    setCachedData(null);
    setError('');
  }, []);

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
    invalidateCache,
    isDataFresh,
    getDataAge
  }), [cachedData, loading, error, getCachedData, invalidateCache, isDataFresh, getDataAge]);

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