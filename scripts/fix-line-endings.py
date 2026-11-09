#!/usr/bin/env python3
"""Restore the CRLF line endings in the working copy.

.gitattributes normalises every text file to LF in the repository, while
core.autocrlf checks them out with CRLF. Scripts that rewrite a file with LF
therefore leave the working copy inconsistent - this puts it back.
"""
import os
import subprocess
import sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
TEXT_EXT = ('.vb', '.cs', '.vbproj', '.csproj', '.sln', '.xml', '.yml', '.yaml',
            '.resx', '.md', '.log', '.txt', '.bat', '.nsi', '.mjs', '.js',
            '.props', '.config', '.settings', '.myapp', '.targets', '.ruleset')


def tracked_changed():
    out = subprocess.run(['git', 'diff', '--name-only', 'HEAD'],
                         cwd=ROOT, capture_output=True, text=True).stdout
    return [l.strip() for l in out.split('\n') if l.strip()]


def main():
    fixed = []
    for rel in tracked_changed():
        p = os.path.join(ROOT, rel)
        if not os.path.isfile(p):
            continue
        if not rel.lower().endswith(TEXT_EXT):
            continue
        with open(p, 'rb') as fh:
            data = fh.read()
        if b'\x00' in data[:8000]:      # binary safety net
            continue
        if b'\r\n' in data:
            continue                     # already CRLF
        if b'\n' not in data:
            continue                     # single line file, nothing to do
        data = data.replace(b'\r\n', b'\n').replace(b'\n', b'\r\n')
        with open(p, 'wb') as fh:
            fh.write(data)
        fixed.append(rel)
    print('%d files back to CRLF' % len(fixed))
    for f in fixed[:8]:
        print('   ', f)
    if len(fixed) > 8:
        print('    ... und %d weitere' % (len(fixed) - 8))


if __name__ == '__main__':
    main()
