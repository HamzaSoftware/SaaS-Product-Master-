"use client";
import { createContext, useCallback, useContext, useEffect, useMemo, useState } from "react";
import { apiRequest, logout, restoreSession, tenantLogin } from "@/lib/platform-api";
type CurrentUser = { userId: string; tenantId: string; permissions: string[] };
type AuthState = { user: CurrentUser | null; loading: boolean; signIn(email: string, password: string, tenantId: string): Promise<void>; signOut(): Promise<void>; reload(): Promise<void> };
const AuthContext = createContext<AuthState | null>(null);
export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<CurrentUser | null>(null); const [loading, setLoading] = useState(true);
  const reload = useCallback(async () => { try { setUser(await apiRequest<CurrentUser>("/api/v1/context")); } catch { setUser(null); } }, []);
  useEffect(() => { void restoreSession().then((ok) => ok ? reload() : undefined).finally(() => setLoading(false)); }, [reload]);
  const value = useMemo<AuthState>(() => ({ user, loading, reload, signIn: async (email, password, tenantId) => { await tenantLogin(email, password, tenantId); await reload(); }, signOut: async () => { await logout(); setUser(null); } }), [user, loading, reload]);
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
export function useAuth() { const value = useContext(AuthContext); if (!value) throw new Error("AuthProvider is required"); return value; }
