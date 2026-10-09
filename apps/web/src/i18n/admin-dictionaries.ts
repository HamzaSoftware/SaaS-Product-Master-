import type { Locale } from "./config";

export const adminDictionaries = {
  en: {
    administration: "Administration", company: "Company profile", sites: "Sites", locationTypes: "Location types",
    locations: "Locations", companyHelp: "Maintain the bilingual identity and regional defaults used across your tenant.",
    sitesHelp: "Manage operational sites, addresses, time zones, coordinates, and status.",
    typesHelp: "Define the tenant-specific categories used by the location hierarchy.",
    locationsHelp: "Browse and maintain each site's location tree.", add: "Add", edit: "Edit", save: "Save changes",
    cancel: "Cancel", search: "Search by code or name", code: "Code", nameEn: "English name", nameAr: "Arabic name",
    status: "Status", active: "Active", inactive: "Inactive", actions: "Actions", description: "Description",
    timeZone: "Time zone", language: "Default language", site: "Site", type: "Type", parent: "Parent location",
    legalNameEn: "Legal name (English)", legalNameAr: "Legal name (Arabic)", email: "Email", phone: "Phone", website: "Website", country: "Country", city: "City", address: "Address", sortOrder: "Sort order", children: "children",
    root: "Top level", empty: "No records match this view.", loading: "Loading…", loadError: "Could not load data.",
    saved: "Changes saved.", configureToken: "Sign in to load tenant data from the API.", open: "Open module",
  },
  ar: {
    administration: "الإدارة", company: "ملف الشركة", sites: "المواقع", locationTypes: "أنواع الأماكن",
    locations: "الأماكن", companyHelp: "إدارة هوية المنشأة ثنائية اللغة والإعدادات الإقليمية للمستأجر.",
    sitesHelp: "إدارة المواقع التشغيلية والعناوين والمناطق الزمنية والإحداثيات والحالة.",
    typesHelp: "تعريف التصنيفات الخاصة بالمستأجر والمستخدمة في التسلسل الهرمي للأماكن.",
    locationsHelp: "استعراض شجرة الأماكن لكل موقع وإدارتها.", add: "إضافة", edit: "تعديل", save: "حفظ التغييرات",
    cancel: "إلغاء", search: "البحث بالرمز أو الاسم", code: "الرمز", nameEn: "الاسم بالإنجليزية", nameAr: "الاسم بالعربية",
    status: "الحالة", active: "نشط", inactive: "غير نشط", actions: "الإجراءات", description: "الوصف",
    timeZone: "المنطقة الزمنية", language: "اللغة الافتراضية", site: "الموقع", type: "النوع", parent: "المكان الأب",
    legalNameEn: "الاسم القانوني بالإنجليزية", legalNameAr: "الاسم القانوني بالعربية", email: "البريد الإلكتروني", phone: "الهاتف", website: "الموقع الإلكتروني", country: "الدولة", city: "المدينة", address: "العنوان", sortOrder: "ترتيب العرض", children: "العناصر الفرعية",
    root: "المستوى الأعلى", empty: "لا توجد سجلات مطابقة.", loading: "جارٍ التحميل…", loadError: "تعذر تحميل البيانات.",
    saved: "تم حفظ التغييرات.", configureToken: "سجّل الدخول لتحميل بيانات المستأجر من الواجهة البرمجية.", open: "فتح الوحدة",
  },
} as const satisfies Record<Locale, Record<string, string>>;

export function getAdminDictionary(locale: Locale) { return adminDictionaries[locale]; }
