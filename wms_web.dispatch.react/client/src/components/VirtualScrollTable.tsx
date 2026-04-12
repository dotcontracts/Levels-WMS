import React, { useState, useEffect, useRef, useMemo, useCallback } from 'react';

interface Column<T> {
  key: string;
  header: string | React.ReactNode;
  width?: number;
  render?: (item: T, index: number) => React.ReactNode;
  sortable?: boolean;
}

interface VirtualScrollTableProps<T> {
  data: T[];
  columns: Column<T>[];
  rowHeight?: number;
  containerHeight?: number;
  onRowClick?: (item: T, index: number) => void;
  className?: string;
  loading?: boolean;
  emptyMessage?: string;
  hasMoreData?: boolean;
  isLoadingMore?: boolean;
  onLoadMore?: () => void;
}

export function VirtualScrollTable<T extends Record<string, any>>({
  data,
  columns,
  rowHeight = 60,
  containerHeight = 500,
  onRowClick,
  className = '',
  loading = false,
  emptyMessage = 'No data available',
  hasMoreData = false,
  isLoadingMore = false,
  onLoadMore
}: VirtualScrollTableProps<T>) {
  const [scrollTop, setScrollTop] = useState(0);
  const [scrollLeft, setScrollLeft] = useState(0);
  const [sortField, setSortField] = useState<string>('');
  const [sortDirection, setSortDirection] = useState<'asc' | 'desc'>('asc');
  
  const containerRef = useRef<HTMLDivElement>(null);
  const scrollElementRef = useRef<HTMLDivElement>(null);
  const headerRef = useRef<HTMLDivElement>(null);

  // Calculate visible range
  const startIndex = Math.floor(scrollTop / rowHeight);
  const endIndex = Math.min(
    startIndex + Math.ceil(containerHeight / rowHeight) + 1,
    data.length
  );

  // Sort data
  const sortedData = useMemo(() => {
    if (!sortField) return data;
    
    return [...data].sort((a, b) => {
      const aVal = a[sortField];
      const bVal = b[sortField];
      
      if (aVal === bVal) return 0;
      
      const comparison = aVal < bVal ? -1 : 1;
      return sortDirection === 'asc' ? comparison : -comparison;
    });
  }, [data, sortField, sortDirection]);

  // Get visible items
  const visibleItems = sortedData.slice(startIndex, endIndex);

  const handleScroll = useCallback((e: React.UIEvent<HTMLDivElement>) => {
    const target = e.currentTarget;
    setScrollTop(target.scrollTop);
    setScrollLeft(target.scrollLeft);
    
    // Sync header scroll
    if (headerRef.current) {
      headerRef.current.scrollLeft = target.scrollLeft;
    }

    // Check if we're near the bottom (within 200px) and should load more data
    if (hasMoreData && !isLoadingMore && onLoadMore) {
      const { scrollTop, scrollHeight, clientHeight } = target;
      const distanceFromBottom = scrollHeight - scrollTop - clientHeight;
      
      if (distanceFromBottom < 200) {
        onLoadMore();
      }
    }
  }, [hasMoreData, isLoadingMore, onLoadMore]);

  const handleSort = useCallback((field: string) => {
    if (field === sortField) {
      setSortDirection(prev => prev === 'asc' ? 'desc' : 'asc');
    } else {
      setSortField(field);
      setSortDirection('asc');
    }
  }, [sortField]);

  const renderCell = (item: T, column: Column<T>, index: number) => {
    if (column.render) {
      return column.render(item, index);
    }
    return item[column.key];
  };

  if (loading) {
    return (
      <div className={`border border-gray-200 rounded-lg ${className}`}>
        <div className="p-8 text-center">
          <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600 mx-auto mb-4"></div>
          <p className="text-gray-500">Loading...</p>
        </div>
      </div>
    );
  }

  if (data.length === 0) {
    return (
      <div className={`border border-gray-200 rounded-lg ${className}`}>
        <div className="p-8 text-center">
          <p className="text-gray-500">{emptyMessage}</p>
        </div>
      </div>
    );
  }

  return (
    <div className={`border border-gray-200 rounded-lg bg-white ${className}`}>
      {/* Header */}
      <div className="bg-gray-50 border-b border-gray-200">
        <div 
          ref={headerRef}
          className="overflow-hidden"
          style={{ overflowX: 'hidden' }}
        >
          <div className="grid gap-4 px-4 py-3" style={{ gridTemplateColumns: columns.map(col => col.width ? `${col.width}px` : '1fr').join(' ') }}>
            {columns.map((column) => (
              <div
                key={column.key}
                className={`font-medium text-gray-900 text-sm ${column.sortable ? 'cursor-pointer hover:text-blue-600' : ''}`}
                onClick={column.sortable ? () => handleSort(column.key) : undefined}
              >
                <div className="flex items-center space-x-1">
                  {typeof column.header === 'string' ? (
                    <>
                      <span>{column.header}</span>
                      {column.sortable && sortField === column.key && (
                        <span className="text-xs">
                          {sortDirection === 'asc' ? '↑' : '↓'}
                        </span>
                      )}
                    </>
                  ) : (
                    column.header
                  )}
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>

      {/* Virtual scroll container */}
      <div
        ref={containerRef}
        className="overflow-auto"
        style={{ height: containerHeight }}
        onScroll={handleScroll}
      >
        {/* Total height to create scrollbar */}
        <div style={{ height: sortedData.length * rowHeight + (isLoadingMore ? 60 : 0), position: 'relative' }}>
          {/* Visible rows */}
          <div style={{ transform: `translateY(${startIndex * rowHeight}px)` }}>
            {visibleItems.map((item, index) => {
              const actualIndex = startIndex + index;
              return (
                <div
                  key={actualIndex}
                  className={`border-b border-gray-100 hover:bg-gray-50 ${onRowClick ? 'cursor-pointer' : ''}`}
                  style={{ height: rowHeight }}
                  onClick={onRowClick ? () => onRowClick(item, actualIndex) : undefined}
                >
                  <div 
                    className="grid gap-4 px-4 py-3 h-full items-center"
                    style={{ gridTemplateColumns: columns.map(col => col.width ? `${col.width}px` : '1fr').join(' ') }}
                  >
                    {columns.map((column) => (
                      <div key={column.key} className="text-sm text-gray-900 truncate">
                        {renderCell(item, column, actualIndex)}
                      </div>
                    ))}
                  </div>
                </div>
              );
            })}
          </div>
          
          {/* Loading indicator at the bottom */}
          {isLoadingMore && (
            <div 
              className="flex items-center justify-center border-b border-gray-100"
              style={{ 
                position: 'absolute',
                top: sortedData.length * rowHeight,
                height: 60,
                width: '100%',
                background: 'white'
              }}
            >
              <div className="flex items-center space-x-2 text-gray-500">
                <div className="animate-spin rounded-full h-5 w-5 border-b-2 border-blue-600"></div>
                <span className="text-sm">Loading more...</span>
              </div>
            </div>
          )}
        </div>
      </div>
      
      {/* Footer with total count */}
      <div className="bg-gray-50 px-4 py-2 border-t border-gray-200">
        <p className="text-sm text-gray-500">
          Showing {visibleItems.length} of {sortedData.length} items{hasMoreData ? ' (more available)' : ''}
        </p>
      </div>
    </div>
  );
}