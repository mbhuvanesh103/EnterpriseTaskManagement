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

function readToken(key: string): string | null {
  if (typeof window === "undefined") return null;
  return window.localStorage.getItem(key);
}

export const tokenStorage = {
  getAccessToken: () => readToken(env.accessTokenKey),
  getRefreshToken: () => readToken(env.refreshTokenKey),
  set(accessToken: string, refreshToken: string) {
    if (typeof window === "undefined") return;
    window.localStorage.setItem(env.accessTokenKey, accessToken);
    window.localStorage.setItem(env.refreshTokenKey, refreshToken);
  },
  clear() {
    if (typeof window === "undefined") return;
    window.localStorage.removeItem(env.accessTokenKey);
    window.localStorage.removeItem(env.refreshTokenKey);
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
}

/**
 * Normalize every failure into ApiError. The refresh-token exchange is an
 * intentional extension point: wire it here once /auth/refresh exists.
 */
apiClient.interceptors.response.use(
  (response) => response,
  (error: AxiosError<ApiProblemDetails>) => {
    const problem = error.response?.data;
    const status = error.response?.status;
    if (status === 401) tokenStorage.clear();
    return Promise.reject(
      new ApiError(
        problem?.detail ?? problem?.title ?? error.message ?? "Request failed",
        status,
        problem?.errors,
      ),
    );
  },
);
