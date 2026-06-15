/**
 * Generates a lightweight browser fingerprint to bind sessions to a device.
 * We hash a combination of stable signals rather than fingerprinting with
 * canvas (too heavy) or navigator battery API (deprecated).
 * The fingerprint is NOT meant to be unbreakable — it is used to detect
 * when a recovery key is used on a new device, at which point the old
 * session's cookie is revoked server-side.
 */
export async function getDeviceFingerprint(): Promise<string> {
  if (typeof window === "undefined") return "ssr";

  const signals = [
    navigator.userAgent,
    navigator.language,
    navigator.platform,
    String(screen.width),
    String(screen.height),
    String(screen.colorDepth),
    String(new Date().getTimezoneOffset()),
    String(navigator.hardwareConcurrency ?? 0),
  ].join("|");

  try {
    const buf = await crypto.subtle.digest(
      "SHA-256",
      new TextEncoder().encode(signals)
    );
    return Array.from(new Uint8Array(buf))
      .map((b) => b.toString(16).padStart(2, "0"))
      .join("")
      .slice(0, 32); // 128-bit prefix is enough
  } catch {
    // Fallback for environments without SubtleCrypto
    return btoa(signals).slice(0, 32);
  }
}
