using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Xml;
using Microsoft.Xna.Framework;

namespace Drilbert
{
    public static class Util
    {
        public static void ReleaseAssert(bool val)
        {
            if (!val)
                throw new Exception("assertion failed");
        }

        public static void DebugAssert(bool val)
        {
#if DEBUG
            if (!val)
                throw new Exception("assertion failed");
#endif
        }

        public static XmlDocument openXML(string path)
        {
            using FileStream f = File.OpenRead(path);
            XmlDocument doc = new XmlDocument();
            doc.Load(f);
            return doc;
        }

        public static XmlElement getUniqueByTagName(XmlElement e, string tagName)
        {
            var list = e.GetElementsByTagName(tagName);
            Util.ReleaseAssert(list.Count <= 1);
            if (list.Count == 0)
                return null;
            return (XmlElement)list.Item(0);
        }

        public static XmlElement getUniqueByTagName(XmlDocument e, string tagName)
        {
            var list = e.GetElementsByTagName(tagName);
            Util.ReleaseAssert(list.Count <= 1);
            if (list.Count == 0)
                return null;
            return (XmlElement)list.Item(0);
        }

        public static List<XmlElement> getElementsByTagName(XmlDocument e, string tagName)
        {
            var retval = new List<XmlElement>();
            foreach (var item in e.GetElementsByTagName(tagName))
                retval.Add((XmlElement)item);
            return retval;
        }

        public static void openStorePage()
        {
            if (!DrilbertSteam.usingSteam || !DrilbertSteam.tryShowStorePage())
            {
                string url = "https://store.steampowered.com/app/2338630/Drilbert";
                openWebpageInBrowser(url);
            }
        }

        public static void openWebpage(string url)
        {
            if (!DrilbertSteam.usingSteam || !DrilbertSteam.tryShowUrlInOverlay(url))
                openWebpageInBrowser(url);
        }

        private static void openWebpageInBrowser(string url)
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    url = url.Replace("&", "^&");
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Process.Start("xdg-open", url);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("open", url);
                }
            }
            catch (Exception e)
            {
                Logger.log("Failed to open in browser: " + url);
                Logger.log(e.ToString());
            }
        }

        public static JsonNode parseJson(string strData)
        {
            return JsonNode.Parse(strData, null, new JsonDocumentOptions(){CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true});
        }

        // copied from https://learn.microsoft.com/en-us/dotnet/standard/io/how-to-copy-directories
        public static void copyDirectory(string sourceDir, string destinationDir, bool recursive=true, List<Regex> ignorePatterns=null)
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

        public static void openFolder(string path)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                Process.Start("explorer.exe", path);
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                Process.Start("open", path);
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                Process.Start("xdg-open", path);
            else
                Util.ReleaseAssert(false);
        }
    }
}