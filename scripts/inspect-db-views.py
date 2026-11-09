#!/usr/bin/env python3
"""Inspect the list views of an Ember database (read only)."""
import sqlite3
import sys

db = sys.argv[1]
con = sqlite3.connect('file:%s?mode=ro' % db.replace('\\', '/'), uri=True)
cur = con.cursor()

for name in ('movielist', 'tvshowlist', 'setslist'):
    cur.execute("SELECT sql FROM sqlite_master WHERE type='view' AND name=?", (name,))
    row = cur.fetchone()
    print('=' * 90)
    print('%s:' % name)
    print(row[0] if row else '  (View existiert nicht)')

print('=' * 90)
for table, col in (('tvshow', 'idShow'), ('movie', 'idMovie'), ('sets', 'idSet')):
    cur.execute('SELECT COUNT(*) FROM %s' % table)
    print('  %-10s Zeilen: %s' % (table, cur.fetchone()[0]))
    cur.execute('SELECT COUNT(*) FROM %s' % table + ' WHERE %s IS NULL' % col)
    print('  %-10s ohne %s: %s' % (table, col, cur.fetchone()[0]))

con.close()
