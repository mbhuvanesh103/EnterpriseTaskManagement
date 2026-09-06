/**
 * Centralized frontend environment configuration.
 * Values come from Vite env vars (see .env.example). Client-exposed only —
 * never place secrets, signing keys or credentials here.
 */

const rawApiBaseUrl = import.meta.env["VITE_API_BASE_URL"] as string | undefined;

export const env = {
  /**
   * Base URL of the ASP.NET Core REST API, including the /api prefix.
   * Configured only through VITE_API_BASE_URL; the localhost value is the
   * documented development default from launchSettings.json.
   */
  apiBaseUrl: (rawApiBaseUrl ?? "https://localhost:7001/api").replace(/\/+$/, ""),
  /** Request timeout in milliseconds for all API calls. */
  apiTimeoutMs: Number(import.meta.env["VITE_API_TIMEOUT_MS"] ?? 30000),
  /** localStorage keys used by the auth token interceptors. */
  accessTokenKey: "etm.accessToken",
  /** Reserved for a future refresh-token exchange; the API issues none today. */
  refreshTokenKey: "etm.refreshToken",
  /** Non-sensitive identity snapshot (public id, name, email, role name). */
  userSnapshotKey: "etm.user",
} as const;

export type AppEnv = typeof env;
