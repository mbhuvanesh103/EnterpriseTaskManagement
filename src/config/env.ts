/**
 * Centralized frontend environment configuration.
 * Values come from Vite env vars (see .env.example). Client-exposed only.
 */

const rawApiBaseUrl = import.meta.env["VITE_API_BASE_URL"] as string | undefined;

export const env = {
  /** Base URL of the ASP.NET Core REST API, e.g. https://localhost:7001/api */
  apiBaseUrl: (rawApiBaseUrl ?? "https://localhost:7001/api").replace(/\/+$/, ""),
  /** Request timeout in milliseconds for all API calls. */
  apiTimeoutMs: Number(import.meta.env["VITE_API_TIMEOUT_MS"] ?? 30000),
  /** localStorage keys used by the auth token interceptors. */
  accessTokenKey: "etm.accessToken",
  refreshTokenKey: "etm.refreshToken",
} as const;

export type AppEnv = typeof env;
