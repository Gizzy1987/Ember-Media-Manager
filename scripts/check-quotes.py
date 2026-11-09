#!/usr/bin/env python3
"""Report files whose string literals do not balance (breaks whole-file masking)."""
import re
import sys

STR = re.compile(r'"(?:[^"]|"")*"')

for path in sys.argv[1:]:
    with open(path, encoding='utf-8-sig', errors='replace') as fh:
        text = fh.read()
    total = text.count('"')
    masked = len(STR.findall(text))
    unmatched = total - 2 * masked
    print('%-70s quotes=%-7d strings=%-7d unmatched=%d'
          % (path, total, masked, unmatched))
