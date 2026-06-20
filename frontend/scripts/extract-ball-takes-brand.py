"""Extract Ball Takes brand assets from collection PNGs."""
from __future__ import annotations

from collections import deque
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
FAV_SRC = ROOT / "images" / "ball-takes-and-favicons.collections.png"
COLL_SRC = ROOT / "images" / "ball-takes-images-collection.png"
BRAND_DIR = ROOT / "public" / "brand"
IMAGES_DIR = ROOT / "public" / "images"
LOGO_BOX = (60, 40, 1194, 780)


def crop_and_save(
    src: Image.Image,
    box: tuple[int, int, int, int],
    dest: Path,
    size: tuple[int, int] | None = None,
) -> None:
    cropped = src.crop(box)
    if size:
        cropped = cropped.resize(size, Image.Resampling.LANCZOS)
    dest.parent.mkdir(parents=True, exist_ok=True)
    cropped.save(dest, optimize=True)
    print(f"Saved {dest.name} ({cropped.size})")


def is_background_pixel(r: int, g: int, b: int, a: int, threshold: int = 32) -> bool:
    if a < 12:
        return True
    return r <= threshold and g <= threshold and b <= threshold


def remove_background(img: Image.Image, threshold: int = 32) -> Image.Image:
    rgba = img.convert("RGBA")
    width, height = rgba.size
    pixels = rgba.load()
    visited = [[False] * width for _ in range(height)]
    queue: deque[tuple[int, int]] = deque()

    for x in range(width):
        for y in (0, height - 1):
            if is_background_pixel(*pixels[x, y], threshold):
                queue.append((x, y))
    for y in range(height):
        for x in (0, width - 1):
            if is_background_pixel(*pixels[x, y], threshold):
                queue.append((x, y))

    while queue:
        x, y = queue.popleft()
        if visited[y][x]:
            continue
        r, g, b, a = pixels[x, y]
        if not is_background_pixel(r, g, b, a, threshold):
            continue
        visited[y][x] = True
        pixels[x, y] = (0, 0, 0, 0)
        if x > 0:
            queue.append((x - 1, y))
        if x < width - 1:
            queue.append((x + 1, y))
        if y > 0:
            queue.append((x, y - 1))
        if y < height - 1:
            queue.append((x, y + 1))

    return rgba


def soften_for_footer(img: Image.Image, opacity: float = 0.62) -> Image.Image:
    rgba = img.convert("RGBA")
    width, height = rgba.size
    pixels = rgba.load()

    for y in range(height):
        vertical_fade = 0.88 + (0.12 * (y / max(height - 1, 1)))
        for x in range(width):
            r, g, b, a = pixels[x, y]
            if a <= 0:
                continue
            pixels[x, y] = (r, g, b, int(a * opacity * vertical_fade))

    return rgba


def save_footer_logo(src: Image.Image, dest: Path) -> None:
    cropped = src.crop(LOGO_BOX)
    transparent = remove_background(cropped)
    softened = soften_for_footer(transparent)
    display = softened.resize((420, 274), Image.Resampling.LANCZOS)
    dest.parent.mkdir(parents=True, exist_ok=True)
    display.save(dest, optimize=True)
    print(f"Saved {dest.name} ({display.size}, transparent footer variant)")


def main() -> None:
    fav = Image.open(FAV_SRC).convert("RGBA")
    coll = Image.open(COLL_SRC).convert("RGBA")

    for name in (
        "ball-takes-default.png",
        "logo-header.png",
        "ball-takes-logo-full.png",
    ):
        crop_and_save(fav, LOGO_BOX, BRAND_DIR / name)

    save_footer_logo(fav, BRAND_DIR / "logo-footer.png")

    crop_and_save(fav, (410, 965, 620, 1175), BRAND_DIR / "app-icon.png", (512, 512))
    crop_and_save(fav, (740, 1020, 900, 1180), BRAND_DIR / "favicon-source.png", (512, 512))

    favicon_src = Image.open(BRAND_DIR / "favicon-source.png").convert("RGBA")
    for size, name in (
        (16, "icon-16x16.png"),
        (32, "icon-32x32.png"),
        (48, "icon-48x48.png"),
        (180, "icon-180x180.png"),
        (192, "icon-192x192.png"),
        (512, "icon-512x512.png"),
    ):
        resized = favicon_src.resize((size, size), Image.Resampling.LANCZOS)
        resized.save(BRAND_DIR / name, optimize=True)
        print(f"Saved {name} ({size}x{size})")

    icon32 = Image.open(BRAND_DIR / "icon-32x32.png")
    icon32.save(BRAND_DIR / "favicon.png", optimize=True)
    icon32.save(BRAND_DIR / "favicon.ico", format="ICO", sizes=[(16, 16), (32, 32), (48, 48)])

    crop_and_save(coll, (0, 0, 768, 512), IMAGES_DIR / "ball-takes-header.png", (1200, 400))

    (BRAND_DIR / "favicon-source.png").unlink(missing_ok=True)
    print("Done.")


if __name__ == "__main__":
    main()
