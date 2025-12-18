using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Task = Microsoft.Build.Utilities.Task;

public class ScanNativeLibrary : Task
{
    [Output]
    public ITaskItem[]? NativeAndroid { get; set; }

    [Output]
    public ITaskItem[]? NativeAll { get; set; }

    [Required]
    public ITaskItem[]? SourceFiles { get; set; }


    public override bool Execute()
    {
        List<ITaskItem> nativeAll = new List<ITaskItem>();
        List<ITaskItem> nativeAndroid = new List<ITaskItem>();

        foreach (ITaskItem item in SourceFiles!)
        {
            string fullPath = item.GetMetadata("FullPath");
            if (!File.Exists(fullPath))
            {
                Log.LogWarning($"File does not exist: {fullPath}");
                continue;
            }

            Log.LogMessage(MessageImportance.High, $"Item: {item.ItemSpec}");

            string[] parts = item.ItemSpec.Split('\\');

            string runtimeId = item.GetMetadata("Runtime");

            if (string.IsNullOrWhiteSpace(runtimeId))
                runtimeId = parts.FirstOrDefault(a => a.StartsWith("win-") || a.StartsWith("android-"));

            if (runtimeId != null)
            {
                TaskItem newItem = new TaskItem(item);

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