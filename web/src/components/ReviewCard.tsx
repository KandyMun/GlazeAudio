import type { ReactNode } from "react";
import { Link } from "react-router-dom";
import type { Review } from "../api/types";
import { AspectBands } from "./AspectBands";
import { formatDate } from "./format";
import { Rating } from "./Rating";

export function ReviewCard({ review, actions }: { review: Review; actions?: ReactNode }) {
  return (
    <article className="review">
      <header className="review__head">
        <Rating value={review.overallRating} label="Overall rating" />
        <p>
          <Link to={`/users/${review.userId}`} className="review__author">
            {review.username}
          </Link>
          <span className="muted">
            {" "}
            {formatDate(review.createdAt)}
            {review.updatedAt && ", edited"}
          </span>
        </p>
        {actions && <div className="review__actions">{actions}</div>}
      </header>
      <p className="review__comment">{review.comment}</p>
      <AspectBands
        values={{
          lyrics: review.lyricsRating,
          melody: review.melodyRating,
          mood: review.moodRating,
          expressiveness: review.expressivenessRating,
        }}
      />
    </article>
  );
}
