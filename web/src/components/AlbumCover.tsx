import { useState } from "react";

/**
 * Album artwork. Albums without a cover URL (or with a broken one) get a generated
 * "glaze tile": a colour picked from the album id with the title's initials.
 */
export function AlbumCover({ id, title, url, size = "medium" }: { id: number; title: string; url: string | null; size?: "medium" | "large" }) {
  const [failed, setFailed] = useState(false);
  const glazes = ["#23408E", "#4E9A7C", "#7A4E8C", "#2F6F8F", "#8C5A2B", "#3E5C3A", "#5A3F78", "#1F6E6A"];
  const initials = title
    .split(/\s+/)
    .filter((w) => /[A-Za-z0-9]/.test(w[0] ?? ""))
    .slice(0, 2)
    .map((w) => w[0]!.toUpperCase())
    .join("");

  if (url && !failed) {
    return (
      <div className={`cover cover--${size}`}>
        <img src={url} alt={`Cover of ${title}`} loading="lazy" onError={() => setFailed(true)} />
      </div>
    );
  }
  return (
    <div className={`cover cover--${size} cover--tile`} style={{ ["--glaze" as string]: glazes[id % glazes.length] }} role="img" aria-label={`${title} (no cover image)`}>
      <span aria-hidden="true">{initials}</span>
    </div>
  );
}
