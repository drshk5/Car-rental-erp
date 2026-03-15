"use client"

import * as React from "react"
import { cn } from "@/lib/utils"

export interface TextareaProps
  extends React.TextareaHTMLAttributes<HTMLTextAreaElement> {}

const Textarea = React.forwardRef<HTMLTextAreaElement, TextareaProps>(
  ({ className, ...props }, ref) => {
    return (
      <textarea
        className={cn(
          "flex min-h-[120px] w-full min-w-0 rounded-[var(--radius-md)] border border-input/90 bg-card px-[var(--control-padding-x)] py-3 text-[0.98rem] text-foreground shadow-[inset_0_1px_0_hsl(0_0%_100%/0.28),0_1px_2px_hsl(var(--foreground)/0.04)] ring-offset-background placeholder:text-muted-foreground/90 focus-visible:border-ring focus-visible:bg-background focus-visible:outline-none focus-visible:ring-4 focus-visible:ring-ring/15 disabled:cursor-not-allowed disabled:border-border/70 disabled:bg-muted/20 disabled:text-muted-foreground disabled:opacity-100 transition-all duration-200",
          className
        )}
        ref={ref}
        {...props}
      />
    )
  }
)
Textarea.displayName = "Textarea"

export { Textarea }
