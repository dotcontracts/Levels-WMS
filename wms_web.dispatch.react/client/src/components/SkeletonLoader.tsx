import React from 'react';

interface SkeletonProps {
  width?: string | number;
  height?: string | number;
  className?: string;
  variant?: 'text' | 'rectangular' | 'circular';
  animate?: boolean;
}

export const Skeleton: React.FC<SkeletonProps> = ({
  width = '100%',
  height = '20px',
  className = '',
  variant = 'rectangular',
  animate = true
}) => {
  const baseClass = 'bg-gray-200 dark:bg-gray-700';
  const animateClass = animate ? 'animate-pulse' : '';
  
  let variantClass = '';
  switch (variant) {
    case 'text':
      variantClass = 'rounded';
      break;
    case 'circular':
      variantClass = 'rounded-full';
      break;
    case 'rectangular':
    default:
      variantClass = 'rounded-md';
      break;
  }

  const style = {
    width: typeof width === 'number' ? `${width}px` : width,
    height: typeof height === 'number' ? `${height}px` : height,
  };

  return (
    <div 
      className={`${baseClass} ${variantClass} ${animateClass} ${className}`}
      style={style}
    />
  );
};

interface SkeletonTableProps {
  rows?: number;
  columns?: number;
  showHeader?: boolean;
  className?: string;
}

export const SkeletonTable: React.FC<SkeletonTableProps> = ({
  rows = 10,
  columns = 5,
  showHeader = true,
  className = ''
}) => {
  return (
    <div className={`space-y-4 ${className}`}>
      {showHeader && (
        <div className="flex space-x-4">
          {Array.from({ length: columns }).map((_, colIndex) => (
            <Skeleton
              key={`header-${colIndex}`}
              height="24px"
              width={colIndex === 0 ? '200px' : '120px'}
              variant="text"
            />
          ))}
        </div>
      )}
      
      <div className="space-y-3">
        {Array.from({ length: rows }).map((_, rowIndex) => (
          <div key={`row-${rowIndex}`} className="flex space-x-4">
            {Array.from({ length: columns }).map((_, colIndex) => (
              <Skeleton
                key={`cell-${rowIndex}-${colIndex}`}
                height="20px"
                width={
                  colIndex === 0 ? '180px' : 
                  colIndex === 1 ? '100px' :
                  colIndex === 2 ? '80px' :
                  '60px'
                }
                variant="text"
              />
            ))}
          </div>
        ))}
      </div>
    </div>
  );
};

interface SkeletonCardProps {
  showTitle?: boolean;
  showSubtitle?: boolean;
  showContent?: boolean;
  lines?: number;
  className?: string;
}

export const SkeletonCard: React.FC<SkeletonCardProps> = ({
  showTitle = true,
  showSubtitle = true,
  showContent = true,
  lines = 3,
  className = ''
}) => {
  return (
    <div className={`p-6 border border-gray-200 dark:border-gray-700 rounded-lg ${className}`}>
      {showTitle && (
        <Skeleton height="28px" width="60%" className="mb-4" variant="text" />
      )}
      
      {showSubtitle && (
        <Skeleton height="20px" width="40%" className="mb-6" variant="text" />
      )}
      
      {showContent && (
        <div className="space-y-3">
          {Array.from({ length: lines }).map((_, index) => (
            <Skeleton
              key={index}
              height="16px"
              width={index === lines - 1 ? '70%' : '100%'}
              variant="text"
            />
          ))}
        </div>
      )}
    </div>
  );
};

interface ProgressBarProps {
  progress: number; // 0-100
  showLabel?: boolean;
  label?: string;
  className?: string;
  size?: 'sm' | 'md' | 'lg';
}

export const ProgressBar: React.FC<ProgressBarProps> = ({
  progress,
  showLabel = true,
  label,
  className = '',
  size = 'md'
}) => {
  const sizeClass = {
    sm: 'h-2',
    md: 'h-3',
    lg: 'h-4'
  }[size];

  const clampedProgress = Math.min(100, Math.max(0, progress));

  return (
    <div className={`${className}`}>
      {showLabel && (
        <div className="flex justify-between text-sm text-gray-600 dark:text-gray-400 mb-1">
          <span>{label || 'Loading...'}</span>
          <span>{Math.round(clampedProgress)}%</span>
        </div>
      )}
      <div className={`w-full bg-gray-200 dark:bg-gray-700 rounded-full ${sizeClass}`}>
        <div
          className={`bg-blue-600 dark:bg-blue-500 ${sizeClass} rounded-full transition-all duration-300 ease-out`}
          style={{ width: `${clampedProgress}%` }}
        />
      </div>
    </div>
  );
};

interface DataLoadingStateProps {
  isLoading: boolean;
  hasData: boolean;
  progress?: { loaded: number; total: number; percentage: number } | null;
  children: React.ReactNode;
  fallback?: React.ReactNode;
  className?: string;
}

export const DataLoadingState: React.FC<DataLoadingStateProps> = ({
  isLoading,
  hasData,
  progress,
  children,
  fallback,
  className = ''
}) => {
  if (!isLoading && hasData) {
    return <>{children}</>;
  }

  if (!isLoading && !hasData) {
    return (
      <div className={`text-center py-12 ${className}`}>
        <div className="text-gray-500 dark:text-gray-400">
          No data available
        </div>
      </div>
    );
  }

  return (
    <div className={`${className}`}>
      {progress && (
        <div className="mb-6">
          <ProgressBar
            progress={progress.percentage}
            label={`Loading data (${progress.loaded.toLocaleString()} of ${progress.total.toLocaleString()} records)`}
            size="md"
          />
        </div>
      )}
      
      {fallback || (
        <div className="space-y-6">
          <SkeletonTable rows={8} columns={6} />
        </div>
      )}
      
      {hasData && (
        <div className="mt-6 opacity-75">
          {children}
        </div>
      )}
    </div>
  );
};