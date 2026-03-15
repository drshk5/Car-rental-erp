"use client"

import * as React from "react"
import { Slot } from "@radix-ui/react-slot"
import { cva, type VariantProps } from "class-variance-authority"
import { cn } from "@/lib/utils"

const buttonVariants = cva(
  "inline-flex min-h-[var(--control-height)] items-center justify-center gap-2 whitespace-nowrap rounded-[var(--radius-md)] px-[var(--control-padding-x)] text-sm font-semibold transition-all duration-200 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:pointer-events-none disabled:opacity-50 [&_svg]:pointer-events-none [&_svg]:size-4 [&_svg]:shrink-0",
  {
    variants: {
      variant: {
        default:
          "bg-primary text-primary-foreground shadow-[0_18px_34px_hsl(var(--primary)/0.22)] hover:bg-primary/90 hover:shadow-[0_22px_40px_hsl(var(--primary)/0.26)] active:scale-[0.98]",
        destructive:
          "bg-destructive text-destructive-foreground shadow-[0_18px_34px_hsl(var(--destructive)/0.2)] hover:bg-destructive/90 hover:shadow-[0_22px_40px_hsl(var(--destructive)/0.24)] active:scale-[0.98]",
        outline:
          "border border-input bg-background/80 shadow-soft hover:border-primary/35 hover:bg-accent hover:text-accent-foreground",
        secondary:
          "bg-secondary text-secondary-foreground shadow-[0_16px_30px_hsl(var(--secondary)/0.18)] hover:bg-secondary/85",
        ghost: "hover:bg-accent/15 hover:text-accent-foreground",
        link: "text-primary underline-offset-4 hover:underline",
        success:
          "bg-success text-success-foreground shadow-[0_18px_34px_hsl(var(--success)/0.22)] hover:bg-success/90 hover:shadow-[0_22px_40px_hsl(var(--success)/0.28)] active:scale-[0.98]",
        warning:
          "bg-warning text-warning-foreground shadow-[0_18px_34px_hsl(var(--warning)/0.22)] hover:bg-warning/90 hover:shadow-[0_22px_40px_hsl(var(--warning)/0.28)] active:scale-[0.98]",
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
