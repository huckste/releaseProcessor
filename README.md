# ReleaseProcessor

A terminal application for releasing carton label print jobs in a book distribution warehouse, and watching them through to completion.

Labels are printed by BarTender, which picks work up out of a set of folders. Feeding it meant splitting a release file by hand, dropping the pieces into those folders, and then watching a file explorer to see whether anything came back. A stuck job was invisible until someone noticed the floor had gone quiet.

ReleaseProcessor does the split, the distribution, and the watching, and reports the result.

## What it does

1. **Finds** unprocessed single-pick release files. If the live directory is empty it falls back to today's archive, so a rerun after a crash picks up where it left off.
2. **Parses** each caret-delimited line into a `PrintJob` keyed by carton ID.
3. **Distributes** jobs round-robin across the PTF folders BarTender watches, so no folder sits idle while another backs up.
4. **Tracks** every job through its lifecycle. `FolderWatcher` watches the PTF folders for renames (`.txt` to `.Processed` or `.Failed`) and the completed folder for new `.PRN` files, and raises events as each job moves.
5. **Retries** failures up to three times before marking a job as a permanent failure.
6. **Reports** a live dashboard while the run is going, and posts a summary to a Teams channel at the end.

## Design

Processing never writes to the console. Services raise events (`JobStatusChanged`, `JobCompleted`, `DashboardUpdate`, `AllJobsCompleted`, `ErrorOccurred`) and the UI subscribes to them. That keeps the Spectre.Console rendering out of the file-watching code and makes the tracker testable on its own.

Job state lives in a `ConcurrentDictionary<string, PrintJob>` because folder-watcher callbacks arrive on several threads at once.

Filesystem operations go through `Safely.Run`, which turns exceptions into `ErrorOr<T>` results. An unattended tool that dies on an unhandled exception halfway through a release is worse than one that reports the failure and keeps going.

## Structure

```
Configuration/   PathSchema (every directory, prod and test), ConfigurationManager, ProcessingSettings
Errors/          Safely, ErrorOr extensions
Events/          event args carrying status changes, completion, dashboard updates, errors
Processing/      SinglePickScanner, SinglePickParser, PtfDistributor, PrintJob, PrintJobTracker,
                 FolderWatcher, ArchiveService, TeamsNotification, BartenderSimulator
UI/              LaunchMenu, Dashboard, ConfigurationMenu, DisplayInfo, EndScreen
```

## Testing without BarTender

`BartenderSimulator` stands in for the real software: it takes one file per folder at a time, holds it for 3 to 10 seconds, and fails at random. That makes it possible to exercise the retry path and the dashboard without a printer or a BarTender license.

## Configuration

`PathSchema` holds every directory the app needs, each with a description, a production path, and a test path. Paths are edited from the configuration menu and stored in `config.json`; nothing is hard-coded at a call site.

## Running

```
dotnet run
```

.NET 10. Dependencies: Spectre.Console, ErrorOr. Formatting is csharpier - `dotnet tool restore` then `dotnet csharpier .`.
