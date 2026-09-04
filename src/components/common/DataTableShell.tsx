import type { ReactNode } from "react";

import { LoadingRows } from "@/components/common/LoadingState";
import { EmptyState } from "@/components/common/EmptyState";
import { ErrorState } from "@/components/common/ErrorState";
import { Button } from "@/components/ui/button";
import { Table, TableBody, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { cn } from "@/lib/utils";

export type ListState = "loading" | "error" | "empty" | "ready" | "restricted";

/**
 * Reusable table shell for every management page: caption, column headers, and
 * the shared loading / empty / error branches in one place.
 */
export function DataTableShell({
  caption,
  columns,
  state,
  children,
  emptyTitle,
  emptyDescription,
  onRetry,
  className,
}: {
  caption: string;
  columns: readonly string[];
  state: ListState;
  children?: ReactNode;
  emptyTitle?: string;
  emptyDescription?: string;
  onRetry?: () => void;
  className?: string;
}) {
  if (state === "loading") return <LoadingRows columns={columns.length} />;
  if (state === "error") return <ErrorState onRetry={onRetry} />;
  if (state === "empty") return <EmptyState title={emptyTitle} description={emptyDescription} />;

  return (
    <div className={cn("w-full overflow-x-auto rounded-lg border border-border", className)}>
      <Table>
        <caption className="sr-only">{caption}</caption>
        <TableHeader>
          <TableRow>
            {columns.map((column) => (
              <TableHead key={column} scope="col">
                {column}
              </TableHead>
            ))}
          </TableRow>
        </TableHeader>
        <TableBody>{children}</TableBody>
      </Table>
    </div>
  );
}

/** Reusable page-size aware pagination bar matching the API's PagedResult. */
export function PaginationBar({
  page,
  pageSize,
  totalCount,
  onPageChange,
}: {
  page: number;
  pageSize: number;
  totalCount: number;
  onPageChange?: (page: number) => void;
}) {
  const totalPages = Math.max(1, Math.ceil(totalCount / Math.max(1, pageSize)));
  const from = totalCount === 0 ? 0 : (page - 1) * pageSize + 1;
  const to = Math.min(page * pageSize, totalCount);

  return (
    <nav
      aria-label="Pagination"
      className="flex flex-wrap items-center justify-between gap-3 text-sm text-muted-foreground"
    >
      <p aria-live="polite">
        Showing {from}–{to} of {totalCount}
      </p>
      <div className="flex items-center gap-2">
        <Button
          size="sm"
          variant="outline"
          disabled={page <= 1}
          onClick={() => onPageChange?.(page - 1)}
        >
          Previous
        </Button>
        <span>
          Page {page} of {totalPages}
        </span>
        <Button
          size="sm"
          variant="outline"
          disabled={page >= totalPages}
          onClick={() => onPageChange?.(page + 1)}
        >
          Next
        </Button>
      </div>
    </nav>
  );
}
