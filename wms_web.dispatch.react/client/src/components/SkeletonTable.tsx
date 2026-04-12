import React from 'react';

interface SkeletonTableProps {
  rows?: number;
  columns?: number;
}

export const SkeletonTable: React.FC<SkeletonTableProps> = ({ 
  rows = 10, 
  columns = 6 
}) => {
  const skeletonRows = Array.from({ length: rows }, (_, index) => (
    <tr key={index} className="border-b border-gray-200">
      {Array.from({ length: columns }, (_, colIndex) => (
        <td 
          key={colIndex} 
          className="p-3 animate-pulse"
          style={{ 
            animationDelay: `${(index * columns + colIndex) * 50}ms`,
            animationDuration: '1000ms',
            animationIterationCount: 'infinite'
          }}
        >
          <div 
            className={`h-4 bg-gray-200 rounded ${
              colIndex === columns - 1 ? 'w-32' : 
              colIndex === 0 ? 'w-40' : 'w-24'
            }`}
          />
        </td>
      ))}
    </tr>
  ));

  return (
    <table className="w-full">
      <tbody>
        {skeletonRows}
      </tbody>
    </table>
  );
};