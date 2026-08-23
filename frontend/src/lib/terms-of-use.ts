import { BRAND } from "./brand";

export const TERMS_VERSION = "2026-06";

const brand = BRAND.name;

export const TERMS_SECTIONS = [
  {
    id: "welcome",
    title: `Who can use ${brand}`,
    paragraphs: [
      `${brand} is open to everyone. No signup is required to play — we create a browser session so you can make predictions, join leagues with friends, and export content scripts.`,
      `By using ${brand} you agree to these Terms of Use. If you do not agree, please do not use the site.`,
    ],
  },
  {
    id: "entertainment",
    title: "Entertainment only — not gambling",
    paragraphs: [
      `${brand} is a free-to-play social predictions game intended for banter, entertainment, and friendly competition among friends and family.`,
      "No real money is wagered, won, or lost. This platform does not encourage, promote, or facilitate gambling in any form.",
      "If you or someone you know is affected by problem gambling, please seek support at BeGambleAware.org or call the National Gambling Helpline: 0808 802 0133.",
    ],
  },
  {
    id: "affiliation",
    title: "Fan game — no official affiliation",
    paragraphs: [
      `${brand} is a fan prediction game for the Premier League. It is not affiliated with the Premier League, the FA, or any football governing body.`,
    ],
  },
  {
    id: "ai-content",
    title: "AI-generated content",
    paragraphs: [
      "Images, media, and AI-generated content on this platform are produced using artificial intelligence tools for entertainment and social fun.",
      "Content is not sourced from real news publications unless explicitly credited and is not intended to represent factual reporting.",
    ],
  },
  {
    id: "session",
    title: "Your session & recovery key",
    paragraphs: [
      "We store your picks against a browser session. After accepting these terms you receive a recovery key — save it if you want to restore your picks after clearing cookies or switching devices.",
      "You are responsible for keeping your recovery key safe. We cannot restore your session without it.",
    ],
  },
] as const;
