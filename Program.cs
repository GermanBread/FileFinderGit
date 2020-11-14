using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Diagnostics;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.Xmp;
using UserInterface;

namespace FileFinder
{
    class Program
    {
        #region Global Variables

        #endregion

        static void Main(string[] args)
        {             
            #region Program Setup
                
                #region Variables
                
                //Variables that don't depend on the settings manager
                char DirNavigationChar = Path.DirectorySeparatorChar;
                bool IsUNIX = DirNavigationChar == '/';
                string TempDirectory = Path.TrimEndingDirectorySeparator(Path.GetTempPath());
                bool IsInTEMP = Directory.GetCurrentDirectory().Contains(TempDirectory);
                List<string> FilePaths = new List<string>();
                List<Exception> ExceptionsThrown = new List<Exception>();
                string AppName = "FileFinder";
                string AppExtension = IsUNIX ? ".x86-64" : ".exe";
                //Release definition
                string FileFinderAppVersion = "v2.1.1";

                #endregion

                #region Interrupt signal handler

                Console.CancelKeyPress += new ConsoleCancelEventHandler(appCancel);

                #endregion
                
                #region Failsafe
                
                //Exit if the programm isn't running in a console window!
                if (Console.WindowHeight == 0 && Console.WindowWidth == 0 && !IsInTEMP)
                {
                    return;
                }

                #endregion
                
                #region Help argument

                //show all possible arguments
                if (args.Contains("-h"))
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Available arguments:");
                    Console.WriteLine("\"-u\" force update menu to show up");
                    return;
                }

                #endregion

                #region Updating the main app

                if (IsInTEMP)
                {
                    if (args.Length == 0)
                    {
                        Console.WriteLine("Updater: This directory is reserved for the updater!");
                        return;
                    }
                    
                    //Scuffed
                    string appDirPath = args[0].Replace("─", " ");
                    
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine("Updater: Preparing");
                    
                    try
                    {
                        //Instead of deleting the application instantly, make a backup.
                        Console.WriteLine("Updater: Making backup");
                        File.Move(appDirPath + DirNavigationChar + AppName + AppExtension, appDirPath + DirNavigationChar + AppName + "Backup" + AppExtension);
                        Console.WriteLine("Updater: Replacing executable with downloaded version");
                        File.Move(Directory.GetCurrentDirectory() + DirNavigationChar + AppName + AppExtension, appDirPath + DirNavigationChar + AppName + AppExtension);

                        //Now delete the backup.
                        Console.WriteLine("Updater: Deleting the backup");
                        File.Delete(appDirPath + DirNavigationChar + AppName + "Backup" + AppExtension);
                    }
                    catch(Exception caughtException)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Updater: Update failed");
                        Console.ResetColor();
                        Console.WriteLine("Exception: " + caughtException.Message);
                        return;
                    }

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Updater: Update complete. You may restart the app");
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine("Note: Any leftover update files will not be deleted until the app has been restarted");
                    Console.ResetColor();
                    return;
                }

                #endregion

                #region Log Init

                //create log directory
                try 
                {
                    if (!Directory.Exists("." + DirNavigationChar + "FileFinderLogs"))
                    {
                        Directory.CreateDirectory("." + DirNavigationChar + "FileFinderLogs");
                    }
                } 
                catch 
                {
                    Console.WriteLine("Logdir creation: The programm encountered a severe error and cannot continue");
                    Console.CursorVisible = true;
                    return;
                }
                
                #endregion

                #region Self-update
                
                    #region Updater log file creation

                    string UpdaterLogFilePath = "." + DirNavigationChar + "FileFinderLogs" + DirNavigationChar + new Random().Next(1000, 9999) + "FileFinderUpdater.log";
                    Logger.CreateLog(UpdaterLogFilePath, 0);

                    #endregion
                    
                    #region TEMP deletion
                    
                    try
                    {
                        //Get a list of files and delete them.
                        Logger.LogToFile("Checking for an existing temp directory", 0, Logger.UrgencyLevel.Info);
                        if (Directory.Exists(TempDirectory + DirNavigationChar + "FileFinderUpdater"))
                        {
                            //Enumerate each file and delete
                            foreach (var file in Directory.EnumerateFiles(TempDirectory + DirNavigationChar + "FileFinderUpdater"))
                            {
                                Logger.LogToFile($"Deleting {file}", 0, Logger.UrgencyLevel.Info);
                                File.Delete(file);
                            }
                            //Enumerate each directory and delete
                            foreach (var directory in Directory.EnumerateDirectories(TempDirectory + DirNavigationChar + "FileFinderUpdater"))
                            {
                                Logger.LogToFile($"Deleting {directory}", 0, Logger.UrgencyLevel.Info);
                                Directory.Delete(directory);
                            }
                            Logger.LogToFile("Temp directory deleted", 0, Logger.UrgencyLevel.Info);
                            Directory.Delete(TempDirectory + DirNavigationChar + "FileFinderUpdater");
                        }
                    }
                    catch (Exception caughtException)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Logger.LogToFile("Temp directory could not be deleted", 0, Logger.UrgencyLevel.Critical);
                        Console.WriteLine("Updater: Temporary directory could not be deleted");
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"Updater: Delete \"{TempDirectory + DirNavigationChar}FileFinderUpdater\" and restart the app");
                        Console.ResetColor();
                        Console.WriteLine("Exception: " + caughtException.Message);
                        Logger.SaveLog(0);
                        Console.CursorVisible = true;
                        return;
                    }

                    #endregion

                    #region Update searching and downloading

                    if (Directory.EnumerateDirectories(Directory.GetCurrentDirectory()).Where(a => a.Contains(DirNavigationChar + "obj")).ToList().Count > 0 || Directory.GetCurrentDirectory().Contains(DirNavigationChar + "obj"))
                    {
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Logger.LogToFile("App is running in local debug, skipping update", 0, Logger.UrgencyLevel.Info);
                        Console.WriteLine("Updater: App is running in local debug; Skipping update");
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.WriteLine("Checking for updates");
                        
                        List<ReleaseData> releases = new List<ReleaseData>();
                        try
                        {
                            //Get all releases if possible
                            Logger.LogToFile("Sending HTTP request to github API", 0, Logger.UrgencyLevel.Info);
                            releases = GetReleases("https://api.github.com/repos/GermanBread/FileFinderGit/releases");
                            Logger.LogToFile("Response recieved", 0, Logger.UrgencyLevel.Info);
                        }
                        catch (WebException webExcep)
                        {
                            Logger.LogToFile($"Recieved {webExcep.Status}", 0, Logger.UrgencyLevel.Error);
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Updater: Error while contacting server");
                            Console.ResetColor();
                            Console.WriteLine(webExcep.Message);
                            Thread.Sleep(2000);
                            releases = new List<ReleaseData>() { new ReleaseData { ReleaseTag = "v0.0.0" } };
                        }

                        //See method "CompareVersions" for more details.
                        int updateLevel = CompareVersions(FileFinderAppVersion, releases[0].ReleaseTag);
                        int selectedVersion = 0;
                        //If there is a new version, start the update menu
                        if (updateLevel > 0 || args.Contains("-u"))
                        {
                            Logger.LogToFile("Update has been found", 0, Logger.UrgencyLevel.Info);
                            int action = 0;
                            do
                            {
                                //Create a "Prompts" object to be used
                                Prompts pr = new Prompts();
                                
                                //Ask the user what the programm should do
                                List<string> options = new List<string>() { "Don't update", "Update", "Select version" };
                                action = pr.SelectionPrompt("Update found!", "What would you like to do?", options.ToArray(), true, action);

                                //If the user chose to skip or to automatically update, skip
                                if (action < 2)
                                {
                                    break;
                                }
                                
                                //Now, since we know each version's tag, show a prompt
                                List<string> versionTags = new List<string>() { "[Go back]" };
                                foreach (var release in releases)
                                {
                                    //Used to mark stuff
                                    string prefix = "";
                                    if (release.IsPreRelease)
                                    {
                                        prefix += "(Pre-release) ";
                                    }
                                    versionTags.Add(prefix + release.ReleaseTag);
                                }
                                selectedVersion = pr.SelectionPrompt("Select version", "", versionTags.ToArray(), true, selectedVersion);

                                //If the user chose a version
                                if (selectedVersion > 0)
                                {
                                    break;
                                }
                            } while (action == 2);

                            //The user chose to update
                            if (action > 0)
                            {
                                Logger.LogToFile("Starting update", 0, Logger.UrgencyLevel.Info);
                                //If the user chose to automatically update, set this
                                if (action == 1)
                                {
                                    Logger.LogToFile("Automatic update has been selected", 0, Logger.UrgencyLevel.Info);
                                    selectedVersion = 1;
                                }
                                
                                ReleaseData cutReleaseData = releases[selectedVersion - 1];
                                updateLevel = CompareVersions(FileFinderAppVersion, cutReleaseData.ReleaseTag);

                                //The code below executes if an update has been found. In other words: If the current version is older than the newer one...
                                Console.ForegroundColor = ConsoleColor.DarkGray;
                                switch (updateLevel)
                                {
                                    case 1:
                                        Console.Write("Patch: ");
                                        break;

                                    case 2:
                                        Console.Write("Minor update: ");
                                        break;

                                    case 3:
                                        Console.Write("Major update: ");
                                        break;

                                    case 0:
                                        Console.Write("Reinstall: ");
                                        break;
                                    
                                    case -1:
                                        Console.Write("Downgrade: ");
                                        break;
                                    
                                    default:
                                        Console.Write("Other: ");
                                        break;
                                }
                                Console.ForegroundColor = ConsoleColor.White;
                                //Looks like this: v1.2.3 >> v4.5.6
                                Console.WriteLine(FileFinderAppVersion + " >> " + cutReleaseData.ReleaseTag);
                                Console.ForegroundColor = ConsoleColor.DarkGray;
                                Console.WriteLine();
                                //Release name
                                Console.WriteLine("Release name: ");
                                Console.ForegroundColor = ConsoleColor.White;
                                //text
                                Console.WriteLine(cutReleaseData.ReleaseTitle);
                                Console.ForegroundColor = ConsoleColor.DarkGray;
                                Console.WriteLine();
                                //Release description
                                Console.WriteLine("Release description: ");
                                Console.ForegroundColor = ConsoleColor.White;
                                //test
                                Console.WriteLine(cutReleaseData.ReleaseDescription.Replace("\\r\\n", "\n"));
                                Console.WriteLine();
                                Console.ResetColor();

                                //Create a directory for the updater
                                Logger.LogToFile("Temp directory was created", 0, Logger.UrgencyLevel.Info);
                                string destPath = TempDirectory + DirNavigationChar + "FileFinderUpdater";
                                Directory.CreateDirectory(destPath);

                                //download the files
                                try
                                {
                                    Console.ForegroundColor = ConsoleColor.DarkGray;
                                    Console.WriteLine("Updater: Downloading release");

                                    Logger.LogToFile("File download started", 0, Logger.UrgencyLevel.Info);
                                    string DownloadURL = cutReleaseData.ReleaseAssets.Find(a => a.AssetName.Equals(AppName + AppExtension)).AssetDownloadURL;
                                    Logger.LogToFile($"Downloading {DownloadURL}", 0, Logger.UrgencyLevel.Info);
                                    DownloadFile(DownloadURL, destPath + DirNavigationChar + "FileFinder_updater" + AppExtension);
                                    Logger.LogToFile("File download completed", 0, Logger.UrgencyLevel.Info);
                                    
                                    //The following step is neccessary for UNIX machines because files require "execute permissions"
                                    if (IsUNIX)
                                    {
                                        Logger.LogToFile("Marking new file as executable", 0, Logger.UrgencyLevel.Info);
                                        RunInBash("chmod +x " + destPath + DirNavigationChar + "FileFinder_updater" + AppExtension);
                                    }

                                    //Now duplicate the executable to avoid a headache later
                                    Logger.LogToFile("Duplicating updater", 0, Logger.UrgencyLevel.Info);
                                    File.Copy(destPath + DirNavigationChar + "FileFinder_updater" + AppExtension, destPath + DirNavigationChar + AppName + AppExtension);

                                    Console.WriteLine("Updater: Starting updater");
                                    Console.WriteLine("Updater: Starting " + destPath + DirNavigationChar + "FileFinder_updater" + AppExtension);
                                    Console.ResetColor();
                                    
                                    //Now start a thread which will replace the main thread.
                                    ProcessStartInfo startInfo = new ProcessStartInfo();
                                    startInfo.FileName = destPath + DirNavigationChar + "FileFinder_updater" + AppExtension;
                                    //This is scuffed, but it'll do
                                    startInfo.Arguments = Directory.GetCurrentDirectory().Replace(" ", "─");
                                    startInfo.WorkingDirectory = destPath;
                                    startInfo.CreateNoWindow = false;
                                    Logger.LogToFile("Starting updater", 0, Logger.UrgencyLevel.Info);
                                    Process.Start(startInfo);

                                    //Close the logfile
                                    Logger.SaveLog(0);
                                    
                                    return;
                                }
                                catch (Exception caughtException)
                                {
                                    Logger.LogToFile("Download failed", 0, Logger.UrgencyLevel.Error);
                                    Logger.LogToFile($"Message {caughtException.Message}", 0, Logger.UrgencyLevel.Info);
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine("Updater: Error while downloading release");
                                    Console.ForegroundColor = ConsoleColor.Yellow;
                                    Console.WriteLine("Updater: Download the newest release at: https://github.com/GermanBread/FileFinderGit/releases");
                                    Console.ResetColor();
                                    Console.WriteLine("Exception: " + caughtException.Message);
                                    Console.ForegroundColor = ConsoleColor.DarkGray;
                                    Thread.Sleep(2000);
                                    Console.WriteLine("Updater: Continuing");
                                    Console.ResetColor();
                                }
                            }
                        }
                        else
                        {
                            Logger.LogToFile("App is up to date", 0, Logger.UrgencyLevel.Info);
                            Console.WriteLine("App is up to date");
                        }
                    }

                    //Close the log file
                    Logger.SaveLog(0);

                    #endregion

                #endregion

                #region Settings manager

                Console.CursorVisible = false;
                Console.WriteLine("Startup: If you don't see a menu appear, restart the app");
                
                //Settings manager is called here
                SettingsUI settingsMenu = new SettingsUI();

                //paths
                settingsMenu.settings.Add(new SettingsEntry("Source path", SettingsEntry.InteractionType.selectableAndInteractable, new List<string>() { "$Path", ""}, 0, "Path to the files you want sorted i.e. \"Family_pictures\""));
                settingsMenu.settings.Add(new SettingsEntry("Destination path", SettingsEntry.InteractionType.selectableAndInteractable, new List<string>() { "$Path", "" }, 0, "Path to the destination folder i.e. \"Family_pictures_sorted\""));
                //options
                settingsMenu.settings.Add(new SettingsEntry("", SettingsEntry.InteractionType.nonSelectableAndNonInteractable, new List<string>() { "" }, 0));
                settingsMenu.settings.Add(new SettingsEntry("Should filenames be changed?", SettingsEntry.InteractionType.selectableAndInteractable, new List<string>() { "No, do not change the name", "Add date only", "Add date and iterator" }, 2));
                settingsMenu.settings.Add(new SettingsEntry("Should files be sorted?", SettingsEntry.InteractionType.selectableAndInteractable, new List<string>() { "No", "Yes" }, 1));
                settingsMenu.settings.Add(new SettingsEntry("Should duplicates be overwritten?", SettingsEntry.InteractionType.selectableAndInteractable, new List<string>() { "No, always keep the duplicate", "Only overwrite if source file is newer", "Only overwrite if source file is older", "Yes, always overwrite the duplicate" }, 1));
                settingsMenu.settings.Add(new SettingsEntry("", SettingsEntry.InteractionType.nonSelectableAndNonInteractable, new List<string>() { "" }, 0));
                settingsMenu.settings.Add(new SettingsEntry("", SettingsEntry.InteractionType.selectableAndInteractable, new List<string>() { "$Done" }, 0));

                //loop until the user presses "DONE"
                while (!settingsMenu.DrawSettings("FileFinder settings manager " + FileFinderAppVersion)) {}
                
                //variables that depend on the settings manager
                string SourcePath = settingsMenu.settings[0].StrValueLabels[1];
                string TargetPath = settingsMenu.settings[1].StrValueLabels[1];
                int FileNameType = settingsMenu.settings[3].IntSelection;
                bool SortingEnabled = settingsMenu.settings[4].IntSelection == 1;
                int OverwriteType = settingsMenu.settings[5].IntSelection;
                bool CreateTargetDir = settingsMenu.settings[6].IntSelection == 1;

                #endregion

            #endregion

            #region Phases Init

                #region Copy phase log file creation
                
                //create log file
                string CopyLogFilePath = "." + DirNavigationChar + "FileFinderLogs" + DirNavigationChar + new Random().Next(1000, 9999) + "FileFinderCopier.log";
                Logger.CreateLog(CopyLogFilePath, 1);

                #endregion
            
                #region Logfile init

                //write to file
                Logger.LogToFile("Arguments:", 1, Logger.UrgencyLevel.Info);
                Logger.LogToFile($"Set[0] = {SourcePath}", 1, Logger.UrgencyLevel.Info);
                Logger.LogToFile($"Set[1] = {TargetPath}", 1, Logger.UrgencyLevel.Info);
                Logger.LogToFile($"Set[2] = {FileNameType}", 1, Logger.UrgencyLevel.Info);
                Logger.LogToFile($"Set[3] = {OverwriteType}", 1, Logger.UrgencyLevel.Info);

                #endregion

                #region Splash
                
                Console.Clear();
                Console.WriteLine("[ FILE FINDER ]");
                System.Threading.Thread.Sleep(250);
                Console.WriteLine($"[ VERSION {FileFinderAppVersion} ]");
                System.Threading.Thread.Sleep(250);
                Console.WriteLine("[ MADE BY: GermanBread#9087 ]");
                System.Threading.Thread.Sleep(250);
                Console.WriteLine("[ GITHUB PROJECT: https://github.com/GermanBread/FileFinderGit ]");
                System.Threading.Thread.Sleep(250);
                Console.Write("[ ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("STARTING");
                Console.ResetColor();
                Console.WriteLine(" ]");
                System.Threading.Thread.Sleep(1000);

                #endregion

            #endregion
            
            #region File Finding Phase
            
            Console.Clear();
            Console.SetCursorPosition(0, 0);
            
            Console.WriteLine("Looking for files in \"" + SourcePath + "\"");
            //define how strings should be compared
            StringComparison stringComparisonType = StringComparison.OrdinalIgnoreCase;
            //look for files with certain extensions
            try
            {
                FilePaths.AddRange(Directory.EnumerateFiles(SourcePath, "*", SearchOption.AllDirectories)
                 .Where(s => s.EndsWith(".bmp", stringComparisonType) || s.EndsWith(".gif", stringComparisonType) || s.EndsWith(".jpg", stringComparisonType)
                  || s.EndsWith(".png", stringComparisonType) || s.EndsWith(".avi", stringComparisonType) || s.EndsWith(".mov", stringComparisonType)
                   || s.EndsWith(".mp4", stringComparisonType)));
            }
            catch (Exception caughtException)
            {
                //this happened once, so I'm handling the exception
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("An error occured. Check logs for more info");
                Console.ResetColor();
                Console.WriteLine("File finding method: " + caughtException.Message);
                Logger.LogToFile($"Exception was thrown: {caughtException}", 1, Logger.UrgencyLevel.Critical);
                Console.CursorVisible = true;
                Logger.SaveLog(1);
                return;
            }

            FilePaths.Sort();

            //log in console how many files were found
            Console.WriteLine(FilePaths.Count + " media file(s) were found");
            Logger.LogToFile($"{FilePaths.Count} media file(s) were found", 1, Logger.UrgencyLevel.Info);

            foreach (var item in FilePaths)
            {
                Logger.LogToFile($"Found \"{item}\"", 1, Logger.UrgencyLevel.Info);
            }

            #endregion

            #region File Copying Phase

            List<string> processedFileList = new List<string>();

            int unsortedCount = 0;
            
            for (int i = 0; i < FilePaths.Count; i++)
            {
                Console.SetCursorPosition(0, 3);
                Console.WriteLine("Copying, please wait                                   ");
                Console.BackgroundColor = ConsoleColor.DarkGray;
                Console.Write(BarGraph(i + 1, FilePaths.Count, 20));
                Console.ResetColor(); 
                Console.WriteLine(" " + (i + 1) + " out of " + FilePaths.Count + " files copied");
                
                //determine the time the image was shot
                string fileShotTime = null;
                try
                {
                    IEnumerable<MetadataExtractor.Directory> fileMetadata = MetadataExtractor.ImageMetadataReader.ReadMetadata(FilePaths[i]);
                    //try extract metadata
                    ExifSubIfdDirectory exifMetadata = fileMetadata?.OfType<ExifSubIfdDirectory>().FirstOrDefault();
                    XmpDirectory xmpMetadata = fileMetadata?.OfType<XmpDirectory>().FirstOrDefault();
                    //output
                    fileShotTime = exifMetadata?.GetDescription(ExifDirectoryBase.TagDateTimeOriginal);
                    if (fileShotTime == null)
                    {
                        xmpMetadata?.GetXmpProperties().TryGetValue("MetadataDate", out fileShotTime);
                        if (fileShotTime == null)
                        {
                            //fallback
                            fileShotTime = File.GetLastWriteTimeUtc(FilePaths[i]).ToString();
                            Logger.LogToFile($"File \"{FilePaths[i]}\" was sorted using the last write time", 1, Logger.UrgencyLevel.Warn);
                            unsortedCount++;
                        }
                    }
                }
                catch (Exception caughtException)
                {
                    //fallback
                    fileShotTime = File.GetLastWriteTimeUtc(FilePaths[i]).ToString();
                    Logger.LogToFile($"File \"{FilePaths[i]}\" was sorted using the last write time", 1, Logger.UrgencyLevel.Warn);
                    unsortedCount++;
                    //count the exeption caught for debugging purposes
                    ExceptionsThrown.Add(caughtException);
                }

                //rename the file and set copy path
                //path + name
                string destpath;
                string filename;
                Func<bool, string> returnIterator = x => x ? i.ToString() : "";
                Func<string, string> addDirNavCharIfNotInName = x => x[x.Length - 1] != DirNavigationChar ? DirNavigationChar.ToString() : "";
                if (fileShotTime.Take(4).All(c => c >= '0' && c <= '9'))
                {
                    //this will execute when the date-format is yyyy:mm:dd-hh:MM:ss
                    if (FileNameType > 0)
                    {
                        destpath = TargetPath + addDirNavCharIfNotInName(TargetPath) + (SortingEnabled ? new string(fileShotTime.Take(4).ToArray()) + "_" + new string(fileShotTime.Skip(5).Take(2).ToArray()) + DirNavigationChar : "");

                        filename = new string(fileShotTime.Take(4).ToArray()) + "_" + new string(fileShotTime.Skip(5).Take(2).ToArray()) + "_" 
                        + new string(fileShotTime.Skip(8).Take(2).ToArray()) + "-" + new string(fileShotTime.Skip(11).Take(2).ToArray()) + "+" + new string(fileShotTime.Skip(14).Take(2).ToArray())
                        + "+" + new string(fileShotTime.Skip(17).Take(2).ToArray()) + "-" + IsolateFilename(FilePaths[i]) + returnIterator(FileNameType == 2) + "." + IsolateFileExtension(FilePaths[i]).ToLower();
                    }
                    else
                    {
                        destpath = TargetPath + addDirNavCharIfNotInName(TargetPath) + (SortingEnabled ? new string(fileShotTime.Take(4).ToArray()) + "_" + new string(fileShotTime.Skip(5).Take(2).ToArray()) + DirNavigationChar : "");
                        filename = IsolateFilename(FilePaths[i]) + "." + IsolateFileExtension(FilePaths[i]);
                    }
                }
                else
                {
                    //this will execute when the date-format is dd:mm:yyyy-hh:MM:ss
                    if (FileNameType > 0)
                    {
                        destpath = TargetPath + addDirNavCharIfNotInName(TargetPath) + (SortingEnabled ? new string(fileShotTime.Skip(6).Take(4).ToArray()) + "_" + new string(fileShotTime.Skip(3).Take(2).ToArray()) + DirNavigationChar : "");

                        filename = new string(fileShotTime.Skip(6).Take(4).ToArray()) + "_" + new string(fileShotTime.Skip(3).Take(2).ToArray()) + "_" 
                        + new string(fileShotTime.Take(2).ToArray()) + "-" + new string(fileShotTime.Skip(11).Take(2).ToArray()) + "+" + new string(fileShotTime.Skip(14).Take(2).ToArray())
                        + "+" + new string(fileShotTime.Skip(17).Take(2).ToArray()) + "-" + IsolateFilename(FilePaths[i]) + returnIterator(FileNameType == 2) + "." + IsolateFileExtension(FilePaths[i]).ToLower();
                    }
                    else
                    {
                        destpath = TargetPath + addDirNavCharIfNotInName(TargetPath) + (SortingEnabled ? new string(fileShotTime.Skip(6).Take(4).ToArray()) + "_" + new string(fileShotTime.Skip(3).Take(2).ToArray()) + DirNavigationChar : "");
                        filename = IsolateFilename(FilePaths[i]) + "." + IsolateFileExtension(FilePaths[i]);
                    }
                }
                
                //merge the paths previously generated
                string copypath = destpath + filename;
                
                //this script handles the copying itself
                try
                {
                    if (!File.Exists(copypath))
                    {
                        //this will execute if no duplicate file is found
                        Directory.CreateDirectory(destpath);
                        File.Copy(FilePaths[i], copypath);
                        //log to file
                        Logger.LogToFile("Copied \"" + FilePaths[i] + "\" to \"" + copypath + "\"", 1, Logger.UrgencyLevel.Info);
                        Console.ForegroundColor = ConsoleColor.Green;
                        //log to console
                        Console.SetCursorPosition(0, Console.CursorTop);
                        Console.WriteLine("Copying file                                          ");
                        Console.ResetColor();
                        processedFileList.Insert(0, "Copied \"" + IsolateFilename(FilePaths[i]) + "." + IsolateFileExtension(FilePaths[i]) + "\"                                       ");                        
                    }
                    else
                    {
                        //this executes when a file is already present in the destination folder
                        if ((File.GetLastWriteTime(FilePaths[i]) < File.GetLastWriteTime(destpath) && OverwriteType == 2) || (File.GetLastWriteTime(FilePaths[i]) > File.GetLastWriteTime(destpath) && OverwriteType == 1) || OverwriteType == 3)
                        {
                            //this will execute when the file in the source folder is newer than the file in the target folder
                            Directory.CreateDirectory(destpath);
                            File.Delete(copypath);
                            File.Copy(FilePaths[i], copypath);

                            if (OverwriteType == 3)
                            {
                                Logger.LogToFile("Overwrote \"" + copypath + "\"", 1, Logger.UrgencyLevel.Info);
                            }
                            else if (OverwriteType == 2)
                            {
                                Logger.LogToFile("Kept \"" + copypath + "\"", 1, Logger.UrgencyLevel.Info);
                            }
                            else
                            {
                                Logger.LogToFile("Overwrote \"" + copypath + "\"", 1, Logger.UrgencyLevel.Info);
                            }
                            Console.WriteLine("Overwrote duplicate file                                  ");

                            processedFileList.Insert(0, "Overwrote \"" + IsolateFilename(FilePaths[i]) + "." + IsolateFileExtension(FilePaths[i]) + "\"                                      ");
                        }
                        else if (OverwriteType == 0)
                        {
                            //this will execute when OVERWRITE equals FALSE a.k.a. when the user wants to keep the files
                            Logger.LogToFile("Kept \"" + copypath + "\"", 1, Logger.UrgencyLevel.Info);
                            processedFileList.Insert(0, "Kept \"" + IsolateFilename(FilePaths[i]) + "." + IsolateFileExtension(FilePaths[i]) + "\"                                      ");
                        }
                        else
                        {
                            //this will execute when the file in the source folder is older than the file in the target folder
                            Logger.LogToFile("Kept \"" + copypath + "\"", 1, Logger.UrgencyLevel.Info);
                            processedFileList.Insert(0, "Kept \"" + IsolateFilename(FilePaths[i]) + "." + IsolateFileExtension(FilePaths[i]) + "\"                                      ");
                        }
                    }
                } catch (Exception caughtException) {
                    //this normally executes when there is a incorrect navigation character or if the filename is invalid
                    Logger.LogToFile("\"" + copypath + "\" caused an error: " + caughtException.Message, 1, Logger.UrgencyLevel.Error);
                    Console.SetCursorPosition(0, Console.CursorTop);
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.SetCursorPosition(0, 6);
                    Console.WriteLine("COPIER: An error occured: View log file for more details                    ");
                    processedFileList.Insert(0, "!" + copypath + " caused an error                                          ");
                    Console.ResetColor();                        
                    ExceptionsThrown.Add(caughtException);
                }
                
                while (processedFileList.Count > 8)
                {
                    processedFileList.RemoveAt(processedFileList.Count - 1);
                }

                //this just shows a list to the user, telling them WHAT this programm is doing
                Console.SetCursorPosition(0, 7);
                Console.WriteLine("LOG:");

                for (int b = 0; b < processedFileList.Count; b++)
                {
                    Console.ForegroundColor = processedFileList[b][0] == '!' ? ConsoleColor.Red : ConsoleColor.Blue;
                    Console.SetCursorPosition(0, b + 8);
                    Console.WriteLine(processedFileList[b]);
                }
                Console.WriteLine("                                                            ");
                Console.ResetColor();
            }

            Logger.SaveLog(1);

            #endregion

            #region Programm Exit

            string SummaryLogFilePath = "." + DirNavigationChar + "FileFinderLogs" + DirNavigationChar + new Random().Next(1000, 9999) + "FileFinderSummary.log";
            Logger.CreateLog(SummaryLogFilePath, 2);
            
            Console.ForegroundColor = ConsoleColor.Green;
            Console.SetCursorPosition(0, 16);
            Console.WriteLine("Done");
            Console.ResetColor();
            Console.WriteLine("You may want to check the log file");
            Console.WriteLine();

            if (FilePaths.Count > 0)
            {
                Logger.LogToFile($"{(int)(((float)unsortedCount / (float)FilePaths.Count) * 100f)}% of files were sorted using fallback method", 2, Logger.UrgencyLevel.Info);
            }
            if (ExceptionsThrown.Count > 0)
            {
                Logger.LogToFile($"The following {ExceptionsThrown.Count} exception(s) were caught:", 2, Logger.UrgencyLevel.Info);
            }
            foreach (var exceptionName in ExceptionsThrown)
            {
                Logger.LogToFile(exceptionName.Message, 2, Logger.UrgencyLevel.Info);
            }
            Logger.LogToFile("Date when programm exited: " + DateTime.Now, 2, Logger.UrgencyLevel.Info);

            //close the stream
            Logger.SaveLog(2);
            Console.CursorVisible = true;

            #endregion
        }
        
        #region Classes
        
        public class ReleaseData
        {
            [JsonPropertyName("tag_name")]
            public string ReleaseTag { get; set; }
            [JsonPropertyName("name")]
            public string ReleaseTitle { get; set; }
            [JsonPropertyName("body")]
            public string ReleaseDescription { get; set; }
            [JsonPropertyName("prerelease")]
            public bool IsPreRelease { get; set; }
            [JsonPropertyName("assets")]
            public List<ReleaseAssetsData> ReleaseAssets { get; set; }
        }

        public class ReleaseAssetsData
        {
            [JsonPropertyName("name")]
            public string AssetName { get; set; }
            [JsonPropertyName("browser_download_url")]
            public string AssetDownloadURL { get; set; }
        }

        public class Logger
        {
            public static int MessageID = 0;
            public static Dictionary<int, StreamWriter> LogFiles = new Dictionary<int, StreamWriter>();
            public static void CreateLog(string SourcePath, int ID)
            {
                LogFiles.Add(ID, new StreamWriter(SourcePath));
                LogFiles.TryGetValue(ID, out StreamWriter writer);
                writer.WriteLine($"Log file created with ID {ID}");
                writer.WriteLine($"Local time is {DateTime.Now}");
            }
            public static void LogToFile(string Message, int ID, UrgencyLevel Urgency)
            {
                StreamWriter writer;
                LogFiles.TryGetValue(ID, out writer);
                switch (Urgency)
                {
                    case UrgencyLevel.Info:
                        writer.WriteLine($"[{MessageID} INFO] " + Message);
                        break;
                    
                    case UrgencyLevel.Warn:
                        writer.WriteLine($"[{MessageID} WARN] " + Message);
                        break;

                    case UrgencyLevel.Error:
                        writer.WriteLine($"[{MessageID} ERROR] " + Message);
                        break;
                    
                    case UrgencyLevel.Critical:
                        writer.WriteLine($"[{MessageID} CRITICAL] " + Message);
                        break;
                }
                MessageID++;
            }
            public static void SaveLog(int ID)
            {
                StreamWriter writer;
                LogFiles.TryGetValue(ID, out writer);
                writer.WriteLine($"Local time is {DateTime.Now}");
                writer.WriteLine($"Log file saved");
                writer.Close();
                writer.Dispose();
                LogFiles.Remove(ID);
            }
            public enum UrgencyLevel
            {
                Info = 0,
                Warn = 1,
                Error = 2,
                Critical = 3
            }
        }

        #endregion
        
        #region Program Methods
        
        static void appCancel(object sender, ConsoleCancelEventArgs cancelEvents)
        {
            Console.CursorVisible = true;
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("Writing to log");
            foreach (var logFile in Logger.LogFiles)
            {
                try
                {
                    Logger.LogToFile($"{cancelEvents.SpecialKey} was recieved", logFile.Key, Logger.UrgencyLevel.Info);
                    Logger.SaveLog(logFile.Key);    
                }
                catch (NullReferenceException)
                {
                }
                catch (Exception excep)
                {
                    Console.WriteLine("Unknown error while writing");
                    Console.WriteLine(excep.Message);
                }
            }
            Console.ResetColor();
            Console.WriteLine("Programm exit");
            return;
        }
        
        static List<ReleaseData> GetReleases(string url)
        {
            List<ReleaseData> releases = new List<ReleaseData>();
            
            var webRequest = WebRequest.Create(url) as HttpWebRequest;

            webRequest.ContentType = "application/json";
            webRequest.UserAgent = "Nothing";

            //I have no idea what this does and how it works, but it works
            using (var s = webRequest.GetResponse().GetResponseStream())
            {
                //Same Here
                using (var sr = new StreamReader(s))
                {
                    var answer = sr.ReadToEnd();
                    releases = JsonSerializer.Deserialize<List<ReleaseData>>(answer);
                    
                    //Free up memory
                    sr.Close();
                    sr.Dispose();
                }
            }
            
            return releases;
        }
        
        //Source: https://gist.github.com/nboubakr/7812375
        static void DownloadFile(string sourceURL, string destinationPath)
        {
            long fileSize = 0;
            int bufferSize = 1024;
            bufferSize *= 1000;
            long existLen = 0;
            
            System.IO.FileStream saveFileStream;
            if (System.IO.File.Exists(destinationPath))
            {
                System.IO.FileInfo destinationFileInfo = new System.IO.FileInfo(destinationPath);
                existLen = destinationFileInfo.Length;
            }

            if (existLen > 0)
                saveFileStream = new System.IO.FileStream(destinationPath, System.IO.FileMode.Append, System.IO.FileAccess.Write, System.IO.FileShare.ReadWrite);
            else
                saveFileStream = new System.IO.FileStream(destinationPath, System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.ReadWrite);
        
            System.Net.HttpWebRequest httpReq;
            System.Net.HttpWebResponse httpRes;
            httpReq = (System.Net.HttpWebRequest) System.Net.HttpWebRequest.Create(sourceURL);
            httpReq.AddRange((int) existLen);
            System.IO.Stream resStream;
            httpRes = (System.Net.HttpWebResponse) httpReq.GetResponse();
            resStream = httpRes.GetResponseStream();
        
            fileSize = httpRes.ContentLength;
        
            int byteSize;
            byte[] downBuffer = new byte[bufferSize];
        
            while ((byteSize = resStream.Read(downBuffer, 0, downBuffer.Length)) > 0)
            {
                saveFileStream.Write(downBuffer, 0, byteSize);
            }
            saveFileStream.Close();
            saveFileStream.Dispose();
        }

        public static void RunInBash(string cmd)
        {
            var escapedArgs = cmd.Replace("\"", "\\\"");

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = "/bin/bash",
                    Arguments = $"-c \"{escapedArgs}\""
                }
            };

            process.Start();
            process.WaitForExit();
            return;
        }
        
        static string RemoveTrailingChar(string input, char character)
        {
            string output = "";
            List<char> charArrayInput = input.ToCharArray().ToList();
            List<char> charArrayOutput = new List<char>();
            
            for (int i = 0; i < charArrayInput.Count; i++)
            {
                if ((i != 0 || i != charArrayInput.Count) && charArrayInput[i] == character)
                {
                    continue;
                }
                charArrayOutput.Add(charArrayInput[i]);
            }
            output = new string(charArrayOutput.ToArray());
            return output;
        }
        
        static int CompareVersions(string currentVersion, string newVersion)
        {
            char[] validVersionChars = new char[] { '1', '2', '3', '4', '5', '6', '7', '8', '9', '0', '.'};
            char[] validIntChars = new char[] { '1', '2', '3', '4', '5', '6', '7', '8', '9', '0'};

            string firstFiltered = new string(currentVersion.ToCharArray().Where((a) => validVersionChars.Contains(a)).ToArray());
            string secondFiltered = new string(newVersion.ToCharArray().Where((a) => validVersionChars.Contains(a)).ToArray());

            List<int> firstSplit = new List<int>();
            foreach (var item in firstFiltered.Split('.'))
            {
                firstSplit.Add(int.Parse(item.Where((a) => validIntChars.Contains(a)).ToArray()));
            }
           
            List<int> secondSplit = new List<int>();
            foreach (var item in secondFiltered.Split('.'))
            {
                secondSplit.Add(int.Parse(item.Where((a) => validIntChars.Contains(a)).ToArray()));
            }

            int updateLevel;
            //Here's how "update levels" work:
            //-1 = downgrade >>> do nothing
            //0 = same version >>> do nothing
            //1 = patch >>> update
            //2 = minor update >>> update
            //3 = major update >>> update

            //compare versions            
            //compare major
            if (firstSplit[0] < secondSplit[0])
            {
                updateLevel = 3;
            }
            //compare minor
            else if (firstSplit[1] < secondSplit[1])
            {
                updateLevel = 2;
            }
            //compare patch
            else if (firstSplit[2] < secondSplit[2])
            {
                updateLevel = 1;
            }
            //check if they are equal
            else if (firstSplit[0] == secondSplit[0] && firstSplit[1] == secondSplit[1] && firstSplit[2] == secondSplit[2])
            {
                updateLevel = 0;
            }
            else
            {
                updateLevel = -1;
            }
            
            return updateLevel;
        }

        static string BarGraph(int value, int maxValue, int width)
        {
            string output = "";
            string format = System.Environment.OSVersion.Platform == PlatformID.Win32NT ? " █" : " ▏▎▍▌▋▊▉█";

            float ratio = (float)value / (float)maxValue;
            for (float i = 0; i < width; i++)
            {
                output += format[(int)Math.Clamp(((ratio * format.Length) * width) - (i * format.Length), 0, format.Length - 1)];
            }
            return output;
        }
        
        static string IsolateFilename(string path)
        {
            string filename = "";
            bool extensionpassed = false;

            for (int i = path.Length - 1; i > 0; i--)
            {
                if (path[i] == '\\' || path[i] == '/')
                {
                    break;
                }
                if (extensionpassed)
                {
                    filename += path[i];
                }
                if (path[i] == '.')
                {
                    extensionpassed = true;
                }
            }
            return ReverseString(filename);
        }

        static string IsolateFileExtension(string path)
        {
            string filename = "";

            for (int i = path.Length - 1; i > 0; i--)
            {
                if (path[i] == '.')
                {
                    break;
                }
                filename += path[i];
            }
            return ReverseString(filename);
        }

        public static string ReverseString(string s)
        {
            char[] charArray = s.ToCharArray();
            Array.Reverse(charArray);
            return new string(charArray);
        }

        #endregion
    }
}
