#!/usr/bin/env python3
"""Embed the splash image into the resource files."""
import base64
import io
import os
import re

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
SPLASH = os.path.join(ROOT, 'EmberAPI', 'splash-jellyfin.jpg')


def read(path):
    with io.open(path, encoding='utf-8-sig') as fh:
        return fh.read()


def write(path, text):
    bom = open(path, 'rb').read(3) == b'\xef\xbb\xbf'
    data = text.encode('utf-8')
    if bom:
        data = b'\xef\xbb\xbf' + data
    with open(path, 'wb') as fh:
        fh.write(data)


def replace_data(text, key, raw):
    pat = re.compile(
        r'(<data name="%s"[^>]*>\s*<value>)(.*?)(</value>)' % re.escape(key),
        re.S)
    m = pat.search(text)
    if not m:
        raise SystemExit('key not found: ' + key)
    b64 = base64.b64encode(raw).decode('ascii')
    body = '\n' + '\n'.join('    ' + line for line in
                            [b64[i:i + 76] for i in range(0, len(b64), 76)]) + '\n    '
    return text[:m.start()] + m.group(1) + body + m.group(3) + text[m.end():]


def main():
    with open(SPLASH, 'rb') as fh:
        raw = fh.read()

    # 1. the splash form background
    p = os.path.join(ROOT, 'EmberAPI', 'frmSplash.resx')
    t = read(p)
    write(p, replace_data(t, '$this.BackgroundImage', raw))
    print('frmSplash.resx aktualisiert')

    # 2. the jpg that My Project\Resources.resx links to
    target = os.path.join(ROOT, 'EmberMediaManager', 'Resources',
                          'EmberSplashScreen.jpg')
    with open(target, 'wb') as fh:
        fh.write(raw)
    print('EmberSplashScreen.jpg aktualisiert')


if __name__ == '__main__':
    main()
