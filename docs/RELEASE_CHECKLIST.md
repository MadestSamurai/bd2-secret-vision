# 发布检查 / Release checklist

Repository: `MadestSamurai/bd2-secret-vision` · Version: `0.1.6` · License: MIT

## 本版验证 / Validation for this version

- Independent C# / WPF source; no parent-project dependencies, Python runtime, game assemblies or private captures.
- 979 offline assertions: 689 core, 106 lifecycle/localization, 21 file/heartbeat, 16 start/control, 32 boss windows, 56 topology, 35 strategy and 24 structural compatibility.
- Both single-EXE editions pass 23 desktop smoke checks each, with runtime configuration and embedded identity checked.
- Current client: 15 required types / 57 members resolve and compile locally. Synthetic renaming, member reordering and unrelated extensions pass; missing or ambiguous interfaces are rejected.
- Stage 1 naturally reached 100% with 125.882 seconds remaining, no revive, and matching server confirmation on 2026-09-27. This does not establish success for the other four stages or every random combination.
- The success-screen delay, next-stage/final-exit sequencing and cancellation are covered by lifecycle tests. Natural process-termination lease expiry remains a separate live check.

## 每次发布 / For each release

- Review the exact staged file list; exclude game data, captures, credentials and local runtime files.
- Keep Chinese and English README behavior, defaults, attribution and links aligned.
- Keep one risk notice directly below each README title. Preserve MIT and dependency notices.
- Build and check both editions with `./package.ps1 -Locked` from a clean checkout.
- Check the ZIP documents and EXE against the reviewed files; generate SHA256 checksums.
- Commit the independent source and create an annotated version tag.
- Push the version tag to trigger **Publish release**, or dispatch it for an existing tag. Actions builds both editions, uploads both EXEs, both ZIPs, checksums and metadata, verifies remote digests, then publishes the standard bilingual Release. Do not upload local binaries.
- Verify public access, default branch, tag target, downloadable asset hashes and automated build results.

PublicRuntime4 is unchanged from local 0.1.4 / 0.1.5 builds; an existing connection can be reused without restarting the game.
