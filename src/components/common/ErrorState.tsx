import { AlertTriangle } from "lucide-react";

import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";

/** Reusable failure state with an optional retry affordance. */
export function ErrorState({
  title = "Something went wrong",
  description = "We couldn't load this information. Please try again.",
  onRetry,
  className,
}: {
  title?: string | undefined;
  description?: string | undefined;
  onRetry?: (() => void) | undefined;
  className?: string | undefined;
}) {
  return (
    <div
      role="alert"
      className={cn(
        "flex flex-col items-center justify-center gap-2 rounded-lg border border-destructive/40 bg-destructive/5 px-6 py-12 text-center",
        className,
      )}
    >
      <AlertTriangle aria-hidden="true" className="size-6 text-destructive" />
      <h3 className="text-sm font-medium text-foreground">{title}</h3>
      <p className="max-w-sm text-sm text-muted-foreground">{description}</p>
      {onRetry ? (
        <Button size="sm" variant="outline" className="mt-2" onClick={onRetry}>
          Try again
        </Button>
      ) : null}
    </div>
  );
}

/** Reusable "you don't have access to this" state. */
export function RestrictedState({
  description = "Your role doesn't include access to this information.",
  className,
}: {
  description?: string | undefined;
  className?: string | undefined;
}) {
  return (
    <div
      role="note"
      className={cn(
        "flex flex-col items-center justify-center gap-2 rounded-lg border border-border bg-muted/40 px-6 py-12 text-center",
        className,
      )}
    >
      <h3 className="text-sm font-medium text-foreground">Not available for your role</h3>
      <p className="max-w-sm text-sm text-muted-foreground">{description}</p>
    </div>
  );
}
