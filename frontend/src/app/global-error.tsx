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
          <button type="button" onClick={() => reset()}>
            Try again
          </button>
        </div>
      </body>
    </html>
  );
}
