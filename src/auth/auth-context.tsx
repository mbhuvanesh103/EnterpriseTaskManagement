import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from "react";

import { apiClient, tokenStorage, setUnauthorizedHandler } from "@/lib/api/client";
import { endpoints } from "@/lib/api/endpoints";
import { USER_ROLES, type PublicId, type UserRole } from "@/types/domain";

/**
 * Real session state (Phase 14).
 *
 * Identity and role come ONLY from the backend login response
 * (POST /api/Auth/login → LoginResponse) and are re-checked against the JWT's
 * own expiry on restore. There are no fake accounts, no hard-coded users and no
 * client-invented roles.
 *
 * SECURITY: role checks in this file are presentation-only. The ASP.NET Core
 * API remains the single security boundary.
 */

export interface AuthenticatedUser {
  publicId: PublicId;
  fullName: string;
  email: string;
  role: UserRole;
}

export type AuthStatus = "initializing" | "authenticated" | "unauthenticated";

export interface LoginCredentials {
  email: string;
  password: string;
}

export interface AuthContextValue {
  status: AuthStatus;
  user: AuthenticatedUser | null;
  role: UserRole | null;
  accessToken: string | null;
  isAuthenticated: boolean;
  login: (credentials: LoginCredentials) => Promise<AuthenticatedUser>;
  logout: () => void;
  hasRole: (role: UserRole) => boolean;
  hasAnyRole: (roles: readonly UserRole[]) => boolean;
}

const AuthContext = createContext<AuthContextValue | null>(null);

/** Exact shape of the backend LoginResponse record. */
interface LoginResponsePayload {
  accessToken: string;
  expiresAt: string;
  userPublicId: string;
  fullName: string;
  email: string;
  role: string;
}

function isUserRole(value: string): value is UserRole {
  return (USER_ROLES as readonly string[]).includes(value);
}

/** Reads the `exp` claim without trusting it for security — expiry check only. */
function isTokenExpired(token: string): boolean {
  const segments = token.split(".");
  if (segments.length !== 3) return true;
  try {
    const payloadSegment = segments[1]!.replace(/-/g, "+").replace(/_/g, "/");
    const payload = JSON.parse(atob(payloadSegment)) as { exp?: number };
    if (typeof payload.exp !== "number") return true;
    return payload.exp * 1000 <= Date.now();
  } catch {
    return true;
  }
}

function toAuthenticatedUser(payload: LoginResponsePayload): AuthenticatedUser {
  if (!isUserRole(payload.role)) {
    throw new Error("The server returned an unrecognised role for this account.");
  }
  return {
    publicId: payload.userPublicId,
    fullName: payload.fullName,
    email: payload.email,
    role: payload.role,
  };
}

function readStoredUser(): AuthenticatedUser | null {
  const raw = tokenStorage.getUserSnapshot();
  if (!raw) return null;
  try {
    const parsed = JSON.parse(raw) as AuthenticatedUser;
    if (!parsed?.publicId || !parsed?.role || !isUserRole(parsed.role)) return null;
    return parsed;
  } catch {
    return null;
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [status, setStatus] = useState<AuthStatus>("initializing");
  const [user, setUser] = useState<AuthenticatedUser | null>(null);
  const [accessToken, setAccessToken] = useState<string | null>(null);

  const clearSession = useCallback(() => {
    tokenStorage.clear();
    setAccessToken(null);
    setUser(null);
    setStatus("unauthenticated");
  }, []);

  // Session restore: a stored, unexpired token plus its identity snapshot.
  // Always resolves (even on error) so the loading state never sticks.
  useEffect(() => {
    let cancelled = false;

    try {
      const existing = tokenStorage.getAccessToken();
      const storedUser = readStoredUser();

      if (cancelled) return;

      if (!existing || isTokenExpired(existing) || !storedUser) {
        tokenStorage.clear();
        setStatus("unauthenticated");
      } else {
        setAccessToken(existing);
        setUser(storedUser);
        setStatus("authenticated");
      }
    } catch {
      if (!cancelled) {
        tokenStorage.clear();
        setStatus("unauthenticated");
      }
    }

    // Fallback: ensure initialization always completes even if something
    // unexpected prevents the state update from taking effect.
    const fallbackTimer = setTimeout(() => {
      if (!cancelled) {
        setStatus("unauthenticated");
      }
    }, 50);

    return () => {
      cancelled = true;
      clearTimeout(fallbackTimer);
    };
  }, []);

  // Central 401 reaction: the API client invalidates the session once, here.
  useEffect(() => {
    setUnauthorizedHandler(() => clearSession());
    return () => setUnauthorizedHandler(null);
  }, [clearSession]);

  const login = useCallback(async (credentials: LoginCredentials) => {
    const { data } = await apiClient.post<LoginResponsePayload>(endpoints.auth.login, credentials);
    const authenticated = toAuthenticatedUser(data);
    tokenStorage.setAccessToken(data.accessToken);
    tokenStorage.setUserSnapshot(JSON.stringify(authenticated));
    setAccessToken(data.accessToken);
    setUser(authenticated);
    setStatus("authenticated");
    return authenticated;
  }, []);

  const value = useMemo<AuthContextValue>(() => {
    const role = user?.role ?? null;
    return {
      status,
      user,
      role,
      accessToken,
      isAuthenticated: status === "authenticated",
      login,
      logout: clearSession,
      hasRole: (candidate) => role === candidate,
      hasAnyRole: (roles) => (role ? roles.includes(role) : false),
    };
  }, [accessToken, clearSession, login, status, user]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) throw new Error("useAuth must be used inside <AuthProvider>.");
  return context;
}
