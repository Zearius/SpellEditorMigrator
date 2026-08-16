# Third-Party Attributions

This project's own source code is released into the public domain (see
`LICENSE`). The following third-party materials are **not** covered by
that dedication and remain the property of their respective owners.

## Application icon

The application icon is sourced from the
[AzerothCore](https://github.com/azerothcore) project
(`worldserver.ico`). AzerothCore's own repositories are released under
the GNU GPL-2.0 (older core components) and GNU AGPL-3.0 (newer
components) — see [azerothcore.org](https://www.azerothcore.org/) and
the [AzerothCore GitHub organization](https://github.com/azerothcore)
for details.

If you are the copyright holder of this icon and have concerns about
its inclusion here, please open an issue on this repository and it
will be removed or replaced promptly.

## Database schema / column names

This tool's column mapping between SpellEditor's `spell` table and
AzerothCore's `spell_dbc` table is derived from the publicly
documented schema of the [AzerothCore](https://github.com/azerothcore)
project. No AzerothCore source code is included or redistributed by
this tool — only the (factual, non-copyrightable) mapping between
column names required for the two databases to interoperate.

## SpellEditor

This tool is designed to be used alongside, and reads from the MySQL
database of,
[stoneharry/WoW-Spell-Editor](https://github.com/stoneharry/WoW-Spell-Editor).
SpellEditor is a separate, independent project and is not included,
bundled, or redistributed with this tool — it must be installed and
configured separately. See that project's own repository for its
license terms.
