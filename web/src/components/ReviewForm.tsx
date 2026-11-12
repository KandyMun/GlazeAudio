import { useState, type FormEvent } from "react";
import { Save } from "lucide-react";
import { ApiError } from "../api/client";
import type { Ratings } from "../api/types";
import { ASPECTS, formatRating } from "./format";

export interface ReviewInput extends Ratings {
  comment: string;
}

interface Props {
  initial?: ReviewInput;
  submitLabel: string;
  onSubmit(value: ReviewInput): Promise<void>;
  onCancel?(): void;
}

const EMPTY: ReviewInput = { lyricsRating: 3, melodyRating: 3, moodRating: 3, expressivenessRating: 3, comment: "" };

/** Four 0–5 sliders (one per aspect, each in its glaze colour) and a comment. */
export function ReviewForm({ initial = EMPTY, submitLabel, onSubmit, onCancel }: Props) {
  const [value, setValue] = useState<ReviewInput>(initial);
  const [error, setError] = useState<ApiError | null>(null);
  const [saving, setSaving] = useState(false);

  const overall = (value.lyricsRating + value.melodyRating + value.moodRating + value.expressivenessRating) / 4;

  async function submit(e: FormEvent) {
    e.preventDefault();
    setSaving(true);
    setError(null);
    try {
      await onSubmit({ ...value, comment: value.comment.trim() });
    } catch (err) {
      setError(err as ApiError);
    } finally {
      setSaving(false);
    }
  }

  const commentError = error?.fieldError("comment");

  return (
    <form className="review-form" onSubmit={submit} noValidate>
      <fieldset className="sliders">
        <legend>Your scores, 0 to 5</legend>
        {ASPECTS.map((a) => (
          <div className={`slider band--${a.key}`} key={a.key}>
            <label htmlFor={`rate-${a.key}`}>
              {a.label}
              <span className="slider__hint">{a.hint}</span>
            </label>
            <input
              id={`rate-${a.key}`}
              type="range"
              min={0}
              max={5}
              step={1}
              value={value[a.field]}
              style={{ ["--value" as string]: value[a.field] / 5 }}
              onChange={(e) => setValue({ ...value, [a.field]: Number(e.target.value) })}
            />
            <output htmlFor={`rate-${a.key}`} className="slider__value">
              {value[a.field]}
            </output>
          </div>
        ))}
        <p className="review-form__overall">
          Overall <strong>{formatRating(overall)}</strong>/5
        </p>
      </fieldset>

      <div className={`field${commentError ? " has-error" : ""}`}>
        <label htmlFor="comment">What did you think?</label>
        <textarea
          id="comment"
          rows={4}
          maxLength={2000}
          required
          placeholder="Say what worked and what didn't."
          value={value.comment}
          aria-invalid={!!commentError}
          aria-describedby="comment-help"
          onChange={(e) => setValue({ ...value, comment: e.target.value })}
        />
        <p id="comment-help" className="field__help">
          {commentError ?? `${value.comment.length}/2000 characters`}
        </p>
      </div>

      {error && !commentError && (
        <p className="form-error" role="alert">
          {error.problem.detail ?? error.problem.title}
        </p>
      )}

      <div className="form-actions">
        <button className="btn btn--primary" type="submit" disabled={saving || value.comment.trim() === ""}>
          <Save size={18} aria-hidden="true" /> {saving ? "Saving…" : submitLabel}
        </button>
        {onCancel && (
          <button className="btn btn--ghost" type="button" onClick={onCancel}>
            Cancel
          </button>
        )}
      </div>
    </form>
  );
}
