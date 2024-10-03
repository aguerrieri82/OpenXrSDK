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

        var checkPath = $"runtimes\\{RuntimeIdentifier}\\native\\";
        var output = new List<ITaskItem>();
        foreach (var item in SourceFiles!)
        {
            var target = item.GetMetadata("TargetPath").Replace('/', '\\');

            if (target.StartsWith(checkPath))
                output.Add(item);
        }

        Output = output.ToArray();

        return true;
    }



}