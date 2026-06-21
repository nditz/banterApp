"use client";

export default function GlobalError({
  error,
  reset,
}: {
  error: Error & { digest?: string };
  reset: () => void;
}) {
  return (
    <html lang="en">
      <body>
        <div style={{ padding: "2rem", fontFamily: "system-ui, sans-serif", textAlign: "center" }}>
          <h2>Something went wrong</h2>
          <p>Please refresh and try again.</p>
          {error.digest ? (
            <p style={{ fontSize: "0.875rem", opacity: 0.7 }}>Reference: {error.digest}</p>
          ) : null}
          <button type="button" onClick={() => reset()}>
            Try again
          </button>
        </div>
      </body>
    </html>
  );
}
