export const USERNAME_MIN = 3;
export const USERNAME_MAX = 20;

const USERNAME_PATTERN = /^[a-zA-Z0-9]+$/;

export function isValidUsername(value: string): boolean {
  const trimmed = value.trim();
  return (
    trimmed.length >= USERNAME_MIN &&
    trimmed.length <= USERNAME_MAX &&
    USERNAME_PATTERN.test(trimmed)
  );
}

export function sanitizeUsernameInput(value: string): string {
  return value.replace(/[^a-zA-Z0-9]/g, "").slice(0, USERNAME_MAX);
}
