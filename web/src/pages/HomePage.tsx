import { useState, type FormEvent } from "react";
import { Link, useNavigate, useSearchParams } from "react-router-dom";
import { Info, Plus, Search, SlidersHorizontal } from "lucide-react";
import { useAuth } from "../api/AuthContext";
import { query } from "../api/client";
import type { Album, Paged } from "../api/types";
import { AlbumCover } from "../components/AlbumCover";
import { AlbumFormModal } from "../components/AlbumFormModal";
import { AspectBands } from "../components/AspectBands";
import { ASPECTS, plural } from "../components/format";
import { Modal } from "../components/Modal";
import { ErrorNotice, Loading } from "../components/Notice";
import { Pagination } from "../components/Pagination";
import { Rating } from "../components/Rating";
import { useToast } from "../components/Toast";
import { useApi } from "../components/useApi";

const GENRES = ["Rock", "Pop", "Hip-Hop", "Electronic", "Jazz", "Folk", "Soul", "Metal"];
const SAMPLE = { lyrics: 4.2, melody: 4.8, mood: 3.6, expressiveness: 4.4 };

export function HomePage() {
  const [params, setParams] = useSearchParams();
  const [aboutOpen, setAboutOpen] = useState(false);
  const [adding, setAdding] = useState(false);
  const { isAdmin } = useAuth();
  const navigate = useNavigate();
  const toast = useToast();
  const page = Number(params.get("page") ?? 1);

  const url = `/api/albums${query({
    page,
    pageSize: 8,
    search: params.get("search"),
    genre: params.get("genre"),
    fromYear: params.get("fromYear"),
    toYear: params.get("toYear"),
  })}`;
  const { data, error, loading, reload } = useApi<Paged<Album>>(url);

  function applyFilters(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const form = new FormData(e.currentTarget);
    const next = new URLSearchParams();
    for (const key of ["search", "genre", "fromYear", "toYear"]) {
      const value = String(form.get(key) ?? "").trim();
      if (value) next.set(key, value);
    }
    setParams(next);
  }

  function goToPage(p: number) {
    const next = new URLSearchParams(params);
    next.set("page", String(p));
    setParams(next);
    window.scrollTo({ top: 0, behavior: "smooth" });
  }

  const filtered = ["search", "genre", "fromYear", "toYear"].some((k) => params.get(k));

  return (
    <>
      <section className="hero">
        <div className="container hero__inner">
          <div className="hero__copy">
            <h1>Rate the part of a song that got you.</h1>
            <p className="hero__lede">
              Every review on GlazeAudio scores four things separately: lyrics, melody, mood and expressiveness.
              Browse albums, see where each one shines, and add your own take.
            </p>
            <button className="btn btn--light" onClick={() => setAboutOpen(true)}>
              <Info size={18} aria-hidden="true" /> How ratings work
            </button>
          </div>
          <figure className="hero__swatch" aria-label="Example rating">
            <AspectBands values={SAMPLE} size="large" animate />
            <figcaption>An example song, rated out of 5 on each aspect.</figcaption>
          </figure>
        </div>
      </section>

      <div className="container page">
        <form className="filters" onSubmit={applyFilters} role="search" key={params.toString()}>
          <div className="field field--grow">
            <label htmlFor="search">Album or artist</label>
            <div className="input-icon">
              <Search size={18} aria-hidden="true" />
              <input id="search" name="search" type="search" placeholder="e.g. Radiohead" defaultValue={params.get("search") ?? ""} />
            </div>
          </div>
          <div className="field field--genre">
            <label htmlFor="genre">Genre</label>
            <select id="genre" name="genre" defaultValue={params.get("genre") ?? ""}>
              <option value="">Any genre</option>
              {GENRES.map((g) => (
                <option key={g} value={g}>
                  {g}
                </option>
              ))}
            </select>
          </div>
          <div className="field field--year">
            <label htmlFor="fromYear">From year</label>
            <input id="fromYear" name="fromYear" type="number" min={1900} max={2100} placeholder="1960" defaultValue={params.get("fromYear") ?? ""} />
          </div>
          <div className="field field--year">
            <label htmlFor="toYear">To year</label>
            <input id="toYear" name="toYear" type="number" min={1900} max={2100} placeholder="2026" defaultValue={params.get("toYear") ?? ""} />
          </div>
          <div className="filters__actions">
            <button className="btn btn--primary" type="submit">
              <SlidersHorizontal size={18} aria-hidden="true" /> Apply
            </button>
            {filtered && (
              <button className="btn btn--ghost" type="button" onClick={() => setParams(new URLSearchParams())}>
                Clear
              </button>
            )}
          </div>
        </form>

        <div className="section-head">
          <h2>{filtered ? "Matching albums" : "All albums"}</h2>
          {data && <p className="muted">{plural(data.totalCount, "album")}</p>}
          {isAdmin && (
            <button className="btn btn--primary section-head__action" onClick={() => setAdding(true)}>
              <Plus size={18} aria-hidden="true" /> Add album
            </button>
          )}
        </div>

        {loading && !data && <Loading label="Loading albums" />}
        {error && <ErrorNotice error={error} onRetry={reload} />}
        {data && data.items.length === 0 && (
          <div className="empty">
            <p>No albums match these filters.</p>
            <button className="btn btn--ghost" onClick={() => setParams(new URLSearchParams())}>
              Clear the filters
            </button>
          </div>
        )}
        {data && data.items.length > 0 && (
          <>
            <ul className={`album-grid${loading ? " is-loading" : ""}`}>
              {data.items.map((album) => (
                <li key={album.id}>
                  <Link to={`/albums/${album.id}`} className="album-tile">
                    <AlbumCover id={album.id} title={album.title} url={album.coverUrl} />
                    <div className="album-tile__text">
                      <h3>{album.title}</h3>
                      <p className="muted">
                        {album.artist}, {album.releaseYear}
                      </p>
                      <p className="album-tile__meta">
                        <Rating value={album.averageRating} />
                        <span className="muted">{plural(album.reviewCount, "review")}</span>
                      </p>
                    </div>
                  </Link>
                </li>
              ))}
            </ul>
            <Pagination page={data.page} totalPages={data.totalPages} onChange={goToPage} />
          </>
        )}
      </div>

      <AlbumFormModal
        open={adding}
        album={null}
        onClose={() => setAdding(false)}
        onSaved={(album) => {
          setAdding(false);
          toast(`"${album.title}" was added. Now add its songs.`);
          navigate(`/albums/${album.id}`);
        }}
      />

      <Modal
        open={aboutOpen}
        title="How ratings work"
        onClose={() => setAboutOpen(false)}
        footer={
          <button className="btn btn--primary" onClick={() => setAboutOpen(false)}>
            Got it
          </button>
        }
      >
        <p>A review gives each song four separate scores from 0 to 5, so you can tell a great tune with weak lyrics from the reverse.</p>
        <ul className="aspect-list">
          {ASPECTS.map((a) => (
            <li key={a.key} className={`aspect-list__item band--${a.key}`}>
              <strong>{a.label}</strong>
              <span>{a.hint}</span>
            </li>
          ))}
        </ul>
        <p>
          The overall score is the average of the four. A song's score averages all of its reviews, and an album's averages
          every review of every song on it.
        </p>
      </Modal>
    </>
  );
}
