#!/usr/bin/env python3
"""Show the module (scraper) order and enabled state of an Ember profile."""
import io
import re
import sys

path = sys.argv[1]
with io.open(path, encoding='utf-8', errors='replace') as fh:
    text = fh.read()

block_re = re.compile(r'<Module>(.*?)</Module>', re.S)
for m in block_re.finditer(text):
    b = m.group(1)
    name = re.search(r'<AssemblyName>([^<]*)</AssemblyName>', b)
    ctype = re.search(r'<ContentType>([^<]*)</ContentType>', b)
    order = re.search(r'<ModuleOrder>([^<]*)</ModuleOrder>', b)
    enabled = re.search(r'<(\w*Enabled)>([^<]*)</\1>', b)
    if not name:
        continue
    print('%-38s %-10s order=%-4s %s' % (
        name.group(1), ctype.group(1) if ctype else '?',
        order.group(1) if order else '-',
        (enabled.group(1) + '=' + enabled.group(2)) if enabled else ''))
