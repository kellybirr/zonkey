# TODO: Virtual / Computed Columns on DataClass (per-field quoting exception)

**Status:** Analyzed 2026-07-18/19, deliberately not fixed yet. This doc captures the findings and the agreed design direction so the work can be picked up cold.

## The feature and its original intent

`UseQuotedIdentifier` is a tri-state (`bool?`) that exists at four levels, consulted in this precedence order (see `DataClassCommandBuilder/Common.cs:144,166,199`, `Select.cs:271`, `Insert.cs:45,177`, `WhereExpressionParser.cs:331,337`):

1. Per-field: `IDataMapField.UseQuotedIdentifier` (field attr / runtime map mutation)
2. Per-item (table name only): `IDataMapItem.UseQuotedIdentifier`
3. Builder/adapter: `DataClassCommandBuilder.UseQuotedIdentifier`, settable via `adapter.SetProperty(AdapterProperty.UseQuotedIdentifiers, ...)` or the static `DataClassAdapterBase.DefaultQuotedIdentifier`
4. Dialect default — and the two dialect families differ **on purpose**:
   - Bracket dialects (SqlServer, Sqlite): quote unless explicitly `false` (`null` => `[quoted]`, brackets never change meaning)
   - ANSI-style dialects (PostgreSql, MySql): quote only when explicitly `true` (`null` => bare, because quoting changes case-folding semantics)

The historical use case for the **per-field `false` override** (from the .NET 4.0 era): put a computed/derived
expression in the SELECT list on MSSQL while every real column stays quoted. Example:

```csharp
[DataField("([Price] * [Qty]) AS [LineTotal]", DbType.Decimal, AccessType = AccessType.ReadOnly)]
public decimal LineTotal { ... }
```

Because `SqlServerDialect` quotes *by default*, that expression would otherwise be emitted as
`[([Price] * [Qty]) AS [LineTotal]]` — a (missing) identifier. The per-field `false` is the only escape
hatch; there is no other reason for a single-field quoting exception (a plain column name is always safe
to quote).

## Current state (verified empirically, SQLite + live test)

| Aspect | Status |
|---|---|
| SQL generation (SELECT list with per-field unquote) | **Works** — quoted identifiers and raw expression coexist correctly |
| WHERE filtering via LINQ (`o => o.LineTotal > 10`) | **Works** — `WhereExpressionParser` emits the raw `FieldName` expression |
| INSERT/UPDATE safety | **Works** — `AccessType.ReadOnly` keeps the field out of writes |
| Declaring the override in the attribute | **Broken** — `UseQuotedIdentifier` is `bool?`, which is not a legal attribute argument type (CS0655). `bool?` since at least the 2017 git import; if attribute syntax ever worked it was pre-git when the property was presumably plain `bool`. Today the only way to set it is runtime map mutation. |
| Populating the property from the result set | **Broken (silently)** — experiment: `Save`/`GetOne` succeed, but the property stays `default` (expected 42, got 0). |

Root cause of the populate failure: `FieldName` plays two roles that coincide for plain columns and diverge
for expressions — it is both the SELECT-list text and the expected result-set column name. All three
column-matching sites compare the reader's column name (the alias, e.g. `LineTotal`) against the verbatim
`FieldName` (the whole expression), case-insensitively, and miss:

- `DataClassAdapter<T>.PopulateSingleObject` — `Populate.cs:26` (reader→map direction; used by `GetOne`, select-back)
- `DataClassReader<T>` QuickFill — `DataClassReader.cs:288` (map→reader; `Fill` reflection path)
- `DataClassReader<T>` IL builder — `DataClassReader.cs:475` (map→reader; `Fill` fast path)

The readable-fields dictionary is keyed by verbatim `FieldName` at `DataMap.cs:516`.

Note: a compile-time proof of "no existing users to break": because CS0655 has rejected the named argument
for the entire git history, no compiled assembly can contain `UseQuotedIdentifier = ...` in an attribute
blob — the compiler could never emit one.

## Agreed design direction

### Fix 1 — restore attribute syntax via explicit interface implementation

`DataFieldAttribute` implements `IDataMapField` directly (the attribute instance IS the map field —
`DataMap.cs:702` assigns it straight to an `IDataMapField`). Every internal consumer reads
`UseQuotedIdentifier` through an interface-typed reference (audited: command builders, WHERE parser, DataMap
— no concrete-typed reads anywhere in src). Therefore the tri-state can retreat behind an explicit
interface implementation with **zero changes at any consumption site**:

```csharp
private bool? _useQuotedIdentifier;

// attribute-syntax face (named args must be public read-write and attribute-legal)
public bool UseQuotedIdentifier          // same simple name is legal alongside the explicit impl
{
    get => _useQuotedIdentifier ?? true; // getter is never read by the runtime; see wart below
    set => _useQuotedIdentifier = value;
}

// contract face — what DataMap / builders / parser see
bool? IDataMapField.UseQuotedIdentifier
{
    get => _useQuotedIdentifier;
    set => _useQuotedIdentifier = value;
}
```

Details settled during analysis:

- An explicit interface implementation and a public member with the same simple name may coexist (the
  explicit one gets the fully-qualified metadata name and is not part of the public surface, and can never
  be an attribute named argument). Keeping the name restores the original
  `[DataField(..., UseQuotedIdentifier = false)]` spelling exactly.
- Attribute materialization only invokes *setters* (blob = (property, value) pairs replayed after the
  ctor), and internal reads go through the interface — so the public getter is decorative. **Wart:** it
  must exist (named-arg rule requires read-write) and must fabricate a `bool` when the backing is `null`;
  there is no honest answer because the effective default is dialect-relative. Document it as
  "meaningful only after explicit assignment". Alternative with a fully honest getter: an attribute-legal
  enum face (`Quoting = IdentifierQuoting.Unquoted`, `null` <=> `Default`) — decide at implementation time.
- `DataItemAttribute` / `IDataMapItem` (Interfaces.cs:104) have the identical problem for the table-level
  setting — apply the same treatment for symmetry.
- `DataMapField` (runtime/implicit-field class) keeps its ordinary public `bool?` implicit implementation —
  the constraint only applies to the attribute.
- Compat: source/binary break only for external code reading `attr.UseQuotedIdentifier` off a *concrete*
  attribute reference (none in this repo); v7.0 already carries breaking changes (strong-name removal).

### Fix 2 — populate via "result name" (preferred over property-name fallback)

Introduce a derived **result name** per field: parse a trailing `AS alias` off `FieldName` once (strip
`[]`, `""`, or backticks; case-insensitive); no alias => result name == field name, so every existing model
is byte-for-byte unaffected. Then:

- Key `_readableFieldsDict` by result name (`DataMap.cs:516`) — fixes the reader→map direction; the
  existing duplicate-key throw gives an early, loud failure on alias collisions.
- The two `DataClassReader` sites look up the reader-fields dictionary by result name — fixes map→reader.
- SQL generation and the WHERE parser keep using raw `FieldName` untouched.
- Implement as an internal static helper (e.g. `DataMapField.GetResultName(IDataMapField)`) — **no
  `IDataMapField` interface change**. This matters because net48 is still a target and has no
  default-interface-member support, so adding an interface member would break external implementors.

Rejected alternative: on `FieldName` miss, retry matching by CLR property name. Simpler, but in the
map→reader direction a fallback can bind another field's column to the wrong property (silent wrong-value
bug); would need heuristic gating ("only for expression-like FieldNames"). The result-name approach has no
such ambiguity.

## Test plan when implemented

- Regression test: the computed-column round trip (SQLite in-memory; expression field with per-field
  unquote; assert the property comes back computed — the experiment that currently yields 0 should yield 42).
- Unit tests for the alias parser (no alias, `AS x`, `AS [x]`, `as "x"`, backticks, trailing whitespace).
- Attribute-syntax compile test: a model declaring `UseQuotedIdentifier = false` in the attribute.
- The existing quoting suite (`Unit/QuotedIdentifierTests.cs`, `Unit/QuotedIdentifierBuilderTests.cs`,
  `Integration/Sqlite|Pgsql/*QuotedIdentifierTests.cs`) covers the dialect matrix, precedence, and
  positive/negative end-to-end behavior and should stay green unchanged.

## Files touched when implemented

`DataFieldAttribute.cs`, `DataItemAttribute.cs`, `ObjectModel/Interfaces.cs` (docs only),
`ObjectModel/DataMapField.cs` (helper), `ObjectModel/DataMap.cs:516`,
`ObjectModel/DataClassReader.cs:288,475`, plus tests. `Populate.cs` needs no change under the result-name
approach (its `GetReadableField` lookup is fixed by the dictionary keying).
