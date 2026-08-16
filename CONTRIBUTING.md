# Contributing

Thanks for your interest in improving this tool. A few ground rules before
you open a PR.

## Review policy

**All pull requests require maintainer review before merge — no exceptions,
including for trivial changes.** `main` is protected: nothing merges
without an approved review, and previously approved PRs are automatically
re-reviewed if new commits are pushed after approval. This applies to the
maintainer too.

This isn't about not trusting contributors — it's standard practice for any
tool that reads from and writes to someone's live database.

## Before you open a PR

- **Open an issue first for anything non-trivial** (new features, changes
  to the column mapping, changes to validation rules) so we can agree on
  the approach before you put in the work.
- **Small, focused PRs** are much easier to review than large ones. If
  your change touches multiple unrelated things, consider splitting it up.
- **Explain your testing.** If you added or changed spell mapping/
  validation logic, say what spell(s) you tested it against and what you
  observed in-game, if applicable. This project's mapping was built and
  verified against real, hand-tested spells across every supported
  archetype (direct damage, DoT, direct heal, HoT, AoE/channeled, proc
  passives) — new changes should hold to that same bar.

## Areas that get extra scrutiny

Given what this tool does, PRs touching the following get a closer look
before merge, regardless of how small the diff looks:

- `AcoreWorldRepository.cs` and anything that builds or executes SQL
- Connection-string handling or credential storage (`SettingsStore`,
  `DatabaseConnectionSettings`)
- `ColumnMap.cs` — the SpellEditor ↔ `spell_dbc` column mapping is
  verified against real tested spells; changes here need a clear
  explanation of what was wrong and how the change was confirmed
- Any GitHub Actions workflow files
- New NuGet package dependencies

None of this is meant to slow down genuine contributions — it's meant to
keep a tool that touches people's live game databases trustworthy for
everyone who uses it.

## Reporting a bug

Open an issue with:
- The spell ID and its SpellEditor export (or a description of the spell's
  effects/type)
- What you expected vs. what actually happened
- Any validation warnings/errors the tool showed

## Code style

Match the existing style in the file you're editing. `SpellMigration.Core`
has no UI dependencies and should stay that way — if your change needs UI
types in `Core`, that's a sign it belongs in `SpellMigration.UI` instead.
