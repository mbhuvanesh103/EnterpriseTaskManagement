import type { ReactNode } from "react";

import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";

/**
 * Shared list toolbar: a client-side search field over the rows already loaded
 * (the API exposes no search parameter, so nothing is faked) plus a slot for
 * page-specific filters and actions.
 */
export function ListToolbar({
  searchId = "list-search",
  searchLabel,
  search,
  onSearch,
  filters,
  actions,
}: {
  searchId?: string;
  searchLabel: string;
  search: string;
  onSearch: (value: string) => void;
  filters?: ReactNode | undefined;
  actions?: ReactNode | undefined;
}) {
  return (
    <div className="flex flex-wrap items-end justify-between gap-3">
      <div className="flex flex-wrap items-end gap-3">
        <div className="w-full max-w-xs">
          <Label htmlFor={searchId}>{searchLabel}</Label>
          <Input
            id={searchId}
            type="search"
            value={search}
            placeholder="Type to filter…"
            onChange={(event) => onSearch(event.target.value)}
            className="mt-1.5"
          />
        </div>
        {filters}
      </div>
      {actions ? <div className="flex flex-wrap items-center gap-2">{actions}</div> : null}
    </div>
  );
}

/** Matches text against several fields, case-insensitively. */
export function matchesSearch(search: string, ...fields: readonly (string | null | undefined)[]) {
  const term = search.trim().toLowerCase();
  if (!term) return true;
  return fields.some((field) => (field ?? "").toLowerCase().includes(term));
}
