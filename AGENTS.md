# AGENTS.md

Operating instructions for any agent working in `E:\Projects\Cyriller\Cyriller`.

## What this repository is

Cyriller is an open-source C# library that puts Russian words into all six grammatical cases:

- **nouns and adjectives** from a large dictionary (≈125k noun entries, ≈43k adjective entries),
  with a nearest-match fallback for words the dictionary does not contain;
- **numbers** spelled out, including money (`CyrNumber.Currency`: roubles, dollars, euros, yuan)
  and an amount of a unit (`CyrNumber.Item`);
- **phrases** of adjective + noun (`CyrPhrase`);
- **personal names** — surname, given name, patronymic — by rule, without a dictionary
  (`CyrName`).

It is a fork of `miyconst/Cyriller`; `origin` is `git@github.com:shogun-kub/Cyriller.git`.
License LGPL-3.0 ([`LICENSE`](LICENSE)), NuGet package `Miyconst.Cyriller` 0.2.0, main solution
[`Cyriller.sln`](Cyriller.sln).

The **library** is the deliverable. `Cyriller.Samples`, `Cyriller.Checker`, `Cyriller.Web` and
`Cyriller.Desktop` are demos around it, `Cyriller.Tests` proves behaviour,
`Cyriller.DictionaryBuilder` regenerates the dictionaries and `Cyriller.Zipper` is a build tool.
[`PROJECT_STRUCTURE.md`](PROJECT_STRUCTURE.md) maps every project, file and data format — read it
before changing code you have not read.

`Test_data/` holds read-only sample corpora for name, noun and phrase testing — surname and name
lists, Petrovich fixtures, SynTagRus treebanks and a Wiktionary extract. It is outside the
solution, is not tracked by git, and must never be edited.

## Current state — 2026-11-13

- **The solution builds and the tests pass on .NET 10.** `util-scripts/build.ps1` and
  `util-scripts/test.ps1` run clean inside the sandbox (no elevated access): 70 passing tests.
  The library now targets `net10.0` and `netstandard2.1`; Samples, Zipper, DictionaryBuilder,
  Tests, Web and Desktop are `net10.0`; Checker is `net10.0-windows` (WinForms).
- External corpora in `Test_data/` (Petrovich, SynTagRus, the Wiktionary extract) are now used by
  `PetrovichTests`, `UdNounTests`, `KaikkiNounTests` and `NameDeclensionTests`; each test skips
  silently when its data is absent.
- A surname/name bug was fixed on the way: feminine and masculine names in `-ша` took `-ы` in the
  genitive (`Машы`) instead of `-и` (`Маши`); see `NameEndingInShaTakesИInGenitive`.
- `CyrNounCollection.GetOrDefault` gained an `out GendersEnum gender` overload (and `Get` has a
  matching one), so callers can tell which gender the dictionary resolved; the three-argument
  overloads are kept for compatibility.
- The EF6/EDMX model and the SQL Server test were removed: Checker and Tests no longer need a
  database. Tests are data-driven against the embedded dictionaries and `Test_data/`.
- The T4 templates were replaced by `Cyriller.DictionaryBuilder`; it reproduces
  `App_Data/nouns.txt` and `adjectives.txt` from the JSON files byte-for-byte.
- `.gitignore` still carries the intentional `/Test_data` line. `AGENTS.md` and
  `PROJECT_STRUCTURE.md` are tracked now.

## Directory contract

| Path | Role | Rule |
| --- | --- | --- |
| [`Cyriller/`](Cyriller) | The library, namespace `Cyriller` | All public behaviour lives here; keep the API source-compatible |
| [`Cyriller.Model/`](Cyriller.Model) | Public enums and dictionary JSON DTOs | No logic beyond validation |
| [`Cyriller.Rule/`](Cyriller.Rule) | Turns dictionary entries into rule strings | Used by `Cyriller.DictionaryBuilder`, not at run time |
| [`Cyriller.Zipper/`](Cyriller.Zipper) | GZip packer for `App_Data/*.txt` → `*.gz` | Invoked by the build; build it first |
| [`Cyriller.DictionaryBuilder/`](Cyriller.DictionaryBuilder) | Regenerates `App_Data/*.txt` from the JSON dictionaries | Replaced the old T4 templates |
| [`Cyriller.Samples/`](Cyriller.Samples) | Console demo of every API area | Keep it working; it is the smoke test |
| [`Cyriller.Tests/`](Cyriller.Tests) | xunit tests | Add a test with every behaviour change |
| [`Cyriller.Checker/`](Cyriller.Checker) | WinForms checker (`net10.0-windows`); also the JSON dictionary exporter | Needs no database |
| [`Cyriller.Web/`](Cyriller.Web) | ASP.NET Core MVC demo site | EPPlus 4.5 handles Excel import/export |
| [`Cyriller.Desktop/`](Cyriller.Desktop) | Avalonia 11 desktop app (`net10.0`) | Builds and starts on this machine |
| [`packages/`](packages) | Legacy old-style NuGet folder, no longer used | Ignored by git; do not edit |
| [`Test_data/`](Test_data) | Read-only sample corpora | **Never modify, move, rename or delete**; ignored by git |

## Hard rules

1. **`Test_data/` is evidence, not a maintained input.** Read it, sample from it, hash it —
   never write into it, never `git add` it, never clean it.
2. **Never run `git clean -xdf`, `git clean -Xdf` or delete ignored paths.** `packages/`,
   `Test_data/` (with its nested clone) and `.vs/` exist only on this machine and cannot be
   restored from the remote.
3. **Do not edit generated dictionaries.** [`Cyriller/App_Data/nouns.txt`](Cyriller/App_Data/nouns.txt)
   and [`adjectives.txt`](Cyriller/App_Data/adjectives.txt) are generated by
   [`Cyriller.DictionaryBuilder`](Cyriller.DictionaryBuilder) from `nouns.json`/`adjectives.json`;
   `nouns.gz`/`adjectives.gz` are packed by `Cyriller.Zipper` during the build.
4. **A build repacks the two tracked `.gz` files.** `git status` then shows
   `M Cyriller/App_Data/nouns.gz` and `M Cyriller/App_Data/adjectives.gz` although the data did
   not change (same size, different bytes). Revert them; never commit a repack.
5. **Keep the public API and the package targets.** `Cyriller`, `Cyriller.Model` and
   `Cyriller.Rule` target `net10.0` **and** `netstandard2.1` and are consumed from NuGet. A public
   behaviour change needs a test; an API change needs a version bump in the csproj, an update to
   [`README.md`](README.md), and a line in `PackageReleaseNotes`.
6. **Prove declension changes with a test.** The rule tables in `CyrName` and the dictionaries are
   chosen by hand; a wrong ending is a visible bug, and the same algorithm is duplicated in
   [`CyrName.js`](CyrName.js).
7. **Keep encodings and line endings.** The tree is UTF-8 with CRLF everywhere; hand-written C#
   and project files carry a BOM, generated ones (some XAML/Razor) do not. `README.md`
   and `.gitignore` are UTF-8 without BOM. Do not let a tool normalise either.
8. **`е` and `ё` are interchangeable in dictionary search** ([`CyrData.Compare`](Cyriller/CyrData.cs))
   and dictionary lookups are case-sensitive by word case. Do not change either without tests.
9. **Add dependencies deliberately.** Restores need an approved `danger-full-access` run; keep
   versions pinned in the csproj files.
10. **Ask before anything irreversible on user files.** Deleting or overwriting a source file,
    rewriting history, or touching `Test_data/` needs explicit approval.

## Environment

| Fact | Value |
| --- | --- |
| Host | Windows 10+ x64, NTFS, PowerShell `7.6.10` (`pwsh`) |
| Git | `2.49.0.windows.1`; branch `master` at `3657f9e`, level with `origin/master`; `core.autocrlf=true` |
| .NET SDK | `10.0.404` (default), also `10.0.115` and `9.0.321`; runtimes 3.1.32 … 10.0.15. All projects target `net10.0` or `netstandard2.1` |
| Visual Studio | Community 2022, Build Tools 2022, Community 2026 (MSBuild `18.11.1`); no longer required for the solution |
| Network | Blocked inside the sandbox; reachable only from an approved `danger-full-access` run, which package restore needs |
| NuGet cache | `C:\Users\shogu\.nuget\packages`; Avalonia 11, EPPlus 4.5.3.2 and xunit 2.9.3 were restored from nuget.org |
| File sandbox | `workspace-write` covers the whole tree: builds and tests run confined through `util-scripts/build.ps1` and `util-scripts/test.ps1`. `danger-full-access` is needed only when a restore has to reach nuget.org (a package version missing from the cache) |
| ACL record | `E:\Projects\Cyriller\.acl-recovery\` keeps DACL backups, the low-label script and its log; leave it in place |

## Building

Build and test run inside the sandbox; elevated access is not needed. Two sandbox rules shape the
commands:

- MSBuild worker nodes talk over named pipes, which the sandbox forbids, so a multi-node build
  exits with code 1 and «Ошибок: 0». Always pass `-m:1`.
- `dotnet test` on the solution hangs for the same reason, and the Avalonia build task writes its
  telemetry log outside the workspace; [`Directory.Build.targets`](Directory.Build.targets) turns
  that target off.

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File util-scripts/build.ps1
pwsh -NoProfile -ExecutionPolicy Bypass -File util-scripts/test.ps1
dotnet run --project Cyriller.Samples
dotnet run --project Cyriller.Web --urls http://127.0.0.1:5199
dotnet run --project Cyriller.Desktop
```

The scripts are thin wrappers around `dotnet build Cyriller.sln -m:1` and a per-project
`dotnet test --no-build --no-restore`; `-NoRestore`, `-NoBuild`, `-Configuration` and `-Filter` are
passed through. A restore that pulls new packages needs an approved `danger-full-access` run.

- `Cyriller.Zipper` must be built before `Cyriller` on a clean tree: the library's PreBuild target
  packs `App_Data/*.txt` into `*.gz` with `dotnet run --no-build --project Cyriller.Zipper`.
- Release paths: [`Cyriller/pack.bat`](Cyriller/pack.bat) packs `Miyconst.Cyriller` 0.2.0;
  [`Cyriller.Desktop/publish.bat`](Cyriller.Desktop/publish.bat) publishes a single-file
  `win-x64` build.

## Testing

- [`Cyriller.Tests/CyrNameTests.cs`](Cyriller.Tests/CyrNameTests.cs) — full-name declension,
  shortened forms, patronymic suffixes, hyphenated surnames, names in `-ша`.
- [`Cyriller.Tests/CyrRuleTests.cs`](Cyriller.Tests/CyrRuleTests.cs) — `CyrRule.Apply`/`Revert`.
- [`Cyriller.Tests/DictionaryTests.cs`](Cyriller.Tests/DictionaryTests.cs) — dictionary lookups,
  gender resolution, similar-word fallback, adjectives, number spelling.
- [`Cyriller.Tests/SurnameTests.cs`](Cyriller.Tests/SurnameTests.cs) — surname endings plus a
  smoke sweep over `Test_data/russian_surnames.csv`; it passes silently when `Test_data` is
  absent and skips rows with an embedded ESC byte.
- [`Cyriller.Tests/PetrovichTests.cs`](Cyriller.Tests/PetrovichTests.cs) — full-name declension
  against [Petrovich](https://github.com/petrovich) fixtures (MIT); skips without them.
- [`Cyriller.Tests/UdNounTests.cs`](Cyriller.Tests/UdNounTests.cs) — noun forms from the
  SynTagRus UD treebank (CC BY-NC-SA 4.0), up to 4000 tokens.
- [`Cyriller.Tests/KaikkiNounTests.cs`](Cyriller.Tests/KaikkiNounTests.cs) — noun paradigms from
  the Wiktionary extract (CC BY-SA 4.0), up to 1200 lemmas from the start of the file.
- [`Cyriller.Tests/NameDeclensionTests.cs`](Cyriller.Tests/NameDeclensionTests.cs) — 5000 sampled
  «фамилия + имя» pairs (2500 male, 2500 female, distinct combinations) from the CSV and the name
  lists, checked against the declensions attested in SynTagRus; the seed is fixed, so the sample
  is reproducible.
- [`Cyriller.Tests/CorpusFiles.cs`](Cyriller.Tests/CorpusFiles.cs) — locates `Test_data`, shares
  one noun collection and normalises stress marks and `ё`.
- 70 tests, xunit 2.9.3 on `net10.0`, no database and no network. The four corpus tests skip
  silently when their files are missing and allow a small mismatch budget (2–10 %), because the
  external corpora contain dialectal, obsolete and suppletive forms the dictionaries regularise.
  Run them with `pwsh -NoProfile -ExecutionPolicy Bypass -File util-scripts/test.ps1`.

## Workflow

1. **State** — `git status`, then `pwsh -NoProfile -ExecutionPolicy Bypass -File util-scripts/test.ps1`
   to confirm the tree is healthy.
2. **Read** — the class, its dictionary format and its tests before editing. `CyrName.cs` is 2274
   lines of per-case rule tables; read the group you touch.
3. **Change** — the smallest edit that fixes the behaviour, in the existing style.
4. **Test** — add or extend an xunit fact that would have failed before the change.
5. **Verify** — build what can be built, record the exact command and its output. A negative
   result is a finding: write down what failed and why.
6. **Document** — public behaviour changes update [`README.md`](README.md) (Russian); package
   releases add `PackageReleaseNotes` in [`Cyriller/Cyriller.csproj`](Cyriller/Cyriller.csproj).

## Using the test data

Both sources are read-only samples, deliberately **not** fixtures: they are input to check
declension quality against, not expected outputs to replay.

| Path | Contents |
| --- | --- |
| [`Test_data/russian_surnames.csv`](Test_data/russian_surnames.csv) | 9,498,549 B, UTF-8 without BOM, LF, header `surname_cyrl,surname_latn,gender` then 318,479 rows: Cyrillic surname, Latin transliteration, gender `m`/`f`/`u` (123,401 male, 123,923 female, 71,155 unisex) |
| [`Test_data/ru-pnames-list/`](Test_data/ru-pnames-list) | Git clone of `Raven-SL/ru-pnames-list` at `48ff79f`: [`lists/male_names_rus.txt`](Test_data/ru-pnames-list/lists/male_names_rus.txt) 736, [`lists/female_names_rus.txt`](Test_data/ru-pnames-list/lists/female_names_rus.txt) 938, [`lists/male_surnames_rus.txt`](Test_data/ru-pnames-list/lists/male_surnames_rus.txt) 14,651 lines — CRLF, UTF-8 without BOM, one name per line, no duplicates |
| `Test_data/petrovich-rules/` | Petrovich rule tables (MIT): `rules.json` 21 KB, `gender.json` 12 KB; rules only, no expected forms |
| `Test_data/petrovich-net/` | .NET port NPetrovich; `src/NPetrovich.Tests/Data/*.csv` holds 55 expected name forms (`FirstNames` 14, `MiddleNames` 6, `LastNames` 40 lines) |
| `Test_data/petrovich.spec.js` | 130 lines with 18 `expect(p.male.first.genitive(...))` style expectations from `petrovich-js` |
| `Test_data/UD/` | Five SynTagRus `.conllu` files, 141 MB total: dev 14.3 MB, test 14.6 MB, train-a/b/c 39.5/41.5/31.8 MB; 87,337 sentences and 1,515,559 tokens, UD 2.19, **CC BY-NC-SA 4.0** |
| `Test_data/ru-extract.jsonl` | 2.5 GB raw [wiktextract](https://kaikki.org/) extract of the *Russian Wiktionary*: records for **all languages**, so filter `lang_code == "ru"`; Russian nouns carry full declension tables with stress marks in `forms`; CC BY-SA 4.0 |

The name lists carry names only — no cases, genders or expected forms (see
[`readme.md`](Test_data/ru-pnames-list/readme.md)) — so they support crash-freedom and
"is the form plausible" sweeps, not assertions without a source of truth. The CSV does carry
gender, which makes it the better basis for masculine/feminine/unisex surname checks.

The corpus files are large and licensed, so they stay out of git (`/Test_data` is ignored).
Never commit them, and keep their licences in mind: SynTagRus is non-commercial, Wiktionary
extracts are share-alike.

## Language

| Where | Language |
| --- | --- |
| Chat replies | Russian; the owner switches between Russian and English, sometimes inside one message, so never ask which to use |
| `README.md`, XML doc comments, code comments, UI strings | **Russian**, matching the existing text |
| Identifiers, type names, tool and API names | English |
| Commit messages | follow the history — mostly English, short imperative subject |

## Style

- Markdown with relative links inside the repo; absolute paths only when quoting a host location.
- Dates as `YYYY-MM-DD`; quote commands verbatim and say what they printed.
- Tables for inventories, lists for reasoning; do not reformat files you did not change.
- Keep credentials and personal data out of tracked files. Do not add logins or connection
  strings anywhere.
