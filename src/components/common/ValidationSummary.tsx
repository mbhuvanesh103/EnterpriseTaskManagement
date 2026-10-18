import { cn } from "@/lib/utils";

/**
 * Renders the field-level validation errors returned by the API's RFC 7807
 * ProblemDetails payload (`errors: Record<string, string[]>`), plus a single
 * inline field error helper.
 */
export function ValidationSummary({
  fieldErrors,
  className,
}: {
  fieldErrors?: Record<string, string[]> | undefined;
  className?: string | undefined;
}) {
  const entries = Object.entries(fieldErrors ?? {});
  if (entries.length === 0) return null;

  return (
    <div
      role="alert"
      className={cn(
        "rounded-md border border-destructive/40 bg-destructive/5 px-4 py-3 text-sm",
        className,
      )}
    >
      <p className="font-medium text-foreground">Please fix the following:</p>
      <ul className="mt-1 list-disc space-y-0.5 pl-5 text-muted-foreground">
        {entries.map(([field, messages]) =>
          messages.map((message) => <li key={`${field}-${message}`}>{message}</li>),
        )}
      </ul>
    </div>
  );
}

export function FieldError({
  id,
  message,
}: {
  id?: string | undefined;
  message?: string | undefined;
}) {
  if (!message) return null;
  return (
    <p id={id} className="mt-1 text-sm text-destructive">
      {message}
    </p>
  );
}
