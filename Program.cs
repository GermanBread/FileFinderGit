using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Diagnostics;
using System.Net;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.Xmp;
using User_Interface;

namespace FileFinder
{
    class Program
    {
        static void Main(string[] args)
        {             
            #region Program Setup
              
                #region Variables
                
                //variables that don't depend on the settings manager
                char DirNavigationChar = System.IO.Path.DirectorySeparatorChar;
                bool IsUNIX = DirNavigationChar == '/';
                string LogFileName = "FileFinder_log_" + new Random().Next(1111, 9999).ToString() + ".txt";
                string TempDirectory = System.IO.Path.TrimEndingDirectorySeparator(System.IO.Path.GetTempPath());
                bool IsInTEMP = Directory.GetCurrentDirectory().Contains(TempDirectory);
                List<string> FilePaths = new List<string>();
                List<Exception> ExceptionsThrown = new List<Exception>();
                string FileFinderAppVersion = "v1.1.1";
                string AppExtension = IsUNIX ? ".x86-64" : ".exe";

                #endregion

                #region Failsafe
                
                //Exit if the programm isn't running in a console window!
                if (Console.WindowHeight == 0 && Console.WindowWidth == 0 && !IsInTEMP)
                {
                    return;
                }

                #endregion
                
                #region Self-update
                
                if (IsInTEMP)
                {
                    if (args.Length == 0)
                    {
                        Console.WriteLine("The temp directory is reserved for the updater!");
                        return;
                    }
                    
                    string appDirPath = args[0];
                    
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine("Preparing");
                    
                    try
                    {
                        //Instead of deleting the application instantly, make a backup.
                        Console.WriteLine("Making backup");
                        File.Move(appDirPath + DirNavigationChar + "FileFinder" + AppExtension, appDirPath + DirNavigationChar + "FileFinder_backup" + AppExtension);
                        Console.WriteLine("Replacing executable with downloaded version");
                        File.Move(Directory.GetCurrentDirectory() + DirNavigationChar + "FileFinder" + AppExtension, appDirPath + DirNavigationChar + "FileFinder" + AppExtension);

                        //Now delete the backup.
                        Console.WriteLine("Deleting the backup");
                        File.Delete(appDirPath + DirNavigationChar + "FileFinder_backup" + AppExtension);
                    }
                    catch(Exception caughtException)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Update failed, make sure the app's path doesn't contain spaces!");
                        Console.ResetColor();
                        Console.WriteLine("Exception:" + caughtException.Message);
                        return;
                    }

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Update complete. You may restart the app.");
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine("Note: Any leftover update files will not be deleted until the app has been restarted.");
                    Console.ResetColor();
                    return;
                }
                
                try
                {
                    //Get a list of files and delete them.
                    if (Directory.Exists(TempDirectory + DirNavigationChar + "FileFinderUpdater"))
                    {
                        foreach (var file in Directory.EnumerateFiles(TempDirectory + DirNavigationChar + "FileFinderUpdater"))
                        {
                            File.Delete(file);
                        }
                        Directory.Delete(TempDirectory + DirNavigationChar + "FileFinderUpdater");
                    }
                }
                catch (Exception caughtException)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Temporary directory could not be deleted.");
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Delete \"{TempDirectory + DirNavigationChar}FileFinderUpdater\" and restart the app.");
                    Console.ResetColor();
                    Console.WriteLine("Exception: " + caughtException.Message);
                    return;
                }

                if (Directory.EnumerateDirectories(Directory.GetCurrentDirectory()).Where(a => a.Contains(DirNavigationChar + "obj")).ToList().Count > 0 || Directory.GetCurrentDirectory().Contains(DirNavigationChar + "obj"))
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine("App is running in local debug; Skipping update...");
                    Console.ResetColor();
                }
                else
                {
                    Console.WriteLine("Checking for updates...");
                    
                    string[] newReleaseData = GetNewestVersion();
                    int updateLevel = CompareVersions(FileFinderAppVersion, newReleaseData[0]);
                    //See method "CompareVersions" for more details.
                    //Here's how "update levels" work:
                    //0 = same version >>> do nothing
                    //1 = patch >>> update
                    //2 = minor update >>> update
                    //3 = major update >>> update

                    //The code below executes if an update has been found. In other words: If the current version is older than the newer one...
                    if (updateLevel > 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write("Update found! ");
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
                        }
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine(FileFinderAppVersion + " >> " + newReleaseData[0]);
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.Write("Release name: ");
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine(newReleaseData[1]);
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.Write("Release description: ");
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine(newReleaseData[2]);
                        Console.ResetColor();

                        string destPath = TempDirectory + DirNavigationChar + "FileFinderUpdater";
                        
                        Directory.CreateDirectory(destPath);

                        //download the files
                        try
                        {
                            Console.ForegroundColor = ConsoleColor.DarkGray;
                            Console.WriteLine("Downloading release...");

                            DownloadFile("https://github.com/GermanBread/FileFinderGit/releases/download/" + newReleaseData[0] + "/FileFinder" + AppExtension, destPath + DirNavigationChar + "FileFinder_updater" + AppExtension);
                            
                            //The following step is neccessary for UNIX machines because files require "execute permissions"
                            if (IsUNIX)
                            {
                                RunInBash("chmod +x " + destPath + DirNavigationChar + "FileFinder_updater" + AppExtension);
                            }

                            //Now duplicate the executable to avoid a headache later
                            File.Copy(destPath + DirNavigationChar + "FileFinder_updater" + AppExtension, destPath + DirNavigationChar + "FileFinder" + AppExtension);

                            Console.WriteLine("Starting updater...");
                            Console.WriteLine("Updater at: " + destPath + DirNavigationChar + "FileFinder_updater" + AppExtension);
                            Console.ResetColor();
                            
                            //Now start a thread which will replace the main thread.
                            ProcessStartInfo startInfo = new ProcessStartInfo();
                            startInfo.FileName = destPath + DirNavigationChar + "FileFinder" + AppExtension;
                            startInfo.Arguments = "\"" + Directory.GetCurrentDirectory() + "\"";
                            startInfo.WorkingDirectory = destPath;
                            startInfo.CreateNoWindow = false;
                            Process.Start(startInfo);
                            
                            return;
                        }
                        catch (Exception caughtException)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Error while downloading release.");
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine("Download the newest release at: https://github.com/GermanBread/FileFinderGit/releases");
                            Console.ResetColor();
                            Console.WriteLine("Exception: " + caughtException.Message);
                            Console.ForegroundColor = ConsoleColor.DarkGray;
                            Thread.Sleep(2000);
                            Console.WriteLine("Continuing...");
                            Console.ResetColor();
                        }
                    }
                    else
                    {
                        Console.WriteLine("This app is up-to-date.");
                    }
                }

                #endregion

                #region Settings manager

                Console.CursorVisible = false;
                Console.WriteLine("If you don't see a menu appear, restart the app.");
                
                //Settings manager is called here
                SettingsUI settingsMenu = new SettingsUI();

                //add menu entries
                //settingsMenu.settings.Add(new SettingsEntry("Paths", SettingsEntry.InteractionType.nonSelectableAndNonInteractable, new List<string>() { "" }, 0));
                settingsMenu.settings.Add(new SettingsEntry("Source path", SettingsEntry.InteractionType.selectableAndInteractable, new List<string>() { "$Path", ""}, 0, "Path to the files you want sorted i.e. \"Family_pictures\""));
                settingsMenu.settings.Add(new SettingsEntry("Destination path", SettingsEntry.InteractionType.selectableAndInteractable, new List<string>() { "$Path", "" }, 0, "Path to the destination folder i.e. \"Family_pictures_sorted\""));
                //settingsMenu.settings.Add(new SettingsEntry("Testpath", SettingsEntry.InteractionType.selectableAndInteractable, new List<string>() { "$Input", "" }, 0));
                
                settingsMenu.settings.Add(new SettingsEntry("", SettingsEntry.InteractionType.nonSelectableAndNonInteractable, new List<string>() { "" }, 0));
                //settingsMenu.settings.Add(new SettingsEntry("Files", SettingsEntry.InteractionType.nonSelectableAndNonInteractable, new List<string>() { "" }, 0));

                settingsMenu.settings.Add(new SettingsEntry("Should filenames be changed?", SettingsEntry.InteractionType.selectableAndInteractable, new List<string>() { "No, do not change the name", "Add date only", "Add date and iterator" }, 2));
                settingsMenu.settings.Add(new SettingsEntry("Should files be sorted?", SettingsEntry.InteractionType.selectableAndInteractable, new List<string>() { "No", "Yes" }, 1));
                settingsMenu.settings.Add(new SettingsEntry("Should duplicates be overwritten?", SettingsEntry.InteractionType.selectableAndInteractable, new List<string>() { "No, always keep the duplicate", "Only overwrite if source file is newer", "Only overwrite if source file is older", "Yes, always overwrite the duplicate" }, 1));
                //settingsMenu.settings.Add(new SettingsEntry("Create a directory to copy the files to?", SettingsEntry.InteractionType.selectableAndInteractable, new List<string>() { "No", "Yes" }, 0));
                
                settingsMenu.settings.Add(new SettingsEntry("", SettingsEntry.InteractionType.nonSelectableAndNonInteractable, new List<string>() { "" }, 0));
                settingsMenu.settings.Add(new SettingsEntry("", SettingsEntry.InteractionType.selectableAndInteractable, new List<string>() { "$Done" }, 0));

                while (!settingsMenu.DrawSettings("FileFinder settings manager " + FileFinderAppVersion));
                
                //variables that depend on the settings manager
                string Path = settingsMenu.settings[0].StrValueLabels[1];
                string TargetPath = settingsMenu.settings[1].StrValueLabels[1];
                int FileNameType = settingsMenu.settings[3].IntSelection;
                bool SortingEnabled = settingsMenu.settings[4].IntSelection == 1;
                int OverwriteType = settingsMenu.settings[5].IntSelection;
                bool CreateTargetDir = settingsMenu.settings[6].IntSelection == 1;

                #endregion

            #endregion

            #region Phases Init

                #region Log-files and directory creation
                
                //create log directory
                try 
                {
                    if (!Directory.Exists("." + DirNavigationChar + "FileFinder_Logs"))
                    {
                        Directory.CreateDirectory("." + DirNavigationChar + "FileFinder_Logs");
                    }
                } 
                catch 
                {
                    Console.WriteLine("Logdir creation: The programm encountered a severe error and cannot continue.");
                    Console.CursorVisible = true;
                    return;
                }
                
                //create log file
                StreamWriter logFile;
                try 
                {
                    logFile = new StreamWriter("." + DirNavigationChar + "FileFinder_Logs" + DirNavigationChar + LogFileName);
                } 
                catch (IOException ioException) 
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Logfile creation: Whoops, looks like something went wrong when creating a log file! Trying again!");
                    Console.ResetColor();
                    ExceptionsThrown.Add(ioException);
                    try 
                    {
                        logFile = new StreamWriter("." + DirNavigationChar + "FileFinder_Logs" + DirNavigationChar + LogFileName + new Random().Next(0, 9));   
                    } 
                    catch 
                    {
                        Console.WriteLine("Logfile creation: The programm encountered a severe error and cannot continue.");
                        Console.CursorVisible = true;
                        return;
                    }
                }

                #endregion

            //initialize the console ... again (just in case)
            Console.Clear();
            Console.SetCursorPosition(0, 0);

                #region Legacy code
                
                //check if the path is valid
                /*
                if (File.Exists(Path))
                {
                    Console.WriteLine("\"" + Path + "\" is a file");
                    logFile.WriteLine("Path-check log:");
                    logFile.WriteLine("Source-path is a file");
                    logFile.WriteLine();
                    logFile.WriteLine("===============================");                
                    logFile.Close();
                    
                    Console.CursorVisible = true;
                    return;
                }
                else if (!Directory.Exists(Path))
                {
                    Console.WriteLine("\"" + Path + "\" does not exist");
                    logFile.WriteLine("Path-check log:");
                    logFile.WriteLine("Source-path does not exist");
                    logFile.WriteLine();
                    logFile.WriteLine("===============================");                
                    logFile.Close();
                    
                    Console.CursorVisible = true;
                    return;
                }

                //create a directory before the check
                if (CreateTargetDir && !Directory.Exists(TargetPath) && !File.Exists(TargetPath))
                {
                    Directory.CreateDirectory(TargetPath);
                }
                
                //check if the target path is valid
                if (File.Exists(TargetPath))
                {
                    Console.WriteLine("\"" + TargetPath + "\" is a file");
                    logFile.WriteLine("Path-check log:");
                    logFile.WriteLine("Target-path is a file");
                    logFile.WriteLine();
                    logFile.WriteLine("===============================");                
                    logFile.Close();
                    
                    Console.CursorVisible = true;
                    return;
                }
                else if (!Directory.Exists(TargetPath))
                {
                    Console.WriteLine("\"" + TargetPath + "\" does not exist");
                    logFile.WriteLine("Path-check log:");
                    logFile.WriteLine("Target-path does not exist");
                    logFile.WriteLine();
                    logFile.WriteLine("===============================");                
                    logFile.Close();
                    
                    Console.CursorVisible = true;
                    return;
                }
                */

                #endregion
            
                #region Logfile init

                //write to file
                logFile.WriteLine("FILE LOCATOR & COPIER LOG");
                logFile.WriteLine();
                logFile.WriteLine("===============================");

                logFile.WriteLine("These arguments were given:");
                logFile.WriteLine("Set[0] = " + Path);
                logFile.WriteLine("Set[1] = " + TargetPath);
                logFile.WriteLine("Set[2] = " + FileNameType);
                logFile.WriteLine("Set[3] = " + OverwriteType);
                logFile.WriteLine();
                logFile.WriteLine("===============================");

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
            
            Console.WriteLine("Looking for files in \"" + Path + "\"");
            //define how strings should be compared
            StringComparison stringComparisonType = StringComparison.OrdinalIgnoreCase;
            //look for files with certain extensions
            try
            {
                FilePaths.AddRange(Directory.EnumerateFiles(Path, "*", SearchOption.AllDirectories)
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
                logFile.WriteLine("File finding method: " + caughtException.Message);
                logFile.Close();
                Console.CursorVisible = true;
                return;
            }

            FilePaths.Sort();

            //log in console how many files were found
            Console.WriteLine(FilePaths.Count + " media files were found");

            if (FilePaths.Count > 0)
            {
                logFile.WriteLine("File-locator log:");
                logFile.WriteLine(FilePaths.Count + " media files were found");
            }

            foreach (var item in FilePaths)
            {
                logFile.WriteLine("Found \"" + IsolateFilename(item) + "." + IsolateFileExtension(item) + "\"");
            }

            #endregion

            #region File Copying Phase

            if (FilePaths.Count > 0)
            {
                logFile.WriteLine();
                logFile.WriteLine("===============================");
                logFile.WriteLine("File-copier log:");
            }

            List<string> processedFileList = new List<string>();

            int unsortedCount = 0;
            
            for (int i = 0; i < FilePaths.Count; i++)
            {
                Console.SetCursorPosition(0, 3);
                Console.WriteLine("Copying, please wait                                   ");
                Console.BackgroundColor = ConsoleColor.DarkGray;
                Console.Write(BarGraph(i + 1, FilePaths.Count, 20));
                Console.ResetColor(); 
                Console.WriteLine(" " + (i + 1) + " out of " + FilePaths.Count + " files copied.");
                
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
                            logFile.WriteLine("WARNING: \"" + FilePaths[i] + "\" has missing metadata, falling back to last write time!");
                            unsortedCount++;
                        }
                    }
                }
                catch (Exception caughtException)
                {
                    //fallback
                    fileShotTime = File.GetLastWriteTimeUtc(FilePaths[i]).ToString();
                    logFile.WriteLine("WARNING: \"" + FilePaths[i] + "\" could not be sorted due to an error, falling back to last write time!");
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
                        logFile.WriteLine("Copied \"" + FilePaths[i] + "\" to \"" + copypath + "\"");
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
                                logFile.WriteLine("INFO: User told me to overwrite \"" + copypath + "\"");
                            }
                            else if (OverwriteType == 2)
                            {
                                logFile.WriteLine("INFO: This file \"" + copypath + "\" is newer, so I'm going to overwrite it");
                            }
                            else
                            {
                                logFile.WriteLine("INFO: This file \"" + copypath + "\" is older, so I'm going to overwrite it");
                            }
                            Console.WriteLine("Overwriting duplicate file                                  ");

                            processedFileList.Insert(0, "Overwritten \"" + IsolateFilename(FilePaths[i]) + "." + IsolateFileExtension(FilePaths[i]) + "\"                                      ");
                        }
                        else if (OverwriteType == 0)
                        {
                            //this will execute when OVERWRITE equals FALSE a.k.a. when the user wants to keep the files
                            logFile.WriteLine("INFO: User told me not to touch existing files!");
                            processedFileList.Insert(0, "Kept \"" + IsolateFilename(FilePaths[i]) + "." + IsolateFileExtension(FilePaths[i]) + "\"                                      ");
                        }
                        else
                        {
                            //this will execute when the file in the source folder is older than the file in the target folder
                            logFile.WriteLine("INFO: Kept \"" + copypath + "\"");
                            processedFileList.Insert(0, "Kept \"" + IsolateFilename(FilePaths[i]) + "." + IsolateFileExtension(FilePaths[i]) + "\"                                      ");
                        }
                    }
                } catch (Exception caughtException) {
                    //this normally executes when there is a incorrect navigation character or if the filename is invalid
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

            #endregion

            #region Programm Exit
            
            Console.ForegroundColor = ConsoleColor.Green;
            Console.SetCursorPosition(0, 16);
            Console.WriteLine("Done");
            Console.ResetColor();
            Console.WriteLine("You may want to check the log file");
            Console.WriteLine();
            
            //log to file
            if (FilePaths.Count > 0 || ExceptionsThrown.Count > 0)
            {
                logFile.WriteLine("===============================");
            }
            if (FilePaths.Count > 0)
            {
                logFile.WriteLine($"{(int)(((float)unsortedCount / (float)FilePaths.Count) * 100f)}% of files were sorted using fallback method");                
            }
            if (ExceptionsThrown.Count > 0)
            {
                logFile.WriteLine($"The following {ExceptionsThrown.Count} exception(s) were caught:");
            }
            foreach (var exceptionName in ExceptionsThrown)
            {
                logFile.WriteLine(exceptionName.Message);
                logFile.WriteLine(exceptionName.TargetSite);
            }
            //add spacer
            if (FilePaths.Count > 0 || ExceptionsThrown.Count > 0)
            {
                logFile.WriteLine();
                logFile.WriteLine("===============================");
            }
            logFile.WriteLine("Date when programm exited: " + DateTime.Now);
            logFile.WriteLine();
            logFile.WriteLine("===============================");

            //close the stream
            logFile.Close();
            logFile.Dispose();
            Console.CursorVisible = true;

            #endregion
        }
        
        #region Program Methods
        
        static string[] GetNewestVersion()
        {
            string versionNumber = "";
            string versionName = "";
            string versionDescription = "";
            char[] validChars = new char[] { '1', '2', '3', '4', '5', '6', '7', '8', '9', '0', '.', 'v'};
            
            var webRequest = WebRequest.Create("https://api.github.com/repos/GermanBread/FileFinderGit/releases/latest") as HttpWebRequest;

            webRequest.ContentType = "application/json";
            webRequest.UserAgent = "Nothing";

            using (var s = webRequest.GetResponse().GetResponseStream())
            {
                using (var sr = new StreamReader(s))
                {
                    var answer = sr.ReadToEnd();
                    //I'm too lazy to deal with this JSON stuff, let's just bruteForcedJSON force it
                    string[] rawData = answer.Split(',');
                    List<string> bruteForcedJSON = new List<string>();
                    bruteForcedJSON.Add(rawData.Where(a => a.Contains("\"tag_name\"")).First());
                    bruteForcedJSON.Add(rawData.Where(a => a.Contains("\"name\"")).First());
                    bruteForcedJSON.Add(rawData.Where(a => a.Contains("\"body\"")).First());
                    versionNumber = RemoveTrailingChar(bruteForcedJSON[0].Split(':')[1], '\"');
                    versionName = RemoveTrailingChar(bruteForcedJSON[1].Split(':')[1], '\"');
                    versionDescription = RemoveTrailingChar(RemoveTrailingChar(bruteForcedJSON[2].Split(':')[1], '}'), '\"');
                    
                    sr.Close();
                    sr.Dispose();
                }
            }
            
            return new string[] { versionNumber, versionName, versionDescription };
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

            int updateLevel = 0;
            //Here's how "update levels" work:
            //0 = same version >>> do nothing
            //1 = minor update >>> update
            //2 = normal update >>> update
            //3 = major update >>> update

            //compare versions            
            if (firstSplit[0] == secondSplit[0])
            {
                if (firstSplit[1] == secondSplit[1])
                {
                    if (firstSplit[2] == secondSplit[2])
                    {
                        updateLevel = 0;
                    }
                    else
                    {
                        updateLevel = 1;
                    }
                }
                else
                {
                    updateLevel = 2;
                }
            }
            else
            {
                updateLevel = 3;
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
                output += format[(int)Math.Clamp(ratio * format.Length * width - i * format.Length, 0, format.Length - 1)];
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
