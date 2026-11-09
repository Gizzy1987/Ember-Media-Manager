#!/usr/bin/env python3
"""Check that every resource key referenced from the designers exists in the
matching .resx (and that every resx key is used)."""
import os
import re
import sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))

PAIRS = [
    ('EmberMediaManager/dlgAbout.resx', 'EmberMediaManager/dlgAbout.Designer.vb'),
    ('EmberMediaManager/My Project/Resources.resx',
     'EmberMediaManager/My Project/Resources.Designer.vb'),
]

GET_RE = re.compile(r'ResourceManager\.Get(?:String|Object)\("([^"]+)"')
KEY_RE = re.compile(r'<data name="([^"]+)"')


def main():
    for resx, designer in PAIRS:
        rp = os.path.join(ROOT, resx)
        dp = os.path.join(ROOT, designer)
        if not os.path.exists(rp):
            print('missing resx:', resx)
            continue
        with open(rp, encoding='utf-8-sig', errors='replace') as fh:
            keys = set(KEY_RE.findall(fh.read()))
        with open(dp, encoding='utf-8-sig', errors='replace') as fh:
            used = set(GET_RE.findall(fh.read()))
        missing = sorted(used - keys)
        unused = sorted(k for k in keys - used
                        if not k.startswith('$this') and k not in ('Name1', 'Color1'))
        print('%s' % resx)
        print('   referenced but missing : %s' % (missing or '-'))
        print('   present but unreferenced: %s' % (unused or '-'))


if __name__ == '__main__':
    main()
