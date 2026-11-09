#!/usr/bin/env python3
"""Verify that the splash image in frmSplash.resx matches the source image."""
import base64
import hashlib
import io
import os
import re

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
RESX = os.path.join(ROOT, 'EmberAPI', 'frmSplash.resx')
SRC = os.path.join(ROOT, 'EmberAPI', 'splash-jellyfin.jpg')

with io.open(RESX, encoding='utf-8-sig') as fh:
    text = fh.read()
m = re.search(r'<data name="\$this\.BackgroundImage"[^>]*>\s*<value>(.*?)</value>',
              text, re.S)
embedded = base64.b64decode(re.sub(r'\s', '', m.group(1)))
with open(SRC, 'rb') as fh:
    source = fh.read()

print('eingebettet : %7d bytes  md5 %s' % (len(embedded),
                                           hashlib.md5(embedded).hexdigest()[:12]))
print('Quelldatei  : %7d bytes  md5 %s' % (len(source),
                                           hashlib.md5(source).hexdigest()[:12]))
print('identisch   : %s' % (embedded == source))
