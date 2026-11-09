#!/usr/bin/env python3
"""Repair and set the assembly version of every project.

  * a version line that lost its closing '>' is repaired
  * <Assembly: AssemblyVersion(...)> and <Assembly: AssemblyFileVersion(...)>
    are set to the release version

Usage: python scripts/set-version.py [version]
"""
import io
import os
import re
import sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
VERSION = sys.argv[1] if len(sys.argv) > 1 else '1.12.0.0'

# a complete line: <Assembly: AssemblyVersion("1.2.3.4")>
VERSION_LINE = re.compile(
    r'^(\s*<Assembly: Assembly(?:File)?Version\(")([^"]+)("\)\s*>)\s*$')
# a broken line: <Assembly: AssemblyVersion("1.2.3.4")      (no closing '>')
BROKEN_LINE = re.compile(
    r'^(\s*<Assembly: Assembly(?:File)?Version\(")([^"]+)("\))\s*$')


def main():
    repaired = 0
    changed = 0
    for dirpath, dirnames, filenames in os.walk(ROOT):
        dirnames[:] = [d for d in dirnames
                       if d.lower() not in ('bin', 'obj', '.git', 'packages',
                                            'node_modules')
                       and not d.startswith('EmberMM - ')]
        for f in filenames:
            if f != 'AssemblyInfo.vb':
                continue
            p = os.path.join(dirpath, f)
            with io.open(p, encoding='utf-8-sig', errors='surrogateescape') as fh:
                lines = fh.read().split('\n')
            out = []
            touched = False
            for line in lines:
                m = BROKEN_LINE.match(line)
                if m and not VERSION_LINE.match(line):
                    line = m.group(1) + m.group(2) + m.group(3) + '>'
                    repaired += 1
                    touched = True
                m = VERSION_LINE.match(line)
                if m and m.group(2) != VERSION:
                    line = m.group(1) + VERSION + m.group(3)
                    changed += 1
                    touched = True
                out.append(line)
            if touched:
                bom = open(p, 'rb').read(3) == b'\xef\xbb\xbf'
                data = '\n'.join(out).encode('utf-8', errors='surrogateescape')
                if bom:
                    data = b'\xef\xbb\xbf' + data
                with open(p, 'wb') as fh:
                    fh.write(data)
    print('repaired %d broken version lines, set %d versions to %s'
          % (repaired, changed, VERSION))


if __name__ == '__main__':
    main()
