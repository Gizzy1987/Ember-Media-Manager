#!/usr/bin/env python3
"""Inspect the episodes of a show in an Ember database."""
import sqlite3
import sys

db = sys.argv[1]
needle = sys.argv[2] if len(sys.argv) > 2 else '%'
con = sqlite3.connect('file:%s?mode=ro' % db.replace('\\', '/'), uri=True)
cur = con.cursor()

cur.execute("SELECT name FROM sqlite_master WHERE type='table'")
tables = [r[0] for r in cur.fetchall()]
print('Tabellen:', ', '.join(tables))

q = """
SELECT e.idEpisode, e.idShow, e.Season, e.Episode, e.Title,
       f.strFilename
FROM episode e LEFT JOIN files f ON f.idFile = e.idFile
WHERE e.Title LIKE ? OR e.idShow = ?
ORDER BY e.Season, e.Episode
"""
cur.execute(q, (needle, int(sys.argv[3]) if len(sys.argv) > 3 else 0))
rows = cur.fetchall()
print('%-9s %-6s %-6s %-38s %s' % ('idEpisode', 'Season', 'Episode', 'Title', 'Datei'))
for r in rows:
    print('%-9s %-6s %-6s %-38s %s' % (r[0], r[2], r[3], (r[4] or '')[:38], (r[5] or '')[-60:]))
print('%d Episoden' % len(rows))
con.close()
