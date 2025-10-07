import * as React from "react"

import { cn } from "@/lib/utils"

const Card = React.forwardRef<
  HTMLDivElement,
  React.HTMLAttributes<HTMLDivElement>
>(({ className, ...props }, ref) => (
  <div
    ref={ref}
    className={cn(
      "rounded-lg border bg-card text-card-foreground shadow-sm",
      className
    )}
    {...props}
  />
))
Card.displayName = "Card"

const CardHeader = React.forwardRef<
  HTMLDivElement,
  React.HTMLAttributes<HTMLDivElement>
>(({ className, ...props }, ref) => (
  <div
    ref={ref}
    className={cn("flex flex-col space-y-1.5 p-6", className)}
    {...props}
  />
))
CardHeader.displayName = "CardHeader"

const CardTitle = React.forwardRef<
  HTMLParagraphElement,
  React.HTMLAttributes<HTMLHeadingElement>
>(({ className, ...props }, ref) => (
  <h3
    ref={ref}
    className={cn(
      "text-2xl font-semibold leading-none tracking-tight",
      className
    )}
    {...props}
  />
))
CardTitle.displayName = "CardTitle"

const CardDescription = React.forwardRef<
  HTMLParagraphElement,
  React.HTMLAttributes<HTMLParagraphElement>
>(({ className, ...props }, ref) => (
  <p
    ref={ref}
    className={cn("text-sm text-muted-foreground", className)}
    {...props}
  />
))
CardDescription.displayName = "CardDescription"

const CardContent = React.forwardRef<
  HTMLDivElement,
  React.HTMLAttributes<HTMLDivElement>
>(({ className, ...props }, ref) => (
  <div ref={ref} className={cn("p-6 pt-0", className)} {...props} />
))
CardContent.displayName = "CardContent"

const CardFooter = React.forwardRef<
  HTMLDivElement,
  React.HTMLAttributes<HTMLDivElement>
>(({ className, ...props }, ref) => (
  <div
    ref={ref}
    className={cn("flex items-center p-6 pt-0", className)}
    {...props}
  />
))
CardFooter.displayName = "CardFooter"

// Additional specialized card components
const StatsCard = React.forwardRef<
  HTMLDivElement,
  React.HTMLAttributes<HTMLDivElement> & {
    title?: string;
    value?: string | number;
    description?: string;
    icon?: React.ReactNode;
  }
>(({ className, title, value, description, icon, ...props }, ref) => (
  <Card
    ref={ref}
    className={cn("p-6", className)}
    {...props}
  >
    <div className="flex items-center justify-between">
      <div>
        <p className="text-sm font-medium text-muted-foreground">{title}</p>
        <p className="text-2xl font-bold">{value}</p>
        {description && (
          <p className="text-xs text-muted-foreground mt-1">{description}</p>
        )}
      </div>
      {icon && <div className="text-muted-foreground">{icon}</div>}
    </div>
  </Card>
))
StatsCard.displayName = "StatsCard"

const NodeCard = React.forwardRef<
  HTMLDivElement,
  React.HTMLAttributes<HTMLDivElement> & {
    nodeId?: string;
    nodeType?: string;
    title?: string;
    description?: string;
    metadata?: Record<string, unknown>;
  }
>(({ className, nodeId, nodeType, title, description, metadata, ...props }, ref) => (
  <Card
    ref={ref}
    className={cn("p-4", className)}
    {...props}
  >
    <CardHeader className="pb-2">
      <div className="flex items-center justify-between">
        <CardTitle className="text-lg">{title || nodeId}</CardTitle>
        {nodeType && (
          <span className="px-2 py-1 bg-secondary text-secondary-foreground rounded text-xs">
            {nodeType}
          </span>
        )}
      </div>
      {description && (
        <CardDescription className="text-sm">{description}</CardDescription>
      )}
    </CardHeader>
    {metadata && Object.keys(metadata).length > 0 && (
      <CardContent className="pt-0">
        <div className="space-y-1">
          {Object.entries(metadata).map(([key, value]) => (
            <div key={key} className="flex justify-between text-xs">
              <span className="text-muted-foreground">{key}:</span>
              <span>{String(value)}</span>
            </div>
          ))}
        </div>
      </CardContent>
    )}
  </Card>
))
NodeCard.displayName = "NodeCard"

export { Card, CardHeader, CardFooter, CardTitle, CardDescription, CardContent, StatsCard, NodeCard }