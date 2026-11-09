#!/usr/bin/env python3
"""Show the source table of an Ember database."""
import sqlite3
import sys

db = sys.argv[1]
con = sqlite3.connect('file:%s?mode=ro' % db.replace('\\', '/'), uri=True)
cur = con.cursor()
cur.execute("SELECT name FROM sqlite_master WHERE type='table' AND name LIKE '%source%'")
tables = [r[0] for r in cur.fetchall()]
print('Quell-Tabellen:', tables)
for t in tables:
    cur.execute('SELECT COUNT(*) FROM %s' % t)
    print('%s: %d Zeilen' % (t, cur.fetchone()[0]))
    cur.execute('SELECT * FROM %s LIMIT 5' % t)
    cols = [d[0] for d in cur.description]
    for row in cur.fetchall():
        print('   ', dict(zip(cols, row)))
con.close()
