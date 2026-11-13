# PROJECT_STRUCTURE.md

A map of the Cyriller repository: what each project, file and data format is for, how the pieces
connect, and where a given kind of change belongs. It is a companion to
[`AGENTS.md`](AGENTS.md), which holds the operating rules; this file holds the layout.

Snapshot: `2026-11-13`, branch `master`, `HEAD` `3657f9e` plus the .NET 10 migration described
below. The whole solution targets `net10.0` (`netstandard2.1` for the library), and
`dotnet build Cyriller.sln` plus `util-scripts/test.ps1` (70 tests) pass inside the sandbox.

## 1. At a glance

| Fact | Value |
| --- | --- |
| Solution | [`Cyriller.sln`](Cyriller.sln), Visual Studio 16 format, `Debug|Any CPU` and `Release|Any CPU` |
| Projects | 10: the library, two supporting `net10.0`/`netstandard2.1` assemblies (Model, Rule), the gzip packer, the dictionary generator, a console demo, tests, and three demos (WinForms, ASP.NET Core MVC, Avalonia 11) |
| Language / license | C#, LGPL-3.0 ([`LICENSE`](LICENSE)) |
| Package | `Miyconst.Cyriller` 0.2.0, packed from [`Cyriller/Cyriller.csproj`](Cyriller/Cyriller.csproj) |
| Upstream | fork of `miyconst/Cyriller`; `origin` `git@github.com:shogun-kub/Cyriller.git` |
| Data | two dictionaries embedded as gzip resources: nouns and adjectives |

## 2. Top-level tree

```
E:\Projects\Cyriller\Cyriller
├─ Cyriller.sln                 # the 10 projects below
├─ README.md                    # user-facing documentation (Russian): build and API guide
├─ LICENSE                      # LGPL-3.0
├─ CyrName.js                   # 1839-line JavaScript port of the CyrName algorithm
├─ AGENTS.md                    # how an agent works in this repository
├─ PROJECT_STRUCTURE.md         # this file
├─ Directory.Build.props        # language level, deterministic build, sandbox switches
├─ Directory.Build.targets      # disables the Avalonia build-telemetry target (sandbox)
├─ util-scripts/                # build.ps1 and test.ps1: sandbox-safe build and test entry points
├─ .gitignore                   # VS ignores + bin/obj/packages + Temp/ + /Test_data
├─ .nuget/                      # NuGet.exe, NuGet.Config, NuGet.targets for the old-style projects
├─ packages/                    # old-style restored packages (ignored by git, present on this machine)
├─ Cyriller/                    # THE LIBRARY (namespace Cyriller)
│  ├─ *.cs                      # 22 source files (see §4)
│  ├─ App_Data/                 # dictionaries: .json (source) → .txt (DictionaryBuilder) → .gz
│  ├─ Cyriller.csproj           # net10.0 + netstandard2.1, Zipper PreBuild
│  └─ pack.bat                  # dotnet pack -c Release --force
├─ Cyriller.Model/              # public enums + dictionary JSON DTOs (9 .cs files)
├─ Cyriller.Rule/               # dictionary entry → rule string (3 .cs files)
├─ Cyriller.Zipper/             # gzip packer used by the library build (1 .cs file)
├─ Cyriller.Samples/            # console demo of every API area
├─ Cyriller.DictionaryBuilder/  # JSON → App_Data/*.txt generator (replaces the T4 templates)
├─ Cyriller.Tests/              # xunit 2.9 tests (net10.0), no database
├─ Cyriller.Checker/            # WinForms checker; its JsonForm exports nouns.json/adjectives.json
├─ Cyriller.Web/                # ASP.NET Core MVC demo site (net10.0)
├─ Cyriller.Desktop/            # Avalonia 11 MVVM desktop app (net10.0)
└─ Test_data/                   # READ-ONLY samples, ignored by git (see §14)
   ├─ russian_surnames.csv
   └─ ru-pnames-list/           # separate git clone (Raven-SL/ru-pnames-list)
```

## 3. Solution, targets and dependency graph

| Project | Path | Type | Target framework(s) | References | Restore |
| --- | --- | --- | --- | --- | --- |
| Cyriller | [`Cyriller/`](Cyriller) | class library | `net10.0;netstandard2.1` | Cyriller.Model, Cyriller.Rule; Cyriller.Zipper at build time | `PackageReference` |
| Cyriller.Model | [`Cyriller.Model/`](Cyriller.Model) | class library | `net10.0;netstandard2.1` | — | `PackageReference` |
| Cyriller.Rule | [`Cyriller.Rule/`](Cyriller.Rule) | class library | `net10.0;netstandard2.1` | Cyriller.Model | `PackageReference` |
| Cyriller.Zipper | [`Cyriller.Zipper/`](Cyriller.Zipper) | console (`Exe`) | `net10.0` | — | `PackageReference` |
| Cyriller.DictionaryBuilder | [`Cyriller.DictionaryBuilder/`](Cyriller.DictionaryBuilder) | console (`Exe`) | `net10.0` | Cyriller.Model, Cyriller.Rule | `PackageReference` |
| Cyriller.Samples | [`Cyriller.Samples/`](Cyriller.Samples) | console (`Exe`) | `net10.0` | Cyriller, Cyriller.Model, Cyriller.Rule | `PackageReference` |
| Cyriller.Tests | [`Cyriller.Tests/`](Cyriller.Tests) | xunit test project | `net10.0` | Cyriller, Cyriller.Model; xunit 2.9.3, Microsoft.NET.Test.Sdk 17.14.2 | `PackageReference` |
| Cyriller.Checker | [`Cyriller.Checker/`](Cyriller.Checker) | WinForms (`WinExe`) | `net10.0-windows` | Cyriller, Cyriller.Model; no package dependencies | `PackageReference` |
| Cyriller.Web | [`Cyriller.Web/`](Cyriller.Web) | ASP.NET Core MVC | `net10.0` | Cyriller, Cyriller.Model; EPPlus 4.5.3.2 | `PackageReference` |
| Cyriller.Desktop | [`Cyriller.Desktop/`](Cyriller.Desktop) | Avalonia (`WinExe`) | `net10.0` | Cyriller, Cyriller.Model, Cyriller.Rule; Avalonia 11.3.23 (+DataGrid 11.3.13, ReactiveUI 11.3.9), EPPlus 4.5.3.2, MS DI 10.0.0 | `PackageReference` |

```
                    ┌──────────────┐
                    │ Cyriller.Model│  enums + JSON DTOs
                    └──────┬───────┘
                 ┌─────────┴─────────┐
        ┌────────▼───────┐   ┌───────▼────────┐
        │ Cyriller.Rule  │   │   Cyriller     │◄── Cyriller.Zipper (build-time)
        └────────┬───────┘   └───┬────────┬───┘
                 └───────────────┘        │
                                          │
       ┌──────────────┬───────────────────┼──────────────┬───────────────┐
       │              │                   │              │               │
  Samples         Tests             Checker            Web         Desktop
```

Runtime data flows one way: the library reads its embedded gzip dictionaries through
`CyrData.GetData`, everything else consumes `Cyriller` classes.

## 4. The `Cyriller` library

Namespace `Cyriller`, 22 files, all under [`Cyriller/`](Cyriller).

### 4.1 Public surface by area

| Area | Types | Entry points |
| --- | --- | --- |
| Names (no dictionary) | `CyrName`, `CyrNameResult` | `Decline(surname, name, patronymic, …)`, `Decline(fullName, …)` in three shapes: one case (`CyrNameResult`), all cases (`CyrResult`), or raw `string[]` |
| Nouns | `CyrNounCollection`, `CyrNoun` | `Get`/`GetOrDefault` (exact or nearest match, with or without gender/case/number), `SelectNouns`, `noun.Decline()`, `noun.DeclinePlural()` |
| Adjectives | `CyrAdjectiveCollection`, `CyrAdjective` | `Get`/`GetOrDefault` (adds animateness), `SelectAdjectives`, `adjective.Decline(gender, animate)`, `DeclinePlural(animate)` |
| Numbers | `CyrNumber` (+ `.Currency`, `.Item`, `.Strings`) | `Decline(long|decimal, …)`, `ToString(…, case, gender, animate)`, `Decline(value, Currency)`, `Decline(value, Item)`, `Case(number, one, few, many)` |
| Phrases | `CyrPhrase` | `Decline(phrase, GetConditionsEnum)`, `DeclinePlural(...)`, `DetermineSpeechPart(word)` |
| Results | `CyrResult`, `CyrDeclineCase` | six cases with Russian and English names, `Get/Set`, `+`, `ToList/ToArray/ToDictionary`, 1-based indexer |
| Infrastructure | `CyrData`, `CyrRule`, `CyrBaseCollection`, `CyrBaseWord`, `Extensions`, `CyrWordNotFoundException` | internal plumbing plus the similarity search and the rule-string evaluator |

### 4.2 File catalogue

| File | Lines | Contents |
| --- | --- | --- |
| [`Cyriller/CyrName.cs`](Cyriller/CyrName.cs) | 2274 | The dictionary-free name algorithm; the largest file in the repo (see §4.3) |
| [`Cyriller/CyrNameResult.cs`](Cyriller/CyrNameResult.cs) | 81 | `Surname`/`Name`/`Patronymic`, `FullName`, value equality; accepts 2 or 3 parts |
| [`Cyriller/CyrNoun.cs`](Cyriller/CyrNoun.cs) | 166 | A noun: `Name`, `Gender`, `Animate`, `WordType`, 11 rules per word part; `Decline`, `DeclinePlural`, `SetName`, hyphenated compounds |
| [`Cyriller/CyrAdjective.cs`](Cyriller/CyrAdjective.cs) | 142 | An adjective: 23 rules per word; `Decline(gender, animate)`, `DeclinePlural(animate)`, `SetName` |
| [`Cyriller/CyrBaseWord.cs`](Cyriller/CyrBaseWord.cs) | 24 | Applies six `CyrRule`s to a name, returning `CyrResult` |
| [`Cyriller/CyrNounCollection.cs`](Cyriller/CyrNounCollection.cs) | 380 | Loads `nouns.gz`, builds the `DictionaryKey → CyrNoun` map, exact/similar lookup, caches |
| [`Cyriller/CyrNounCollection.DictionaryKey.cs`](Cyriller/CyrNounCollection.DictionaryKey.cs) | 48 | `(Name, Gender, Case, Number)` struct used as the dictionary key |
| [`Cyriller/CyrNounCollectionRow.cs`](Cyriller/CyrNounCollectionRow.cs) | 35 | Parses the `gender,animate,type,ruleIndex` tail of a dictionary line |
| [`Cyriller/CyrAdjectiveCollection.cs`](Cyriller/CyrAdjectiveCollection.cs) | 437 | Loads `adjectives.gz`, key adds animateness, exact/similar lookup, caches |
| [`Cyriller/CyrAdjectiveCollection.DictionaryKey.cs`](Cyriller/CyrAdjectiveCollection.DictionaryKey.cs) | 50 | `(Name, Gender, Case, Number, Animate)` key |
| [`Cyriller/CyrBaseCollection.cs`](Cyriller/CyrBaseCollection.cs) | 65 | Shared state: candidate list, similar-search cache, `EnableCache` |
| [`Cyriller/CyrData.cs`](Cyriller/CyrData.cs) | 242 | Embedded-resource reader and the suffix-weighted similarity search (`е`/`ё` equal) |
| [`Cyriller/CyrRule.cs`](Cyriller/CyrRule.cs) | 141 | Parses and applies a rule string (`ого2`, `ом`, `*`, empty), `Apply`/`Revert` |
| [`Cyriller/CyrDeclineCase.cs`](Cyriller/CyrDeclineCase.cs) | 276 | Static case metadata: `Case1`…`Case6`, English/Russian synonyms, question, index |
| [`Cyriller/CyrResult.cs`](Cyriller/CyrResult.cs) | 321 | Six-case value object: `Nominative`…`Prepositional`, Russian synonyms, `operator+`, conversions, 1-based `this[int]` |
| [`Cyriller/CyrNumber.cs`](Cyriller/CyrNumber.cs) | 391 | Number/money/unit declension; `MaxValue = 9 999 999 999` |
| [`Cyriller/CyrNumber.Currency.cs`](Cyriller/CyrNumber.Currency.cs) | 235 | `abstract Currency` + `RurCurrency`, `UsdCurrency`, `EurCurrency`, `YuanCurrency` |
| [`Cyriller/CyrNumber.Item.cs`](Cyriller/CyrNumber.Item.cs) | 159 | `Item` wraps a `CyrNoun` so a number agrees with a unit ("5 котов") |
| [`Cyriller/CyrNumber.Strings.cs`](Cyriller/CyrNumber.Strings.cs) | 283 | Per-case word tables (hundreds, tens, numerals, thousands, millions, decimals) |
| [`Cyriller/CyrPhrase.cs`](Cyriller/CyrPhrase.cs) | 191 | Splits a phrase into words, guesses adjective vs noun by ending, agrees them, concatenates results |
| [`Cyriller/CyrWordNotFoundException.cs`](Cyriller/CyrWordNotFoundException.cs) | 30 | Carries the word, gender, case, number and animate that failed |
| [`Cyriller/Extensions.cs`](Cyriller/Extensions.cs) | 49 | `IsNullOrEmpty`, `UppercaseFirst`, two regex helpers |

### 4.3 `CyrName` — names by rule

`CyrName` needs no dictionary: it holds per-case rule tables keyed by the last two or three
letters of the word. The flow of the core overload
(`Decline(surname, name, patronymic, int case, int gender, bool shorten)`, line 88):

1. **Patronymic splitting** — `SplitPatronymic` (line 2142) separates a prefix (`ибн `, regex at
   line 14) and a suffix (`‑оглы`, `‑улы`, `‑уулу`, `‑кызы`, `‑гызы`, regex at line 13), which stay
   unchanged in every case.
2. **Case normalisation** — case outside 1–6 becomes 1; gender outside 0–2 becomes 0.
3. **Gender inference** — when no gender is passed, a patronymic ending in `-на` means feminine
   and anything else masculine (lines 123–128). With no patronymic at all the default is
   masculine, so callers testing surname forms must pass the gender explicitly.
4. **Hyphenated surnames** — split on `-`, each part declined separately and joined back (lines
   130–140); the same convention as composite nouns.
5. **Declension** — the surname, given name and patronymic each have five methods
   (`DeclineSurnameGenitive` … `DeclineSurnamePrepositional`, lines 994–2097;
   `DeclineName*`, lines 294–727; `DeclinePatronymic*`, lines 728–993), plus a switch
   `DeclineSurname(surname, case, isFeminine)` (line 2098). The nominative is the input itself.
6. **Shortening** — `shorten: true` turns the given name and patronymic into initials
   (`Иванов И. И.`) through `Shorten` (line 2246); already-shortened parts are detected by
   `IsShorten` (line 2264).
7. **Result** — `string[]` of `[surname, name, patronymic]`, wrapped by `CyrNameResult` or
   combined into a `CyrResult` by `DeclinePerCaseAndCombine` (line 2222).

Rules are literal `switch` blocks over the word ending, applied through `SetEnd(value, cut, add)`,
so a change means editing the right case method — and checking the mirror implementation in
[`CyrName.js`](CyrName.js).

### 4.4 Collections, searching and caching

Both collections follow the same design:

1. Read the embedded `.gz` through `CyrData.GetData("nouns.gz" / "adjectives.gz")`.
2. The text has two blocks separated by `// -- End of the Rules block -- //`: rule rows, then
   word rows.
3. Every word row instantiates a `CyrNoun`/`CyrAdjective` and **declines it into all forms at
   load time**; each form is stored under its own `DictionaryKey`. That is why construction costs
   memory and time: the README quotes ~200 MB and ~2 s for nouns and ~250 MB for adjectives.
4. Lookup: exact match first; otherwise `CyrData.GetSimilars` scores candidates by matching
   letters from the end of the word (`SameLetterWeights = {1000, 100, 10}`, then 1), preferring
   the same case of the first letter, and the winning dictionary word is declined and its stem
   reverted through `CyrRule.Revert` to cover the unknown word.
5. Tuning: `NounMinSameLetters = 2`, `NounMaxSameLetters = int.MaxValue`;
   `AdjectiveMinSameLetters = 3`, `AdjectiveMaxSameLetters = 4`. `EnableCache` (default true)
   memoises similar-search results (`similarSearchCache`, `similarSearchByKeyCache`).
6. The dictionary search order for nouns is Masculine, Feminine, Neuter, then Undefined — which
   is why a caller cannot tell from `Get` alone which gender matched, and why the in-flight
   `out GendersEnum gender` parameter exists.

### 4.5 `CyrRule` — the rule string

A rule is "drop N characters from the end, then append this text". The letters are the text to
append; a digit among them is the cut length (multi-digit cuts are supported); no digit means
nothing is cut; an empty rule returns the word unchanged (the nominative); `*` marks a form that
does not exist, so `Apply` returns an empty string and `Revert` leaves the input alone.

| Rule (and where it comes from) | Applied to | Result | Meaning |
| --- | --- | --- | --- |
| `ого2` (`adjectives.txt`) | `красный` | `красного` | drop 2, append `ого` |
| `ому2` (`adjectives.txt`) | `красный` | `красному` | drop 2, append `ому` |
| `ом` (`nouns.txt`, rule 3) | `кот` | `котом` | append only |
| `` (empty) | `кот` | `кот` | nominative, unchanged |
| `*` | `кот` | empty string | the form is unavailable |

`Revert` is what lets an unknown word reuse a known word's paradigm: it keeps the unknown stem and
puts back the letters the rule cut. The `*` marker is why `CyrResult` can hold empty strings for
defective words (a noun with no plural, for instance).

### 4.6 `CyrNumber`

`CyrNumber` is independent of the collections. Variants: `Decline(long)`,
`Decline(long, gender, animate)`, `Decline(decimal …)`, `Decline(value, Currency)` for money,
`Decline(value, Item)` for "N units", `ToString(value, case, …)`, and `Case(n, "год", "года",
"лет")` for picking a form by the number's last digits. The word tables live in
`CyrNumber.Strings.cs`, one array set per case/gender/animate; the maximum is 9 999 999 999.

### 4.7 `CyrPhrase`

`DetermineSpeechPart` guesses from endings (`-ый`, `-ий`, `-ой`, `-ая`, `-яя`, `-ое`, `-ее`,
`-ся` → adjective, otherwise noun). Adjectives adopt the gender and animateness of the nearest noun
(next, else previous). Results are joined with `CyrResult.operator+`, which inserts one space.
It is a heuristic: titles and multi-word job names are documented as unsupported.

## 5. `Cyriller.Model`

Public enums and the JSON DTOs, no behaviour beyond `Validate()`.

| File | Contents |
| --- | --- |
| `CasesEnum` | `Nominative=1 … Prepositional=6` |
| `GendersEnum` | `Undefined=0`, `Masculine=1`, `Feminine=2`, `Neuter=3` |
| `NumbersEnum` | `Singular=1`, `Plural=2` |
| `AnimatesEnum` | `Animated=1`, `Inanimated=2` |
| `GetConditionsEnum` | `Strict`, `Similar` |
| `SpeechPartsEnum` | `Noun=1 … Interjection=11` (`CyrPhrase` only uses the first two) |
| `WordTypesEnum` | `None=0`, `Abbreviation=1`, `Name=2`, `Surname=3`, `Patronymic=4`, `Toponym=5`, `Organization=6`, `TradeMark=7`, `Superlative=8`, `Qualitative=9`, `Pronominal=10`, `Ordinal=11`, `Possessive=12`, `FormEy=13`, `FormOy=14`, `Comparative=15`, `ComparativeEj=16` |
| `Json/NounJson` | `Name`, `Gender`, `Animate`, `WordType`, `Singular[6]`, `Plural[6]`; `Validate()`, `ToDictionaryString(ruleIndex)` |
| `Json/AdjectiveJson` | `Name`, `Masculine[6]`, `Feminine[6]`, `Neuter[6]`, `Plural[6]`; `Validate()` |

## 6. `Cyriller.Rule`

Converts a dictionary entry into the compact rule string that `Cyriller` reads:

- `BaseRule.GetRuleString(word, variants)` — for each of the 11 (noun) or 23 (adjective) forms,
  strip the common prefix with the base form and write the remainder as `end + cut`, `*` when the
  form is empty, joined by `,`; compound words are joined by `|`.
- `NounRule` — takes the singular forms 2–6 and plural 1–6 (the singular nominative is the base),
  splitting hyphenated nouns part by part.
- `AdjectiveRule` — takes masculine 2–6, then feminine, neuter and plural forms.
- Both validate their `*Json` input first.

This project is used by `Cyriller.DictionaryBuilder`, so its output is baked into
`App_Data/*.txt`; it is not loaded at run time.

## 7. Dictionary pipeline

### 7.1 Chain

```
App_Data/nouns.json  (64.2 MB) ──Cyriller.DictionaryBuilder──► App_Data/nouns.txt  (3.6 MB)
App_Data/adjectives.json (43.9 MB) ──Cyriller.DictionaryBuilder─► App_Data/adjectives.txt (1.2 MB)
        │                                                              │
        │  Cyriller.Checker → JsonForm exports these JSON files        │  build-time
        │  from the current collections                                ▼
        │                                              Cyriller.Zipper → nouns.gz (606 KB)
        │                                                              adjectives.gz (211 KB)
        ▼                                                              │
   (source of truth for the data)                                      ▼
                                            embedded resource "Cyriller.App_Data.nouns.gz"
                                            read at run time by CyrData.GetData
```

`Cyriller.DictionaryBuilder` reads the JSON, builds rule strings with `Cyriller.Rule`, deduplicates
them (624 noun rule rows, 53 adjective rule rows) and writes the rules block delimited by
`// -- Start of the Rules block -- //` / `// -- End of the Rules block -- //`, then one line per
word. `Cyriller.csproj` runs `Cyriller.Zipper` before compiling:

```
dotnet run --no-build --no-restore --project ..\Cyriller.Zipper $(ProjectDir)App_Data\nouns.txt $(ProjectDir)App_Data\nouns.gz OnlyIfSourceIsNewer
```

`Cyriller.Zipper` takes `[source] [destination] [OnlyIfSourceIsNewer|Always]`, gzips the source and
deletes/replaces the destination. The file name must stay `nouns.gz`/`adjectives.gz` because
`CyrData` resolves `Cyriller.App_Data.<name>`.

### 7.2 Formats

`nouns.txt` — 124,931 lines, 124,299 word entries:

```
// This file is autogenerated by Cyriller.DictionaryBuilder from nouns.json.
// DO NOT MODIFY IT!
// Use nouns.json file instead.
// -- Start of the Rules block -- //
,,,,,*,*,*,*,*,*
а,у,а,ем,е,и,ей,ам,ей,ами,ах
ы1,е1,у1,ой1,е1,ы1,ен2,м,ен2,ми,х
...
// -- End of the Rules block -- //
компьютер 1,2,0,8
кот 1,1,0,3
кошка 2,1,0,75
```

- A rule row is 11 comma-separated rules: singular genitive, dative, accusative, instrumental and
  prepositional (5), then all six plural cases. The singular nominative is the word itself and has
  no rule, which is why the first field of the very first row is empty.
- A word row is `слово ` followed by `gender,animate,type,ruleIndex` (the enums of
  `Cyriller.Model` as integers, then an index into the rules block).

`adjectives.txt` — 42,944 lines, 42,883 word entries, 52 rule rows of 23 rules each (masculine
2–6, feminine 1–6, neuter 1–6, plural 1–6; singular masculine nominative is the base form):

```
ого2,ому2,ого2,м1,ом2,ая2,ой2,ой2,ую2,ой2,ой2,ое2,ого2,ому2,ое2,м1,ом2,е1,х1,м1,х1,ми1,х1
...
красный 0
```

The JSON side is a plain array of `NounJson`/`AdjectiveJson` objects (`Animate` as a name, forms
as six-element arrays).

### 7.3 Changing dictionary data

1. Edit `nouns.json` / `adjectives.json` (or regenerate them with Checker's JsonForm).
2. Regenerate the `.txt`:
   `dotnet run --project Cyriller.DictionaryBuilder -- nouns Cyriller\App_Data\nouns.json Cyriller\App_Data\nouns.txt`
   (and the same with `adjectives`).
3. Build `Cyriller` so `Cyriller.Zipper` repacks the `.gz`; revert the `.gz` afterwards unless the
   data change is what you intend to commit.

## 8. `Cyriller.Tests`

`net10.0` project, xunit 2.9.3 + Microsoft.NET.Test.Sdk 17.14.2, 70 tests, no database:

| File | Coverage |
| --- | --- |
| [`CyrNameTests.cs`](Cyriller.Tests/CyrNameTests.cs) | Full-name declension for both genders, `-Кызы`/`-Оглы` and hyphenated patronymics, shortened names, individual surname/name/patronymic cases, initials, names in `-ша` |
| [`CyrRuleTests.cs`](Cyriller.Tests/CyrRuleTests.cs) | `Apply` and `Revert` for empty, cut-only, add-only, combined and `*` rules |
| [`DictionaryTests.cs`](Cyriller.Tests/DictionaryTests.cs) | Real dictionary lookups, resolved gender, similar-word fallback, adjectives in all genders, number spelling and unit forms |
| [`SurnameTests.cs`](Cyriller.Tests/SurnameTests.cs) | Surname endings, hyphenated surnames and a sampled sweep over `Test_data/russian_surnames.csv`; the sweep is skipped when the folder is absent and ignores rows with an embedded ESC byte |
| [`PetrovichTests.cs`](Cyriller.Tests/PetrovichTests.cs) | 55 name forms from the NPetrovich CSV fixtures and 18 expectations from `petrovich.spec.js`; both skip when absent |
| [`UdNounTests.cs`](Cyriller.Tests/UdNounTests.cs) | Up to 4000 noun tokens of `ru_syntagrus-ud-test.conllu`: the corpus form must be the one the dictionary gives for the lemma, its case and its number |
| [`KaikkiNounTests.cs`](Cyriller.Tests/KaikkiNounTests.cs) | Up to 1200 dictionary nouns from the first 400 000 lines of `ru-extract.jsonl`: every Wiktionary form of an attested case/number must match, with stress marks and non-Russian letters filtered out |
| [`NameDeclensionTests.cs`](Cyriller.Tests/NameDeclensionTests.cs) | 100 sampled surname + given-name pairs (50 male, 50 female, seed `20261113`) declined by `CyrName` and compared with the `Sur`/`Giv` `PROPN` forms attested in SynTagRus; the name type is part of the key, because the same string can be both a surname and a given name |
| [`CorpusFiles.cs`](Cyriller.Tests/CorpusFiles.cs) | Finds `Test_data` from the build output, shares a single `CyrNounCollection` and normalises `ё` and stress marks |

The EF6 model and the SQL Server fixture were removed together with the old `NounTests`; the
dictionary checks now run against the embedded resources and the external corpora, so
`dotnet test` needs neither a database nor network access. The corpus tests allow a small mismatch
budget (Petrovich 10 %, SynTagRus 5 %, Wiktionary 10 %), because the corpora contain dialectal,
obsolete and suppletive forms (`человек → людей`, `год → году`) that the dictionaries regularise.

## 9. `Cyriller.Samples`

Console demo, one method per area, called in order: `NounSamples`, `AdjectiveSamples`,
`PhraseSamples`, `NameSamples`, `NumberSamples`
([`Program.cs`](Cyriller.Samples/Program.cs), 337 lines). It creates both collections up front, so
it doubles as the memory/startup smoke test, and it prints every API variant with Russian labels.

## 10. `Cyriller.Checker`

WinForms application (`net10.0-windows`) with a `StartForm` hosting one `UserControl` per area:
`AboutForm`, `NounForm`, `AdjectiveForm`, `PhraseForm`, `NumberForm`, `JsonForm`, plus
`LoadingForm` shown while a collection loads. `NounForm` keeps one static `CyrNounCollection` and
logs load/declension timings. `JsonForm` is the dictionary **exporter**: it walks
`SelectNouns()`/`SelectAdjectives()` and writes `nouns.json`/`adjectives.json` — the way
`App_Data/*.json` is regenerated after dictionary changes. The EF model, `App.config` and the
database requirement were removed; the JSON export uses `System.Text.Json`.

## 11. `Cyriller.Web`

ASP.NET Core MVC site (`net10.0`), the public demo. Structure:

- **Controllers**: `HomeController` (Index, Decline redirect, Download, DownloadFile, Contacts,
  Thanks, News), `DeclineController` (Noun, Adjective, Phrase, Number, List GET/POST,
  File GET/POST), `ErrorController`, `BaseController` (404 helper).
- **`Decline/File`** accepts an uploaded `.xlsx`, `.xls` or `.txt` word list and returns a
  generated file; `Decline/List` handles up to `MaxPhrasesToDecline = 50` phrases per request.
  Generated files and uploads go to a `Temp` folder under the app root (git-ignored).
- **Compression and startup** live in [`Program.cs`](Cyriller.Web/Program.cs): response
  compression middleware, `UseStatusCodePagesWithReExecute("/Error/p{0}")`, singleton
  `CyrNounCollection`/`CyrAdjectiveCollection`, static files from `wwwroot/` and the `Temp`
  folder mapped at `/Temp`.
- **`Models`**: `CyrBaseDeclineResult`, `CyrNounDeclineResult`, `CyrAdjectiveDeclineResult` —
  view models that bundle the word, the matched dictionary word and its case/number/gender.
- **Views**: 20 Razor files (`Home/*`, `Decline/*`, `Error/*`, shared layout/donate partial),
  bootstrap 3, jQuery 1.10, Font Awesome, served from `wwwroot/`; `_ViewImports.cshtml` brings in
  `Cyriller`/`Cyriller.Model` namespaces and the tag helpers.
- The only package is EPPlus 4.5.3.2 (Excel import/export); MVC, Razor and compression come from
  the shared framework.

## 12. `Cyriller.Desktop`

Avalonia 11.3.23 desktop app (`net10.0`), MVVM + ReactiveUI + Microsoft DI:

- **Entry point** [`Program.cs`](Cyriller.Desktop/Program.cs): sets `ru-RU` culture, installs
  unhandled-exception logging, builds `AppBuilder`, registers `CyrCollectionContainer`, `CyrName`,
  `CyrNumber`, the view models and `MainWindow` in a `ServiceProvider`.
- **`CyrCollectionContainer`** loads `CyrNounCollection` + `CyrAdjectiveCollection` once, in the
  background, on first use (`InitCollectionsInBackground` / `InitOrDefault`).
- **ViewModels**: `MainWindowViewModel`, `NounViewModel`, `AdjectiveViewModel`, `NameViewModel`,
  `NumberViewModel`, `PhraseViewModel`, `DeclineViewModel` (shared decline/export logic),
  `ViewModelBase`.
- **Views**: `MainWindow` plus one view per area; `ViewLocator` (an `IDataTemplate`) maps
  view-model names to views; XAML classes are `partial` and listed as `AvaloniaXaml` in the csproj.
- **Models**: row models for the data grids, `GenderModel`/`NumberModel`/`AnimateModel`/
  `WordTypeModel` (choice lists), `CyrCollectionContainer`.
- **Logger.cs** writes timestamped JSON dumps into `Log/` next to the executable; `ViewExtensions`
  and `DesktopServices` wrap the Avalonia 11 storage-provider file dialog and clipboard;
  `publish.bat` produces a single-file `win-x64` build.

## 13. `CyrName.js`

A 1839-line ES5 port of `CyrName` kept at the repository root for browser use (linked from the
README as the simplified JS variant). Same `Decline(surname, name, patronymic, case, gender,
shorten)` contract and the same ending tables, but it lags the C# implementation — the C# side
gained patronymic regexes, hyphen handling and shortening refinements later. Treat it as a second
implementation to keep in sync, not as a source of truth.

## 14. `Test_data` (read-only, not tracked)

| Path | What it is |
| --- | --- |
| [`Test_data/russian_surnames.csv`](Test_data/russian_surnames.csv) | 9,498,549 B, UTF-8 without BOM, LF, no quoting: header `surname_cyrl,surname_latn,gender`, then 318,479 rows — Cyrillic surname, Latin transliteration, `m`/`f`/`u` (123,401 / 123,923 / 71,155) |
| [`Test_data/ru-pnames-list/`](Test_data/ru-pnames-list) | Separate git clone of `https://github.com/Raven-SL/ru-pnames-list` at `48ff79f`; `readme.md`, empty `.gitignore`, and `lists/` with 736 male names, 938 female names and 14,651 male surnames (CRLF, UTF-8 without BOM, one per line, no duplicates) |
| `Test_data/petrovich-rules/` | Rule tables of the MIT-licensed [Petrovich](https://github.com/petrovich/petrovich-rules) project (`rules.json` 21 KB, `gender.json` 12 KB); rules only, without expected forms |
| `Test_data/petrovich-net/` | .NET port NPetrovich; `src/NPetrovich.Tests/Data/*.csv` holds 55 expected forms for first names, patronymics and surnames (`Иван, Male, Genitive, Ивана` style) |
| `Test_data/petrovich.spec.js` | 130-line Jasmine spec from `petrovich-js` with 18 chained expectations (`expect(p.male.first.genitive('Мария')).toBe('Марии')`) |
| `Test_data/UD/*.conllu` | Five SynTagRus files (`dev` 14.3 MB, `test` 14.6 MB, `train-a/b/c` 39.5/41.5/31.8 MB), UD 2.19: 87,337 sentences, 1,515,559 tokens, **CC BY-NC-SA 4.0** |
| `Test_data/ru-extract.jsonl` | 2.5 GB raw [wiktextract](https://kaikki.org/) dump of the Russian Wiktionary (2026-10-30), CC BY-SA 4.0: records for all languages, Russian nouns (`"lang_code": "ru"`) carry full declension tables with stress marks |

Data-quality notes for anyone writing tests over the CSV:

- every row has exactly three fields, no empty fields, `surname_latn` is pure ASCII, and only
  `m`/`f`/`u` appear in `gender`;
- two entries (four lines) carry an embedded ESC (`U+001B`) byte inside both surname fields — lines
  132906/132907 (`Крас`**`ESC`**`нов` / `Крас`**`ESC`**`нова`) and their verbatim duplicates at
  133088/133089. They are the only duplicate lines in the file; strip or skip them;
- a surname can legitimately appear under more than one gender, so key expectations by
  `(surname_cyrl, gender)`.

The folder is listed in `.gitignore` as `/Test_data` and has no `.gitmodules` entry. Only the test
project reads it: `SurnameTests` uses the CSV, `PetrovichTests`, `UdNounTests` and
`KaikkiNounTests` use the corpora, and every one of them skips silently when its data is absent.

## 15. Generated, ignored and stale artefacts

| Path | Status |
| --- | --- |
| `*/bin/`, `*/obj/` | ignored build output, regenerated by a build |
| `packages/` | ignored legacy old-style NuGet folder; present here, not used by the solution |
| `.vs/` | ignored Visual Studio state (`suo`, solution caches) |
| `Cyriller/App_Data/*.txt`, `*.gz` | **tracked** generated data; `.txt` comes from `Cyriller.DictionaryBuilder`, `.gz` from Zipper |
| `Cyriller.Web/Temp/` | ignored runtime folder for uploaded/generated files |
| `Cyriller.Desktop/Log/` | written at run time by `Logger` |
| `.acl-recovery/` (sibling of this repo) | host-level record of the file-permission repair, not part of git |

## 16. Where a change belongs

| Task | Files |
| --- | --- |
| Surname/name/patronymic endings | `Cyriller/CyrName.cs` (the matching `DeclineSurname*`/`DeclineName*`/`DeclinePatronymic*` method), then `CyrName.js`, then `Cyriller.Tests/CyrNameTests.cs` |
| What gender a noun lookup resolves | `Cyriller/CyrNounCollection.cs` (`GetOrDefault`, `DictionaryKey`), `Cyriller.Model/GendersEnum.cs` |
| Noun paradigm rules | `Cyriller/CyrNoun.cs` (11 rules per part), `Cyriller/App_Data/nouns.json` updated through `Cyriller.DictionaryBuilder` |
| Adjective paradigm rules | `Cyriller/CyrAdjective.cs` (23 rules), `Cyriller/App_Data/adjectives.json` updated through `Cyriller.DictionaryBuilder` |
| Similar-word matching | `Cyriller/CyrData.cs` (`SameLetterWeights`, `Compare`), the `*MinSameLetters`/`*MaxSameLetters` properties |
| Rule-string semantics | `Cyriller/CyrRule.cs` and `Cyriller.Rule/*` (they must agree), `Cyriller.Tests/CyrRuleTests.cs` |
| Public enums/DTOs | `Cyriller.Model/`, then `README.md` (public docs) |
| Numbers, money, units | `Cyriller/CyrNumber*.cs` |
| Phrase handling | `Cyriller/CyrPhrase.cs`, `Cyriller.Model/SpeechPartsEnum.cs` |
| Web behaviour | `Cyriller.Web/Controllers/`, `Cyriller.Web/Views/`, `Cyriller.Web/Program.cs` |
| Desktop behaviour | `Cyriller.Desktop/ViewModels/`, `Views/`, `Models/` |
| Dictionary export/JSON shape | `Cyriller.Checker/JsonForm.cs`, `Cyriller.Model/Json/*`, `Cyriller.DictionaryBuilder/Program.cs` |
| Packaging | `Cyriller/Cyriller.csproj` (`Version`, `PackageReleaseNotes`), `Cyriller/pack.bat` |

## 17. Known gaps and risks

- **Two implementations of name declension** (`CyrName.cs`, `CyrName.js`) already differ; a change
  in one can silently leave the other wrong.
- **Generated data is tracked**: `.txt` and `.gz` dictionaries are committed, so a data change is
  a multi-megabyte diff and a build dirties the `.gz` files.
- **`Test_data` is only sampled**: `SurnameTests` sweeps a subset of the CSV for invariants, but
  there is no expected-form corpus, so coverage of surname spelling is still thin.
- **Desktop UI is only smoke-tested**: the Avalonia app builds and starts, but its views and
  commands have no automated tests.
- **Hand-written word tables carry typos**, e.g. `весьмидесяти` (for `восьмидесяти`) in
  [`CyrNumber.Strings.cs`](Cyriller/CyrNumber.Strings.cs); spot-check strings when touching them.
- `CyrNounCollection.Get` scans 6 cases × 2 numbers × 4 genders per lookup and similar search is
  linear over the candidate list, which is why the caches exist; keep that in mind before adding
  callers that search in bulk.

## 18. Keeping this file current

Update this document when a project is added or removed, a target framework or package changes, a
new file takes on a distinct responsibility, the dictionary pipeline changes, or the shape of
`Test_data` changes. Facts that age quickly (versions, commit ids, counts) carry the date they were
measured; re-measure rather than trusting an old number.
