import type { InputHTMLAttributes, ReactNode } from "react";

interface Props extends InputHTMLAttributes<HTMLInputElement> {
  label: string;
  error?: string;
  help?: ReactNode;
}

/** Labelled input with help text and an inline error message. */
export function Field({ label, error, help, id, ...input }: Props) {
  const helpId = `${id}-help`;
  return (
    <div className={`field${error ? " has-error" : ""}`}>
      <label htmlFor={id}>{label}</label>
      <input id={id} aria-invalid={!!error} aria-describedby={error || help ? helpId : undefined} {...input} />
      {(error || help) && (
        <p id={helpId} className="field__help">
          {error ?? help}
        </p>
      )}
    </div>
  );
}
