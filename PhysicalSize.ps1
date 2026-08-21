param(
    [Parameter(Mandatory = $true)]
    [string]$Path
)

Add-Type @"
using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

public static class FileIdNative
{
    [StructLayout(LayoutKind.Sequential)]
    public struct BY_HANDLE_FILE_INFORMATION
    {
        public uint FileAttributes;
        public System.Runtime.InteropServices.ComTypes.FILETIME CreationTime;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastAccessTime;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastWriteTime;
        public uint VolumeSerialNumber;
        public uint FileSizeHigh;
        public uint FileSizeLow;
        public uint NumberOfLinks;
        public uint FileIndexHigh;
        public uint FileIndexLow;
    }

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern SafeFileHandle CreateFile(
        string lpFileName,
        uint dwDesiredAccess,
        uint dwShareMode,
        IntPtr lpSecurityAttributes,
        uint dwCreationDisposition,
        uint dwFlagsAndAttributes,
        IntPtr hTemplateFile);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool GetFileInformationByHandle(
        SafeFileHandle hFile,
        out BY_HANDLE_FILE_INFORMATION lpFileInformation);
}
"@

function Format-Size([long]$bytes)
{
    if ($bytes -ge 1TB) { return "{0:N2} TB" -f ($bytes / 1TB) }
    if ($bytes -ge 1GB) { return "{0:N2} GB" -f ($bytes / 1GB) }
    if ($bytes -ge 1MB) { return "{0:N2} MB" -f ($bytes / 1MB) }
    if ($bytes -ge 1KB) { return "{0:N2} KB" -f ($bytes / 1KB) }

    return "$bytes B"
}

$GENERIC_READ = 0
$FILE_SHARE_READ = 1
$FILE_SHARE_WRITE = 2
$FILE_SHARE_DELETE = 4
$OPEN_EXISTING = 3
$FILE_FLAG_BACKUP_SEMANTICS = 0x02000000

$seen = [System.Collections.Generic.HashSet[string]]::new()

[long]$logicalSize = 0
[long]$physicalSize = 0
[long]$fileCount = 0
[long]$physicalFileCount = 0
[long]$hardLinkDuplicates = 0

$startTime = [DateTime]::Now

Get-ChildItem -LiteralPath $Path -Directory -Recurse -Force -ErrorAction SilentlyContinue |
Where-Object { $_.Name -eq "bin" -or $_.Name -eq "obj" } |
Where-Object {
    $parent = $_.Parent

    while ($parent)
    {
        if ($parent.Name -eq "bin" -or $parent.Name -eq "obj")
        {
            return $false
        }

        $parent = $parent.Parent
    }

    return $true
} |
ForEach-Object {
    Get-ChildItem -LiteralPath $_.FullName -File -Recurse -Force -ErrorAction SilentlyContinue
} |
ForEach-Object {

    $file = $_

    $fileCount++
    $logicalSize += $file.Length

    $handle = [FileIdNative]::CreateFile(
        $file.FullName,
        $GENERIC_READ,
        $FILE_SHARE_READ -bor $FILE_SHARE_WRITE -bor $FILE_SHARE_DELETE,
        [IntPtr]::Zero,
        $OPEN_EXISTING,
        $FILE_FLAG_BACKUP_SEMANTICS,
        [IntPtr]::Zero)

    if ($handle.IsInvalid)
    {
        # Failed to query identity: conservatively count as unique.
        $identity = $file.FullName
    }
    else
    {
        try
        {
            $info = New-Object FileIdNative+BY_HANDLE_FILE_INFORMATION

            if ([FileIdNative]::GetFileInformationByHandle($handle, [ref]$info))
            {
                $fileIndex = ([uint64]$info.FileIndexHigh -shl 32) -bor $info.FileIndexLow

                $identity = "{0:X8}:{1:X16}" -f `
                    $info.VolumeSerialNumber,
                    $fileIndex
            }
            else
            {
                $identity = $file.FullName
            }
        }
        finally
        {
            $handle.Dispose()
        }
    }

    if ($seen.Add($identity))
    {
        $physicalSize += $file.Length
        $physicalFileCount++
    }
    else
    {
        $hardLinkDuplicates++
    }

    if (($fileCount % 1000) -eq 0)
    {
        $saved = $logicalSize - $physicalSize

        Write-Host (
            "{0:N0} files | Logical {1} | Physical {2} | Saved {3} | Hard-link duplicates {4:N0}" -f
            $fileCount,
            (Format-Size $logicalSize),
            (Format-Size $physicalSize),
            (Format-Size $saved),
            $hardLinkDuplicates
        )
    }
}

$elapsed = [DateTime]::Now - $startTime
$savedSize = $logicalSize - $physicalSize

Write-Host ""
Write-Host "Completed"
Write-Host "---------"
Write-Host "Path:                  $Path"
Write-Host "Files scanned:         $fileCount"
Write-Host "Unique physical files: $physicalFileCount"
Write-Host "Hard-link duplicates:  $hardLinkDuplicates"
Write-Host "Logical size:          $(Format-Size $logicalSize)"
Write-Host "Physical size:         $(Format-Size $physicalSize)"
Write-Host "Saved by hard links:   $(Format-Size $savedSize)"

if ($logicalSize -gt 0)
{
    $savedPercent = ($savedSize / $logicalSize) * 100.0
    Write-Host ("Saved:                 {0:N2}%" -f $savedPercent)
}

Write-Host ("Elapsed:               {0:hh\:mm\:ss}" -f $elapsed)