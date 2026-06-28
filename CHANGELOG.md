# Changelog

All notable changes to ClaudeCodeSpeaketh. Newest first.

## 0.20.0
- Updating the app now refreshes the Claude Code hook scripts automatically. The
  in-app updater redeploys them to ~/.claude/hooks the moment the new version is
  applied (via Velopack's after-update callback), so speech fixes take effect on
  Claude's next turn with no manual restart. (A startup redeploy stays as backup.)

## 0.19.0
- Table skipping now covers every style, not just Unicode box-drawing grids:
  markdown pipe tables ("| a | b |", "|---|---|") and ASCII "+----+" tables are
  also dropped before speaking. Prose containing a stray "a | b" is unaffected.
  (If a table was still being read, restart the app once so the updated hook
  redeploys to ~/.claude/hooks.)

## 0.18.0
- Pronunciation: "lich" is now read as "litch" (and "liches" as "litches"), so the
  undead spellcaster sounds right. Add more such fixes in speak-response.ps1.

## 0.17.0
- Sessions tab now shows only currently-open terminals: it lists sessions active in
  the last 15 minutes (was 24h) and auto-refreshes every 30s, so closed/idle terminals
  drop off and active ones appear on their own. Muting still persists per terminal.

## 0.16.0
- Cleaner speech: box-drawing tables (┌─┬┐ │ └─┴┘) are now skipped entirely instead
  of being read as a jumble of cell text.
- Pronunciation fixes: "~10" is read as "about 10", "PF2e"/"SF2e" as
  "Pathfinder"/"Starfinder", plus TTRPG, AoN, e.g., i.e. and vs. More can be added
  easily in speak-response.ps1.

## 0.15.0
- Playback transport: Back / Pause-Resume / Skip controls on both the main window
  (footer) and the karaoke overlay. Back replays the previous response, Pause/Resume
  pauses the current one (neural or Classic karaoke), Skip stops it and moves on.
- The karaoke overlay is now resizeable — drag the bottom-right corner grip; drag the
  text to move it. Your size and position are remembered for the session.

## 0.14.0
- Volume now works on the neural (edge) engine, not just Classic/SAPI. The slider
  was previously ignored for the default voice; it now sets the playback volume on
  both the plain and karaoke neural paths. As with every setting, it applies live
  on the next response — no Save click needed.
- Startup launch mode: choose whether sign-in launch opens hidden in the tray or
  with the window showing (a combo box next to the Startup tick box).

## 0.13.0
- Start at Windows sign-in: a "Startup" tick box on the General tab. When on,
  the app launches hidden in the tray at logon so the speech daemon is always
  ready (registered via the per-user registry Run key — no admin prompt).
- This scrollable changelog, shown right here on the Updates tab.

## 0.12.0
- Settings now apply live: changing the voice, engine, karaoke colour/size/
  position, length, or a session mute takes effect on the next response with no
  Save click (the daemon re-reads config each turn).
- Richer Sessions list: each terminal shows a second line with its git branch,
  full working path, and last-active time.
- App logo generated locally with SDXL/DreamShaper.

## 0.11.0
- Taskbar icon: the .exe now carries the logo as its Win32 icon (previously only
  the title bar / alt-tab were branded).

## 0.9.0
- Sessions tab discovers already-running terminals from the transcript store
  (not just ones heard live), with a Refresh button.
- Offline karaoke for the Classic (SAPI) engine via word-boundary events.
- Karaoke font-size and on-screen position (Center / Bottom / Top).
- Self-signed code-signing for the installer (removes "Unknown Publisher"
  locally; SmartScreen for downloads still needs an EV cert).

## 0.8.0
- Hook tab: install/uninstall the Claude Code Stop hook from the UI, preserving
  any other settings.json entries. Completes the original roadmap.

## 0.7.0
- Karaoke window: highlights each word as it is spoken, in a colour you choose.

## 0.6.0
- Resident tray app + cross-session speech queue: multiple Claude Code terminals
  are spoken in turn; mute individual terminals from the Sessions tab.

## 0.5.0
- Neural engine (edge-tts): free, keyless, natural voices. The Irish female
  voice "Emily" is the default on fresh installs.

## 0.4.0
- In-app auto-update via the public GitHub release feed.

## 0.2.0
- Install extra Windows voices from the UI, with a real progress percentage.

## 0.1.0
- First release: configure the Claude Code TTS hook — on/off, voice, rate,
  volume, and how much of each response to read (default: the whole thing).
