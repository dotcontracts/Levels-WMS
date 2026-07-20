import React, { useEffect, useRef, useState } from 'react';
import { 
  Filter, 
  X, 
  ChevronDown, 
  ChevronUp
} from 'lucide-react';
import { clsx } from 'clsx';
import Button from './Button';
import Input from './Input';
import Select from './Select';
import Card from './Card';

export interface FilterOption {
  value: string;
  label: string;
  disabled?: boolean;
}

export interface FilterField {
  key: string;
  label: string;
  type: 'text' | 'select' | 'date' | 'dateRange' | 'number' | 'numberRange';
  placeholder?: string;
  options?: FilterOption[];
  icon?: React.ReactNode;
  disabled?: boolean;
}

export interface FilterState {
  [key: string]: string | { from: string; to: string };
}

export interface FilterChip {
  key: string;
  label: string;
  value: string;
  onRemove: () => void;
  locked?: boolean;
}

interface FilterPanelProps {
  fields: FilterField[];
  filters: FilterState;
  onFilterChange: (key: string, value: string | { from: string; to: string }) => void;
  onClearFilters: () => void;
  activeFilters: FilterChip[];
  className?: string;
  collapsible?: boolean;
  defaultExpanded?: boolean;
}

const FilterPanel: React.FC<FilterPanelProps> = ({
  fields,
  filters,
  onFilterChange,
  onClearFilters,
  activeFilters,
  className,
  collapsible = true,
  defaultExpanded = false
}) => {
  const [isExpanded, setIsExpanded] = useState(defaultExpanded);
  const [showAdvanced, setShowAdvanced] = useState(false);
  const [searchDraft, setSearchDraft] = useState((filters.search as string) || '');
  const searchDebounceRef = useRef<number | null>(null);

  useEffect(() => {
    setSearchDraft((filters.search as string) || '');
  }, [filters.search]);

  useEffect(() => {
    return () => {
      if (searchDebounceRef.current !== null) {
        window.clearTimeout(searchDebounceRef.current);
      }
    };
  }, []);

  const basicFields = fields.slice(0, 3);
  const advancedFields = fields.slice(3);

  const renderField = (field: FilterField) => {
    const value = filters[field.key] || '';

    switch (field.type) {
      case 'text':
        if (field.key === 'search') {
          return (
            <Input
              key={field.key}
              label={field.label}
              placeholder={field.placeholder}
              value={searchDraft}
              onChange={(e: React.ChangeEvent<HTMLInputElement>) => {
                const nextValue = e.target.value;
                setSearchDraft(nextValue);
                if (searchDebounceRef.current !== null) {
                  window.clearTimeout(searchDebounceRef.current);
                }
                searchDebounceRef.current = window.setTimeout(() => {
                  onFilterChange(field.key, nextValue);
                }, 400);
              }}
              icon={field.icon}
              size="sm"
            />
          );
        }
        return (
          <Input
            key={field.key}
            label={field.label}
            placeholder={field.placeholder}
            value={value as string}
            onChange={(e: React.ChangeEvent<HTMLInputElement>) => onFilterChange(field.key, e.target.value)}
            icon={field.icon}
            size="sm"
          />
        );

      case 'select':
        return (
          <Select
            key={field.key}
            label={field.label}
            placeholder={field.placeholder}
            value={value as string}
            onChange={(valueOrEvent: any) => {
              if (field.disabled) return;
              // Handle both direct value and event object
              const actualValue = typeof valueOrEvent === 'string' 
                ? valueOrEvent 
                : valueOrEvent?.target?.value || '';
              onFilterChange(field.key, actualValue);
            }}
            options={field.options || []}
            size="sm"
            disabled={field.disabled}
          />
        );

      case 'date':
        return (
          <div key={field.key}>
            <label className="block text-sm font-medium text-gray-700 mb-1 dark:text-gray-300">
              {field.label}
            </label>
            <div className="relative">
              {field.icon && (
                <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                  <span className="text-gray-400 dark:text-gray-500">{field.icon}</span>
                </div>
              )}
              <input
                type="date"
                value={value as string}
                onChange={(e) => onFilterChange(field.key, e.target.value)}
                className={clsx(
                  'block w-full px-3 py-2 text-sm border border-gray-200 rounded-lg',
                  'focus:outline-none focus:ring-4 focus:ring-brand-100 focus:border-brand-500',
                  'transition-all duration-200 dark:bg-gray-800 dark:border-gray-700 dark:text-white',
                  field.icon && 'pl-10'
                )}
              />
            </div>
          </div>
        );

      case 'dateRange':
        const dateRangeValue = value as { from: string; to: string } || { from: '', to: '' };
        return (
          <div key={field.key} className="space-y-2">
            <label className="block text-sm font-medium text-gray-700 dark:text-gray-300">
              {field.label}
            </label>
            <div className="grid grid-cols-2 gap-2">
              <div className="relative">
                <input
                  type="date"
                  placeholder="From"
                  value={dateRangeValue.from}
                  onChange={(e) => onFilterChange(field.key, { ...dateRangeValue, from: e.target.value })}
                  className="block w-full px-3 py-2 text-sm border border-gray-200 rounded-lg focus:outline-none focus:ring-4 focus:ring-brand-100 focus:border-brand-500 transition-all duration-200 dark:bg-gray-800 dark:border-gray-700 dark:text-white"
                />
              </div>
              <div className="relative">
                <input
                  type="date"
                  placeholder="To"
                  value={dateRangeValue.to}
                  onChange={(e) => onFilterChange(field.key, { ...dateRangeValue, to: e.target.value })}
                  className="block w-full px-3 py-2 text-sm border border-gray-200 rounded-lg focus:outline-none focus:ring-4 focus:ring-brand-100 focus:border-brand-500 transition-all duration-200 dark:bg-gray-800 dark:border-gray-700 dark:text-white"
                />
              </div>
            </div>
          </div>
        );

      case 'number':
        return (
          <div key={field.key}>
            <label className="block text-sm font-medium text-gray-700 mb-1 dark:text-gray-300">
              {field.label}
            </label>
            <div className="relative">
              {field.icon && (
                <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                  <span className="text-gray-400 dark:text-gray-500">{field.icon}</span>
                </div>
              )}
              <input
                type="number"
                placeholder={field.placeholder}
                value={value as string}
                onChange={(e) => onFilterChange(field.key, e.target.value)}
                className={clsx(
                  'block w-full px-3 py-2 text-sm border border-gray-200 rounded-lg',
                  'focus:outline-none focus:ring-4 focus:ring-brand-100 focus:border-brand-500',
                  'transition-all duration-200 dark:bg-gray-800 dark:border-gray-700 dark:text-white',
                  field.icon && 'pl-10'
                )}
              />
            </div>
          </div>
        );

      case 'numberRange':
        const numberRangeValue = value as { from: string; to: string } || { from: '', to: '' };
        return (
          <div key={field.key} className="space-y-2">
            <label className="block text-sm font-medium text-gray-700 dark:text-gray-300">
              {field.label}
            </label>
            <div className="grid grid-cols-2 gap-2">
              <div className="relative">
                <input
                  type="number"
                  placeholder="Min"
                  value={numberRangeValue.from}
                  onChange={(e) => onFilterChange(field.key, { ...numberRangeValue, from: e.target.value })}
                  className="block w-full px-3 py-2 text-sm border border-gray-200 rounded-lg focus:outline-none focus:ring-4 focus:ring-brand-100 focus:border-brand-500 transition-all duration-200 dark:bg-gray-800 dark:border-gray-700 dark:text-white"
                />
              </div>
              <div className="relative">
                <input
                  type="number"
                  placeholder="Max"
                  value={numberRangeValue.to}
                  onChange={(e) => onFilterChange(field.key, { ...numberRangeValue, to: e.target.value })}
                  className="block w-full px-3 py-2 text-sm border border-gray-200 rounded-lg focus:outline-none focus:ring-4 focus:ring-brand-100 focus:border-brand-500 transition-all duration-200 dark:bg-gray-800 dark:border-gray-700 dark:text-white"
                />
              </div>
            </div>
          </div>
        );

      default:
        return null;
    }
  };

  const hasActiveFilters = activeFilters.length > 0;

  return (
    <Card className={clsx('space-y-4', className)}>
      {/* Filter Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <Filter className="w-5 h-5 text-gray-600 dark:text-gray-400" />
          <h3 className="text-lg font-semibold text-gray-900 dark:text-white">
            Filters
          </h3>
          {hasActiveFilters && (
            <span className="px-2 py-1 text-xs font-medium bg-brand-100 text-brand-800 rounded-full dark:bg-brand-500/20 dark:text-brand-400">
              {activeFilters.length} active
            </span>
          )}
        </div>
        
        <div className="flex items-center gap-2">
          {hasActiveFilters && (
            <Button
              variant="outline"
              size="sm"
              onClick={onClearFilters}
              className="text-gray-600 hover:text-gray-800"
            >
              <X className="w-4 h-4 mr-1" />
              Clear All
            </Button>
          )}
          
          {collapsible && (
            <Button
              variant="outline"
              size="sm"
              onClick={() => setIsExpanded(!isExpanded)}
            >
              {isExpanded ? (
                <>
                  <ChevronUp className="w-4 h-4 mr-1" />
                  Hide
                </>
              ) : (
                <>
                  <ChevronDown className="w-4 h-4 mr-1" />
                  Show
                </>
              )}
            </Button>
          )}
        </div>
      </div>

      {/* Active Filter Chips */}
      {hasActiveFilters && (
        <div className="flex flex-wrap gap-2">
          {activeFilters.map((chip) => (
            <div
              key={chip.key}
              className={clsx(
                'inline-flex items-center gap-1 px-3 py-1 rounded-full text-sm',
                chip.locked
                  ? 'bg-gray-100 text-gray-700 dark:bg-gray-700 dark:text-gray-300'
                  : 'bg-brand-100 text-brand-800 dark:bg-brand-500/20 dark:text-brand-400'
              )}
            >
              <span className="font-medium">{chip.label}:</span>
              <span>{chip.value}</span>
              {!chip.locked && (
                <button
                  onClick={chip.onRemove}
                  className="ml-1 hover:bg-brand-200 rounded-full p-0.5 dark:hover:bg-brand-500/30"
                >
                  <X className="w-3 h-3" />
                </button>
              )}
            </div>
          ))}
        </div>
      )}

      {/* Filter Fields */}
      {(isExpanded || !collapsible) && (
        <div className="space-y-4">
          {/* Basic Filters */}
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            {basicFields.map(renderField)}
          </div>

          {/* Advanced Filters */}
          {advancedFields.length > 0 && (
            <>
              <div className="border-t border-gray-200 dark:border-gray-700 pt-4">
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setShowAdvanced(!showAdvanced)}
                  className="mb-4"
                >
                  {showAdvanced ? (
                    <>
                      <ChevronUp className="w-4 h-4 mr-1" />
                      Hide Advanced Filters
                    </>
                  ) : (
                    <>
                      <ChevronDown className="w-4 h-4 mr-1" />
                      Show Advanced Filters ({advancedFields.length})
                    </>
                  )}
                </Button>

                {showAdvanced && (
                  <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                    {advancedFields.map(renderField)}
                  </div>
                )}
              </div>
            </>
          )}
        </div>
      )}
    </Card>
  );
};

export default FilterPanel;