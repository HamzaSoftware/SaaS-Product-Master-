"use client";

import { useEffect, useRef } from "react";
import { Button } from "./primitives";
import { cn } from "@/lib/cn";

type OverlayProps = {
  open: boolean;
  title: string;
  children: React.ReactNode;
  onClose: () => void;
  closeLabel?: string;
};

export function Dialog({ open, title, children, onClose, closeLabel = "Close" }: OverlayProps) {
  const ref = useRef<HTMLDialogElement>(null);
  useEffect(() => {
    if (open && !ref.current?.open) ref.current?.showModal();
    if (!open && ref.current?.open) ref.current.close();
  }, [open]);

  return <dialog ref={ref} className="dialog" onClose={onClose}><OverlayHeader title={title} onClose={onClose} closeLabel={closeLabel} /><div className="overlay-body">{children}</div></dialog>;
}

export function Drawer({ open, title, children, onClose, closeLabel = "Close" }: OverlayProps) {
  return <div className={cn("drawer-layer", open && "is-open")} aria-hidden={!open}><button className="drawer-backdrop" aria-label={closeLabel} onClick={onClose} /><aside className="drawer" role="dialog" aria-modal="true" aria-label={title}><OverlayHeader title={title} onClose={onClose} closeLabel={closeLabel} /><div className="overlay-body">{children}</div></aside></div>;
}

export function ConfirmationDialog({ open, title, description, confirmLabel, cancelLabel, dangerous, busy, onConfirm, onClose }: { open: boolean; title: string; description: string; confirmLabel: string; cancelLabel: string; dangerous?: boolean; busy?: boolean; onConfirm(): void; onClose(): void }) {
  return <Dialog open={open} title={title} onClose={onClose} closeLabel={cancelLabel}><p>{description}</p><div className="form-actions"><Button variant={dangerous ? "danger" : "primary"} loading={busy} onClick={onConfirm}>{confirmLabel}</Button><Button variant="secondary" disabled={busy} onClick={onClose}>{cancelLabel}</Button></div></Dialog>;
}

function OverlayHeader({ title, onClose, closeLabel }: { title: string; onClose: () => void; closeLabel: string }) {
  return <header className="overlay-header"><h2>{title}</h2><Button variant="ghost" size="icon" aria-label={closeLabel} onClick={onClose}>×</Button></header>;
}
