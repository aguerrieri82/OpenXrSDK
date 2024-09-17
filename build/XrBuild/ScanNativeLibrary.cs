using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Task = Microsoft.Build.Utilities.Task;

public class ScanNativeLibrary : Task
{
    [Output]
    public ITaskItem[]? NativeAndroid { get; set;  }

    [Output]
    public ITaskItem[]? NativeAll { get; set; }

    [Required]
    public ITaskItem[]? SourceFiles { get; set; }


    public override bool Execute()
    {
        var nativeAll = new List<ITaskItem>();
        var nativeAndroid = new List<ITaskItem>();

        foreach (var item in SourceFiles!)
        {
            var fullPath = item.GetMetadata("FullPath");
            if (!File.Exists(fullPath))
            {
                Log.LogWarning($"File does not exist: {fullPath}"); 
                continue;
            }

            Log.LogMessage(MessageImportance.High, $"Item: {item.ItemSpec}");

            var parts = item.ItemSpec.Split('\\');

            var runtimeId = item.GetMetadata("Runtime");

            if (string.IsNullOrWhiteSpace(runtimeId))
                 runtimeId = parts.FirstOrDefault(a => a.StartsWith("win-") || a.StartsWith("android-"));

            if (runtimeId != null)
            {
                var newItem = new TaskItem(item);

                newItem.SetMetadata("CopyToOutputDirectory", "PreserveNewest");

                newItem.SetMetadata("Link", $"runtimes\\{runtimeId}\\native\\{Path.GetFileName(item.ItemSpec)}");

                nativeAll.Add(newItem);

                if (runtimeId.StartsWith(
                    "android-"))
                {
                    newItem = new TaskItem(item);
                    newItem.SetMetadata("Abi", "arm64-v8a");
                    nativeAndroid.Add(newItem);
                }

                Log.LogMessage(MessageImportance.High, $"Found {runtimeId}: {newItem.ItemSpec}");
            }
        }

        NativeAll = nativeAll.ToArray();

        NativeAndroid = nativeAndroid.ToArray();

        return true;
    }



}