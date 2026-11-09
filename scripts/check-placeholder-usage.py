#!/usr/bin/env python3
"""Check which numeric placeholder IDs of db-DB are actually referenced in the
source code."""
import io
import os
import re

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
TR = os.path.join(ROOT, 'EmberAPI', 'Translations')
LINE_RE = re.compile(r'^\s*<string name="(\d+)">(.*?)</string>\s*$')

with io.open(os.path.join(TR, 'db-DB.xml'), encoding='utf-8') as fh:
    placeholders = set()
    for line in fh:
        m = LINE_RE.match(line.rstrip('\n'))
        if m and m.group(2).strip().isdigit():
            placeholders.add(int(m.group(1)))

# collect all GetString(...) ids used in the sources
used = set()
pat = re.compile(r'GetString\(\s*(\d+)')
for base in ('EmberAPI', 'EmberMediaManager', 'Addons'):
    for dirpath, dirnames, filenames in os.walk(os.path.join(ROOT, base)):
        dirnames[:] = [d for d in dirnames if d.lower() not in ('bin', 'obj')]
        for f in filenames:
            if not f.lower().endswith('.vb'):
                continue
            p = os.path.join(dirpath, f)
            try:
                with io.open(p, encoding='utf-8-sig', errors='replace') as fh:
                    for m in pat.finditer(fh.read()):
                        used.add(int(m.group(1)))
            except Exception:
                pass

unused = sorted(placeholders - used)
print('db-DB Platzhalter gesamt     : %d' % len(placeholders))
print('davon im Code verwendet      : %d' % len(placeholders & used))
print('davon ungenutzt (Loeschkandidaten): %d' % len(unused))
if unused:
    print('  z. B.:', unused[:25])
