#!/usr/bin/env python3
"""Report how complete a translation file is (entries that only contain their
own number are untranslated placeholders)."""
import io
import os
import re
import sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
TR = os.path.join(ROOT, 'EmberAPI', 'Translations')

for name in sys.argv[1:]:
    path = os.path.join(TR, name + '.xml')
    with io.open(path, encoding='utf-8') as fh:
        text = fh.read()
    vals = re.findall(r'<string name="(\d+)">(.*?)</string>', text)
    numeric = [k for k, v in vals if v.strip().isdigit()]
    print('%-8s gesamt=%-6d Platzhalter(nur Zahl)=%d' % (name, len(vals), len(numeric)))
    if numeric:
        print('          z. B.:', ', '.join(numeric[:12]))
