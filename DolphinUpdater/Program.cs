﻿using System;
using System.Diagnostics;
using System.Net;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using Ionic.Zip;

namespace DolphinUpdater
{
    class Program
    {
        static string path;
        static string tempPath;
        static string updatedPath;
        static string zipPath;
        private static int counter;

        static async Task Main(string[] args)
        {
            if (args.Length == 0)
                Environment.Exit(0);

            string downloadLink = args[0];
            path = args[1];
            tempPath = path + "/temp/";
            zipPath = tempPath + "temp.zip";

            CloseDolphin();

            if (File.Exists(zipPath))
                File.Delete(zipPath);

            await Task.Run(() => DownloadZip(downloadLink));
            ExtractZip(zipPath, tempPath);

            GetDolphinPath();

            if (updatedPath == null)
                updatedPath = tempPath;

            Console.WriteLine("Moving files. Please wait...");
            MoveUpdateFiles(updatedPath, path);

            Directory.Delete(tempPath, true);

            Console.WriteLine("Finished! You can close this window if it's still open!");

            string dolphinPath = path + "/Dolphin.exe";
            if (File.Exists(dolphinPath))
                Process.Start(dolphinPath);
            else
            {
                do
                {
                    Console.WriteLine("Dolphin.exe not found! Press the enter key to close this application.");
                } while (Console.ReadKey(true).Key != ConsoleKey.Enter);
            }
        }

        private static void CloseDolphin()
        {
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = "/C taskkill /f /im \"Dolphin.exe\"";
            process.StartInfo = startInfo;
            process.Start();
            process.Close();
        }

        private static async Task DownloadZip(string downloadlink)
        {
            if (!Directory.Exists(tempPath))
                Directory.CreateDirectory(tempPath);

            if (File.Exists(zipPath))
                File.Delete(zipPath);

            Console.WriteLine("Starting download....");
            using (var client = new WebClient())
            {
                client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(client_DownloadProgressChanged);
                await client.DownloadFileTaskAsync(new Uri(downloadlink), zipPath);
            }
        }

        public static void ExtractZip(string FileZipPath, string OutputFolder)
        {
            using (ZipFile zip = ZipFile.Read(FileZipPath))
            {
                foreach (ZipEntry e in zip)
                {
                    string[] skipFiles = { "dolphin.log", "dolphin.ini", "gfx.ini", "vcruntime140_1.dll", "gckeynew.ini", "gcpadnew.ini", "hotkeys.ini", "logger.ini", "debugger.ini", "wiimotenew.ini" };

                    if (!skipFiles.Any(e.FileName.ToLower().Contains))
                    {
                        Console.WriteLine("Extracting " + e.FileName);
                        e.Extract(OutputFolder, ExtractExistingFileAction.OverwriteSilently);
                    }
                }
            }
        }

        private static void client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            counter++;
            if (counter % 5500 == 0)
            {
                Console.Clear();
                Console.WriteLine("\rDownloaded "
                                  + ((e.BytesReceived / 1024f) / 1024f).ToString("#0.##") + "mb"
                                  + " of "
                                  + ((e.TotalBytesToReceive / 1024f) / 1024f).ToString("#0.##") + "mb"
                                  + "  (" + e.ProgressPercentage + "%)"
                    );
            }
        }

        private static void GetDolphinPath()
        {
            DirectoryInfo diTop = new DirectoryInfo(tempPath);
            foreach (var di in diTop.EnumerateDirectories("*"))
            {
                try
                {
                    foreach (var fi in di.EnumerateFiles("*", SearchOption.AllDirectories))
                    {
                        try
                        {
                            if (fi.Name.ToLower() == "dolphin.exe")
                            {
                                updatedPath = fi.FullName.ToLower().Replace("dolphin.exe", "");
                            }
                        }
                        catch (UnauthorizedAccessException unAuthFile)
                        {
                            Console.WriteLine($"unAuthFile: {unAuthFile.Message}");
                        }
                    }
                }
                catch (UnauthorizedAccessException unAuthSubDir)
                {
                    Console.WriteLine($"unAuthSubDir: {unAuthSubDir.Message}");
                }
            }
        }

        private static void MoveUpdateFiles(string updateFilesPath, string destinationPath)
        {
            if (!Directory.Exists(destinationPath))
            {
                Directory.CreateDirectory(destinationPath);
            }

            foreach (string folder in Directory.GetDirectories(updateFilesPath))
            {
                string dest = Path.Combine(destinationPath, Path.GetFileName(folder));
                MoveUpdateFiles(folder, dest);
            }

            foreach (string file in Directory.GetFiles(updateFilesPath))
            {
                string dest = Path.Combine(destinationPath, Path.GetFileName(file));

                if (File.Exists(dest))
                    File.Delete(dest);

                if (!file.Contains("temp.zip"))
                    File.Copy(file, dest);
            }
        }
    }
}
