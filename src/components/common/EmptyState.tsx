import { Inbox } from "lucide-react";
import type { ReactNode } from "react";

import { cn } from "@/lib/utils";

/** Reusable "nothing here yet" state for lists and dashboards. */
export function EmptyState({
  title = "Nothing to show yet",
  description,
  action,
  className,
}: {
  title?: string | undefined;
  description?: string | undefined;
  action?: ReactNode | undefined;
  className?: string | undefined;
}) {
  return (
    <div
      className={cn(
        "flex flex-col items-center justify-center gap-2 rounded-lg border border-dashed border-border bg-card px-6 py-14 text-center",
        className,
      )}
    >
      <Inbox aria-hidden="true" className="size-6 text-muted-foreground" />
      <h3 className="text-sm font-medium text-foreground">{title}</h3>
      {description ? <p className="max-w-sm text-sm text-muted-foreground">{description}</p> : null}
      {action ? <div className="mt-2">{action}</div> : null}
    </div>
  );
}
