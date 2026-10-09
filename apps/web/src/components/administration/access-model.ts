export type AccessScope = "tenant" | "site";
export type AccessAssignment = { roleId: string; scopeKind: 0 | 1; scopeId: string | null };

export function buildAccessAssignments(roleIds: string[], scope: AccessScope, siteIds: string[]): AccessAssignment[] {
  const roles = [...new Set(roleIds)];
  if (scope === "tenant") return roles.map((roleId) => ({ roleId, scopeKind: 0, scopeId: null }));
  return roles.flatMap((roleId) => [...new Set(siteIds)].map((scopeId) => ({ roleId, scopeKind: 1, scopeId })));
}

export function temporaryPasswordIsValid(value: string) {
  return value.length >= 12 && /[A-Z]/.test(value) && /[a-z]/.test(value) && /\d/.test(value) && /[^A-Za-z0-9]/.test(value);
}
