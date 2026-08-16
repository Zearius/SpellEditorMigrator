# AzerothCore Spell Migration Tool

A standalone Windows tool that migrates custom spells created in **SpellEditor**
into AzerothCore's `spell_dbc` table — reading directly from SpellEditor's
MySQL database, validating the record, mapping it to AzerothCore's column
names, and writing it to your `acore_world` database.

This replaces the manual workflow of exporting a SQL insert from SpellEditor,
hand-editing column names, and pasting it into a query tool — a process that's
easy to get subtly wrong (dropped values, mismatched columns) in ways that
don't show up until you're testing in-game.

## Prerequisites

- **[SpellEditor](https://github.com/stoneharry/WoW-Spell-Editor)** by
  stoneharry, set up and configured to use a **MySQL** database (not SQLite).
  This tool reads directly from that database, so SpellEditor must already be
  pointed at a MySQL instance you can reach.
- **An AzerothCore server** with a running `acore_world` database (or
  equivalently named world database) that you have write access to.
- **Windows 10/11 (x64)**. This is a WPF application and Windows-only.

## Installation

1. Download the latest release zip from the
   [Releases](../../releases) page.
2. Extract the **entire contents** of the zip to a folder of your choice —
   do not run the `.exe` from inside the zip, and do not move the `.exe` out
   of its folder on its own. WPF applications ship with a handful of native
   (non-.NET) DLLs alongside the executable that must stay in the same
   folder for it to run.
3. Run the `.exe`. No installer, no admin rights required.

## First-time setup

The tool needs to know how to reach two separate MySQL databases:

1. **SpellEditor's database** — where your custom spells live as you create
   them.
2. **AzerothCore's world database** (`acore_world`) — where the finished
   `spell_dbc` records get written.

To configure both:

1. Launch the tool.
2. Go to **File → Settings...**
3. Fill in the connection details for **both** databases:
   - Server / Port
   - Database name (SpellEditor's schema name is whatever you named it when
     you set it up — it is *not* assumed to be any particular name)
   - Table name (defaults are pre-filled: `spell` for SpellEditor,
     `spell_dbc` for AzerothCore — change these if your setup differs)
   - Username / Password
4. Click **Test SpellEditor** and **Test AzerothCore** to confirm each
   connection works before saving.
5. Click **Save**.

Your settings are stored under your Windows user profile
(`%AppData%\SpellMigrationTool\settings.json`), not next to the application —
this means they persist across updates and are never touched by the zip
extraction step. Your password is encrypted on disk using Windows' built-in
credential protection (DPAPI) and is never stored in plain text.

## Using the tool

1. **Build your spell in SpellEditor** as normal, and note its Spell ID.
2. **Switch to the migration tool.**
3. Enter the **Spell ID** in the main window and click **Fetch & Validate**.
4. Review the **validation results**:
   - **Errors** mean something is structurally wrong with the source record
     (most commonly, a malformed export) and must be fixed in SpellEditor
     before you can proceed — the tool will not generate a preview if there
     are errors.
   - **Warnings** are things worth a second look but won't block you —
     for example, a proc-chance spell that isn't flagged as passive, or an
     effect that triggers another spell ID (worth confirming that spell
     actually exists before applying).
5. Review the **generated `spell_dbc` INSERT statement** shown in the
   preview pane. This is exactly what will be written to `acore_world` — take
   a moment to sanity-check it, especially on a spell type you haven't
   migrated before.
6. Click **Apply to AzerothCore** to write the record. If a spell with that
   ID already exists in `spell_dbc`, you'll be warned before proceeding.
7. Build your client-side MPQ patch from SpellEditor's DBC export as you
   normally would, restart your worldserver (or `.reload spell_dbc` if your
   core supports it), and test in-game.

Click **Clear** at any point to reset the ID field, validation results, and
preview without closing the tool.

## What this tool supports

The SpellEditor → `spell_dbc` column mapping has been tested and confirmed
across the following spell archetypes:

- Direct damage spells
- Damage-over-time (DoT) effects
- Direct heal spells
- Heal-over-time (HoT) effects
- Ground-targeted AoE / channeled zone effects (Blizzard-style, including
  spells that trigger a separate damage sub-spell on tick)
- Passive auras with a chance to proc a triggered spell

## What this tool does *not* do

- **Custom scripted spell behavior** (e.g. an Ignite-style effect where the
  triggered spell's value is dynamically calculated from the damage that
  procced it) requires a C++ `AuraScript` registered via
  `spell_script_names` and a server rebuild. This is genuinely outside the
  scope of a data-migration tool — see AzerothCore's own custom script
  documentation for that workflow.
- **Client-side DBC/MPQ patching.** This tool only writes to `acore_world`.
  You still need to build and apply your client patch from SpellEditor's own
  DBC export separately — the client and server must both know about the
  spell.
- **Overwriting existing spells.** If a spell ID already exists in
  `spell_dbc`, the tool will warn you but will not currently perform an
  update/replace. Use a new ID, or manually remove the existing row first.
- **Batch migration of multiple spells at once**, in this version — one
  spell ID at a time.

## Troubleshooting

**"Failed to connect to SpellEditor DB"** — double-check Server/Port/
Database/Username/Password in Settings, and confirm SpellEditor is actually
configured to use MySQL (not its default local/SQLite storage).

**Validation shows an Error about column count** — this generally means the
source row in SpellEditor's database is incomplete or was written by a tool
that didn't populate every column. Try re-saving the spell in SpellEditor.

**Spell casts but has no visible effect (e.g. "0 damage")** — this is
usually not a mapping problem; check the Duration and Effect configuration
in SpellEditor itself. A stray, half-configured second effect slot (Effect
type set but no target/base points) is a common culprit and will show as a
Warning during validation.

**A proc/triggered spell doesn't seem to fire** — confirm the spell ID
referenced by `EffectTriggerSpell` actually exists in `acore_world`. The
validator will warn you about this, but can't verify existence on its own.

## Contributing / feedback

Issues and pull requests are welcome. This tool was built and tested
against real custom spells across every major archetype listed above, but
the AzerothCore spell system is large — if you find a spell type that maps
incorrectly, please open an issue with the SpellEditor export and the
generated output so it can be reproduced.

## License

MIT — see [LICENSE](LICENSE).
