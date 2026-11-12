import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from "react";
import { api, auth } from "./client";
import type { AuthResponse, User } from "./types";

interface RegisterData {
  username: string;
  email: string;
  password: string;
  bio?: string;
}

interface AuthState {
  user: User | null;
  isAdmin: boolean;
  login(login: string, password: string): Promise<void>;
  register(data: RegisterData): Promise<void>;
  logout(): Promise<void>;
  setUser(user: User): void;
}

const AuthContext = createContext<AuthState | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUserState] = useState<User | null>(auth.user);

  useEffect(() => {
    const unsubscribe = auth.subscribe(setUserState);
    // Confirm a stored session is still valid (this also refreshes an expired access token).
    if (auth.user) {
      api<User>("/api/auth/me")
        .then((me) => auth.updateUser(me))
        .catch(() => {});
    }
    return () => {
      unsubscribe();
    };
  }, []);

  const value = useMemo<AuthState>(
    () => ({
      user,
      isAdmin: user?.role === "Admin",
      async login(login, password) {
        auth.accept(await api<AuthResponse>("/api/auth/login", { method: "POST", body: { login, password } }));
      },
      async register(data) {
        auth.accept(await api<AuthResponse>("/api/auth/register", { method: "POST", body: data }));
      },
      async logout() {
        const refreshToken = auth.refreshToken;
        auth.clear();
        if (refreshToken) {
          await api("/api/auth/logout", { method: "POST", body: { refreshToken } }).catch(() => {});
        }
      },
      setUser(next) {
        auth.updateUser(next);
      },
    }),
    [user],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthState {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used inside <AuthProvider>");
  return ctx;
}
