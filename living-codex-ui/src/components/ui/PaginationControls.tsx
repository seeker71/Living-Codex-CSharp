'use client';

import React from 'react';

interface PaginationControlsProps {
  currentPage: number;
  pageSize: number;
  totalCount: number;
  onPageChange: (page: number) => void;
}

export function PaginationControls({ currentPage, pageSize, totalCount, onPageChange }: PaginationControlsProps) {
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));
  const startPage = Math.max(1, Math.min(currentPage - 2, totalPages - 4));
  const pages = Array.from({ length: Math.min(5, totalPages) }, (_, i) => startPage + i).filter(p => p <= totalPages);

  return (
    <div className="flex items-center justify-between">
      <div className="text-sm text-gray-700 dark:text-gray-300">
        Showing {((currentPage - 1) * pageSize) + 1} to {Math.min(currentPage * pageSize, totalCount)} of {totalCount}
      </div>
      <div className="flex items-center space-x-2">
        <button
          onClick={() => onPageChange(Math.max(1, currentPage - 1))}
          disabled={currentPage === 1}
          className="px-3 py-1 text-sm border border-gray-300 dark:border-gray-600 rounded-md hover:bg-gray-50 dark:hover:bg-gray-700 disabled:opacity-50 disabled:cursor-not-allowed"
        >
          Previous
        </button>
        <div className="flex items-center space-x-1">
          {pages.map(page => (
            <button
              key={page}
              onClick={() => onPageChange(page)}
              className={`px-3 py-1 text-sm border rounded-md ${currentPage === page ? 'bg-blue-600 text-white border-blue-600' : 'border-gray-300 dark:border-gray-600 hover:bg-gray-50 dark:hover:bg-gray-700'}`}
            >
              {page}
            </button>
          ))}
        </div>
        <button
          onClick={() => onPageChange(Math.min(totalPages, currentPage + 1))}
          disabled={currentPage === totalPages}
          className="px-3 py-1 text-sm border border-gray-300 dark:border-gray-600 rounded-md hover:bg-gray-50 dark:hover:bg-gray-700 disabled:opacity-50 disabled:cursor-not-allowed"
        >
          Next
        </button>
      </div>
    </div>
  );
}




