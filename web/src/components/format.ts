export const ASPECTS = [
  { key: "lyrics", field: "lyricsRating", label: "Lyrics", hint: "What the words say and how well they're written" },
  { key: "melody", field: "melodyRating", label: "Melody", hint: "Tunes, hooks and the music itself" },
  { key: "mood", field: "moodRating", label: "Mood", hint: "The feeling and atmosphere it creates" },
  { key: "expressiveness", field: "expressivenessRating", label: "Expressiveness", hint: "How much the performance conveys" },
] as const;

export type AspectKey = (typeof ASPECTS)[number]["key"];

export function formatDuration(seconds: number): string {
  const m = Math.floor(seconds / 60);
  const s = seconds % 60;
  return `${m}:${s.toString().padStart(2, "0")}`;
}

export function formatLongDuration(seconds: number): string {
  const m = Math.round(seconds / 60);
  return m >= 60 ? `${Math.floor(m / 60)} h ${m % 60} min` : `${m} min`;
}

export function formatRating(value: number | null | undefined): string {
  return value === null || value === undefined ? "–" : value.toFixed(1).replace(/\.0$/, "");
}

const dateFormat = new Intl.DateTimeFormat("en-GB", { day: "numeric", month: "short", year: "numeric" });
export function formatDate(iso: string): string {
  return dateFormat.format(new Date(iso));
}

export function plural(n: number, one: string, many = `${one}s`): string {
  return `${n} ${n === 1 ? one : many}`;
}
