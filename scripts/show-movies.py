#!/usr/bin/env python3
"""Show the movies of an Ember database with path, title, ids and nfo state."""
import sqlite3
import sys

db = sys.argv[1]
con = sqlite3.connect('file:%s?mode=ro' % db.replace('\\', '/'), uri=True)
cur = con.cursor()

SQL = (
    "SELECT m.idMovie, m.Title, m.MoviePath, m.Year, m.New, m.Mark, m.NfoPath, "
    "(SELECT GROUP_CONCAT(type || '=' || value) FROM uniqueid u "
    "  WHERE u.media_id = m.idMovie AND u.media_type = 'movie') AS IDs "
    "FROM movie m ORDER BY m.Title"
)
cur.execute(SQL)
for r in cur.fetchall():
    print('Film %s  "%s" (%s)  New=%s Mark=%s' % (r[0], r[1], r[3], r[4], r[5]))
    print('    Pfad : %s' % r[2])
    print('    NFO  : %s' % (r[6] or '(keine)'))
    print('    IDs  : %s' % (r[7] or '(keine)'))
con.close()
