"use client";

import { createContext, useCallback, useContext, useEffect, useMemo, useState } from "react";
import { apiRequest, logout, platformLogin, restoreSession, setAccessToken } from "@/lib/platform-api";

export type PlatformTenant = { id: string; name: string; slug: string; isActive: boolean; version: string; createdAtUtc: string; updatedAtUtc: string };
export type CreatePlatformTenant = { name: string; slug: string; nameAr: string | null; timeZone: string; defaultLanguage: "en" | "ar"; adminEmail: string; adminPassword: string; adminDisplayName: string };
type PlatformAuthState = {
  authenticated: boolean;
  loading: boolean;
  tenants: PlatformTenant[];
  signIn(email: string, password: string): Promise<void>;
  signOut(): Promise<void>;
  reloadTenants(): Promise<void>;
  createTenant(payload: CreatePlatformTenant): Promise<PlatformTenant>;
  setTenantStatus(tenant: PlatformTenant, isActive: boolean): Promise<PlatformTenant>;
};

const PlatformAuthContext = createContext<PlatformAuthState | null>(null);

export function PlatformAuthProvider({ children }: { children: React.ReactNode }) {
  const [authenticated, setAuthenticated] = useState(false);
  const [loading, setLoading] = useState(true);
  const [tenants, setTenants] = useState<PlatformTenant[]>([]);

  const reloadTenants = useCallback(async () => {
    const next = await apiRequest<PlatformTenant[]>("/api/v1/platform/tenants/");
    setTenants(next); setAuthenticated(true);
  }, []);

  useEffect(() => {
    // Restoring the HttpOnly-cookie session is the only browser persistence path.
    void restoreSession().then(async (restored) => {
      if (!restored) return;
      try { await reloadTenants(); }
      catch { setAccessToken(null); setAuthenticated(false); }
    }).finally(() => setLoading(false));
  }, [reloadTenants]);

  const value = useMemo<PlatformAuthState>(() => ({
    authenticated, loading, tenants, reloadTenants,
    signIn: async (email, password) => { await platformLogin(email, password); await reloadTenants(); },
    signOut: async () => { await logout(); setTenants([]); setAuthenticated(false); },
    createTenant: async (payload) => {
      const created = await apiRequest<PlatformTenant>("/api/v1/platform/tenants/", { method: "POST", body: JSON.stringify(payload) });
      setTenants((current) => [...current.filter((tenant) => tenant.id !== created.id), created].sort((a, b) => a.name.localeCompare(b.name)));
      return created;
    },
    setTenantStatus: async (tenant, isActive) => {
      const updated = await apiRequest<PlatformTenant>(`/api/v1/platform/tenants/${tenant.id}/status`, { method: "PATCH", body: JSON.stringify({ isActive, version: tenant.version }) });
      setTenants((current) => current.map((item) => item.id === updated.id ? updated : item));
      return updated;
    },
  }), [authenticated, loading, tenants, reloadTenants]);

  return <PlatformAuthContext.Provider value={value}>{children}</PlatformAuthContext.Provider>;
}

export function usePlatformAuth() {
  const value = useContext(PlatformAuthContext);
  if (!value) throw new Error("PlatformAuthProvider is required");
  return value;
}
