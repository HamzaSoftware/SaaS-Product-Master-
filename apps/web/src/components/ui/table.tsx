import type { HTMLAttributes, TableHTMLAttributes } from "react";
import { cn } from "@/lib/cn";

export function Table({ className, ...props }: TableHTMLAttributes<HTMLTableElement>) {
  return <div className="table-scroll"><table className={cn("table", className)} {...props} /></div>;
}

export function TableEmpty({ children, ...props }: HTMLAttributes<HTMLTableCellElement>) {
  return <td className="table-empty" colSpan={100} {...props}>{children}</td>;
}
