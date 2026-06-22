using ClaudeCodeSpeaketh.Helpers;

namespace ClaudeCodeSpeaketh.Services;

// Installs the Irish English (en-IE) text-to-speech voice ("Orla") as a Windows
// Feature on Demand, via one elevated PowerShell call. Falls back to installing
// the full en-IE language if the targeted TTS capability is unavailable.
internal sealed class IrishVoiceInstallService
{
    // Single-line so it survives -Command quoting; no Read-Host (runs headless-ish).
    private const string InstallCommand =
        "$ErrorActionPreference='Stop'; " +
        "$c = Get-WindowsCapability -Online | Where-Object { $_.Name -like 'Language.TextToSpeech*en-IE*' }; " +
        "if ($c) { $c | Where-Object { $_.State -ne 'Installed' } | " +
        "ForEach-Object { Add-WindowsCapability -Online -Name $_.Name | Out-Null } } " +
        "else { Install-Language en-IE }";

    public ElevatedResult Install() => ElevatedRunner.RunPowerShell(InstallCommand);
}
