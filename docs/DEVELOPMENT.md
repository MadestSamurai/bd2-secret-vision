# 开发 / Development

- `core/`: pure C# timing, boundary search, route selection, orchestration and local communication.
- `hook/`: owned in-process source using the game's normal movement and UI methods.
- `compatibility/`: structural binding contracts and local component compilation.
- `tests/`: 624 deterministic synthetic oracle cases, 689 assertions including result identity and settings.
- `compatibility-tests/`: 24 synthetic metadata assertions.
- `desktop/`: WPF UI, live read-only board, persistent settings, bilingual controls and in-window diagnostics.

The core builds without the game installed. The compatibility CLI compiles against local game assemblies but does not inject or run them. Test fixtures contain synthetic boards and values, not private captures.

控制设计 / Control design: commands carry a unique ID, component session, owner and expiry. A process-bound six-second renewable lease gates gameplay. A stopped or expired owner releases input and pauses. Server success must match stage, round, session, timestamp and natural end. Unknown command outcomes stop rather than retry.

The lifecycle suite uses a fake game port to cover stages, retries, attempt limits, stop, exclusive ownership and stale/missing server evidence. The desktop smoke path never finds or connects to a game process. Stage 1 has one naturally completed 100% run with matching server confirmation. Other stages, natural disconnection, and killing the application while movement is held remain separate live acceptance cases.

Build with `./build.ps1 -Locked`, then package with `./package.ps1 -Locked`. The script checks both single-file flavors, their runtime configuration, identity and bilingual UI before writing final hashes.


## File and heartbeat reliability (0.1.1)

Existing-file publication uses `File.Replace`, with unique temporary files and at most about 600 ms of retry for transient sharing/access conflicts. New files are created without overwriting another publisher. Cleanup cannot mask the first exception; the old destination is never deliberately deleted before publication. Readers tolerate transient open failures and retain freshness limits.

`ControlHeartbeat` renews the six-second runtime lease every second. File failures retry every 200 ms within the last valid lease; the controller cancels before expiry on sustained failure and never resumes that failed lease automatically. Stop joins the renewal task before writing the disabled record. Diagnostic failures are best effort; failure to revoke still leaves the runtime expiry as the final fail-safe.

`FileReliabilityTests` exercises actual Windows file handles at normal privilege, including concurrent reads, brief/permanent exclusive locks, and actual heartbeat cancellation/recovery. Lifecycle tests additionally verify that progress logging does not abort gameplay and shutdown errors do not replace the original cause. All test roots are isolated; tests never connect to the live game.


## Live start (0.1.2)

There is no pause-on-start or automatic resume command. The host waits for a fresh Playing board with the current round ID, then plans from live snapshots. Native departure guards recheck the current attack window before drawing. Route completion releases movement without pausing; Stop and unrecoverable control conditions still release input safely. The pause/resume UI animation race is removed from the normal path. Manual pauses and control faults end the task instead of invoking unlimited round retries. StartFlowTests exercises live planning and interrupted states; fake-game lifecycle tests use Playing start snapshots.

## Suppressed attacks (0.1.3)

The normal/special elapsed timers keep advancing while `_carveState` blocks their dispatch. Power items cancel the special coroutine without clearing Ready; when Power expires, movement remains excluded by Ready. `AttackOpportunity.cs` is compiled into both host and in-game component. It uses a confirmed canceled Ready coroutine as a bounded 30-second rolling planning horizon, or the earliest possible wall arrival for active carving. It does not infer immobility from observed position alone. Active time-stop transitions cap the bound. Windows are re-read before each route and adjusted for travel.

The wall cache contains immutable Wall cells for the current grid identity, including interior walls. Distances use cell areas with a two-cell margin. Moving carving bodies and tails retain their collision forecasts. Only a confirmed interrupted head receives the stationary horizon. Existing projectile guards still apply. Synthetic tests reproduce overdue timers, long-route selection, release, short control, wall contact, active tails and stationary collision hazards. In-game diagnostics record carve state, special-coroutine interruption and each departure blocker. This is not evidence of an actual clear with the new component.

## Damaged topology and border lightning (0.1.4)

Cells and edges are separate native state. A promoted ClaimedBorder can remain between two empty cells; inferring every safe edge from adjacent claimed cells loses this information. Snapshots now include horizontal and vertical edges plus a hash of the complete graph, not just claimed percentage. The planner searches the current reachable component each time and predicts fill using the native outside-seeded flood rule. There is no fabricated straight-line closing segment. Route execution ends at the first actual closure, and the host records actual gained/lost cells. A completed route with no graph change is excluded until the graph changes, with direction-independent keys.

Border lightning hits safe-boundary traversal. The runtime prefers an immediate non-safe drawable edge followed by a short closure, with body/projectile and return-position checks. Only an imminent border threat permits a short exception to the ordinary attack timer gate. Normal routes keep their gate. Safe walking is the fallback where drawing is impossible. Swept segment distance detects crossings between samples. Protection runs during host planning gaps; queued host routes are deferred during an escape, then replanned from the new location.

Synthetic tests cover detached promoted edges, straight final closures, reverse zero-gain routes, cut-map reconnection, rectangular maps, actual fill, emergency first steps and early closure, fast projectile crossing and unchanged ordinary attack timing. This is offline evidence, not a claim of natural-game completion.

## Strategic priority regression (0.1.5)

The 0.1.4 unified local score starved rim access: optional item candidates suppressed access generation, and square-root cell rewards favored tiny central cuts. Access is now generated independently and ranked by actual reduction in distance to the outer rim. Once connected, routes are ranked by actual rim progress. A nearly complete closure takes precedence over minor repairs. Items must repay their full travel cost, and a strategic advance is required between optional pickups.

When every candidate is currently infeasible, the fallback is a short closure for the next safe window rather than the longest high-ratio route. A temporary attack-window refusal does not blacklist that objective; topology failures still trigger exclusions. Native escape execution is allowed to finish before the desktop submits another plan. Weighted safe-edge routing prefers vertices with drawable exits, avoiding long transfers trapped against map walls. All movement remains on native safe edges.

Every decision now logs its board input. Full-size 256×256 scenarios test rim arrival and complete closure, including nearby items and damaged sections, rather than only verifying small local captures. The in-game component is unchanged from 0.1.4.

## Confirmed result display (0.1.6)

After saving a confirmed success, the host holds the result for 8 seconds before the next stage or final exit. The run cancellation token remains active; stopping preserves the recorded clear and sends no subsequent start or exit. Failure retry timing is unchanged. Lifecycle tests inject a controlled delay to verify timing, evidence order, both transitions and immediate cancellation.

## GitHub Actions releases

Push an annotated `vX.Y.Z` tag matching `Directory.Build.props`. The **Publish release** workflow checks out that tag, builds and validates both editions on GitHub, then uploads six release assets. SHA256 digests and sizes are checked against GitHub before the draft becomes public. Local EXE uploads are not part of the release process.

For an existing tag, dispatch **Publish release** from the default branch with its tag name. This also supports tags created before the release workflow existed. Only draft assets may be replaced; an already published release requires a new version. Main-branch and pull-request checks remain separate.
