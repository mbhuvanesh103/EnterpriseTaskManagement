import type { ReactNode } from "react";

import { ValidationSummary } from "@/components/common/ValidationSummary";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { describeApiError } from "@/lib/api/errors";

/**
 * Reusable create/update modal used by every management page: accessible title
 * and description, a live-region error area for the API's response, field-level
 * validation from the RFC 7807 `errors` bag, and a submit button that reflects
 * the mutation's pending state.
 */
export function FormDialog({
  open,
  onOpenChange,
  title,
  description,
  submitLabel,
  onSubmit,
  isPending = false,
  error,
  children,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  title: string;
  description: string;
  submitLabel: string;
  onSubmit: () => void;
  isPending?: boolean;
  error?: unknown;
  children: ReactNode;
}) {
  const described = error ? describeApiError(error) : null;

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>{title}</DialogTitle>
          <DialogDescription>{description}</DialogDescription>
        </DialogHeader>

        <form
          className="flex flex-col gap-4"
          noValidate
          onSubmit={(event) => {
            event.preventDefault();
            onSubmit();
          }}
        >
          {described ? (
            <div
              role="alert"
              className="rounded-md border border-destructive/40 bg-destructive/5 px-4 py-3 text-sm"
            >
              <p className="font-medium text-foreground">{described.title}</p>
              <p className="mt-0.5 text-muted-foreground">{described.message}</p>
            </div>
          ) : null}

          <ValidationSummary fieldErrors={described?.fieldErrors} />

          <div className="grid gap-4">{children}</div>

          <DialogFooter>
            <Button
              type="button"
              variant="outline"
              onClick={() => onOpenChange(false)}
              disabled={isPending}
            >
              Cancel
            </Button>
            <Button type="submit" disabled={isPending}>
              {isPending ? "Saving…" : submitLabel}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
