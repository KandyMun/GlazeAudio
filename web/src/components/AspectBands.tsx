import { ASPECTS, formatRating } from "./format";

interface Props {
  values: { lyrics: number; melody: number; mood: number; expressiveness: number };
  size?: "large" | "small";
  /** Animate the bands filling in (once, on first render). */
  animate?: boolean;
}

/**
 * The four review aspects as glaze bands: each band fills to its 0–5 rating
 * in its own glaze colour. Used for album averages, reviews and the review form.
 */
export function AspectBands({ values, size = "small", animate = false }: Props) {
  return (
    <dl className={`bands bands--${size}${animate ? " bands--animate" : ""}`}>
      {ASPECTS.map((a, i) => {
        const value = values[a.key];
        return (
          <div className={`band band--${a.key}`} key={a.key} style={{ ["--delay" as string]: `${i * 90}ms` }}>
            <dt className="band__label">{a.label}</dt>
            <dd className="band__track">
              <span className="band__fill" style={{ ["--value" as string]: value / 5 }} />
            </dd>
            <dd className="band__value">{formatRating(value)}</dd>
          </div>
        );
      })}
    </dl>
  );
}
