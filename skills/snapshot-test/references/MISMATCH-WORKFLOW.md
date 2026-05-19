# Handling Snapshot Mismatches

Snapshooter compares the rendered JSON of the value passed to `MatchSnapshot` against a committed `.snap` file under `__snapshots__/`. On mismatch it does two things:

1. Fails the test.
2. Writes the *actual* output to `__snapshots__/__mismatch__/<TestClass>.<TestMethod>.snap`.

The `.snap` under `__snapshots__/` is the accepted reference; the one under `__snapshots__/__mismatch__/` is what the test produced this run.

## The rule

When a snapshot test fails, Snapshooter writes the actual output to `__snapshots__/__mismatch__/`. Inspect the diff, then if correct copy the mismatch over the original and remove the `__mismatch__/` folder.

The "inspect diff first" is load-bearing. The whole reason the snapshot exists is to catch unintended shape changes; auto-accepting bypasses the check.

## Step by step

### 1. Run the failing test with a filter

```bash
dotnet test <path/to/Project.Tests.csproj> \
  --filter "FullyQualifiedName~CreateBookTests.CreateBook_ShouldReturnBook_WhenInputIsValid"
```

Or, if the project uses Microsoft.Testing.Platform (xunit v3):

```bash
cd <path/to/Project.Tests>
dotnet run -- --filter-method "*CreateBookTests.CreateBook_ShouldReturnBook_WhenInputIsValid" --report-ctrf
```

### 2. Diff the mismatch against the original

```bash
diff -u \
  __snapshots__/CreateBookTests.CreateBook_ShouldReturnBook_WhenInputIsValid.snap \
  __snapshots__/__mismatch__/CreateBookTests.CreateBook_ShouldReturnBook_WhenInputIsValid.snap
```

Read the diff. There are four possibilities:

| What the diff shows | What it means | What to do |
|---|---|---|
| New non-deterministic value (random GUID, timestamp, signed URL) | Missing `IgnoreField` glob | Extend the `IgnoreField` chain in the test; don't accept the snapshot |
| New field that is a real schema addition | Intentional capability | Accept the snapshot |
| Field removed / renamed | Schema change | Accept only if the change is intentional; otherwise it's a regression |
| Reordered collection items | Non-deterministic ordering in the query | Fix the source query with `OrderBy`; don't accept the snapshot |

### 3. Accept the new snapshot (only after diffing)

```bash
cp __snapshots__/__mismatch__/CreateBookTests.CreateBook_ShouldReturnBook_WhenInputIsValid.snap \
   __snapshots__/CreateBookTests.CreateBook_ShouldReturnBook_WhenInputIsValid.snap

rm -rf __snapshots__/__mismatch__/
```

Re-run the test. It should pass.

### 4. CI mismatches

When a snapshot test fails on CI, Snapshooter still writes the mismatch file, but you can't `cp` it locally. Two options:

- **Download the test artifacts** for the failed CI run. The pipeline uploads `__snapshots__/__mismatch__/` as a build artifact. Pull the file you need, replace the original locally, commit.
- **Reproduce locally** by running the same filter, then accept locally. This is faster when the test reproduces deterministically (which is the goal — flaky snapshots mean an `IgnoreField` is missing).

### 5. Verify before pushing

```bash
git status
```

If `__snapshots__/__mismatch__/` shows up, you forgot the `rm -rf`. Clean it before committing — mismatch files are local-only.

```bash
git diff __snapshots__/
```

Re-read the diff one more time. The snapshot diff is part of the PR and reviewers will look at it.

## Bulk accept across many failing tests

When a *legitimate* schema change cascades into many snapshots (e.g. adding a field that appears in 30 GraphQL queries), bulk-accept after a thorough diff of one representative file:

```bash
# from the test project root
find __snapshots__/__mismatch__ -name '*.snap' -exec sh -c '
  for f; do
    cp "$f" "__snapshots__/$(basename "$f")"
  done
' _ {} +
rm -rf __snapshots__/__mismatch__/
```

Do this only after you have manually inspected at least one representative diff and confirmed every other mismatch is the same shape. If different `__mismatch__` files show different kinds of changes, accept them one at a time — bulk-accepting hides regressions.
