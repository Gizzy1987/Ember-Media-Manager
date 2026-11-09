#!/usr/bin/env python3
"""Print the full text of the logged exceptions (the CSV holds them as
multi-line records)."""
import csv
import io
import os
import sys

LOG = sys.argv[1]
LIMIT = int(sys.argv[2]) if len(sys.argv) > 2 else 2

with io.open(LOG, encoding='utf-8-sig', errors='replace', newline='') as fh:
    rows = list(csv.DictReader(fh, delimiter=';'))

errors = [r for r in rows if (r.get('level') or '').upper() in ('ERROR', 'FATAL')]
print('%d Fehler-Eintraege gesamt\n' % len(errors))

seen = set()
shown = 0
for r in reversed(errors):
    msg = (r.get('message') or '')
    exc = (r.get('exception') or '')
    key = (r.get('callsite'), exc[:200])
    if key in seen:
        continue
    seen.add(key)
    print('=' * 100)
    print('ZEIT    :', r.get('time'))
    print('CALLSITE:', r.get('callsite'))
    print('LEVEL   :', r.get('level'))
    print('MESSAGE :', msg)
    print('EXCEPTION:')
    print(exc if exc.strip() else '  (keine Exception-Spalte)')
    shown += 1
    if shown >= LIMIT:
        break
