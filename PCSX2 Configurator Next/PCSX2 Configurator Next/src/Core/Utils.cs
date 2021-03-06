﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using IniParser.Model;
using Unbroken.LaunchBox.Plugins;
using Unbroken.LaunchBox.Plugins.Data;

namespace PCSX2_Configurator_Next.Core
{
    public class Utils
    {
        public static void SystemRemoveDir(string dir)
        {
            if (!Directory.Exists(dir)) return;
            var removeDirProcess = new Process
            {
                StartInfo =
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    FileName = "cmd.exe",
                    Arguments = $"/c rmdir /s /q \"{dir}\""
                }
            };

            removeDirProcess.Start();
            removeDirProcess.WaitForExit();
        }

        public static void SevenZipExtract(string archive, string outputDir)
        {
            if (!File.Exists(archive)) return;
            var sevenZipProcess = new Process
            {
                StartInfo =
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    FileName = $"{Configurator.Model.LaunchBoxDir}\\7-Zip\\7z.exe",
                    Arguments = $"x \"{archive}\" -o\"{outputDir}\""
                }
            };

            sevenZipProcess.Start();
            sevenZipProcess.WaitForExit();
        }

        public static string SvnCheckout(string remotePath, string workingDir)
        {
            var arguments = $"checkout \"{remotePath}\"";
            var output = SvnRun(arguments, workingDir);
            return output;
        }

        public static bool SvnDirNeedsUpdate(string svnDir)
        {
            var headInfo = SvnRun("info -r HEAD", svnDir);
            var info = SvnRun("info", svnDir);

            bool WithLastChagedRev(string str) => str.StartsWith("Last Changed Rev");
            return SvnOutputLine(headInfo, WithLastChagedRev) != SvnOutputLine(info, WithLastChagedRev);
        }

        public static string SvnFindPathInRemote(string remotePath, Func<string, bool> withCondition)
        {
            var arguments = $"list {remotePath}";
            var output = SvnRun(arguments);

            var path = SvnOutputLine(output, withCondition);
            path = path?.Substring(0, path.Length - 1);

            return path;
        }

        public static string LaunchBoxRelativePathToAbsolute(string launchRelativeBoxPath)
        {
            return RelativePathToAbsolute(Configurator.Model.LaunchBoxDir, launchRelativeBoxPath);
        }

        public static string Pcsx2RelativePathToAbsolute(string pcsx2RelativePath)
        {
            return RelativePathToAbsolute(Configurator.Model.Pcsx2AbsoluteDir, pcsx2RelativePath);
        }

        public static IEmulator[] LaunchBoxFindEmulatorsByTitle(string title)
        {
            var emulators = PluginHelper.DataManager.GetAllEmulators();
            emulators = emulators.Where(_ => _.Title.ToLower().Contains(title.ToLower())).ToArray();

            return emulators;
        }

        public static KeyDataCollection RocketLauncherCliToIni(string cliParams)
        {
            var cliParamsArr = cliParams.Replace("--", "-").Split('-').Select(_ => _.Trim()).Where(_ => !string.IsNullOrWhiteSpace(_));

            var gameCliIni = new KeyDataCollection();
            foreach (var param in cliParamsArr)
            {
                if (!param.Contains(" "))
                {
                    gameCliIni.AddKey(param, "true");
                }
                else
                {
                    var keyData = param.Split(' ');
                    var key = keyData[0];
                    var data = keyData[1];

                    gameCliIni.AddKey(key, data);
                }
            }

            return gameCliIni;
        }

        private static string SvnOutputLine(string svnOutput, Func<string, bool> withCondition)
        {
            var arr = svnOutput.Replace("\r\n", "\n").Split('\n');
            var ret = arr.FirstOrDefault(withCondition);
            return ret;
        }

        private static string SvnRun(string arguments, string workingDir = "")
        {
            var svnProcess = new Process
            {
                StartInfo =
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    FileName = $"{Configurator.Model.SvnDir}\\bin\\svn.exe",
                    Arguments = arguments,
                    WorkingDirectory = workingDir
                }
            };

            svnProcess.Start();
            var output = svnProcess.StandardOutput.ReadToEnd();
            svnProcess.WaitForExit();

            return output;
        }

        private static string RelativePathToAbsolute(string relativeTo, string relativePath)
        {
            if (Path.IsPathRooted(relativePath))
            {
                return relativePath;
            }

            var absolutePath = $"{relativeTo}\\{relativePath}";
            return Path.GetFullPath(absolutePath);
        }
    }
}
