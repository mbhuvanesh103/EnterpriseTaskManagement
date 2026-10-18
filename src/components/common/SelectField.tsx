import { FieldError } from "@/components/common/ValidationSummary";
import { Label } from "@/components/ui/label";
import { cn } from "@/lib/utils";

export interface SelectOption {
  value: string;
  label: string;
}

/**
 * Accessible select built on the native control, so it works with the keyboard,
 * screen readers and mobile pickers without extra wiring.
 *
 * `RelationSelect` adds the states a relationship picker needs when its options
 * come from a real API call: loading, empty, failed and not-permitted. It never
 * invents placeholder options — an unavailable list says so instead.
 */
export function SelectField({
  id,
  label,
  value,
  onChange,
  options,
  placeholder,
  required = false,
  disabled = false,
  error,
  hint,
}: {
  id: string;
  label: string;
  value: string;
  onChange: (value: string) => void;
  options: readonly SelectOption[];
  placeholder?: string | undefined;
  required?: boolean;
  disabled?: boolean;
  error?: string | undefined;
  hint?: string | undefined;
}) {
  const describedBy = [error ? `${id}-error` : null, hint ? `${id}-hint` : null]
    .filter(Boolean)
    .join(" ");

  return (
    <div>
      <Label htmlFor={id}>
        {label}
        {required ? <span aria-hidden="true"> *</span> : null}
      </Label>
      <select
        id={id}
        value={value}
        required={required}
        disabled={disabled}
        aria-invalid={error ? true : undefined}
        aria-describedby={describedBy || undefined}
        onChange={(event) => onChange(event.target.value)}
        className={cn(
          "mt-1.5 h-9 w-full rounded-md border border-input bg-background px-3 text-sm shadow-xs",
          "focus-visible:ring-[3px] focus-visible:ring-ring/50 focus-visible:outline-none",
          "disabled:cursor-not-allowed disabled:opacity-50",
          error && "border-destructive",
        )}
      >
        {placeholder ? <option value="">{placeholder}</option> : null}
        {options.map((option) => (
          <option key={option.value} value={option.value}>
            {option.label}
          </option>
        ))}
      </select>
      {hint ? (
        <p id={`${id}-hint`} className="mt-1 text-xs text-muted-foreground">
          {hint}
        </p>
      ) : null}
      <FieldError id={`${id}-error`} message={error} />
    </div>
  );
}

export function RelationSelect({
  id,
  label,
  value,
  onChange,
  options,
  isLoading,
  isError,
  isForbidden = false,
  permitted = true,
  placeholder = "Select…",
  emptyMessage = "Nothing to choose from yet.",
  required = false,
  disabled = false,
  error,
  onRetry,
}: {
  id: string;
  label: string;
  value: string;
  onChange: (value: string) => void;
  options: readonly SelectOption[];
  isLoading: boolean;
  isError: boolean;
  isForbidden?: boolean;
  /** False when the caller's role may not read this list at all. */
  permitted?: boolean;
  placeholder?: string;
  emptyMessage?: string;
  required?: boolean;
  disabled?: boolean;
  error?: string | undefined;
  onRetry?: (() => void) | undefined;
}) {
  if (!permitted || isForbidden) {
    return (
      <Note label={label}>
        You don&apos;t have access to this list, so it can&apos;t be changed here.
      </Note>
    );
  }

  if (isLoading) {
    return (
      <div>
        <Label htmlFor={id}>{label}</Label>
        <div
          role="status"
          aria-live="polite"
          className="mt-1.5 h-9 animate-pulse rounded-md border border-input bg-muted/50"
        >
          <span className="sr-only">Loading {label.toLowerCase()}…</span>
        </div>
      </div>
    );
  }

  if (isError) {
    return (
      <div>
        <Label htmlFor={id}>{label}</Label>
        <div
          role="alert"
          className="mt-1.5 flex items-center justify-between gap-2 rounded-md border border-destructive/40 bg-destructive/5 px-3 py-2 text-sm"
        >
          <span className="text-muted-foreground">We couldn&apos;t load this list.</span>
          {onRetry ? (
            <button
              type="button"
              onClick={onRetry}
              className="font-medium text-foreground underline"
            >
              Try again
            </button>
          ) : null}
        </div>
      </div>
    );
  }

  if (options.length === 0) {
    return <Note label={label}>{emptyMessage}</Note>;
  }

  return (
    <SelectField
      id={id}
      label={label}
      value={value}
      onChange={onChange}
      options={options}
      placeholder={placeholder}
      required={required}
      disabled={disabled}
      error={error}
    />
  );
}

function Note({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div>
      <span className="text-sm font-medium">{label}</span>
      <p
        role="note"
        className="mt-1.5 rounded-md border border-border bg-muted/40 px-3 py-2 text-sm text-muted-foreground"
      >
        {children}
      </p>
    </div>
  );
}
