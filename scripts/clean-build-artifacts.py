#!/usr/bin/env python3
"""Remove every build artifact folder (bin/obj) and the release output.

Uses git clean so the deletion stays recoverable and ignored files are
included (-x); tracked files are never touched.
"""
import os
import subprocess
import sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))

SKIP = ('packages', '.nuget', '.git', 'node_modules')


def find_artifacts():
    found = []
    for dirpath, dirnames, filenames in os.walk(ROOT):
        rel = os.path.relpath(dirpath, ROOT)
        parts = rel.split(os.sep)
        if any(p in SKIP for p in parts):
            dirnames[:] = []
            continue
        for d in list(dirnames):
            if d in ('bin', 'obj'):
                found.append(os.path.join(rel, d))
                dirnames.remove(d)
        if parts and parts[0].startswith('EmberMM - '):
            found.append(rel)
            dirnames[:] = []
    return sorted(set(found))


def main():
    artifacts = find_artifacts()
    # the release output folders live in the repo root
    for name in os.listdir(ROOT):
        if name.startswith('EmberMM - '):
            rel = name
            if rel not in artifacts:
                artifacts.append(rel)
    artifacts = sorted(set(artifacts))
    print('entferne %d Artefaktordner' % len(artifacts))
    if not artifacts:
        return
    # git clean needs forward slashes
    paths = [a.replace(os.sep, '/') for a in artifacts]
    subprocess.run(['git', 'clean', '-xdf', '-q', '--'] + paths, cwd=ROOT,
                   check=False)
    left = [a for a in artifacts if os.path.exists(os.path.join(ROOT, a))]
    print('verbleibend: %d' % len(left))
    for a in left:
        print('   ', a)


if __name__ == '__main__':
    main()
