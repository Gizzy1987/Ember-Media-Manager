#!/usr/bin/env python3
"""Show the log lines around a given time window."""
import io
import sys

LOG = sys.argv[1]
FROM = sys.argv[2] if len(sys.argv) > 2 else '23:10:15'
TO = sys.argv[3] if len(sys.argv) > 3 else '23:10:26'
LEVELS = sys.argv[4].split(',') if len(sys.argv) > 4 else None

with io.open(LOG, encoding='utf-8-sig', errors='replace') as fh:
    for line in fh:
        if not line.startswith('2026-'):
            continue
        stamp = line[11:23]
        if not (FROM <= stamp <= TO):
            continue
        parts = line.split(';')
        if len(parts) < 6:
            continue
        level = parts[4]
        if LEVELS and level not in LEVELS:
            continue
        msg = parts[5].strip()
        if len(msg) > 150:
            msg = msg[:150] + ' ...'
        print('%s  %-5s  %-58s %s' % (stamp, level, parts[2][:58], msg))
