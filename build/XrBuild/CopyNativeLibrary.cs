using Microsoft.Build.Framework;
using Task = Microsoft.Build.Utilities.Task;

public class CopyNativeLibrary : Task
{
    [Output]
    public ITaskItem[]? Output { get; set; }

    [Required]
    public ITaskItem[]? SourceFiles { get; set; }

    [Required]
    public string? RuntimeIdentifier { get; set; }


    public override bool Execute()
    {
        Log.LogMessage(MessageImportance.High, $"RuntimeIdentifier: {RuntimeIdentifier}");

        string checkPath = $"runtimes\\{RuntimeIdentifier}\\native\\";
        List<ITaskItem> output = new List<ITaskItem>();
        foreach (ITaskItem item in SourceFiles!)
        {
            string target = item.GetMetadata("TargetPath").Replace('/', '\\');

            if (target.StartsWith(checkPath))
                output.Add(item);
        }

        Output = output.ToArray();

        return true;
    }



}