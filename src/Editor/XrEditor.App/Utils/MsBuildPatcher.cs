using System.Diagnostics;
using System.IO;
using System.Text;

public static class MsBuildPatcher
{
    private const string TargetFileName = "Microsoft.Common.CurrentVersion.targets";

    private static readonly (string OldValue, string NewValue)[] Patches =
    [
        (
            "Condition=\"'$(BuildingInsideVisualStudio)' == 'true' or '$(CreateHardLinksForCopyLocalIfPossible)' == ''\"",
            "Condition=\"'$(CreateHardLinksForCopyLocalIfPossible)' == ''\""
        ),
        (
            "Condition=\"'$(BuildingInsideVisualStudio)' == 'true' or '$(CreateSymbolicLinksForCopyLocalIfPossible)' == ''\"",
            "Condition=\"'$(CreateSymbolicLinksForCopyLocalIfPossible)' == ''\""
        )
    ];

    public static void PatchVisualStudioLinks()
    {
        var vsInstallPath = FindVisualStudioInstallPath();

        var targetFiles = new[]
        {
            Path.Combine(
                vsInstallPath,
                "MSBuild",
                "Current",
                "Bin",
                TargetFileName),

            Path.Combine(
                vsInstallPath,
                "MSBuild",
                "Current",
                "Bin",
                "amd64",
                TargetFileName)
        };

        var patched = false;

        foreach (var targetFile in targetFiles)
            patched |= PatchTargets(targetFile);

        if (patched)
            Debugger.Break();
    }

    private static bool PatchTargets(string path)
    {
        if (!File.Exists(path))
            return false;

        var text = File.ReadAllText(path);
        var changed = false;

        foreach (var patch in Patches)
        {
            if (!text.Contains(patch.OldValue, StringComparison.Ordinal))
                continue;

            text = text.Replace(
                patch.OldValue,
                patch.NewValue,
                StringComparison.Ordinal);

            changed = true;
        }

        if (!changed)
            return false;

        File.WriteAllText(path, text, new UTF8Encoding(false));

        return true;
    }

    private static string FindVisualStudioInstallPath()
    {
        var programFilesX86 = Environment.GetFolderPath(
            Environment.SpecialFolder.ProgramFilesX86);

        var vsWherePath = Path.Combine(
            programFilesX86,
            "Microsoft Visual Studio",
            "Installer",
            "vswhere.exe");

        if (!File.Exists(vsWherePath))
            throw new FileNotFoundException("vswhere.exe not found.", vsWherePath);

        var startInfo = new ProcessStartInfo
        {
            FileName = vsWherePath,
            Arguments = "-latest -prerelease -products *  -requires Microsoft.Component.MSBuild -property installationPath",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Unable to start vswhere.exe.");

        var output = process.StandardOutput.ReadToEnd().Trim();
        var error = process.StandardError.ReadToEnd();

        process.WaitForExit();

        if (process.ExitCode != 0)
            throw new InvalidOperationException($"vswhere.exe failed: {error}");

        if (string.IsNullOrWhiteSpace(output))
            throw new InvalidOperationException(
                "No Visual Studio installation containing MSBuild was found.");

        return output;
    }
}