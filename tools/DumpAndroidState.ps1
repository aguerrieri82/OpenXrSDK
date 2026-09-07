param(
    [string]$Label = "snapshot",
    [string]$Package = "net.eusoft.oculus",
    [string]$Serial = ""
)

$ErrorActionPreference = "Stop"
$env:ANDROID_USER_HOME = Join-Path $env:USERPROFILE ".android"

if ([string]::IsNullOrWhiteSpace($Serial))
{
    $devices = @(& adb devices | Select-Object -Skip 1 | Where-Object { $_ -match "\sdevice$" } | ForEach-Object { ($_ -split "\s+")[0] })

    if ($devices.Count -ne 1)
        throw "Expected exactly one connected ADB device; found $($devices.Count). Pass -Serial explicitly."

    $Serial = $devices[0]
}

$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$outputRoot = Join-Path $PSScriptRoot "diagnostics\quest-state"
$outputPath = Join-Path $outputRoot "$stamp-$Label"
New-Item -ItemType Directory -Path $outputPath -Force | Out-Null

function Write-AdbDump([string]$Name, [string[]]$Arguments)
{
    $path = Join-Path $outputPath "$Name.txt"
    "adb -s $Serial $($Arguments -join ' ')" | Set-Content $path
    "Captured: $(Get-Date -Format o)" | Add-Content $path
    "" | Add-Content $path

    & adb -s $Serial @Arguments 2>&1 | Add-Content $path
    "" | Add-Content $path
    "Exit code: $LASTEXITCODE" | Add-Content $path
}

function Write-ShellDump([string]$Name, [string]$Command)
{
    Write-AdbDump $Name @("shell", $Command)
}

@(
    "Captured: $(Get-Date -Format o)"
    "Serial: $Serial"
    "Package: $Package"
    "Host: $env:COMPUTERNAME"
) | Set-Content (Join-Path $outputPath "manifest.txt")

Write-AdbDump "adb-devices" @("devices", "-l")
Write-ShellDump "properties" "getprop"
Write-ShellDump "settings-global" "settings list global"
Write-ShellDump "settings-secure" "settings list secure"
Write-ShellDump "settings-system" "settings list system"
Write-ShellDump "device-config" "device_config list"
Write-ShellDump "environment" "env"
Write-ShellDump "packages" "pm list packages -f -U"
Write-ShellDump "packages-renderdoc" "pm list packages -f | grep -i renderdoc"
Write-ShellDump "package-target" "dumpsys package $Package"
Write-ShellDump "package-renderdoc" "for p in \$(pm list packages | cut -d: -f2 | grep -i renderdoc); do echo ==== \$p; dumpsys package \$p; done"
Write-ShellDump "processes" "ps -A -o USER,PID,PPID,VSZ,RSS,WCHAN,ADDR,S,NAME"
Write-ShellDump "process-target" "pid=\$(pidof $Package); echo PID=\$pid; if [ -n \"\$pid\" ]; then cat /proc/\$pid/status; cat /proc/\$pid/limits; cat /proc/\$pid/smaps_rollup; tr '\\0' '\\n' < /proc/\$pid/environ; fi"
Write-ShellDump "proc-cpuinfo" "cat /proc/cpuinfo"
Write-ShellDump "proc-meminfo" "cat /proc/meminfo"
Write-ShellDump "proc-vmstat" "cat /proc/vmstat"
Write-ShellDump "proc-loadavg" "cat /proc/loadavg"
Write-ShellDump "proc-uptime" "cat /proc/uptime"
Write-ShellDump "proc-version" "cat /proc/version"
Write-ShellDump "proc-mounts" "cat /proc/mounts"
Write-ShellDump "sys-kgsl" "for f in /sys/class/kgsl/kgsl-3d0/*; do if [ -f \"\$f\" ]; then echo ==== \$f; cat \$f 2>&1; fi; done"
Write-ShellDump "sys-devfreq" "find /sys/class/devfreq -maxdepth 3 -type f 2>/dev/null | while read f; do echo ==== \$f; cat \$f 2>&1; done"
Write-ShellDump "sys-thermal" "for z in /sys/class/thermal/thermal_zone*; do echo ==== \$z; cat \$z/type 2>&1; cat \$z/temp 2>&1; cat \$z/mode 2>&1; done"
Write-ShellDump "dumpsys-services" "dumpsys -l"
Write-ShellDump "dumpsys-all" "dumpsys"
Write-ShellDump "dumpsys-activity-processes" "dumpsys activity processes"
Write-ShellDump "dumpsys-activity-activities" "dumpsys activity activities"
Write-ShellDump "dumpsys-window" "dumpsys window"
Write-ShellDump "dumpsys-display" "dumpsys display"
Write-ShellDump "dumpsys-surfaceflinger" "dumpsys SurfaceFlinger"
Write-ShellDump "dumpsys-gfxinfo" "dumpsys gfxinfo $Package"
Write-ShellDump "dumpsys-meminfo" "dumpsys meminfo $Package"
Write-ShellDump "dumpsys-cpuinfo" "dumpsys cpuinfo"
Write-ShellDump "dumpsys-gpu" "dumpsys gpu"
Write-ShellDump "dumpsys-power" "dumpsys power"
Write-ShellDump "dumpsys-thermal" "dumpsys thermalservice"
Write-ShellDump "dumpsys-battery" "dumpsys battery"
Write-ShellDump "dumpsys-audio-flinger" "dumpsys media.audio_flinger"
Write-ShellDump "dumpsys-audio-policy" "dumpsys media.audio_policy"
Write-ShellDump "cmd-gpu-vkjson" "cmd gpu vkjson"
Write-ShellDump "renderdoc-focused" "echo ==== PROPERTIES; getprop | grep -Ei 'renderdoc|rdoc|vulkan|gpu|angle|vr.profiler|perf_harden'; echo ==== GLOBAL_SETTINGS; settings list global | grep -Ei 'renderdoc|gpu|vulkan|angle|profiler'; echo ==== PACKAGES; pm list packages | grep -i renderdoc"
Write-AdbDump "logcat" @("logcat", "-d", "-v", "threadtime")

Write-Output $outputPath
