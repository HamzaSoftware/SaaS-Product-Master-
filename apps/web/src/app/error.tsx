"use client";

import { ErrorState } from "@/components/ui/states";

export default function GlobalError({ reset }: { error: Error & { digest?: string }; reset: () => void }) {
  return <main className="standalone-state"><ErrorState title="Something went wrong" description="The page could not be loaded. Use the correlation ID from the API response when contacting support." retry={reset} /></main>;
}
