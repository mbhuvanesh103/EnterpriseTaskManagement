import { Loader2 } from "lucide-react";

import { Skeleton } from "@/components/ui/skeleton";
import { cn } from "@/lib/utils";

/** Reusable spinner-style loading state, announced to assistive tech. */
export function LoadingState({
  label = "Loading",
  className,
}: {
  label?: string | undefined;
  className?: string | undefined;
}) {
  return (
    <div
      role="status"
      aria-live="polite"
      className={cn(
        "flex flex-col items-center justify-center gap-3 rounded-lg border border-border bg-card px-6 py-14 text-center",
        className,
      )}
    >
      <Loader2 aria-hidden="true" className="size-6 animate-spin text-muted-foreground" />
      <p className="text-sm text-muted-foreground">{label}…</p>
    </div>
  );
}

/** Skeleton placeholder for card grids. */
export function LoadingCards({ count = 4 }: { count?: number }) {
  return (
    <div
      role="status"
      aria-live="polite"
      aria-label="Loading"
      className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4"
    >
      {Array.from({ length: count }, (_, index) => (
        <Skeleton key={index} className="h-28 w-full rounded-lg" />
      ))}
    </div>
  );
}

/** Skeleton placeholder for table shells. */
export function LoadingRows({ rows = 5, columns = 4 }: { rows?: number; columns?: number }) {
  return (
    <div role="status" aria-live="polite" aria-label="Loading" className="space-y-2">
      {Array.from({ length: rows }, (_, rowIndex) => (
        <div key={rowIndex} className="flex gap-3">
          {Array.from({ length: columns }, (_, columnIndex) => (
            <Skeleton key={columnIndex} className="h-10 flex-1 rounded-md" />
          ))}
        </div>
      ))}
    </div>
  );
}
