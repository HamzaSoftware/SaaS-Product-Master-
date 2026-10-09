"use client";

import type {
  ButtonHTMLAttributes,
  InputHTMLAttributes,
  SelectHTMLAttributes,
  TextareaHTMLAttributes,
} from "react";
import { useState } from "react";
import { cn } from "@/lib/cn";

export function Button({ className, type = "button", variant = "primary", size = "default", loading, children, disabled, ...props }: ButtonHTMLAttributes<HTMLButtonElement> & { variant?: "primary" | "secondary" | "ghost" | "danger"; size?: "default" | "small" | "icon"; loading?: boolean }) {
  return <button type={type} className={cn("button", `button-${variant}`, `button-${size}`, className)} disabled={disabled || loading} aria-busy={loading || undefined} {...props}>{loading && <span className="button-spinner" aria-hidden="true" />}{children}</button>;
}

export function Input({ className, ...props }: InputHTMLAttributes<HTMLInputElement>) {
  return <input className={cn("field", className)} {...props} />;
}

export function Select({ className, children, ...props }: SelectHTMLAttributes<HTMLSelectElement>) {
  return <select className={cn("field", className)} {...props}>{children}</select>;
}

export function Textarea({ className, ...props }: TextareaHTMLAttributes<HTMLTextAreaElement>) {
  return <textarea className={cn("field min-h-28 resize-y", className)} {...props} />;
}

export function Card({ className, children }: { className?: string; children: React.ReactNode }) {
  return <section className={cn("card", className)}>{children}</section>;
}

export function Badge({ children, tone = "neutral" }: { children: React.ReactNode; tone?: "neutral" | "success" | "warning" }) {
  return <span className={cn("badge", `badge-${tone}`)}>{children}</span>;
}

export function SearchField({ label, ...props }: InputHTMLAttributes<HTMLInputElement> & { label: string }) {
  return (
    <label className="search-field">
      <span aria-hidden="true">⌕</span>
      <span className="sr-only">{label}</span>
      <Input type="search" placeholder={label} {...props} />
    </label>
  );
}

export function FilterBar({ children }: { children: React.ReactNode }) {
  return <div className="filter-bar" role="search">{children}</div>;
}

export function PasswordInput({ showLabel = "Show password", hideLabel = "Hide password", ...props }: InputHTMLAttributes<HTMLInputElement> & { showLabel?: string; hideLabel?: string }) {
  const [visible, setVisible] = useState(false);
  return <div className="password-field"><Input {...props} type={visible ? "text" : "password"} /><button type="button" aria-label={visible ? hideLabel : showLabel} title={visible ? hideLabel : showLabel} onClick={() => setVisible((value) => !value)}><span aria-hidden="true">{visible ? "◉" : "◎"}</span></button></div>;
}

export function Checkbox({ label, description, ...props }: InputHTMLAttributes<HTMLInputElement> & { label: string; description?: string }) {
  return <label className="check-control"><input {...props} type="checkbox" /><span className="check-box" aria-hidden="true" /><span><strong>{label}</strong>{description && <small>{description}</small>}</span></label>;
}

export function Switch({ label, ...props }: InputHTMLAttributes<HTMLInputElement> & { label: string }) {
  return <label className="switch-control"><input {...props} type="checkbox" role="switch" /><span aria-hidden="true" /><strong>{label}</strong></label>;
}

export function StatusIndicator({ label, tone = "neutral" }: { label: string; tone?: "neutral" | "success" | "warning" | "error" | "info" }) { return <span className={cn("status-indicator", `status-${tone}`)}><i aria-hidden="true" />{label}</span>; }

export function FormSection({ title, description, children }: { title: string; description?: string; children: React.ReactNode }) { return <section className="form-section"><header><h2>{title}</h2>{description && <p>{description}</p>}</header><div>{children}</div></section>; }

export function Tabs({ items, active, onChange, label }: { items: { id: string; label: string }[]; active: string; onChange(id: string): void; label: string }) { return <div className="tabs" role="tablist" aria-label={label}>{items.map((item) => <button key={item.id} role="tab" aria-selected={active === item.id} onClick={() => onChange(item.id)}>{item.label}</button>)}</div>; }

export function Pagination({ page, totalPages, label, previousLabel, nextLabel, onChange }: { page: number; totalPages: number; label: string; previousLabel: string; nextLabel: string; onChange(page: number): void }) { return <nav className="pagination" aria-label={label}><Button variant="secondary" size="small" disabled={page <= 1} onClick={() => onChange(page - 1)}>{previousLabel}</Button><span>{page} / {totalPages}</span><Button variant="secondary" size="small" disabled={page >= totalPages} onClick={() => onChange(page + 1)}>{nextLabel}</Button></nav>; }

export function MultiSelect({ label, options, value, onChange, emptyLabel }: { label: string; options: { value: string; label: string; description?: string; disabled?: boolean }[]; value: string[]; onChange(value: string[]): void; emptyLabel: string }) {
  return <fieldset className="multi-select"><legend>{label}</legend>{options.length === 0 ? <p>{emptyLabel}</p> : options.map((option) => <Checkbox key={option.value} label={option.label} description={option.description} checked={value.includes(option.value)} disabled={option.disabled} onChange={() => onChange(value.includes(option.value) ? value.filter((item) => item !== option.value) : [...value, option.value])} />)}</fieldset>;
}
