#!/usr/bin/env python3
"""Extract the rename error messages from the Ember log."""
import io
import re
import sys

log = sys.argv[1]
pattern = sys.argv[2] if len(sys.argv) > 2 else 'Datei konnte nicht umbenannt werden'

with io.open(log, encoding='utf-8-sig', errors='replace') as fh:
    for line in fh:
        if pattern in line:
            # the message itself contains ';' -> take everything from the marker
            i = line.find(pattern)
            print('  ' + line[i:].rstrip())
