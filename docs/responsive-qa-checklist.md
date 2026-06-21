# Responsive Web App QA Checklist

Manual and automated checks for layout, navigation, and usability from 320px mobile through large desktop.

## Breakpoints to test

| Width | Device class | Tailwind token |
|------:|--------------|----------------|
| 320px | Small mobile | base (mobile-first) |
| 375px | iPhone standard | base |
| 390px | Modern iPhone | base |
| 414px | Large phone | base |
| 768px | Tablet portrait | `md` |
| 1024px | Tablet landscape / small laptop | `lg` |
| 1280px | Desktop | `xl` |
| 1440px+ | Large desktop | `2xl` / wide |

## Browsers

- [ ] Chrome (desktop)
- [ ] Safari (desktop)
- [ ] Firefox
- [ ] Microsoft Edge
- [ ] iOS Safari
- [ ] Android Chrome

## Automated tests

```bash
cd frontend
npx playwright install
npm run test:e2e
```

Optional admin auth (enables authenticated admin viewport tests):

```bash
E2E_ADMIN_EMAIL=you@example.com E2E_ADMIN_PASSWORD=secret npm run test:e2e
```

## Pages to test

### Public

| Page | Route | Pass |
|------|-------|------|
| Home | `/` | [ ] |
| Predictions | `/#predictions` | [ ] |
| Banter feed | `/#banter-feed` | [ ] |
| Rankings / pundits | `/#rankings` | [ ] |
| Prediction history | `/predictions/history` | [ ] |
| Studio | `/studio` | [ ] |
| Brackets | `/brackets` | [ ] |
| Bonuses | `/bonuses` | [ ] |
| Leagues | `/leagues` | [ ] |
| Rules | `/rules` | [ ] |
| Login | `/auth/login` | [ ] |
| Register | `/auth/register` | [ ] |

### Admin

| Page | Route | Pass |
|------|-------|------|
| Overview | `/admin` | [ ] |
| Jobs | `/admin/jobs` | [ ] |
| Errors | `/admin/errors` | [ ] |
| Review | `/admin/review` | [ ] |
| Stats | `/admin/stats` | [ ] |
| Health | `/admin/health` | [ ] |
| Sources | `/admin/sources` | [ ] |
| Source items | `/admin/source-items` | [ ] |

## Per-page checklist

For each page at each breakpoint:

- [ ] Page loads without error
- [ ] No unintended horizontal page scroll
- [ ] Primary navigation visible and usable (header nav ≥1024px, bottom nav / hamburger below)
- [ ] Primary actions visible without zoom
- [ ] Forms: inputs full-width, labels visible, buttons tappable (≥44px)
- [ ] Modals fit viewport; content scrolls inside modal
- [ ] Tables scroll inside container only (admin)
- [ ] Images/videos stay within containers
- [ ] Focus states visible when tabbing
- [ ] No text clipped unreadably

## Common issues to watch

1. **Horizontal overflow** — wide tables, long URLs, fixed `min-width` elements
2. **Tap targets too small** — icon buttons under 44px on mobile
3. **Sticky header overlap** — hash links should use `scroll-mt-14`
4. **Modal clipping** — dialog taller than viewport on 320px
5. **Admin nav missing** — hamburger drawer below 768px
6. **Bottom nav overlap** — main content needs bottom padding on mobile public pages
7. **Cramped tabs** — leaderboard / studio tabs at 320px

## Known limitations

- **Knockout bracket tree** — horizontal scroll inside the bracket panel is intentional on narrow screens.
- **Admin wide tables** — desktop table view may scroll horizontally inside `.table-scroll-container`; mobile uses card layout.
- **Job run detail `<pre>` blocks** — may scroll horizontally for long JSON/logs.
- **No native mobile app** — web-only experience.

## Manual design review (recommended)

- Home welcome hero at 320px
- Feed reaction row density at 320px
- Bracket board visual density on phone
- Admin jobs mobile card layout
- Session restore sheet on small phones

## Sign-off

| Tester | Date | Breakpoints | Browsers | Result |
|--------|------|-------------|----------|--------|
| | | | | Pass / Fail |

Notes:
