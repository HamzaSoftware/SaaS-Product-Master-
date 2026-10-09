import type { Locale } from "./config";

export const sectionKeys = ["dashboard", "my-work", "assets", "maintenance", "inventory", "locations", "analytics", "administration"] as const;
export type SectionKey = (typeof sectionKeys)[number];

type Dictionary = { productName: string; workspace: string; workspaceLabel: string; navigation: string; environment: string; overview: string; settings: string; signOut: string; account: string; search: string; openNavigation: string; closeNavigation: string; switchLanguage: string; userMenu: string; foundation: string; foundationDescription: string; comingSoon: string; placeholderDescription: string; nav: Record<SectionKey, string> };

const dictionaries: Record<Locale, Dictionary> = {
  en: {
    productName: "Nexa Operations", workspace: "Tenant workspace", workspaceLabel: "Organization", navigation: "Workspace navigation", environment: "Local development", overview: "Overview", settings: "Security", signOut: "Sign out", account: "Account", search: "Search the platform", openNavigation: "Open navigation", closeNavigation: "Close navigation", switchLanguage: "العربية", userMenu: "Open user menu", foundation: "Operations workspace", foundationDescription: "Manage your organization, locations, access, and security from one precise workspace.", comingSoon: "Module foundation", placeholderDescription: "This area is reserved for a future business workflow.",
    nav: { dashboard: "Dashboard", "my-work": "My work", assets: "Assets", maintenance: "Maintenance", inventory: "Inventory", locations: "Locations", analytics: "Analytics", administration: "Administration" },
  },
  ar: {
    productName: "نيكسا للعمليات", workspace: "مساحة المستأجر", workspaceLabel: "المنشأة", navigation: "تنقل مساحة العمل", environment: "بيئة التطوير المحلية", overview: "نظرة عامة", settings: "الأمان", signOut: "تسجيل الخروج", account: "الحساب", search: "البحث في المنصة", openNavigation: "فتح قائمة التنقل", closeNavigation: "إغلاق قائمة التنقل", switchLanguage: "English", userMenu: "فتح قائمة المستخدم", foundation: "مساحة العمليات", foundationDescription: "أدر المنشأة والأماكن والوصول والأمان من مساحة عمل دقيقة وموحدة.", comingSoon: "أساس الوحدة", placeholderDescription: "هذه المساحة مخصصة لإجراء عمل مستقبلي.",
    nav: { dashboard: "لوحة المعلومات", "my-work": "عملي", assets: "الأصول", maintenance: "الصيانة", inventory: "المخزون", locations: "الأماكن", analytics: "التحليلات", administration: "الإدارة" },
  },
};

export function getDictionary(locale: Locale): Dictionary { return dictionaries[locale]; }
export function isSection(value: string): value is SectionKey { return sectionKeys.includes(value as SectionKey); }
