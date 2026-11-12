import { formatRating } from "./format";

/** Overall rating out of 5, as a compact badge. */
export function Rating({ value, label = "Average rating" }: { value: number | null; label?: string }) {
  return (
    <span className={`rating${value === null ? " rating--empty" : ""}`} title={label}>
      <span className="rating__value">{formatRating(value)}</span>
      <span className="rating__max">/5</span>
      <span className="visually-hidden">{label}</span>
    </span>
  );
}
