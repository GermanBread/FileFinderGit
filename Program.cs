using System;
using System.IO;
using System.Collections.Generic;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.Xmp;
using System.Linq;
using User_Interface;

namespace FileFinder
{
    class Program
    {
        static void Main(string[] args)
        {   
            #region Program Setup

            //Exit if the programm isn't running in a console window!
            if (Console.WindowHeight == 0 && Console.WindowWidth == 0)
            {
                return;
            }
            
            Console.CursorVisible = false;
            Console.WriteLine("If you don't see a menu appear, restart the app.");
            
            //variables that don't depend on the settings manager
            char dirNavigationChar = System.Environment.OSVersion.Platform == PlatformID.Win32NT ? '\\' : '/';
            string LogFileName = "FileFinder_log_" + new Random().Next(1111, 9999).ToString() + ".txt";
            List<Exception> ExceptionsThrown = new List<Exception>();

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

            while (!settingsMenu.DrawSettings("FileFinder settings manager"));
            
            //variables that depend on the settings manager
            string Path = settingsMenu.settings[0].StrValueLabels[1];
            string TargetPath = settingsMenu.settings[1].StrValueLabels[1];
            int FileNameType = settingsMenu.settings[3].IntSelection;
            bool SortingEnabled = settingsMenu.settings[4].IntSelection == 1;
            int OverwriteType = settingsMenu.settings[5].IntSelection;
            bool CreateTargetDir = settingsMenu.settings[6].IntSelection == 1;

            #endregion

            #region Phases Init

            //create log directory
            try 
            {
                if (!Directory.Exists("." + dirNavigationChar + "FileFinder_Logs"))
                {
                    Directory.CreateDirectory("." + dirNavigationChar + "FileFinder_Logs");
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
                logFile = new StreamWriter("." + dirNavigationChar + "FileFinder_Logs" + dirNavigationChar + LogFileName);
            } 
            catch (IOException ioException) 
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Logfile creation: Whoops, looks like something went wrong when creating a log file! Trying again!");
                Console.ResetColor();
                ExceptionsThrown.Add(ioException);
                try 
                {
                    logFile = new StreamWriter("." + dirNavigationChar + "FileFinder_Logs" + dirNavigationChar + LogFileName + new Random().Next(0, 9));   
                } 
                catch 
                {
                    Console.WriteLine("Logfile creation: The programm encountered a severe error and cannot continue.");
                    Console.CursorVisible = true;
                    return;
                }
            }

            //initialize the console ... again (just in case)
            Console.CursorVisible = false;
            Console.Clear();
            Console.SetCursorPosition(0, 0);

            //check if the path is valid (obsolete?)
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

            //file lists
            List<string> FilePaths = new List<string>();

            //splash screen
            Console.Clear();
            Console.WriteLine("[ FILE FINDER ]");
            System.Threading.Thread.Sleep(500);
            Console.WriteLine("[ STABLE BRANCH ]");
            System.Threading.Thread.Sleep(500);
            Console.WriteLine("[ MADE BY: GermanBread#9087 ]");
            System.Threading.Thread.Sleep(500);
            Console.WriteLine("[ GITHUB PROJECT: https://github.com/GermanBread/FileFinderGit ]");
            System.Threading.Thread.Sleep(500);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[ STARTING ]");
            Console.ResetColor();
            System.Threading.Thread.Sleep(1000);

            #endregion
            
            #region File Finding Phase
            
            Console.Clear();
            Console.SetCursorPosition(0, 0);
            /////////////////////////////////////////
                        //File LOCATOR//
            /////////////////////////////////////////
            Console.WriteLine("Looking for files in \"" + Path + "\"");
            
            //search all directories for files
            //define how strings should be compared
            var stringComparisonType = StringComparison.OrdinalIgnoreCase;
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
                Console.WriteLine();
                Console.WriteLine("File finding method: " + caughtException.Message);
                Console.ResetColor();
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
                Func<string, string> addDirNavCharIfNotInName = x => x[x.Length - 1] != dirNavigationChar ? dirNavigationChar.ToString() : "";
                if (fileShotTime.Take(4).All(c => c >= '0' && c <= '9'))
                {
                    //this will execute when the date-format is yyyy:mm:dd-hh:MM:ss
                    if (FileNameType > 0)
                    {
                        destpath = TargetPath + addDirNavCharIfNotInName(TargetPath) + (SortingEnabled ? new string(fileShotTime.Take(4).ToArray()) + "_" + new string(fileShotTime.Skip(5).Take(2).ToArray()) + dirNavigationChar : "");

                        filename = new string(fileShotTime.Take(4).ToArray()) + "_" + new string(fileShotTime.Skip(5).Take(2).ToArray()) + "_" 
                        + new string(fileShotTime.Skip(8).Take(2).ToArray()) + "-" + new string(fileShotTime.Skip(11).Take(2).ToArray()) + "+" + new string(fileShotTime.Skip(14).Take(2).ToArray())
                        + "+" + new string(fileShotTime.Skip(17).Take(2).ToArray()) + "-" + IsolateFilename(FilePaths[i]) + returnIterator(FileNameType == 2) + "." + IsolateFileExtension(FilePaths[i]).ToLower();
                    }
                    else
                    {
                        destpath = TargetPath + addDirNavCharIfNotInName(TargetPath) + (SortingEnabled ? new string(fileShotTime.Take(4).ToArray()) + "_" + new string(fileShotTime.Skip(5).Take(2).ToArray()) + dirNavigationChar : "");
                        filename = IsolateFilename(FilePaths[i]) + "." + IsolateFileExtension(FilePaths[i]);
                    }
                }
                else
                {
                    //this will execute when the date-format is dd:mm:yyyy-hh:MM:ss
                    if (FileNameType > 0)
                    {
                        destpath = TargetPath + addDirNavCharIfNotInName(TargetPath) + (SortingEnabled ? new string(fileShotTime.Skip(6).Take(4).ToArray()) + "_" + new string(fileShotTime.Skip(3).Take(2).ToArray()) + dirNavigationChar : "");

                        filename = new string(fileShotTime.Skip(6).Take(4).ToArray()) + "_" + new string(fileShotTime.Skip(3).Take(2).ToArray()) + "_" 
                        + new string(fileShotTime.Take(2).ToArray()) + "-" + new string(fileShotTime.Skip(11).Take(2).ToArray()) + "+" + new string(fileShotTime.Skip(14).Take(2).ToArray())
                        + "+" + new string(fileShotTime.Skip(17).Take(2).ToArray()) + "-" + IsolateFilename(FilePaths[i]) + returnIterator(FileNameType == 2) + "." + IsolateFileExtension(FilePaths[i]).ToLower();
                    }
                    else
                    {
                        destpath = TargetPath + addDirNavCharIfNotInName(TargetPath) + (SortingEnabled ? new string(fileShotTime.Skip(6).Take(4).ToArray()) + "_" + new string(fileShotTime.Skip(3).Take(2).ToArray()) + dirNavigationChar : "");
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
            Console.CursorVisible = true;

            #endregion
        }
        
        #region Program Methods
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
