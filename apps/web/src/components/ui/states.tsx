import { Button, Card } from "./primitives";

export function EmptyState({ title, description }: { title: string; description: string }) {
  return <Card className="state"><span className="state-icon" aria-hidden="true">◇</span><h2>{title}</h2><p>{description}</p></Card>;
}

export function LoadingState({ label = "Loading" }: { label?: string }) {
  return <div className="state" role="status"><span className="spinner" aria-hidden="true" /><span>{label}</span></div>;
}

export function Skeleton({ lines = 3, label = "Loading" }: { lines?: number; label?: string }) {
  return <div className="skeleton" role="status" aria-label={label}>{Array.from({ length: lines }, (_, index) => <span key={index} style={{ width: index === lines - 1 ? "68%" : "100%" }} />)}</div>;
}

export function ErrorState({ title, description, retry, retryLabel = "Try again" }: { title: string; description: string; retry?: () => void; retryLabel?: string }) {
  return <Card className="state state-error"><span className="state-icon" aria-hidden="true">!</span><h2>{title}</h2><p>{description}</p>{retry && <Button onClick={retry}>{retryLabel}</Button>}</Card>;
}
