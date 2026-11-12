import { useCallback, useEffect, useState } from "react";
import { api, ApiError } from "../api/client";

interface State<T> {
  data: T | null;
  error: ApiError | null;
  loading: boolean;
}

/** Loads a URL and reloads whenever it changes. Pass null to skip loading. */
export function useApi<T>(url: string | null) {
  const [state, setState] = useState<State<T>>({ data: null, error: null, loading: url !== null });
  const [version, setVersion] = useState(0);

  useEffect(() => {
    if (url === null) {
      setState({ data: null, error: null, loading: false });
      return;
    }
    const controller = new AbortController();
    setState((s) => ({ ...s, loading: true, error: null }));
    api<T>(url, { signal: controller.signal })
      .then((data) => setState({ data, error: null, loading: false }))
      .catch((err: unknown) => {
        if ((err as Error).name === "AbortError") return;
        setState({ data: null, error: err as ApiError, loading: false });
      });
    return () => controller.abort();
  }, [url, version]);

  const reload = useCallback(() => setVersion((v) => v + 1), []);
  return { ...state, reload };
}
