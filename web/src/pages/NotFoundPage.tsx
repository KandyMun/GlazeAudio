import { Link } from "react-router-dom";

export function NotFoundPage() {
  return (
    <div className="container page page--narrow">
      <div className="empty">
        <h1>Page not found</h1>
        <p>This address doesn't match anything on GlazeAudio.</p>
        <Link className="btn btn--primary" to="/">
          Browse albums
        </Link>
      </div>
    </div>
  );
}
