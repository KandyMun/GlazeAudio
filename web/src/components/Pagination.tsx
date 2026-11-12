import { ChevronLeft, ChevronRight } from "lucide-react";

interface Props {
  page: number;
  totalPages: number;
  onChange(page: number): void;
}

export function Pagination({ page, totalPages, onChange }: Props) {
  if (totalPages <= 1) return null;
  const pages = Array.from({ length: totalPages }, (_, i) => i + 1).filter(
    (p) => p === 1 || p === totalPages || Math.abs(p - page) <= 1,
  );

  return (
    <nav className="pagination" aria-label="Pages">
      <button className="btn btn--ghost" disabled={page <= 1} onClick={() => onChange(page - 1)}>
        <ChevronLeft size={18} aria-hidden="true" /> Previous
      </button>
      <ol className="pagination__pages">
        {pages.map((p, i) => (
          <li key={p}>
            {i > 0 && p - pages[i - 1]! > 1 && <span className="pagination__gap">…</span>}
            <button
              className={`pagination__page${p === page ? " is-current" : ""}`}
              aria-current={p === page ? "page" : undefined}
              onClick={() => onChange(p)}
            >
              {p}
            </button>
          </li>
        ))}
      </ol>
      <button className="btn btn--ghost" disabled={page >= totalPages} onClick={() => onChange(page + 1)}>
        Next <ChevronRight size={18} aria-hidden="true" />
      </button>
    </nav>
  );
}
