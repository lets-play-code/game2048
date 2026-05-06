# Game2048 Safety Net and Module Refactor Implementation Plan

> **Execution model:** This plan is designed for a single continuous executor. Start it with `/run-plan <plan-file>` after approval. The runner creates task branches for the touched repo(s), keeps status in `.pi/runs/...`, and only stops early for explicit stop conditions.

**Goal:** Add deterministic safety-net tests around `Game2048`, enforce >90% branch coverage for the Game2048 test project, then refactor `src/Game2048.Game/Game2048.cs` into loosely coupled core-rules, presentation, and leaderboard modules while keeping the current public API and Web HTTP contract compatible.

**Architecture:** Keep `Game2048` as the public façade. First add two `protected virtual` seams on the façade for randomness and wall-posting, then build approval-style scenario fixtures and targeted guard tests around it. Once the safety net passes, extract `GameBoard`, `GameStatePresenter`, and `LeaderboardService`/store modules behind the façade, then add explicit boundary tests for each module and promote the new architecture into durable project docs.

**Tech Stack:** .NET 7, C#, xUnit, Coverlet MSBuild coverage

**Repo Scope:** single repo (`/Users/wuke/code/course-practice/legacy-code-csharp`)

---

## File Structure / Responsibility Map

### Production files
- Modify: `src/Game2048.Game/Game2048.cs` — add override seams first; later reduce to façade/orchestration and remove embedded rule/render/leaderboard logic
- Create: `src/Game2048.Game/Properties/AssemblyInfo.cs` — expose internal module types to `Game2048.Tests` via `InternalsVisibleTo`
- Create: `src/Game2048.Game/Core/BoardTile.cs` — core tile value object and board-level helpers used by the rules module
- Create: `src/Game2048.Game/Core/GameBoard.cs` — board state, reset, move/merge, scoring, win/lose, can-move, and random-tile insertion through a callback
- Create: `src/Game2048.Game/Presentation/GameStatePresenter.cs` — map board/game status into `Game2048State` / `TileState`
- Create: `src/Game2048.Game/Leaderboard/InMemoryLeaderboardStore.cs` — dictionary-backed leaderboard storage and sorting
- Create: `src/Game2048.Game/Leaderboard/LeaderboardService.cs` — save-once, best-score, rank calculation, and publish-message orchestration
- Create: `src/Game2048.Game/Models/Game2048State.cs` — move the current state DTO out of `Game2048.cs`
- Create: `src/Game2048.Game/Models/TileState.cs` — move the current tile DTO out of `Game2048.cs`
- Create: `src/Game2048.Game/Models/LeaderboardEntry.cs` — move the current leaderboard DTO out of `Game2048.cs`
- Optional modify: `src/Game2048.Web/Program.cs` — only if namespace/file moves require minimal using/reference updates; route shape and endpoint behavior must stay unchanged

### Test files
- Delete: `tests/Game2048.Tests/Game2048Test.cs` — discard the current legacy tests per approved scope
- Modify: `tests/Game2048.Tests/Game2048.Tests.csproj` — add coverage package and approval-file copy settings
- Create: `tests/Game2048.Tests/Game2048FacadeTest.cs` — first failing tests for override seams and façade compatibility
- Create: `tests/Game2048.Tests/Game2048GuardTest.cs` — targeted guard/branch tests that do not belong in approval snapshots
- Create: `tests/Game2048.Tests/Game2048ApprovalFixture.cs` — deterministic scenario runner and snapshot renderer
- Create: `tests/Game2048.Tests/ApprovalTest.cs` — approval-style comparisons against checked-in `.approved.txt` files
- Create: `tests/Game2048.Tests/TestDoubles/DeterministicGame2048.cs` — test subclass overriding random and wall posting
- Create: `tests/Game2048.Tests/TestSupport/Game2048TestSupport.cs` — shared helpers for resetting shared leaderboard state and, if needed, seeding exact board layouts without changing production API
- Create: `tests/Game2048.Tests/Core/GameBoardTest.cs` — module boundary tests for rules only
- Create: `tests/Game2048.Tests/Presentation/GameStatePresenterTest.cs` — module boundary tests for rendering only
- Create: `tests/Game2048.Tests/Leaderboard/LeaderboardServiceTest.cs` — module boundary tests for leaderboard logic only
- Create: `tests/Game2048.Tests/ApprovalFiles/FullJourney.approved.txt`
- Create: `tests/Game2048.Tests/ApprovalFiles/WinJourney.approved.txt`
- Create: `tests/Game2048.Tests/ApprovalFiles/LeaderboardJourney.approved.txt`

### Docs / config
- Modify: `README.md` — document the new Game2048 module split, deterministic seam rationale, and the Game2048-specific test/coverage commands

---

### Gate 1: Carve deterministic seams without changing the public API

**Goal:**
- Make `Game2048` testable through subclass overrides for randomness and wall-posting
- Prove the façade can be exercised deterministically without real network traffic

**Files:**
- Delete: `tests/Game2048.Tests/Game2048Test.cs`
- Create: `tests/Game2048.Tests/Game2048FacadeTest.cs`
- Create: `tests/Game2048.Tests/TestDoubles/DeterministicGame2048.cs`
- Create: `tests/Game2048.Tests/TestSupport/Game2048TestSupport.cs`
- Modify: `src/Game2048.Game/Game2048.cs`

**Preconditions / Notes:**
- Do not refactor rendering, rules, or leaderboard storage yet; this gate is only about adding override seams and the smallest test harness needed to prove them
- Keep existing public methods, public fields, and the Web-facing behavior unchanged
- Keep `Game2048.Tile` available unless a later compile-verified cleanup proves it is unused outside the façade
- Preserve process-wide leaderboard semantics for now; tests may need a reset helper because the existing implementation is static/shared

**Verification:**
- Run: `dotnet test tests/Game2048.Tests/Game2048.Tests.csproj --filter "FullyQualifiedName~Game2048FacadeTest"`
- Expected: PASS for tests that prove a test subclass can control tile generation and capture leaderboard wall messages without making a real HTTP call

**Continue when:**
- `addTile()` uses a façade seam for random values
- `saveLeaderboardRecord()` uses a façade seam for wall posting
- The seam can be overridden entirely from the test project, and the targeted tests pass without network access

**Stop and report when:**
- Achieving deterministic behavior would require changing the public `Game2048` API or the Web endpoint contract
- The override-only approach cannot isolate the HTTP side effect without a broader architectural change than the approved design
- Shared static state cannot be safely reset from tests without introducing destructive behavior into production code

- [ ] Step 1: Write `Game2048FacadeTest.cs` first, including at least one test for deterministic tile generation and one test proving wall posting can be intercepted in a subclass
- [ ] Step 2: Run the targeted test command and confirm the RED state (compile failure or runtime failure for the expected missing seam reason)
- [ ] Step 3: Implement the minimal `protected virtual` methods in `Game2048.cs` and route `addTile()` / `saveLeaderboardRecord()` through them
- [ ] Step 4: Add or update the test double and support helper only as needed to make the targeted tests expressive; avoid adding production test-only APIs
- [ ] Step 5: Run the targeted test command again and confirm GREEN before moving on

---

### Gate 2: Build approval-style safety nets and lock coverage before refactoring

**Goal:**
- Add approval-style scenario tests that lock the current externally visible behavior of the façade
- Reach a hard branch-coverage threshold of 90%+ for the Game2048 test project before structural refactoring begins

**Files:**
- Modify: `tests/Game2048.Tests/Game2048.Tests.csproj`
- Modify: `tests/Game2048.Tests/TestDoubles/DeterministicGame2048.cs`
- Modify: `tests/Game2048.Tests/TestSupport/Game2048TestSupport.cs`
- Create: `tests/Game2048.Tests/Game2048ApprovalFixture.cs`
- Create: `tests/Game2048.Tests/ApprovalTest.cs`
- Create: `tests/Game2048.Tests/Game2048GuardTest.cs`
- Create: `tests/Game2048.Tests/ApprovalFiles/FullJourney.approved.txt`
- Create: `tests/Game2048.Tests/ApprovalFiles/WinJourney.approved.txt`
- Create: `tests/Game2048.Tests/ApprovalFiles/LeaderboardJourney.approved.txt`

**Preconditions / Notes:**
- Gate 1 must be green first
- Keep production structure mostly intact during this gate; avoid mixing test creation with module extraction
- Approval output must be deterministic: normalize line endings, keep ordering stable, and render only behaviorally meaningful data
- Use approval scenarios for long flows and separate precise tests for branches such as invalid player names, unfinished games, repeat saves, and unknown-player rank lookup
- If a scenario needs an exact board shape, use test-side helpers (for example reflection-based seeding) rather than adding new production methods solely for tests

**Verification:**
- Run: `dotnet test tests/Game2048.Tests/Game2048.Tests.csproj`
- Expected: PASS for the full Game2048 test project, including approval and guard tests
- Run: `dotnet test tests/Game2048.Tests/Game2048.Tests.csproj /p:CollectCoverage=true /p:CoverletOutputFormat=json /p:Threshold=90 /p:ThresholdType=branch /p:ThresholdStat=total`
- Expected: PASS, with the branch threshold enforced by the command itself

**Continue when:**
- `FullJourney`, `WinJourney`, and `LeaderboardJourney` are checked in as stable approval files and pass consistently
- Guard tests cover the missing branches that do not fit naturally into approval snapshots
- The explicit branch-coverage command passes at or above 90%

**Stop and report when:**
- Stable deterministic scenarios still cannot reach the required branches without adding test-only production hooks
- The coverage threshold can only be met by excluding meaningful production files or masking real behavior
- Approval output keeps changing for reasons unrelated to intentional behavior changes

- [ ] Step 1: Write `ApprovalTest.cs`, `Game2048ApprovalFixture.cs`, and `Game2048GuardTest.cs` first, covering the three approved scenario families plus branch-focused guards
- [ ] Step 2: Run the Game2048 test project and confirm RED because approvals are missing and/or guard assertions fail for the current behavior
- [ ] Step 3: Update the test project file with coverage support and approval-file copy settings so the tests can locate `.approved.txt` snapshots from `AppContext.BaseDirectory`
- [ ] Step 4: Implement the minimal fixture/rendering/test-double support needed to make the scenario output stable; create the `.approved.txt` files from reviewed actual output, then rerun the tests
- [ ] Step 5: Run both the normal test command and the explicit coverage-threshold command; do not begin refactoring until both are green

---

### Gate 3: Extract the core rules module under the safety net

**Goal:**
- Move board rules, movement, scoring, win/lose logic, and tile insertion out of `Game2048.cs` into a dedicated rules module while preserving façade behavior

**Files:**
- Create: `src/Game2048.Game/Properties/AssemblyInfo.cs`
- Create: `src/Game2048.Game/Core/BoardTile.cs`
- Create: `src/Game2048.Game/Core/GameBoard.cs`
- Modify: `src/Game2048.Game/Game2048.cs`
- Optional modify: `tests/Game2048.Tests/TestSupport/Game2048TestSupport.cs` if helper access must adjust to the new structure without changing assertions

**Preconditions / Notes:**
- Gate 2 must be fully green, including branch coverage
- This is a refactor gate: behavior must stay locked by the approval files already checked in
- Keep the façade method names and semantics stable; `right()/up()/down()` may still delegate through rotation internally if that remains the easiest equivalent implementation inside `GameBoard`
- Preserve the score, win, lose, can-move, and reset semantics already captured by the snapshots

**Verification:**
- Run: `dotnet test tests/Game2048.Tests/Game2048.Tests.csproj`
- Expected: PASS, with no approval file changes required

**Continue when:**
- `Game2048.cs` no longer owns the rule implementation details directly
- `GameBoard` owns board state and movement behavior
- All existing approval and guard tests still pass unchanged

**Stop and report when:**
- Extraction requires changing the façade API, static/shared behavior, or the captured approval output for reasons other than a true bug fix
- The rules module cannot be tested internally without making a broad public-surface expansion not covered by the approved design

- [ ] Step 1: Use the existing green safety-net tests as the refactor protection baseline; if any new helper test is needed for the extraction, write it first and watch it fail
- [ ] Step 2: Extract `BoardTile` and `GameBoard` with the smallest possible moves, keeping randomness driven by a callback so the façade seam still controls determinism
- [ ] Step 3: Rewire `Game2048` to delegate reset/move/canMove/score/win/lose concerns to the rules module while keeping the public façade intact
- [ ] Step 4: Run the full Game2048 test project immediately after the extraction and resolve any regressions before touching the presentation or leaderboard concerns
- [ ] Step 5: Keep any checkpoint commit narrowly scoped to the rules extraction if the runner workflow benefits from a rollback point

---

### Gate 4: Extract presentation and leaderboard modules, then add explicit module-boundary tests

**Goal:**
- Move rendering/state mapping and leaderboard logic into separate modules
- Add dedicated tests that describe each module boundary explicitly

**Files:**
- Create: `src/Game2048.Game/Presentation/GameStatePresenter.cs`
- Create: `src/Game2048.Game/Leaderboard/InMemoryLeaderboardStore.cs`
- Create: `src/Game2048.Game/Leaderboard/LeaderboardService.cs`
- Create: `src/Game2048.Game/Models/Game2048State.cs`
- Create: `src/Game2048.Game/Models/TileState.cs`
- Create: `src/Game2048.Game/Models/LeaderboardEntry.cs`
- Modify: `src/Game2048.Game/Game2048.cs`
- Optional modify: `src/Game2048.Web/Program.cs` if only namespace/file organization changes require it
- Create: `tests/Game2048.Tests/Core/GameBoardTest.cs`
- Create: `tests/Game2048.Tests/Presentation/GameStatePresenterTest.cs`
- Create: `tests/Game2048.Tests/Leaderboard/LeaderboardServiceTest.cs`
- Modify: `tests/Game2048.Tests/Game2048.Tests.csproj` if folder globs or content items need adjustment

**Preconditions / Notes:**
- Gate 3 must be green first
- Keep `Game2048` as the façade/orchestrator that exposes the existing public API and the override seams
- The presentation module should own mapping to `Game2048State`, tile colors/fonts/offsets, overlay/messages, `ScoreText`, and `ScoreTextDrawCount`
- The leaderboard module should own save-once validation, best-score retention, rank calculation, and publish-message orchestration, but wall posting must still flow through the façade seam rather than directly creating `HttpClient`
- Prefer `internal` module classes plus `InternalsVisibleTo` over widening the public API solely for tests

**Verification:**
- Run: `dotnet test tests/Game2048.Tests/Game2048.Tests.csproj`
- Expected: PASS for approval tests, guard tests, and the new module-boundary tests

**Continue when:**
- `Game2048.cs` is reduced to façade/orchestration responsibilities
- `GameStatePresenter` and `LeaderboardService` own their respective concerns
- `GameBoardTest`, `GameStatePresenterTest`, and `LeaderboardServiceTest` each describe only their module’s contract and pass together with the safety net

**Stop and report when:**
- The extraction would require removing or renaming existing public façade members to keep the code compiling
- Module tests can only be written by reaching through multiple layers at once, indicating the proposed boundaries are not actually holding
- Approval diffs show user-visible behavior changes that are not justified by an intentional bug fix agreed in scope

- [ ] Step 1: Write the first boundary tests for each new module before finalizing the extraction, and run them to confirm RED for the missing types/behavior
- [ ] Step 2: Extract `GameStatePresenter` and move `paint()` / `drawTile()` behavior into it while preserving message order, tile colors, offsets, and `ScoreTextDrawCount`
- [ ] Step 3: Extract `InMemoryLeaderboardStore` and `LeaderboardService`, routing save/get-position/get-entries through the service while preserving shared in-memory semantics and tie-ranking behavior
- [ ] Step 4: Rewire `Game2048` to orchestrate rules + presenter + leaderboard service, then run the full Game2048 test project again and confirm GREEN
- [ ] Step 5: Tighten visibility (`internal` where possible) only after the tests prove the module boundaries are stable

---

### Gate 5: Promote durable docs and run final repository verification

**Goal:**
- Move the durable architecture/testing knowledge into the project’s long-lived docs
- Finish with fresh evidence that the repository still builds and tests correctly

**Files:**
- Modify: `README.md`
- Review (no changes expected unless verification reveals a compatibility issue): `src/Game2048.Web/Program.cs`, `tests/Game2048.Tests/Game2048.Tests.csproj`

**Preconditions / Notes:**
- Gates 1–4 must already be green
- The docs update should describe the architecture that actually exists in the code, not the original design intent if the implementation evolved slightly
- Keep the README changes focused on durable knowledge: module split, deterministic seams, and how to run the new approval/coverage checks

**Verification:**
- Run: `dotnet test tests/Game2048.Tests/Game2048.Tests.csproj`
- Expected: PASS
- Run: `dotnet test tests/Game2048.Tests/Game2048.Tests.csproj /p:CollectCoverage=true /p:CoverletOutputFormat=json /p:Threshold=90 /p:ThresholdType=branch /p:ThresholdStat=total`
- Expected: PASS at or above the required branch threshold
- Run: `dotnet test LegacyCode.sln`
- Expected: PASS for the full solution

**Continue when:**
- The README accurately documents the new Game2048 structure and verification commands
- The Game2048 test project and the full solution both pass on a fresh run
- Coverage still clears the 90% branch threshold after all refactoring and cleanup

**Stop and report when:**
- Final solution verification fails after one bounded local fix attempt
- The durable documentation target in `README.md` proves misleading or conflicts with established project documentation structure
- The final coverage threshold drops below 90% and cannot be recovered without reopening the approved scope

- [ ] Step 1: Update `README.md` to describe the new Game2048 module split, the deterministic test seams, and the exact Game2048 verification commands
- [ ] Step 2: Run the Game2048 test project and the explicit coverage-threshold command; confirm both are GREEN with fresh output
- [ ] Step 3: Run `dotnet test LegacyCode.sln` and confirm the whole repository still passes
- [ ] Step 4: Re-read the approved design and this plan, then verify the implementation matches the promised compatibility constraints and module boundaries
- [ ] Step 5: Only after all verification commands are green, prepare the final summary with evidence and any follow-up notes
