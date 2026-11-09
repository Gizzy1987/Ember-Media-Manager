#!/usr/bin/env python3
"""Print the full SQL of the list views."""
import io
import re
import sqlite3
import sys

db = sys.argv[1]
con = sqlite3.connect('file:%s?mode=ro' % db.replace('\\', '/'), uri=True)
cur = con.cursor()
cur.execute("SELECT name, sql FROM sqlite_master WHERE type='view' ORDER BY name")
for name, sql in cur.fetchall():
    print('=' * 90)
    print(name)
    print(re.sub(r'\s+', ' ', sql or '').strip())
con.close()
