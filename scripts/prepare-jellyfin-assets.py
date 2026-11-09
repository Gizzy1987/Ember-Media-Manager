#!/usr/bin/env python3
"""Prepare the Jellyfin brand assets used by the app and the new addon."""
import os

from PIL import Image

BRAND = os.path.join(os.environ['TEMP'], 'jfbrand')
OUT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..',
                                   'Addons', 'generic.Interface.Jellyfin',
                                   'Resources'))


def main():
    social = Image.open(os.path.join(BRAND, 'social.png')).convert('RGBA')
    print('social:', social.size)

    # icon + wordmark, without the tagline
    mark = social.crop((215, 140, 970, 380))
    mark.save(os.path.join(BRAND, 'jellyfin-wordmark.png'))

    # standalone gradient icon
    icon = social.crop((220, 145, 450, 375)).resize((256, 256), Image.LANCZOS)
    icon.save(os.path.join(BRAND, 'jellyfin-icon.png'))

    # small icon for module lists (32px)
    small = icon.resize((32, 32), Image.LANCZOS)
    small.save(os.path.join(BRAND, 'jellyfin-icon-32.png'))

    os.makedirs(OUT, exist_ok=True)
    icon.save(os.path.join(OUT, 'icon.png'))
    mark.save(os.path.join(OUT, 'logo.png'))
    print('addon resources written to', OUT)
    for f in sorted(os.listdir(OUT)):
        p = os.path.join(OUT, f)
        if f.endswith('.png'):
            print(' ', f, Image.open(p).size, os.path.getsize(p), 'bytes')


if __name__ == '__main__':
    main()
