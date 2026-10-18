import axios, { AxiosError, type AxiosInstance, type InternalAxiosRequestConfig } from "axios";

import { env } from "@/config/env";

/**
 * Single Axios instance used by every feature module. No feature should call
 * axios directly — always go through this client so auth, error normalization
 * and base URL handling stay in one place.
 */
export const apiClient: AxiosInstance = axios.create({
  baseURL: env.apiBaseUrl,
  timeout: env.apiTimeoutMs,
  headers: { "Content-Type": "application/json" },
  withCredentials: false,
});

function readItem(key: string): string | null {
  if (typeof window === "undefined") return null;
  return window.localStorage.getItem(key);
}

/**
 * Session storage. The access token is the only credential kept, in
 * localStorage under a namespaced key, so a page reload restores the session.
 * The backend issues no refresh token yet (Phase 7 contract), so the refresh
 * slot stays an unused extension point. Passwords are never stored.
 */
export const tokenStorage = {
  getAccessToken: () => readItem(env.accessTokenKey),
  getRefreshToken: () => readItem(env.refreshTokenKey),
  /** Persist the access token returned by POST /api/Auth/login. */
  setAccessToken(accessToken: string) {
    if (typeof window === "undefined") return;
    window.localStorage.setItem(env.accessTokenKey, accessToken);
  },
  set(accessToken: string, refreshToken?: string) {
    if (typeof window === "undefined") return;
    window.localStorage.setItem(env.accessTokenKey, accessToken);
    if (refreshToken) window.localStorage.setItem(env.refreshTokenKey, refreshToken);
  },
  getUserSnapshot: () => readItem(env.userSnapshotKey),
  setUserSnapshot(serialized: string) {
    if (typeof window === "undefined") return;
    window.localStorage.setItem(env.userSnapshotKey, serialized);
  },
  clear() {
    if (typeof window === "undefined") return;
    window.localStorage.removeItem(env.accessTokenKey);
    window.localStorage.removeItem(env.refreshTokenKey);
    window.localStorage.removeItem(env.userSnapshotKey);
  },
};

/** Attach the JWT bearer token issued by the API's auth endpoints. */
apiClient.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  const token = tokenStorage.getAccessToken();
  if (token) config.headers.set("Authorization", `Bearer ${token}`);
  return config;
});

/** Shape the API is expected to return for problem details (RFC 7807). */
export interface ApiProblemDetails {
  title?: string;
  status?: number;
  detail?: string;
  errors?: Record<string, string[]>;
}

export class ApiError extends Error {
  readonly status: number | undefined;
  readonly fieldErrors: Record<string, string[]> | undefined;

  constructor(message: string, status?: number, fieldErrors?: Record<string, string[]>) {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.fieldErrors = fieldErrors;
  }

  /** Expired/absent/invalid session — the session is invalidated centrally. */
  get isUnauthorized(): boolean {
    return this.status === 401;
  }

  /** Authenticated but not permitted. Never triggers a sign-in redirect. */
  get isForbidden(): boolean {
    return this.status === 403;
  }
}

/**
 * Central 401 reaction. The auth provider registers a handler that clears the
 * session and sends the user to /login exactly once; the login request itself
 * is exempt so a wrong password never causes a redirect loop.
 */
type UnauthorizedHandler = () => void;
let unauthorizedHandler: UnauthorizedHandler | null = null;

export function setUnauthorizedHandler(handler: UnauthorizedHandler | null) {
  unauthorizedHandler = handler;
}

function isLoginRequest(url: string | undefined): boolean {
  return Boolean(url && url.toLowerCase().includes("/auth/login"));
}

/** Normalize every failure into ApiError and centralize the 401 reaction. */
apiClient.interceptors.response.use(
  (response) => response,
  (error: AxiosError<ApiProblemDetails>) => {
    const problem = error.response?.data;
    const status = error.response?.status;

    if (status === 401 && !isLoginRequest(error.config?.url)) {
      tokenStorage.clear();
      unauthorizedHandler?.();
    }

    const message =
      status === 401
        ? "Your session has ended. Please sign in again."
        : status === 403
          ? "Access denied. Your role doesn't allow this action."
          : (problem?.detail ?? problem?.title ?? error.message ?? "Request failed");

    return Promise.reject(new ApiError(message, status, problem?.errors));
  },
);
