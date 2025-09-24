'use client';

import { ReactNode } from 'react';
import { cn } from '@/lib/utils';

interface CardProps {
  children: ReactNode;
  className?: string;
  hover?: boolean;
  clickable?: boolean;
  onClick?: () => void;
}

interface CardHeaderProps {
  children: ReactNode;
  className?: string;
}

interface CardTitleProps {
  children: ReactNode;
  className?: string;
}

interface CardDescriptionProps {
  children: ReactNode;
  className?: string;
}

interface CardContentProps {
  children: ReactNode;
  className?: string;
}

interface CardFooterProps {
  children: ReactNode;
  className?: string;
}

export function Card({ 
  children, 
  className = '', 
  hover = false, 
  clickable = false, 
  onClick 
}: CardProps) {
  return (
    <div
      className={cn(
        'card-surface',
        hover && 'card-surface-hover',
        clickable && 'cursor-pointer',
        clickable && 'hover:bg-slate-800/80',
        className
      )}
      onClick={onClick}
    >
      {children}
    </div>
  );
}

export function CardHeader({ children, className = '' }: CardHeaderProps) {
  return (
    <div className={cn('p-6 pb-4', className)}>
      {children}
    </div>
  );
}

export function CardTitle({ children, className = '' }: CardTitleProps) {
  return (
    <h3 className={cn(
      'text-lg font-semibold',
      'text-gray-900 dark:text-gray-100',
      'mb-2',
      className
    )}>
      {children}
    </h3>
  );
}

export function CardDescription({ children, className = '' }: CardDescriptionProps) {
  return (
    <p className={cn(
      'text-sm',
      'text-gray-600 dark:text-gray-300',
      'leading-relaxed',
      className
    )}>
      {children}
    </p>
  );
}

export function CardContent({ children, className = '' }: CardContentProps) {
  return (
    <div className={cn('p-6 pt-0', className)}>
      {children}
    </div>
  );
}

export function CardFooter({ children, className = '' }: CardFooterProps) {
  return (
    <div className={cn(
      'p-6 pt-4',
      'border-t border-gray-100 dark:border-gray-700',
      className
    )}>
      {children}
    </div>
  );
}

// Specialized card variants
export function NewsCard({ children, className = '', onClick }: CardProps) {
  return (
    <Card 
      className={cn('hover:shadow-lg', className)}
      hover
      clickable={!!onClick}
      onClick={onClick}
    >
      {children}
    </Card>
  );
}

export function ConceptCard({ children, className = '', onClick }: CardProps) {
  return (
    <Card 
      className={cn('hover:shadow-lg', className)}
      hover
      clickable={!!onClick}
      onClick={onClick}
    >
      {children}
    </Card>
  );
}

export function NodeCard({ children, className = '', onClick }: CardProps) {
  return (
    <Card 
      className={cn('hover:shadow-md', className)}
      hover
      clickable={!!onClick}
      onClick={onClick}
    >
      {children}
    </Card>
  );
}

export function EdgeCard({ children, className = '', onClick }: CardProps) {
  return (
    <Card 
      className={cn('hover:shadow-md', className)}
      hover
      clickable={!!onClick}
      onClick={onClick}
    >
      {children}
    </Card>
  );
}

export function AxisCard({ children, className = '', onClick }: CardProps) {
  return (
    <Card 
      className={cn('hover:shadow-md', className)}
      hover
      clickable={!!onClick}
      onClick={onClick}
    >
      {children}
    </Card>
  );
}

export function StatsCard({ children, className = '' }: CardProps) {
  return (
    <Card className={cn('hover:shadow-md', className)} hover>
      {children}
    </Card>
  );
}
