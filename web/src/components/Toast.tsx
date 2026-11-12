import { createContext, useCallback, useContext, useRef, useState, type ReactNode } from "react";
import { CheckCircle2, X } from "lucide-react";

interface Toast {
  id: number;
  message: string;
}

const ToastContext = createContext<(message: string) => void>(() => {});

/**
 * Short confirmation messages ("Album saved.") that slide in at the bottom of the screen
 * and disappear after a few seconds. Used instead of browser alert() windows.
 */
export function ToastProvider({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<Toast[]>([]);
  const nextId = useRef(1);

  const dismiss = useCallback((id: number) => setToasts((all) => all.filter((t) => t.id !== id)), []);

  const show = useCallback(
    (message: string) => {
      const id = nextId.current++;
      setToasts((all) => [...all.slice(-2), { id, message }]);
      window.setTimeout(() => dismiss(id), 4500);
    },
    [dismiss],
  );

  return (
    <ToastContext.Provider value={show}>
      {children}
      <div className="toasts" role="status" aria-live="polite">
        {toasts.map((t) => (
          <div key={t.id} className="toast">
            <CheckCircle2 size={20} aria-hidden="true" />
            <span>{t.message}</span>
            <button className="icon-btn toast__close" onClick={() => dismiss(t.id)} aria-label="Dismiss">
              <X size={16} aria-hidden="true" />
            </button>
          </div>
        ))}
      </div>
    </ToastContext.Provider>
  );
}

/** Returns a function that shows a confirmation message. */
export function useToast() {
  return useContext(ToastContext);
}
