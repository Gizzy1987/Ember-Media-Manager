#!/usr/bin/env python3
"""Generate the startup splash for 'Ember Media Manager for Jellyfin'.

The wordmark sits centred in the image so that it stays readable at the
373x373 pixel size the splash form actually shows.
"""
import math
import os

from PIL import Image, ImageDraw, ImageFilter, ImageFont

W, H = 500, 500
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)),
                   '..', 'EmberAPI', 'splash-jellyfin.jpg')

PURPLE = (170, 92, 195)
BLUE = (0, 164, 220)
WHITE = (255, 255, 255)
LILAC = (226, 205, 240)


def lerp(a, b, t):
    return tuple(int(round(a[i] + (b[i] - a[i]) * t)) for i in range(3))


def gradient(size, c1, c2):
    w, h = size
    img = Image.new('RGB', size)
    px = img.load()
    for y in range(h):
        for x in range(w):
            px[x, y] = lerp(c1, c2, x / w * 0.55 + y / h * 0.45)
    return img


def swooshes(img):
    layer = Image.new('RGBA', img.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    w = img.size[0]
    bands = [
        ((255, 255, 255), 30, 46, 120, 0.0, 120, -0.34),
        ((255, 255, 255), 22, 26, 96, 1.7, 402, -0.29),
        ((255, 255, 255), 16, 14, 70, 3.1, 62, -0.41),
        ((255, 255, 255), 18, 10, 54, 4.4, 452, -0.25),
        ((40, 30, 60), 16, 34, 106, 2.3, 176, -0.32),
        ((40, 30, 60), 12, 18, 76, 0.9, 312, -0.28),
    ]
    for colour, alpha, width, amp, phase, ybase, ang in bands:
        pts = []
        for i in range(-40, w + 41, 6):
            t = i / w
            pts.append((i, ybase + amp * math.sin(t * 2.4 * math.pi + phase) + ang * i))
        d.line(pts, fill=colour + (alpha,), width=width, joint='curve')
    return Image.alpha_composite(img.convert('RGBA'),
                                 layer.filter(ImageFilter.GaussianBlur(1.2)))


def vignette(img):
    """Darken the centre so the white wordmark pops."""
    layer = Image.new('RGBA', img.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    d.ellipse([-120, -110, img.size[0] + 120, img.size[1] + 110],
              fill=(12, 8, 24, 96))
    layer = layer.filter(ImageFilter.GaussianBlur(95))
    return Image.alpha_composite(img, layer)


def font(name, size):
    path = os.path.join(r'C:\Windows\Fonts', name)
    if os.path.exists(path):
        return ImageFont.truetype(path, size)
    return ImageFont.load_default()


def centred(draw, text, fnt, y, fill, shadow=(0, 0, 0, 120)):
    """Draw text horizontally centred, with a soft drop shadow."""
    w = draw.textlength(text, font=fnt)
    x = (W - w) / 2
    draw.text((x + 2, y + 3), text, font=fnt, fill=shadow)
    draw.text((x, y), text, font=fnt, fill=fill)
    return w


def main():
    img = gradient((W, H), PURPLE, BLUE)
    img = swooshes(img)
    img = vignette(img)

    d = ImageDraw.Draw(img)

    f_ember = font('seguisb.ttf', 76)
    f_media = font('segoeui.ttf', 44)
    f_for = font('seguisb.ttf', 26)

    block_top = 168
    centred(d, 'Ember', f_ember, block_top, WHITE + (255,))

    rule_y = block_top + 96
    rule_w = 250
    d.rounded_rectangle([(W - rule_w) / 2, rule_y, (W + rule_w) / 2, rule_y + 4],
                        radius=2, fill=(255, 255, 255, 210))

    centred(d, 'Media Manager', f_media, rule_y + 26, WHITE + (245,))
    centred(d, 'for Jellyfin', f_for, rule_y + 88, LILAC + (255,))

    out = os.path.abspath(OUT)
    img.convert('RGB').save(out, 'JPEG', quality=93, optimize=True)
    print('splash written:', out, os.path.getsize(out), 'bytes')


if __name__ == '__main__':
    main()
