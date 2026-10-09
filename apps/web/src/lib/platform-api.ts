const apiBase = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5080";
let accessToken: string | null = null;
let refreshPromise: Promise<boolean> | null = null;

export function setAccessToken(token: string | null) { accessToken = token; }
export class ApiError extends Error {
  constructor(message: string, public readonly status: number, public readonly correlationId?: string) { super(message); }
}
async function refresh(): Promise<boolean> {
  if (!refreshPromise) refreshPromise = fetch(`${apiBase}/api/v1/auth/refresh`, { method: "POST", credentials: "include" })
    .then(async (response) => { if (!response.ok) return false; const body = await response.json() as { accessToken: string }; accessToken = body.accessToken; return true; })
    .catch(() => false).finally(() => { refreshPromise = null; });
  return refreshPromise;
}
export async function restoreSession() { return refresh(); }

export async function apiRequest<T>(path: string, init?: RequestInit, mayRefresh = true): Promise<T> {
  const response = await fetch(`${apiBase}${path}`, { ...init, credentials: "include",
    headers: { "Content-Type": "application/json", ...(accessToken ? { Authorization: `Bearer ${accessToken}` } : {}), ...init?.headers } });
  if (response.status === 401 && mayRefresh && await refresh()) return apiRequest<T>(path, init, false);
  if (!response.ok) {
    const problem = await response.json().catch(() => null) as { detail?: string; title?: string; correlationId?: string; errors?: Record<string, string[]> } | null;
    const validation = problem?.errors ? Object.values(problem.errors).flat().join(" ") : null;
    throw new ApiError(validation || problem?.detail || problem?.title || `Request failed (${response.status})`, response.status, problem?.correlationId);
  }
  if (response.status === 204) return undefined as T;
  return response.json() as Promise<T>;
}

export async function tenantLogin(email: string, password: string, tenantId: string) {
  const result = await apiRequest<{ accessToken: string }>("/api/v1/auth/login", { method: "POST", body: JSON.stringify({ email, password, tenantId }) }, false);
  accessToken = result.accessToken; return result;
}
export async function platformLogin(email: string, password: string) {
  const result = await apiRequest<{ accessToken: string; expiresAtUtc: string }>("/api/v1/auth/platform-login", { method: "POST", body: JSON.stringify({ email, password }) }, false);
  accessToken = result.accessToken; return result;
}
export async function logout() { try { await apiRequest("/api/v1/auth/logout", { method: "POST" }); } finally { accessToken = null; } }
