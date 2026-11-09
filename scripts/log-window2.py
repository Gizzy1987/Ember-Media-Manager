#!/usr/bin/env python3
"""Show log lines of a time window (robust against multi-line records)."""
import io
import sys

path, frm, to = sys.argv[1], sys.argv[2], sys.argv[3]
limit = int(sys.argv[4]) if len(sys.argv) > 4 else 20

with io.open(path, encoding='utf-8-sig', errors='replace') as fh:
    lines = fh.read().split('\n')

shown = 0
for line in lines:
    if len(line) < 19:
        continue
    stamp = line[11:19]
    if not (frm <= stamp <= to):
        continue
    parts = line.split(';')
    msg = parts[5].strip() if len(parts) > 5 else ''
    level = parts[4] if len(parts) > 4 else ''
    print('  %s %-6s %s' % (stamp, level, msg[:120]))
    shown += 1
    if shown >= limit:
        break
