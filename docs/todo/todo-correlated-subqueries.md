# TODO: Correlated Subqueries in `SqlIn`

**Status:** Deferred at v7.0 ship; targeted for v7.1. Documented as unsupported in
`docs/querying.md` (["Correlated subqueries are not supported"](../querying.md#correlated-subqueries-are-not-supported)).
This doc captures the design so the work can be picked up cold.

## Problem

The subquery `SqlIn` where-lambda can reference its own parameter and captured
locals, but not the *outer* lambda's parameter:

```csharp
// fails today
await adapter.Fill(animals, a => a.ExhibitId.SqlIn(
    (Exhibit e) => e.ExhibitId,
    e => e.ZookeeperId == a.ZookeeperId));   // `a` is the outer parameter
```

## Current failure mode

`TranslateSqlInSubquery` (`src/Zonkey.Data/ObjectModel/QueryTranslation/MethodTranslators.cs`)
builds the child translator with a maps dictionary containing **only** the inner
lambda's parameter:

```csharp
var childMaps = new Dictionary<string, DataMap> { { whereLambda.Parameters[0].Name, map } };
var child = new ExpressionTranslator(childMaps, t.Dialect);
SqlNode where = child.TranslatePredicate(PartialEvaluator.Reduce(whereLambda.Body));
```

When the subquery's where-body touches the outer parameter (`a.ZookeeperId` above),
`ExpressionTranslator.TranslateMember` (`src/Zonkey.Data/ObjectModel/QueryTranslation/ExpressionTranslator.cs`)
looks it up by name and finds nothing:

```csharp
if (m.Expression is ParameterExpression pex)
{
    DataMap map = _maps[pex.Name];   // KeyNotFoundException — pex.Name is the OUTER param, not in childMaps
    ...
}
```

The result is a raw, unhandled `KeyNotFoundException` with no context about what
went wrong or which parameter was unresolvable — a confusing failure for something
that reads like valid, idiomatic C#.

## Interim improvement (cheap, could ship in a 7.0.x patch)

Catch the unresolved parameter in `TranslateMember` and throw a proper
`SqlExpressionException` naming the parameter and stating that correlated
subqueries are not yet supported, instead of letting `KeyNotFoundException` leak
through. This is a few lines — replace the dictionary index with a `TryGetValue`
and a targeted throw — and turns a confusing crash into an actionable message. It
does not implement correlation; it only makes the unsupported case fail clearly.

## Real feature (v7.1)

1. **Thread the outer maps into the child translator.** When constructing the
   child `ExpressionTranslator` in `TranslateSqlInSubquery`, merge the outer
   translator's maps dictionary with the inner parameter's map (inner parameter
   name must win on collision — see the self-join case below) instead of passing
   only `{ innerParam: innerMap }`. This alone lets the child resolve
   `MemberExpression`s rooted at the outer parameter the same way it resolves its
   own.

2. **Force table-qualified column rendering inside the subquery.** Once both outer
   and inner columns can appear in the subquery's WHERE, same-named columns (e.g.
   both tables have `ZookeeperId`) must render qualified
   (`table.column`) to disambiguate. `SqlTextGenerator.QualifyColumns` (the
   generator-level flag; the facade knob is `WhereExpressionParser.UseTableWithFieldNames`,
   see `src/Zonkey.Data/ObjectModel/WhereExpressionParser.cs`) already does exactly
   this for the existing multi-map/InnerJoin path — the subquery generation needs
   to force it on rather than add a new mechanism.

3. **The blocking design issue: self-joins need aliases.** Cross-entity
   correlation (different outer/inner types, as in the example above) is
   satisfied by table-name qualification alone. But same-entity correlation —

   ```csharp
   a.ManagerId.SqlIn((Animal m) => m.AnimalId, m => m.ExhibitId == a.ExhibitId)
   ```

   — has both the outer parameter (`a`) and inner parameter (`m`) mapping to the
   *same* table name (`Animal`). Table-qualified rendering alone produces
   `Animal.ExhibitId = Animal.ExhibitId`, which is ambiguous/wrong SQL — this case
   requires per-parameter aliases (`t0`, `t1`) in both the outer FROM/subquery
   SELECT and every qualified column reference. This is why the feature was
   deferred rather than shipped as "cross-entity only": it's a half-feature that
   would need a second incompatible mechanism bolted on later for self-joins.
   `SqlColumn`'s qualifier field is already alias-ready for this
   (see `docs/superpowers/specs/2026-07-24-where-expression-translator-design.md`,
   "Join qualification (future)" and the `SqlColumn(field, map, qualifier?)` node
   description) — the alias groundwork should land once, shared with the
   still-experimental `InnerJoin` path, rather than duplicated for subqueries.

## Test plan sketch

- Cross-entity correlation (different outer/inner types) against all three
  integration providers (SQLite, MSSQL, PostgreSQL) — confirms table-qualification
  is sufficient there.
- Same-entity self-join case (`Animal`/`Animal`) — alias-dependent; should be
  skipped/marked pending until aliasing lands, then exercised once it does.
- Diagnostic-error test for the interim fix: assert `SqlExpressionException` (not
  `KeyNotFoundException`) is thrown, naming the unresolved outer parameter, when a
  correlated reference is attempted before real support ships.

## Pointers

- `TranslateSqlInSubquery` — `src/Zonkey.Data/ObjectModel/QueryTranslation/MethodTranslators.cs`
- `ExpressionTranslator._maps` / `TranslateMember` — `src/Zonkey.Data/ObjectModel/QueryTranslation/ExpressionTranslator.cs`
- `SqlTextGenerator.VisitInSubquery` + `QualifyColumns` — `src/Zonkey.Data/ObjectModel/QueryTranslation/SqlTextGenerator.cs`
- Facade knob: `WhereExpressionParser.UseTableWithFieldNames` — `src/Zonkey.Data/ObjectModel/WhereExpressionParser.cs`
- Design spec (alias groundwork, `SqlColumn` qualifier): `docs/superpowers/specs/2026-07-24-where-expression-translator-design.md`
- User-facing docs: `docs/querying.md` ("Correlated subqueries are not supported", "Subquery SqlIn")
