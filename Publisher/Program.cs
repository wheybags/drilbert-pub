using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

public static class Program
{
    static string getRootDir()
    {
        string root = Assembly.GetEntryAssembly()!.Location;
        while (!Directory.Exists(root + "/gfx"))
            root = Directory.GetParent(root).FullName;
        return root;
    }

    // copied from https://learn.microsoft.com/en-us/dotnet/standard/io/how-to-copy-directories
    static void copyDirectory(string sourceDir, string destinationDir, bool recursive=true, List<Regex> ignorePatterns=null)
    {
        var dir = new DirectoryInfo(sourceDir);

        if (!dir.Exists)
            throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

        Directory.CreateDirectory(destinationDir);

        foreach (FileInfo file in dir.GetFiles())
        {
            if (ignorePatterns != null)
            {
                bool ignore = false;
                foreach (Regex pattern in ignorePatterns)
                {
                    if (pattern.IsMatch(file.Name))
                    {
                        ignore = true;
                        break;
                    }
                }
                if (ignore)
                    continue;
            }

            string targetFilePath = Path.Combine(destinationDir, file.Name);
            file.CopyTo(targetFilePath);
        }

        if (recursive)
        {
            foreach (DirectoryInfo subDir in dir.GetDirectories())
            {
                if (ignorePatterns != null)
                {
                    bool ignore = false;
                    foreach (Regex pattern in ignorePatterns)
                    {
                        if (pattern.IsMatch(subDir.Name))
                        {
                            ignore = true;
                            break;
                        }
                    }
                    if (ignore)
                        continue;
                }

                string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                copyDirectory(subDir.FullName, newDestinationDir, true, ignorePatterns);
            }
        }
    }

    static void copySourceCode(string destinationDir)
    {
        List<Regex> ignoreList = new List<Regex>()
        {
            new Regex(@"^bin$"),
            new Regex(@"^obj$"),
            new Regex(@"^.*.user$"),
            new Regex(@"^Artifacts$"),
            new Regex(@"^\.idea$"),
        };

        copyDirectory(getRootDir() + "/src", destinationDir, true, ignoreList);
    }

    static void buildRelease(string configuration, string os, string outputFolder, bool demo)
    {
        Directory.CreateDirectory(outputFolder);

        // build binaries + bundle runtime
        {
            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                FileName = "dotnet",
                Arguments = "publish Drilbert/Drilbert.csproj --os " + os + " -property:DrilbertTargetOs=" + os + " --arch x64 --self-contained true --configuration " + configuration + " --output \"" + outputFolder + "\"",
                WorkingDirectory = getRootDir() + "/src",
            };
            Process process = Process.Start(startInfo);
            process.WaitForExit();

            if (process.ExitCode != 0)
                throw new Exception("build failed");
        }

        copyDirectory(getRootDir() + "/sfx", outputFolder + "/sfx");
        copyDirectory(getRootDir() + "/gfx", outputFolder + "/gfx");
        copyDirectory(getRootDir() + "/levels", outputFolder + "/levels");

        string strData = File.ReadAllText(outputFolder + "/levels/levels.json");
        JsonNode data = JsonNode.Parse(strData, null, new JsonDocumentOptions(){CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true});

        if (demo)
        {
            foreach(var section in data.AsArray())
            {
                if (section["name"].GetValue<string>() == "bomb" || section["name"].GetValue<string>() == "megadrill")
                {
                    JsonArray levels = section["levels"].AsArray();
                    for (int i = levels.Count-1; i > 0; i--)
                        levels.RemoveAt(i);
                }
                else if (section["name"].GetValue<string>() == "final")
                {
                    section["levels"].AsArray().Clear();
                }
            }
        }
        else
        {
            Directory.CreateDirectory(outputFolder + "/src");
            copySourceCode(outputFolder + "/src");

            Directory.CreateDirectory(outputFolder + "/Tiled/" + os);
            copyDirectory(getRootDir() + "/Tiled/" + os, outputFolder + "/Tiled/" + os);

            Directory.CreateDirectory(outputFolder + "/template");
            copyDirectory(getRootDir() + "/template", outputFolder + "/template");

            File.Copy(getRootDir() + "/Drilbert.tiled-project", outputFolder + "/Drilbert.tiled-project");
            File.Copy(getRootDir() + "/Drilbert.tiled-session.template", outputFolder + "/Drilbert.tiled-session.template");
        }

        HashSet<string> keepLevels = new HashSet<string>() {"levels/main_menu.tmx", "levels/tileset.tsx"};
        foreach(var section in data.AsArray())
        {
            foreach (var item in section["levels"].AsArray())
                keepLevels.Add(item.GetValue<string>());
        }

        foreach(var child in Directory.GetFiles(outputFolder + "/levels"))
        {
            string path = child.Replace("\\", "/").Substring(outputFolder.Length + 1);
            if (!keepLevels.Contains(path))
                File.Delete(child);
        }

        File.WriteAllText(outputFolder + "/levels/levels.json", data.ToJsonString(new JsonSerializerOptions(){WriteIndented = true}));

        // Force each build to be unique
        File.WriteAllText(outputFolder + "/buildtime.txt", DateTime.UtcNow.ToString(CultureInfo.InvariantCulture) + " " + Guid.NewGuid());

        string outputZipPath = outputFolder + ".zip";
        if (os == "osx")
        {
            string tempZipPath = outputFolder + "_temp.zip";
            ZipFile.CreateFromDirectory(outputFolder, tempZipPath, CompressionLevel.Fastest, false);

            {
                using FileStream file = new FileStream(outputZipPath, FileMode.CreateNew);
                using ZipArchive archive = new ZipArchive(file, ZipArchiveMode.Update);

                {
                    ZipArchiveEntry zipEntry = archive.CreateEntry("Drilbert.zip", CompressionLevel.NoCompression);
                    using Stream outStream = zipEntry.Open();
                    using Stream inStream = File.OpenRead(tempZipPath);
                    inStream.CopyTo(outStream);
                }

                {
                    ZipArchiveEntry zipEntry = archive.CreateEntry("runme", CompressionLevel.NoCompression);
                    using Stream outStream = zipEntry.Open();
                    using Stream inStream = File.OpenRead(getRootDir() + "/asset_sources/runme");
                    inStream.CopyTo(outStream);
                }

                {
                    ZipArchiveEntry zipEntry = archive.CreateEntry("instructions.png", CompressionLevel.NoCompression);
                    using Stream outStream = zipEntry.Open();
                    using Stream inStream = File.OpenRead(getRootDir() + "/asset_sources/instructions.png");
                    inStream.CopyTo(outStream);
                }
            }

            Process p = Process.Start(Path.Join(getRootDir(), "zip_exec", "zip_exec.exe"), "\"" + outputZipPath + "\" runme");
            p.WaitForExit();
            if (p.ExitCode != 0)
                throw new Exception();

            File.Delete(tempZipPath);
        }
    }

    static void uploadToSteam(string appid, string depot, string path, string tempPath, string description)
    {
        Directory.CreateDirectory(tempPath);


        string vdfSource = $$"""
        "AppBuild"
        {
            "AppID" "{{appid}}"
            "Desc" "drilbert build script: {{ description }}"
            "SetLive" "build"

            "ContentRoot" "{{path}}"
            "BuildOutput" "{{tempPath}}/logs_{{appid}}_{{depot}}"

            "Depots"
            {
                "{{depot}}"
                {
                    "FileMapping"
                    {
                        "LocalPath" "*" // all files from contentroot folder
                        "DepotPath" "." // mapped into the root of the depot
                        "recursive" "1" // include all subfolders
                    }
                }
            }
        }
        """;

        string vdfPath = tempPath + "/build.vdf";
        File.WriteAllText(vdfPath, vdfSource);

        string steamSdkRoot = Environment.GetEnvironmentVariable("STEAM_SDK_ROOT");
        if (steamSdkRoot == null)
            throw new Exception("STEAM_SDK_ROOT env var not set");

        string steamUser = Environment.GetEnvironmentVariable("STEAM_UPLOAD_USER");

        string password = Environment.GetEnvironmentVariable("STEAM_UPLOAD_PASSWORD");
        if (password == null)
            throw new Exception("STEAM_UPLOAD_PASSWORD not set");

        {
            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                FileName = steamSdkRoot + "/tools/ContentBuilder/builder/steamcmd.exe",
                Arguments = "+login \"" + steamUser + "\" \"" + password + "\" +run_app_build \"" + vdfPath + "\" +quit",
            };
            Process process = Process.Start(startInfo);
            process.WaitForExit();

            if (process.ExitCode != 0)
                throw new Exception("steam upload failed");
        }
    }

    static void Main(string[] args)
    {
        bool steam = !args.Contains("--no-steam");
        bool demo = !args.Contains("--no-demo");

        string buildsDir = getRootDir() + "/builds";
        if (Directory.Exists(buildsDir))
            Directory.Delete(buildsDir, true);
        Directory.CreateDirectory(buildsDir);

        // main
        {
            buildRelease("Release", "win", buildsDir + "/win/Drilbert", false);
            buildRelease("Release", "linux", buildsDir + "/linux/Drilbert", false);
            buildRelease("Release", "osx", buildsDir + "/osx/Drilbert", false);

            if (steam)
            {
                string appid = "2338630";
                uploadToSteam(appid, "2338631", buildsDir + "/win/Drilbert", buildsDir + "/steam_temp", "win main");
                uploadToSteam(appid, "2338632", buildsDir + "/linux/Drilbert", buildsDir + "/steam_temp", "linux main");
                uploadToSteam(appid, "2338633", buildsDir + "/osx/Drilbert", buildsDir + "/steam_temp", "osx main");
            }
        }

        // demo
        if (demo)
        {
            buildRelease("ReleaseDemo", "win", buildsDir + "/win/Drilbert Demo", true);
            buildRelease("ReleaseDemo", "linux", buildsDir + "/linux/Drilbert Demo", true);
            buildRelease("ReleaseDemo", "osx", buildsDir + "/osx/Drilbert Demo", true);

            if (steam)
            {
                string appid = "2338690";
                uploadToSteam(appid, "2338691", buildsDir + "/win/Drilbert Demo", buildsDir + "/steam_temp", "win demo");
                uploadToSteam(appid, "2338692", buildsDir + "/linux/Drilbert Demo", buildsDir + "/steam_temp", "linux demo");
                uploadToSteam(appid, "2338693", buildsDir + "/osx/Drilbert Demo", buildsDir + "/steam_temp", "osx demo");
            }
        }
    }
}