export type LocationItem = { id: string; parentLocationId: string | null; code: string; nameEn: string; nameAr?: string | null; sortOrder: number; isActive: boolean };
export type LocationTreeNode = LocationItem & { children: LocationTreeNode[] };

export function buildLocationTree(items: LocationItem[]): LocationTreeNode[] {
  const nodes = new Map(items.map((item) => [item.id, { ...item, children: [] as LocationTreeNode[] }]));
  const roots: LocationTreeNode[] = [];
  for (const node of nodes.values()) {
    const parent = node.parentLocationId ? nodes.get(node.parentLocationId) : undefined;
    (parent?.children ?? roots).push(node);
  }
  const sort = (values: LocationTreeNode[]) => values.sort((a, b) => a.sortOrder - b.sortOrder || a.code.localeCompare(b.code)).forEach((x) => sort(x.children));
  sort(roots); return roots;
}

export function displayLocationName(item: LocationItem, locale: "en" | "ar") {
  return locale === "ar" && item.nameAr ? item.nameAr : item.nameEn;
}
