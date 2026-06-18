# Project Cleanup Design

Date: 2026-06-18

## Goal

Clean up the Smart Emergency Route Planner repository end to end while preserving the existing routing behavior. The cleanup should leave the project easier to build, easier to explain, and easier to maintain.

## Current State

- The project now builds as a Blazor WebAssembly app using `Microsoft.NET.Sdk.BlazorWebAssembly`.
- The README still describes the app primarily as a console application with a 14-option CLI.
- `bin/` and `obj/` are tracked in Git even though `.gitignore` now ignores build outputs.
- There are two frontend surfaces:
  - `src/Pages/Home.razor` and `wwwroot/` for the Blazor app.
  - `visualizer/` for the older static client-side visualizer.
- `src/Pages/Home.razor` is very large and mixes markup, UI state, graph interaction, route solving, animation, logging, and formatting.

## Scope

### Repository Hygiene

- Stop tracking generated build outputs under `bin/` and `obj/`.
- Keep `.gitignore` as the source of truth for generated files.
- Do not delete user-authored source, reports, benchmark CSVs, or docs.

### Documentation

- Update README to describe the current Blazor WebAssembly application.
- Keep the academic algorithm explanation, benchmark instructions, and report context, but remove or revise stale console-first instructions.
- Document the static `visualizer/` as a legacy/reference visualizer if it remains in the repo.
- Clarify build and run commands for the current app.

### Frontend Structure

- Treat the Blazor app as the primary application surface.
- Preserve `visualizer/` unless it is clearly redundant and safe to move. If moved, keep it available under a clearly named legacy/reference location.
- Refactor `Home.razor` into smaller units without changing UI behavior:
  - Component markup for major panels.
  - Small local models for UI-only records.
  - Helper methods or services for route solving, formatting, and log handling where that reduces file size and responsibility overlap.
- Keep existing styling unless targeted fixes are needed for broken layout or duplicated assets.

### Algorithm Code

- Preserve existing solver APIs unless a small change is required by the Blazor refactor.
- Do not rewrite pathfinding algorithms as part of cleanup.
- Keep tests focused on ensuring existing route behavior still works after file movement.

## Non-Goals

- No visual redesign.
- No new routing features.
- No replacement of the existing algorithms with external libraries.
- No broad performance rewrite.
- No destructive reset of the current dirty worktree.

## Verification

- Run `dotnet build`.
- Run the existing correctness suite through the Blazor UI code path if practical, or through a small command/test harness if one already exists.
- Inspect `git status --short` to confirm generated build outputs are no longer tracked.
- If a dev server is started, verify that the Blazor app loads and the primary route controls render.

## Risks

- Refactoring `Home.razor` can accidentally break event binding or Blazor state updates.
- Removing tracked build outputs changes Git status substantially, but it is the correct repository hygiene fix.
- Keeping both frontend surfaces may confuse future contributors unless README names the primary app clearly.

## Decisions

- Blazor WebAssembly is the primary app.
- Static `visualizer/` is retained as legacy/reference during this cleanup unless the implementation audit finds exact duplicated files that can be moved without losing access.
- Cleanup proceeds incrementally: repository hygiene, docs, structure, refactor, verification.
