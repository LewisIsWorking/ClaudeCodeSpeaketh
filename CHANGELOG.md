# Changelog

All notable changes to ClaudeCodeSpeaketh. Newest first.

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
