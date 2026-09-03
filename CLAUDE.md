# releaseProcessor - working notes

Terminal app that takes single-pick release files, distributes them across the PTF folders that BarTender watches, tracks each job to completion, and reports the run.

## Status

Builds clean (`dotnet build`, 0 warnings).

## Layout

```
Configuration/   PathSchema (all directories, prod + test values), ConfigurationManager, ProcessingSettings
Errors/          Safely, ErrorOrExtensions, Error
Events/          event args for dashboard updates, job status changes, completion, errors
Processing/      SinglePickScanner, SinglePickParser, PtfDistributor, PrintJob, PrintJobTracker,
                 FolderWatcher, ArchiveService, TeamsNotification, BartenderSimulator
UI/              Spectre.Console screens: LaunchMenu, Dashboard, ConfigurationMenu, DisplayInfo, EndScreen
ReleaseApp.cs    main loop, menu dispatch
```

## How a run works

1. `SinglePickScanner` finds unprocessed `*SNGL` files (falls back to today's archive if the live directory is empty).
2. `SinglePickParser` turns them into `PrintJob` records keyed by carton ID.
3. `PtfDistributor` round-robins jobs across the PTF folders.
4. `FolderWatcher` + `PrintJobTracker` follow each job's file through the folders and raise events.
5. `UI/Dashboard` renders progress from those events; `TeamsNotification` posts the summary; `ArchiveService` files the inputs away.

## Conventions

- Filesystem work goes through `Safely.Run` and returns `ErrorOr<T>`. Same pattern as PrintFlow_V2 - the two `Errors/` folders are near-copies of each other.
- Directories come from `PathSchema`, never a literal at the call site. Each entry carries a description plus prod and test paths.
- Progress reaches the UI through the `Events/` classes, not by services writing to the console.

## Known state

- `Processing/BartenderSimulator.cs` is a fake BarTender for testing without the real software: one file per folder at a time, 3-10 second delays, random failures. Nothing calls it - the only reference is a commented-out line at `ReleaseApp.cs:168`. Keep it, it is a test harness, not abandoned code.
- `.config/dotnet-tools.json` pins csharpier. Run `dotnet tool restore` then `dotnet csharpier .` before committing formatting changes.

Dependencies: Spectre.Console, ErrorOr.
