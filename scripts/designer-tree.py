#!/usr/bin/env python3
"""Parse a WinForms .Designer.vb file and print the control tree.

Used to plan the Kodi -> Jellyfin preset surgery in dlgSettings.Designer.vb.
"""
import re
import sys
from collections import OrderedDict

DECL = re.compile(r'^\s*Me\.(\w+) = New System\.Windows\.Forms\.(\w+)\(\)')
ADD = re.compile(r'^\s*Me\.(\w+)\.Controls\.Add\(Me\.(\w+)(.*)$')
WITH = re.compile(r'^\s*Friend WithEvents (\w+) As System\.Windows\.Forms\.(\w+)')
NAME = re.compile(r'^\s*Me\.(\w+)\.Name = "([^"]*)"')
TEXT = re.compile(r'^\s*Me\.(\w+)\.Text = "([^"]*)"')


def main(path):
    with open(path, 'r', encoding='utf-8-sig', errors='replace') as fh:
        lines = fh.read().split('\n')

    decls = OrderedDict()      # name -> type
    order = []                 # declaration order
    parent = {}                # child -> parent
    texts = {}
    names = {}

    for line in lines:
        m = DECL.match(line)
        if m:
            decls[m.group(1)] = m.group(2)
            order.append(m.group(1))
            continue
        m = ADD.match(line)
        if m:
            parent[m.group(2)] = m.group(1)
            continue
        m = TEXT.match(line)
        if m:
            texts[m.group(1)] = m.group(2)
            continue
        m = NAME.match(line)
        if m:
            names[m.group(1)] = m.group(2)

    children = {}
    for child, par in parent.items():
        children.setdefault(par, []).append(child)

    roots = [n for n in order if n not in parent]

    filter_txt = sys.argv[2] if len(sys.argv) > 2 else None

    def walk(node, depth):
        t = decls.get(node, '?')
        lbl = texts.get(node, '')
        nm = names.get(node, '')
        line = '  ' * depth + f'{node} [{t}] text={lbl!r} name={nm!r}'
        if filter_txt and filter_txt.lower() not in line.lower():
            for c in children.get(node, []):
                walk(c, depth + 1)
            return
        print(line)
        for c in children.get(node, []):
            walk(c, depth + 1)

    for r in roots:
        walk(r, 0)


if __name__ == '__main__':
    main(sys.argv[1])
