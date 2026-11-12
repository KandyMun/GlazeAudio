import type { AuthResponse, Problem, User } from "./types";

/** API base URL: empty in development (Vite proxy), the App Service URL in production. */
export const API_URL = (import.meta.env.VITE_API_URL ?? "").replace(/\/$/, "");

export class ApiError extends Error {
  readonly status: number;
  readonly problem: Problem;

  constructor(status: number, problem: Problem) {
    super(problem.detail ?? problem.title ?? `Request failed (${status})`);
    this.status = status;
    this.problem = problem;
  }

  /** First validation message for a field, e.g. fieldError("email"). */
  fieldError(field: string): string | undefined {
    return this.problem.errors?.[field]?.[0];
  }
}

// ---------------------------------------------------------------- token storage
interface Session {
  accessToken: string;
  refreshToken: string;
  user: User;
}

const STORAGE_KEY = "glazeaudio.session";
let session: Session | null = load();
const listeners = new Set<(user: User | null) => void>();

function load(): Session | null {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    return raw ? (JSON.parse(raw) as Session) : null;
  } catch {
    return null;
  }
}

function save(next: Session | null) {
  session = next;
  try {
    if (next) localStorage.setItem(STORAGE_KEY, JSON.stringify(next));
    else localStorage.removeItem(STORAGE_KEY);
  } catch {
    /* storage unavailable: keep the session in memory only */
  }
  listeners.forEach((fn) => fn(next?.user ?? null));
}

export const auth = {
  get user(): User | null {
    return session?.user ?? null;
  },
  get refreshToken(): string | null {
    return session?.refreshToken ?? null;
  },
  /** Store tokens from register / login / refresh. */
  accept(response: AuthResponse) {
    save({ accessToken: response.accessToken, refreshToken: response.refreshToken, user: response.user });
  },
  updateUser(user: User) {
    if (session) save({ ...session, user });
  },
  clear() {
    save(null);
  },
  subscribe(fn: (user: User | null) => void) {
    listeners.add(fn);
    return () => listeners.delete(fn);
  },
};

// ---------------------------------------------------------------- requests
let refreshing: Promise<boolean> | null = null;

/** Exchanges the refresh token for a new pair. Concurrent callers share one refresh. */
function refreshTokens(): Promise<boolean> {
  if (!session?.refreshToken) return Promise.resolve(false);
  refreshing ??= fetch(`${API_URL}/api/auth/refresh`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ refreshToken: session.refreshToken }),
  })
    .then(async (res) => {
      if (!res.ok) return false;
      auth.accept((await res.json()) as AuthResponse);
      return true;
    })
    .catch(() => false)
    .finally(() => {
      refreshing = null;
    });
  return refreshing;
}

interface RequestOptions {
  method?: string;
  body?: unknown;
  signal?: AbortSignal;
}

async function send(url: string, options: RequestOptions, retry: boolean): Promise<Response> {
  const headers: Record<string, string> = { Accept: "application/json" };
  if (options.body !== undefined) headers["Content-Type"] = "application/json";
  if (session?.accessToken) headers.Authorization = `Bearer ${session.accessToken}`;

  const res = await fetch(url, {
    method: options.method ?? "GET",
    headers,
    body: options.body === undefined ? undefined : JSON.stringify(options.body),
    signal: options.signal,
  });

  // Expired access token: refresh once and repeat the request.
  if (res.status === 401 && retry && session) {
    if (await refreshTokens()) return send(url, options, false);
    auth.clear();
  }
  return res;
}

/** Calls the API. Accepts a path ("/api/albums") or an absolute URL taken from a hypermedia link. */
export async function api<T>(pathOrUrl: string, options: RequestOptions = {}): Promise<T> {
  const url = pathOrUrl.startsWith("http") ? pathOrUrl : `${API_URL}${pathOrUrl}`;
  let res: Response;
  try {
    res = await send(url, options, true);
  } catch (err) {
    if ((err as Error).name === "AbortError") throw err;
    throw new ApiError(0, {
      title: "Can't reach the server.",
      detail: "Check your connection. If the database was asleep, try again in a minute.",
    });
  }

  if (!res.ok) {
    let problem: Problem = { title: `Request failed (${res.status})` };
    try {
      problem = (await res.json()) as Problem;
    } catch {
      /* empty or non-JSON error body */
    }
    throw new ApiError(res.status, problem);
  }
  if (res.status === 204) return undefined as T;
  return (await res.json()) as T;
}

/** Builds a query string from the defined, non-empty values. */
export function query(params: Record<string, string | number | undefined | null>): string {
  const q = new URLSearchParams();
  Object.entries(params).forEach(([k, v]) => {
    if (v !== undefined && v !== null && v !== "") q.set(k, String(v));
  });
  const s = q.toString();
  return s ? `?${s}` : "";
}
