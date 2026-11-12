export interface Link {
  href: string;
  method: string;
}
export type Links = Record<string, Link>;

export interface Paged<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  _links: Links;
}

export interface Album {
  id: number;
  title: string;
  artist: string;
  releaseYear: number;
  genre: string;
  coverUrl: string | null;
  songCount: number;
  reviewCount: number;
  averageRating: number | null;
  _links: Links;
}

export interface Song {
  id: number;
  albumId: number;
  title: string;
  trackNumber: number;
  durationSeconds: number;
  reviewCount: number;
  averageRating: number | null;
  _links: Links;
}

export interface Ratings {
  lyricsRating: number;
  melodyRating: number;
  moodRating: number;
  expressivenessRating: number;
}

export interface Review extends Ratings {
  id: number;
  songId: number;
  userId: number;
  username: string;
  overallRating: number;
  comment: string;
  createdAt: string;
  updatedAt: string | null;
  _links: Links;
}

export interface User {
  id: number;
  username: string;
  email: string | null;
  role: "User" | "Admin";
  bio: string | null;
  createdAt: string;
  reviewCount: number;
  _links: Links;
}

export interface UserReview {
  id: number;
  albumId: number;
  albumTitle: string;
  albumArtist: string;
  songId: number;
  songTitle: string;
  overallRating: number;
  comment: string;
  createdAt: string;
  updatedAt: string | null;
  _links: Links;
}

export interface AspectAverages {
  lyrics: number;
  melody: number;
  mood: number;
  expressiveness: number;
}

export interface AlbumOverview {
  album: { id: number; title: string; artist: string; releaseYear: number; genre: string; coverUrl: string | null };
  stats: {
    songCount: number;
    reviewCount: number;
    totalDurationSeconds: number;
    averageRating: number | null;
    aspectAverages: AspectAverages | null;
  };
  songs: (Omit<Song, "albumId">)[];
  topRatedSong: Omit<Song, "albumId"> | null;
  latestReviews: {
    id: number;
    songId: number;
    songTitle: string;
    userId: number;
    username: string;
    overallRating: number;
    comment: string;
    createdAt: string;
  }[];
  _links: Links;
}

export interface AuthResponse {
  accessToken: string;
  accessTokenExpiresAt: string;
  refreshToken: string;
  refreshTokenExpiresAt: string;
  tokenType: string;
  user: User;
}

/** RFC 9457 problem details returned by the API for every error. */
export interface Problem {
  title?: string;
  detail?: string;
  status?: number;
  errors?: Record<string, string[]>;
}
