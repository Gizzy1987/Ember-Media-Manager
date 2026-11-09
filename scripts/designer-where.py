#!/usr/bin/env python3
"""Report the location of every control whose name matches a pattern,
plus the descendant set of given root controls.

Usage:
  python designer-where.py <designer.vb> Kodi           -> controls matching /Kodi/
  python designer-where.py <designer.vb> --children A B -> descendants of A,B
"""
import re
import sys
from collections import OrderedDict

DECL = re.compile(r'^\s*Me\.(\w+) = New System\.Windows\.Forms\.(\w+)\(\)')
ADD = re.compile(r'^\s*Me\.(\w+)\.Controls\.Add\(Me\.(\w+)(.*)$')


def main():
    path = sys.argv[1]
    mode = sys.argv[2]
    with open(path, 'r', encoding='utf-8-sig', errors='replace') as fh:
        lines = fh.read().split('\n')

    decls = OrderedDict()
    order = []
    parent = {}
    for line in lines:
        m = DECL.match(line)
        if m:
            decls[m.group(1)] = m.group(2)
            order.append(m.group(1))
            continue
        m = ADD.match(line)
        if m:
            parent[m.group(2)] = m.group(1)

    children = {}
    for child, par in parent.items():
        children.setdefault(par, []).append(child)

    if mode == '--children':
        roots = sys.argv[3:]
        seen = set()

        def collect(n):
            if n in seen:
                return
            seen.add(n)
            for c in children.get(n, []):
                collect(c)

        for r in roots:
            collect(r)
        for n in order:
            if n in seen:
                print(f'{n}\t{decls.get(n)}\tparent={parent.get(n, "-")}')
    else:
        pat = re.compile(mode, re.I)
        for n in order:
            if pat.search(n):
                print(f'{n}\t{decls.get(n)}\tparent={parent.get(n, "-")}')


if __name__ == '__main__':
    main()
