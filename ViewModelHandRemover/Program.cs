// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

void Help() {
    Console.WriteLine( "Usage:");
    Console.WriteLine( "\tViewModelHandRemover [sven coop root game directory] [output directory]");
    Console.WriteLine( "The [sven coop root game directory] contains svencoop, and svencoop_downloads.");
    Console.WriteLine( "The [output directory] is where decompiled models go.");
}

if (args.Length < 1) {
    Help();
    return;
}

if (args.Length > 2) {
    Help();
    return;
}

DirectoryInfo outputDirectory = new DirectoryInfo(args[1]);
if (!outputDirectory.Exists) {
    Directory.CreateDirectory(outputDirectory.FullName);
}

DirectoryInfo rootGameFolder = new DirectoryInfo(args[0]);
if (!rootGameFolder.Exists) {
    Console.WriteLine( "ERR: Invalid game folder." );
    Help();
    return;
}
DirectoryInfo downloadFolder = new DirectoryInfo(Path.Combine(rootGameFolder.FullName, "svencoop_downloads"));
if (!downloadFolder.Exists) {
    Console.WriteLine("ERR: Couldn't find svencoop_download folder.");
    Help();
    return;
}
DirectoryInfo svencoopFolder = new DirectoryInfo(Path.Combine(rootGameFolder.FullName, "svencoop"));
if (!svencoopFolder.Exists) {
    Console.WriteLine("ERR: Couldn't find svencoop folder.");
    Help();
    return;
}

string[] blackList = [
    "[fF]orearm",
    "[gG]love",
    "_[aA]rm",
    "[aA]rms",
    "_sleeve",
    "fatigues",
    "dm_base\\.bmp",
    "v_soldier_revamp",
    "braccia", // Arm in italian
    "thumb\\.bmp", // Great many arms are built out of cs thumb.bmp's
    "remaps_[0-9][0-9][0-9]_[0-9][0-9][0-9]_[0-9][0-9][0-9]", // lots of hev suit arms use textures representing their colors like so.
    "[hH]and(?!le|el)" // Hand, but not handle
];

List<string> manualForceIgnore = [
    "shotgun_cz_handsm3",
    "cso_femalehands_shotgun_v2",
];

HashSet<string> failedToDecompile = new HashSet<string>();
HashSet<string> suspiciousRemovals = new HashSet<string>();

void ProcessSMD(string path, string qcpath) {
    var fileName = Path.GetFileNameWithoutExtension(path);
    if (manualForceIgnore.Contains(fileName)) {
        return;
    }
    foreach (var name in blackList) {
        if (!Regex.IsMatch(fileName, name)) continue;
        Console.WriteLine($"\tSMD Removal: {fileName}");
        DirectoryInfo smdDir = new DirectoryInfo(Path.GetDirectoryName(path) ?? string.Empty);
        HashSet<string> removeTextures = new HashSet<string>();
        
        foreach (var line in File.ReadAllLines(path)) {
            if (line.EndsWith(".bmp")) {
                if (line.StartsWith("#") && !removeTextures.Contains(line.Substring(1))) {
                    removeTextures.Add(RenameTexture(line, qcpath, line.Substring(1)));
                } else {
                    removeTextures.Add(line);
                }
            }
            

            var split = line.Trim().Split(" ");
            if (split.Length != 3) {
                continue;
            }

            if (split[1] == "\"\"") {
                failedToDecompile.Add(qcpath);
            }
        }

        int handCount = 0;
        foreach (var texture in removeTextures) {
            if (manualForceIgnore.Contains(texture.Trim())) {
                continue;
            }

            foreach (var test in blackList) {
                if (Regex.IsMatch(texture.Trim(), test)) {
                    handCount++;
                    break;
                }
            }

            Console.WriteLine($"\t\tExploded {texture.Trim()}");
            File.Copy("./transparent.bmp", Path.Combine(smdDir.FullName, "maps_8bit", texture.Trim()), true);
            File.AppendAllText(qcpath, $"$texrendermode {texture.Trim()} masked\n");
            
            // These are treated as colorable team textures, due to their name. Which messes up the masked rendering.
            if (Regex.IsMatch(texture.Trim(), ".*_[0-9][0-9][0-9]_[0-9][0-9][0-9]_[0-9][0-9][0-9]\\.bmp")) {
                RenameTexture(texture.Trim(), qcpath, texture.Trim()[..^16] + ".bmp");
            }
        }

        if (handCount < removeTextures.Count/2) {
            suspiciousRemovals.Add(path);
        }
    }
}

string RenameTexture(string currentFilename, string qcpath, string newFilename) {
    Console.WriteLine($"\t\tMoved {currentFilename} to {newFilename}");
    string qcContents = File.ReadAllText(qcpath);
    File.WriteAllText(qcpath, qcContents.Replace(currentFilename, newFilename));
    string qcDirectory = Path.GetDirectoryName(qcpath) ?? string.Empty;
    foreach (var file in Directory.GetFiles(qcDirectory)) {
        if (!file.EndsWith(".smd")) continue;
        string smdContents = File.ReadAllText(file);
        File.WriteAllText(file, smdContents.Replace(currentFilename, newFilename));
    }

    if (!File.Exists(Path.Combine(qcDirectory, "maps_8bit", currentFilename))) {
        return newFilename;
    }
    File.Move(Path.Combine(qcDirectory, "maps_8bit", currentFilename), Path.Combine(qcDirectory, "maps_8bit", newFilename), true);
    return newFilename;
}

void ProcessTexture(string path, string qcpath) {
    var fileName = Path.GetFileName(path);
    foreach (var name in blackList) {
        if (Regex.IsMatch(fileName, name)) {
            if (manualForceIgnore.Contains(fileName.Trim())) {
                continue;
            }

            Console.WriteLine($"\tExploded {fileName}");
            File.Copy("./transparent.bmp", path, true);
            File.AppendAllText(qcpath, $"$texrendermode {fileName} masked\n");
            
            if (Regex.IsMatch(fileName.Trim(), ".*_[0-9][0-9][0-9]_[0-9][0-9][0-9]_[0-9][0-9][0-9]\\.bmp")) {
                RenameTexture(fileName.Trim(), qcpath, fileName.Trim()[..^16] + ".bmp");
            }
        }
    }
}

void ProcessQC(string path, string modelname, string relativePath) {
    StringBuilder builder = new StringBuilder();
    foreach (var line in File.ReadAllLines(path)) {
        if (line.StartsWith("$modelname")) {
            builder.Append($"$modelname {relativePath}/{modelname}\n");
            continue;
        }
        builder.Append(line+"\n");
    }
    File.WriteAllText(path, builder.ToString());
}

void ProcessFolder(DirectoryInfo directory) {
    foreach (var model in Directory.GetFiles(directory.FullName, "v_*.mdl", SearchOption.AllDirectories)) {
        string relativePath = Path.GetDirectoryName(model) ?? string.Empty;
        if (relativePath.StartsWith(directory.FullName)) {
            relativePath = relativePath.Substring(directory.FullName.Length+1);
        }

        relativePath = relativePath.Replace('\\', '/');
        
        string outputFolder = Path.Combine(outputDirectory.FullName, relativePath);
        if (!Directory.Exists(outputFolder)) {
            Directory.CreateDirectory(outputFolder);
        }
        
        ProcessStartInfo startInfo = new ProcessStartInfo {
            CreateNoWindow = true,
            UseShellExecute = false,
            FileName = "decompmdl.exe",
            Arguments = $"\"{model}\" \"{outputFolder}\""
        };
        if (Process.GetCurrentProcess().MainModule != null) {
            startInfo.WorkingDirectory = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule?.FileName);
        }

        Console.WriteLine($"Decompiling {model}...");
        using Process? exeProcess = Process.Start(startInfo);
        exeProcess?.WaitForExit();
        var fileName = Path.GetFileNameWithoutExtension(model);
        if (!Directory.Exists(Path.Combine(outputFolder, fileName, "maps_8bit"))) {
            failedToDecompile.Add(Path.Combine(outputFolder, fileName, fileName+".qc"));
            continue;
        }
        
        foreach (var file in Directory.GetFiles(Path.Combine(outputFolder,fileName,"maps_8bit"))) {
            ProcessTexture(file, Path.Combine(outputFolder, fileName, fileName+".qc"));
        }

        foreach (var file in Directory.GetFiles(Path.Combine(outputFolder, fileName))) {
            if (file.EndsWith(".smd")) {
                ProcessSMD(file,Path.Combine(outputFolder, fileName, fileName+".qc"));
            }
        }
        ProcessQC(Path.Combine(outputFolder, fileName, fileName + ".qc"), fileName + ".mdl", relativePath);
    }
}

ProcessFolder(svencoopFolder);
ProcessFolder(downloadFolder);

Console.WriteLine("The following models failed to decompile (due to decompiler not being perfect):");
foreach (var file in failedToDecompile) {
    Console.WriteLine("\t"+file);
}

Console.WriteLine("The following SMDs were removed, but are likely to not be arms:");
foreach (var file in suspiciousRemovals) {
    Console.WriteLine("\t"+file);
}

Console.WriteLine("Now you can use crowbar's mass compile feature to compile them back into the svencoop_addon folder!");