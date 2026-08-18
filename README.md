# ClaudeCodeSpeaketh 🗣️

## Open Source Maintenance Fee

This project requires an [Open Source Maintenance Fee](https://opensourcemaintenancefee.org/) from
organisations that use the **official binary release** as part of revenue-generating activity and
have annual gross revenue of **US$10,000 or more**.

- The **source stays free** under this repository's LICENSE. The fee is not a licence fee.
- **Self-compiled binaries are exempt.** You may always build from source under the LICENSE.
- Organisations under US$10,000 annual gross revenue are **exempt**.
- Individuals, hobbyists and personal use are **exempt**.
- Issues, discussions and pull requests stay **open to everyone**, fee or not.

Pay the fee via [GitHub Sponsors](https://github.com/sponsors/LewisIsWorking) at the
**Maintenance Fee** tier. Full terms: [OSMFEULA.txt](OSMFEULA.txt).

A small Avalonia desktop app to configure **Claude Code's text-to-speech** — the
feature that reads Claude's responses aloud on Windows via a Claude Code `Stop` hook.

## What it does

- **On / off** — master toggle for spoken responses.
- **Voice** — pick from classic Windows **SAPI** voices *or* downloadable **Piper**
  neural voices, with a switchable default engine.
- **Download more voices**
  - *SAPI:* detect Windows OneCore voices (incl. the Irish **Orla**, en-IE) and
    one-click enable them for the speech engine (single elevation), plus a shortcut
    to add the English (Ireland) speech pack.
  - *Piper:* download free, local, GPU-capable `.onnx` neural voices in-app.
- **Read more / less per turn** — cap how much of each response is spoken; the
  default is **ALL** (the entire response).
- **Preview** any voice before committing.
- **Hook management** — install / remove the `Stop` hook in Claude Code's
  `settings.json` without disturbing other hooks.

## How it works

The app writes a shared config file at `~/.claude/hooks/tts-config.json`. The Claude
Code `Stop` hook (`speak-response.ps1` + `tts-speaker.ps1`) reads that file each turn
and speaks the last assistant response through the chosen engine.

## Build & run

```powershell
dotnet run --project ClaudeCodeSpeaketh/ClaudeCodeSpeaketh.csproj
```

Windows-only (uses `System.Speech` and the Windows speech registry).

## Stack

.NET 10 · Avalonia 12.0.4 (Fluent + Inter) · CommunityToolkit.Mvvm.
