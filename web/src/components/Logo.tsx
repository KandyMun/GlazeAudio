/** A drip of glaze over a disc: the GlazeAudio mark. */
export function Logo({ size = 32 }: { size?: number }) {
  return (
    <svg width={size} height={size} viewBox="0 0 32 32" aria-hidden="true" focusable="false">
      <circle cx="16" cy="16" r="15" fill="var(--porcelain)" />
      <circle cx="16" cy="16" r="4" fill="var(--cobalt)" />
      <path
        d="M3.2 11.5A13.6 13.6 0 0 1 28.8 11.5V14c0 1.6-1.1 2.3-2 2.3s-1.9-.8-1.9-2.2c0 2.6-1.2 4.1-2.6 4.1s-2.4-1.3-2.4-3.1V13H12v4.6c0 1.6-1 2.8-2.4 2.8S7 19 7 17.3c0 1.4-.9 2.2-1.9 2.2s-1.9-.8-1.9-2.4z"
        fill="var(--celadon)"
        opacity="0.92"
      />
    </svg>
  );
}
