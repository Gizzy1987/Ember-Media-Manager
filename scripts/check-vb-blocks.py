#!/usr/bin/env python3
"""Report unbalanced block constructs in a VB source file."""
import re
import sys

MODS = (r'(?:Public |Private |Friend |Protected |Static |Shared |Overridable '
        r'|MustOverride |NotOverridable |Overloads |Default |Iterator |Async '
        r'|Partial |ReadOnly |WriteOnly |Shadows |Overrides |Widening |Narrowing )*')

OPENERS = [
    (re.compile(r'^\s*' + MODS + r'Sub\s+\w+', re.I), 'Sub'),
    (re.compile(r'^\s*' + MODS + r'Function\s+\w+', re.I), 'Function'),
    (re.compile(r'^\s*' + MODS + r'Property\s+\w+', re.I), 'Property'),
    (re.compile(r'^\s*If\b', re.I), 'If'),
    (re.compile(r'^\s*With\b', re.I), 'With'),
    (re.compile(r'^\s*For\b', re.I), 'For'),
    (re.compile(r'^\s*Do\b', re.I), 'Do'),
    (re.compile(r'^\s*Try\b', re.I), 'Try'),
    (re.compile(r'^\s*Select\s+Case\b', re.I), 'Select'),
    (re.compile(r'^\s*#Region\b', re.I), 'Region'),
    (re.compile(r'^\s*#ExternalSource\b', re.I), 'ExternalSource'),
]

CLOSERS = {
    re.compile(r'^\s*End\s+Sub\b', re.I): 'Sub',
    re.compile(r'^\s*End\s+Function\b', re.I): 'Function',
    re.compile(r'^\s*End\s+Property\b', re.I): 'Property',
    re.compile(r'^\s*End\s+If\b', re.I): 'If',
    re.compile(r'^\s*End\s+With\b', re.I): 'With',
    re.compile(r'^\s*Next\b', re.I): 'For',
    re.compile(r'^\s*Loop\b', re.I): 'Do',
    re.compile(r'^\s*End\s+Try\b', re.I): 'Try',
    re.compile(r'^\s*End\s+Select\b', re.I): 'Select',
    re.compile(r'^\s*#End\s+Region\b', re.I): 'Region',
    re.compile(r'^\s*#End\s+ExternalSource\b', re.I): 'ExternalSource',
}


def main():
    path = sys.argv[1]
    with open(path, encoding='utf-8-sig', errors='replace') as fh:
        lines = fh.read().split('\n')

    stack = []
    for i, raw in enumerate(lines, 1):
        line = raw.rstrip()
        s = line.strip()
        if not s or s.startswith("'"):
            continue
        # strip trailing comment
        code = s.split("'")[0].strip()
        if not code:
            continue

        closed = None
        for rx, kind in CLOSERS.items():
            if rx.match(code):
                closed = kind
                break
        if closed:
            if stack and stack[-1][0] == closed:
                stack.pop()
            else:
                expected = stack[-1][0] if stack else '(none)'
                print('%s:%d: unexpected End %s (expected %s) | %s'
                      % (path, i, closed, expected, code[:70]))
                # resync: pop until match
                for j in range(len(stack) - 1, -1, -1):
                    if stack[j][0] == closed:
                        del stack[j:]
                        break
            continue

        for rx, kind in OPENERS:
            if rx.match(code):
                if kind == 'If':
                    # a block If opens only when the statement ends with Then
                    # (the condition may continue on the next line)
                    stmt = code
                    j = i
                    while not re.search(r'\bThen\b', stmt, re.I) and j < len(lines):
                        j += 1
                        stmt += ' ' + lines[j - 1].split("'")[0].strip()
                    if not re.search(r'\bThen\b', stmt, re.I):
                        break
                    # single-line If ... Then <stmt> does not open a block
                    after = re.split(r'\bThen\b', stmt, flags=re.I)[1].strip()
                    if after:
                        break
                stack.append((kind, i, code[:60]))
                break

    print('--- still open at EOF ---')
    for kind, i, code in stack:
        print('  %s opened at line %d: %s' % (kind, i, code))


if __name__ == '__main__':
    main()
