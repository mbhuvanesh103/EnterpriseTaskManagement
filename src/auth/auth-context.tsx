import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from "react";

import { tokenStorage } from "@/lib/api/client";
import type { PublicId, UserRole } from "@/types/domain";

/**
 * Auth foundation ONLY (Phase 13).
 *
 * There is no login request, no token issuing, and no hard-coded user here.
 * The provider models the conceptual session shape the real Phase 14
 * integration will fill in: it reads any token that already exists in storage,
 * exposes role helpers for UX gating, and clears state on logout.
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
  /** Phase 14 will implement this against POST /api/Auth/login. */
  login: (credentials: LoginCredentials) => Promise<void>;
  logout: () => void;
  hasRole: (role: UserRole) => boolean;
  hasAnyRole: (roles: readonly UserRole[]) => boolean;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export class AuthNotImplementedError extends Error {
  constructor() {
    super("Sign-in is wired up in Phase 14. No credentials are sent yet.");
    this.name = "AuthNotImplementedError";
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [status, setStatus] = useState<AuthStatus>("initializing");
  const [user, setUser] = useState<AuthenticatedUser | null>(null);
  const [accessToken, setAccessToken] = useState<string | null>(null);

  // Session rehydration placeholder: a token present in storage is the only
  // signal available until Phase 14 adds GET /api/Auth/me.
  useEffect(() => {
    const existing = tokenStorage.getAccessToken();
    setAccessToken(existing);
    setStatus(existing ? "authenticated" : "unauthenticated");
  }, []);

  const login = useCallback(async (_credentials: LoginCredentials) => {
    throw new AuthNotImplementedError();
  }, []);

  const logout = useCallback(() => {
    tokenStorage.clear();
    setAccessToken(null);
    setUser(null);
    setStatus("unauthenticated");
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
      logout,
      hasRole: (candidate) => role === candidate,
      hasAnyRole: (roles) => (role ? roles.includes(role) : false),
    };
  }, [accessToken, login, logout, status, user]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) throw new Error("useAuth must be used inside <AuthProvider>.");
  return context;
}
