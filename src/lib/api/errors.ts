import { ApiError } from "@/lib/api/client";

/**
 * Single place that turns a normalized {@link ApiError} into user-facing copy.
 * Every feature module and every page uses this, so the reaction to each HTTP
 * status is consistent across the app:
 *
 * - 400 — the request shape or a domain invariant was rejected. Field-level
 *   details from the RFC 7807 `errors` bag are surfaced next to the inputs.
 * - 401 — handled centrally by the API client (session cleared, redirect to
 *   /login). Nothing is retried here.
 * - 403 — Access denied. Never a login redirect and never a retry: retrying
 *   cannot change the answer.
 * - 404 — the record is no longer available (it may have been soft-deleted).
 * - 409 — a duplicate value, or the row changed concurrently. The user is asked
 *   to refresh and try again; nothing is ever silently overwritten.
 * - 5xx / network — a safe generic message, retryable.
 */
export interface DescribedError {
  title: string;
  message: string;
  fieldErrors: Record<string, string[]> | undefined;
  /** Whether offering a "Try again" affordance makes sense. */
  canRetry: boolean;
  /** 409 only: the user must reload the latest version before retrying. */
  requiresRefresh: boolean;
  isForbidden: boolean;
  isNotFound: boolean;
}

const GENERIC: DescribedError = {
  title: "Something went wrong",
  message: "We couldn't complete that just now. Please try again.",
  fieldErrors: undefined,
  canRetry: true,
  requiresRefresh: false,
  isForbidden: false,
  isNotFound: false,
};

export function describeApiError(error: unknown): DescribedError {
  if (!(error instanceof ApiError)) return GENERIC;

  const base = {
    fieldErrors: error.fieldErrors,
    requiresRefresh: false,
    isForbidden: false,
    isNotFound: false,
  };

  switch (error.status) {
    case 400:
      return {
        ...base,
        title: "Please check the details",
        message: error.message || "Some of the information provided isn't valid.",
        canRetry: false,
      };
    case 401:
      return {
        ...base,
        title: "Your session has ended",
        message: "Please sign in again to continue.",
        canRetry: false,
      };
    case 403:
      return {
        ...base,
        title: "Access denied",
        message: "Your role doesn't allow this action.",
        canRetry: false,
        isForbidden: true,
      };
    case 404:
      return {
        ...base,
        title: "No longer available",
        message: "This record can't be found any more. It may have been removed.",
        canRetry: false,
        isNotFound: true,
      };
    case 409:
      return {
        ...base,
        title: "This record changed elsewhere",
        message:
          error.message ||
          "Someone else changed this record, or the value is already in use. Refresh to load the latest version, then try again.",
        canRetry: true,
        requiresRefresh: true,
      };
    default:
      if (typeof error.status === "number" && error.status >= 500) return { ...GENERIC, ...base };
      return {
        ...base,
        title: "Something went wrong",
        message: error.message || GENERIC.message,
        canRetry: true,
      };
  }
}

/** Short single-line form, for toasts. */
export function toErrorMessage(error: unknown): string {
  const described = describeApiError(error);
  return described.message;
}
