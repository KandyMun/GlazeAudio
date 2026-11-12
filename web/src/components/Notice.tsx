import { AlertCircle, Loader2 } from "lucide-react";
import type { ApiError } from "../api/client";

export function ErrorNotice({ error, onRetry }: { error: ApiError; onRetry?: () => void }) {
  return (
    <div className="notice notice--error" role="alert">
      <AlertCircle size={20} aria-hidden="true" />
      <div>
        <p className="notice__title">{error.problem.title ?? "Something went wrong."}</p>
        {error.problem.detail && <p>{error.problem.detail}</p>}
        {onRetry && (
          <button className="btn btn--ghost btn--small" onClick={onRetry}>
            Try again
          </button>
        )}
      </div>
    </div>
  );
}

export function Loading({ label = "Loading" }: { label?: string }) {
  return (
    <p className="loading" role="status">
      <Loader2 size={20} className="spin" aria-hidden="true" /> {label}…
    </p>
  );
}
