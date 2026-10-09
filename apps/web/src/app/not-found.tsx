import Link from "next/link";
import { Card } from "@/components/ui/primitives";

export default function NotFound() {
  return <main className="standalone-state"><Card className="state"><span className="state-icon">404</span><h1>Page not found</h1><p>The requested page or locale is unavailable.</p><Link className="button" href="/en">Return to dashboard</Link></Card></main>;
}
