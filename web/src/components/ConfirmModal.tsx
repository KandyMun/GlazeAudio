import { useEffect, useState, type ReactNode } from "react";
import { Loader2, Trash2 } from "lucide-react";
import type { ApiError } from "../api/client";
import { Field } from "./Field";
import { Modal } from "./Modal";
import { ErrorNotice } from "./Notice";

interface Props {
  open: boolean;
  title: string;
  confirmLabel: string;
  cancelLabel?: string;
  /** If set, the user has to type this text before the button is enabled (for actions that can't be undone). */
  typeToConfirm?: string;
  onConfirm(): Promise<void>;
  onClose(): void;
  children: ReactNode;
}

/** "Are you sure?" dialog for deleting things. Shows the API error inside the dialog if the delete fails. */
export function ConfirmModal({ open, title, confirmLabel, cancelLabel = "Cancel", typeToConfirm, onConfirm, onClose, children }: Props) {
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<ApiError | null>(null);
  const [typed, setTyped] = useState("");

  useEffect(() => {
    if (open) {
      setError(null);
      setTyped("");
    }
  }, [open]);

  async function confirm() {
    setBusy(true);
    setError(null);
    try {
      await onConfirm();
    } catch (err) {
      setError(err as ApiError);
    } finally {
      setBusy(false);
    }
  }

  const blocked = typeToConfirm !== undefined && typed.trim() !== typeToConfirm;

  return (
    <Modal
      open={open}
      title={title}
      onClose={onClose}
      footer={
        <>
          <button className="btn btn--ghost" onClick={onClose}>
            {cancelLabel}
          </button>
          <button className="btn btn--danger" onClick={confirm} disabled={busy || blocked}>
            {busy ? <Loader2 size={18} className="spin" aria-hidden="true" /> : <Trash2 size={18} aria-hidden="true" />} {confirmLabel}
          </button>
        </>
      }
    >
      {children}
      {typeToConfirm !== undefined && (
        <Field
          id="confirm-text"
          label={`Type "${typeToConfirm}" to confirm`}
          autoComplete="off"
          value={typed}
          onChange={(e) => setTyped(e.target.value)}
        />
      )}
      {error && <ErrorNotice error={error} />}
    </Modal>
  );
}
