import { type ClassValue, clsx } from 'clsx';
import { twMerge } from 'tailwind-merge';

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}

/**
 * HTML encode a string for safe display in React components
 * This prevents XSS attacks and ensures proper rendering of special characters
 */
export function htmlEncode(text: string): string {
  if (!text) return '';
  
  return text
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#x27;')
    .replace(/\//g, '&#x2F;');
}

/**
 * HTML decode a string (reverse of htmlEncode)
 * This is used when we need to decode HTML entities back to their original characters
 */
export function htmlDecode(text: string): string {
  if (!text) return '';
  
  return text
    .replace(/&amp;/g, '&')
    .replace(/&lt;/g, '<')
    .replace(/&gt;/g, '>')
    .replace(/&quot;/g, '"')
    .replace(/&#x27;/g, "'")
    .replace(/&#x2F;/g, '/');
}

/**
 * Format a date/time string into a user-friendly relative time format
 * Examples: "Just now", "2m ago", "1h ago", "3d ago", "2w ago", "1mo ago", "2y ago"
 */
export function formatRelativeTime(dateString: string | Date): string {
  if (!dateString) return 'Unknown';
  
  const date = typeof dateString === 'string' ? new Date(dateString) : dateString;
  const now = new Date();
  const diffInSeconds = Math.floor((now.getTime() - date.getTime()) / 1000);
  
  // Handle future dates
  if (diffInSeconds < 0) {
    return 'Just now';
  }
  
  // Less than 1 minute
  if (diffInSeconds < 60) {
    return 'Just now';
  }
  
  // Less than 1 hour
  if (diffInSeconds < 3600) {
    const minutes = Math.floor(diffInSeconds / 60);
    return `${minutes}m ago`;
  }
  
  // Less than 1 day
  if (diffInSeconds < 86400) {
    const hours = Math.floor(diffInSeconds / 3600);
    return `${hours}h ago`;
  }
  
  // Less than 1 week
  if (diffInSeconds < 604800) {
    const days = Math.floor(diffInSeconds / 86400);
    return `${days}d ago`;
  }
  
  // Less than 1 month (30 days)
  if (diffInSeconds < 2592000) {
    const weeks = Math.floor(diffInSeconds / 604800);
    return `${weeks}w ago`;
  }
  
  // Less than 1 year
  if (diffInSeconds < 31536000) {
    const months = Math.floor(diffInSeconds / 2592000);
    return `${months}mo ago`;
  }
  
  // More than 1 year
  const years = Math.floor(diffInSeconds / 31536000);
  return `${years}y ago`;
}

/**
 * Format a date/time string into a compact absolute format
 * Examples: "Dec 15", "Dec 15, 2023", "Today 2:30 PM", "Yesterday 9:15 AM"
 */
export function formatCompactTime(dateString: string | Date): string {
  if (!dateString) return 'Unknown';
  
  const date = typeof dateString === 'string' ? new Date(dateString) : dateString;
  const now = new Date();
  const today = new Date(now.getFullYear(), now.getMonth(), now.getDate());
  const yesterday = new Date(today);
  yesterday.setDate(yesterday.getDate() - 1);
  
  const dateOnly = new Date(date.getFullYear(), date.getMonth(), date.getDate());
  
  // Today
  if (dateOnly.getTime() === today.getTime()) {
    return `Today ${date.toLocaleTimeString('en-US', { 
      hour: 'numeric', 
      minute: '2-digit',
      hour12: true 
    })}`;
  }
  
  // Yesterday
  if (dateOnly.getTime() === yesterday.getTime()) {
    return `Yesterday ${date.toLocaleTimeString('en-US', { 
      hour: 'numeric', 
      minute: '2-digit',
      hour12: true 
    })}`;
  }
  
  // This year - show month and day
  if (date.getFullYear() === now.getFullYear()) {
    return date.toLocaleDateString('en-US', { 
      month: 'short', 
      day: 'numeric' 
    });
  }
  
  // Previous years - show month, day, and year
  return date.toLocaleDateString('en-US', { 
    month: 'short', 
    day: 'numeric',
    year: 'numeric'
  });
}

/**
 * Format a date/time string into a full readable format
 * Examples: "December 15, 2023 at 2:30 PM", "Today at 9:15 AM"
 */
export function formatFullTime(dateString: string | Date): string {
  if (!dateString) return 'Unknown';
  
  const date = typeof dateString === 'string' ? new Date(dateString) : dateString;
  const now = new Date();
  const today = new Date(now.getFullYear(), now.getMonth(), now.getDate());
  const dateOnly = new Date(date.getFullYear(), date.getMonth(), date.getDate());
  
  // Today
  if (dateOnly.getTime() === today.getTime()) {
    return `Today at ${date.toLocaleTimeString('en-US', { 
      hour: 'numeric', 
      minute: '2-digit',
      hour12: true 
    })}`;
  }
  
  // This year
  if (date.getFullYear() === now.getFullYear()) {
    return date.toLocaleDateString('en-US', { 
      weekday: 'long',
      month: 'long', 
      day: 'numeric',
      hour: 'numeric',
      minute: '2-digit',
      hour12: true
    });
  }
  
  // Previous years
  return date.toLocaleDateString('en-US', { 
    weekday: 'long',
    month: 'long', 
    day: 'numeric',
    year: 'numeric',
    hour: 'numeric',
    minute: '2-digit',
    hour12: true
  });
}

/**
 * Get a human-readable time range between two dates
 * Examples: "2 hours", "3 days", "1 week", "2 months"
 */
export function formatTimeRange(startDate: string | Date, endDate: string | Date): string {
  if (!startDate || !endDate) return 'Unknown';
  
  const start = typeof startDate === 'string' ? new Date(startDate) : startDate;
  const end = typeof endDate === 'string' ? new Date(endDate) : endDate;
  const diffInSeconds = Math.floor((end.getTime() - start.getTime()) / 1000);
  
  if (diffInSeconds < 60) {
    return `${diffInSeconds}s`;
  }
  
  if (diffInSeconds < 3600) {
    const minutes = Math.floor(diffInSeconds / 60);
    return `${minutes}m`;
  }
  
  if (diffInSeconds < 86400) {
    const hours = Math.floor(diffInSeconds / 3600);
    return `${hours}h`;
  }
  
  if (diffInSeconds < 604800) {
    const days = Math.floor(diffInSeconds / 86400);
    return `${days}d`;
  }
  
  if (diffInSeconds < 2592000) {
    const weeks = Math.floor(diffInSeconds / 604800);
    return `${weeks}w`;
  }
  
  if (diffInSeconds < 31536000) {
    const months = Math.floor(diffInSeconds / 2592000);
    return `${months}mo`;
  }
  
  const years = Math.floor(diffInSeconds / 31536000);
  return `${years}y`;
}


