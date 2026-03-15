"use client"

import * as React from "react"
import { Slot } from "@radix-ui/react-slot"
import { cva, type VariantProps } from "class-variance-authority"
import { cn } from "@/lib/utils"

const buttonVariants = cva(
  "inline-flex min-h-[var(--control-height)] items-center justify-center gap-2.5 whitespace-nowrap rounded-[calc(var(--radius-md)-2px)] px-[var(--control-padding-x)] text-sm font-semibold tracking-[-0.01em] transition-all duration-200 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:pointer-events-none disabled:opacity-50 [&_svg]:pointer-events-none [&_svg]:size-4 [&_svg]:shrink-0",
  {
    variants: {
      variant: {
        default:
          "bg-primary text-primary-foreground shadow-[0_10px_24px_hsl(var(--primary)/0.18)] hover:bg-primary/92 hover:shadow-[0_14px_28px_hsl(var(--primary)/0.2)] active:scale-[0.99]",
        destructive:
          "bg-destructive text-destructive-foreground shadow-[0_10px_24px_hsl(var(--destructive)/0.18)] hover:bg-destructive/92 hover:shadow-[0_14px_28px_hsl(var(--destructive)/0.2)] active:scale-[0.99]",
        outline:
          "border border-input bg-background text-foreground shadow-none hover:border-primary/25 hover:bg-muted hover:text-foreground",
        secondary:
          "bg-secondary text-secondary-foreground shadow-[0_10px_22px_hsl(var(--secondary)/0.14)] hover:bg-secondary/90",
        ghost: "hover:bg-accent/10 hover:text-accent-foreground",
        link: "text-primary underline-offset-4 hover:underline",
        success:
          "bg-success text-success-foreground shadow-[0_10px_24px_hsl(var(--success)/0.18)] hover:bg-success/92 hover:shadow-[0_14px_28px_hsl(var(--success)/0.2)] active:scale-[0.99]",
        warning:
          "bg-warning text-warning-foreground shadow-[0_10px_24px_hsl(var(--warning)/0.18)] hover:bg-warning/92 hover:shadow-[0_14px_28px_hsl(var(--warning)/0.2)] active:scale-[0.99]",
      },
      size: {
        default: "",
        sm: "min-h-9 rounded-[var(--radius-sm)] px-3 text-xs",
        lg: "min-h-[var(--control-height-lg)] rounded-[var(--radius-lg)] px-8",
        xl: "min-h-[calc(var(--control-height-lg)+0.5rem)] rounded-[var(--radius-xl)] px-10 text-base",
        icon: "h-[var(--control-height)] w-[var(--control-height)] px-0",
      },
    },
    defaultVariants: {
      variant: "default",
      size: "default",
    },
  }
)

export interface ButtonProps
  extends React.ButtonHTMLAttributes<HTMLButtonElement>,
    VariantProps<typeof buttonVariants> {
  asChild?: boolean
}

const Button = React.forwardRef<HTMLButtonElement, ButtonProps>(
  ({ className, variant, size, asChild = false, ...props }, ref) => {
    const Comp = asChild ? Slot : "button"
    return (
      <Comp
        className={cn(buttonVariants({ variant, size, className }))}
        ref={ref}
        {...props}
      />
    )
  }
)
Button.displayName = "Button"

export { Button, buttonVariants }
