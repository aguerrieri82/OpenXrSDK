

using System.Diagnostics;
using System.Text;

var bin = "c:/android/ndk/26.1.10909125/toolchains/llvm/prebuilt/windows-x86_64/bin/llvm-ar.exe";
var libPath = "D:\\Development\\Library\\filament\\out\\cmake-android-release-aarch64\\";
var input = "Inputs.txt";
var outPath = "";
var outName = "libfilament-all.a";


string? FindLib(string name)
{
    string? FindDir(string path)
    {
        var fileName = Path.Join(path, name);
        if (File.Exists(fileName))
            return fileName;

        foreach (var dir in Directory.GetDirectories(path))
        {
            var res = FindDir(dir);
            if (res != null)
                return res;
        }

        return null;
    }

    return FindDir(libPath);
}


IList<string> GetAllFiles(string path)
{
    var result = new List<string>();

    void ProcessDir(string curPath)
    {
        foreach (var file in Directory.GetFiles(curPath))
            result.Add(file);

        foreach (var dir in Directory.GetDirectories(curPath))
            ProcessDir(dir);
    }

    ProcessDir(path);
    return result;
}

void Exec(string command, params string[] args)
{
    var proc = Process.Start(command, args);
    proc.WaitForExit();
}

var libs = File.ReadAllLines(input);

var curDir = Directory.GetCurrentDirectory();

Directory.CreateDirectory(Path.Join(outPath, "libs"));

var mkFile = new StringBuilder();

foreach (var lib in libs)
{
    var fullPath = FindLib(lib);
    if (fullPath == null)
    {
        Console.WriteLine("xxx");
    }

    var outFile = Path.Join(outPath, "libs", lib);
    File.Copy(fullPath!, outFile, true);

    var objPath = Path.Join(outPath, "temp", lib);

    Directory.CreateDirectory(objPath);

    Directory.SetCurrentDirectory(objPath);

    Exec(bin, "-x", fullPath);

    Directory.SetCurrentDirectory(curDir);

    mkFile.Append("LOCAL_MODULE := ").Append(Path.GetFileNameWithoutExtension(lib)).AppendLine();
    mkFile.Append("LOCAL_SRC_FILES := $(FILAMENT_LIBS)/").Append(lib).AppendLine();
    mkFile.Append("include $(PREBUILT_STATIC_LIBRARY)").AppendLine();
    mkFile.AppendLine();
}

mkFile.Append("LOCAL_STATIC_LIBRARIES ");
foreach (var lib in libs)
    mkFile.Append(Path.GetFileNameWithoutExtension(lib)).Append(' ');

mkFile.AppendLine();

var files = GetAllFiles(Path.Join(outPath, "temp"));

var joinArgs = new List<string>();
joinArgs.Add("-rc");
joinArgs.Add("--format=gnu");
joinArgs.Add("--rsp-quoting=posix");
joinArgs.Add(Path.Join(outPath, outName));
joinArgs.AddRange(files);

Exec(bin, joinArgs.ToArray());

File.WriteAllText(Path.Join(outPath, "Android.mk"), mkFile.ToString());