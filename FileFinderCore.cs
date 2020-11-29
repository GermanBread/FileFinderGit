//System
using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Threading;
using System.Text.Json;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text.Json.Serialization;
//Metadata Extractor
using MetadataExtractor.Formats.Xmp;
using MetadataExtractor.Formats.Exif;
//Custom
using UserInterface;

namespace FileFinder
{
    /// <summary>
    /// This class acts as your entry point, it handles errors, logging etc.
    /// </summary>
    public static class FileFinder
    {
        #region Constants
        
        public static string LOGFILE_BASE_PATH = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar/* + "FileFinder_Logs" + Path.DirectorySeparatorChar*/;
        public static string PREFS_FILE_PATH = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "Preferences.json";
        public static string TEMP_DIRECTORY_PATH = Path.TrimEndingDirectorySeparator(Path.GetTempPath());
        public static bool IS_IN_TEMP = Directory.GetCurrentDirectory().Contains(TEMP_DIRECTORY_PATH);
        public static bool IS_UNIX = System.Environment.OSVersion.Platform.Equals(System.PlatformID.Unix);
        public static string APP_NAME = "FileFinder";
        public static string APP_EXTENSION = IS_UNIX ? ".x86-64" : ".exe";
        //Release definition
        public static string APP_VERSION = "v3.0.0";

        #endregion
        
        //Now start the methods
        /// <summary>
        /// This method initializes the app
        /// </summary>
        /// <returns>A class that contains all data needed to troubleshoot in case something goes wrong.</returns>
        public static FFInitData Init(ref string[] args)
        {
            //Create log files
            Logger.CreateLog(0, LOGFILE_BASE_PATH + "Init.log");
            Logger.CreateLog(1, LOGFILE_BASE_PATH + "Updater.log");
            Logger.CreateLog(2, LOGFILE_BASE_PATH + "Runtime.log");
            
            //Variables
            Logger.LogToFile(0, "Instantiated classes", Logger.UrgencyLevel.Success);
            FFInitData InitData = new FFInitData { };
            FFUpdater FFUpdater = new FFUpdater { };
            FFCore FFMain = new FFCore { };

            //Methods
            try
            {
                if (IS_IN_TEMP)
                {
                    FFUpdater.UpdateApp(ref InitData);
                    throw new QuitRequestedException();
                }
                
                FFUpdater.DeleteTemp(ref InitData);
                FFUpdater.FetchUpdates(ref InitData);
                FFUpdater.CompareUpdates(ref InitData);
                if (FFUpdater.UpdaterData.UpdateLevel > 0)
                {
                    FFUpdater.ShowUpdateMenu(ref InitData);
                }
                
                FFMain.Settings(ref InitData);
                FFMain.FindFiles(ref InitData);
                if (FFMain.CoreData.FilePaths.Count > 0)
                    FFMain.CopyFiles(ref InitData);
                FFMain.FinalizeResults(ref InitData);
            }
            catch (QuitRequestedException excep)
            {
                //Write results to file
                Logger.LogToFile(0, "A method requested an application exit", Logger.UrgencyLevel.Info);
                if (excep.Message != null)
                    Logger.LogToFile(0, "Message: " + excep.Message, Logger.UrgencyLevel.Info);
            }
            catch (Exception excep) {
                //Write results to file
                Logger.LogToFile(0, "A method threw an exception", Logger.UrgencyLevel.Critical);
                Logger.LogToFile(0, $"Exception: {excep}", Logger.UrgencyLevel.Info);
                Console.WriteLine("The app ran into an error. View logs for more details.");
            }
            
            //Save log files
            foreach (int key in Logger.LogFiles.Keys)
            {
                Logger.SaveLog(key);
            }
            
            //Return
            return InitData;
        }
    }

    #region Functional classes
    
    /// <summary>
    /// This class contains all methods that are responsible for updating. It contains methods that:
    /// <para>
    /// - Fetch the newest Github release<para />
    /// - Show a menu in the event that an update has been found<para />
    /// - Compare the versions between releases<para />
    /// - Download the new releases<para />
    /// - Install the newest release<para />
    /// </para>
    /// </summary>
    public class FFUpdater
    {
        public FFUpdaterData UpdaterData = new FFUpdaterData { };
        /// <summary>
        /// This method deletes the temp directory
        /// </summary>
        public void DeleteTemp(ref FFInitData InitData)
        {
            try
            {
                //Get a list of files and delete them.
                Logger.LogToFile(1, "Checking for an existing temp directory", Logger.UrgencyLevel.Info);
                if (Directory.Exists(FileFinder.TEMP_DIRECTORY_PATH + Path.DirectorySeparatorChar + "FileFinderUpdater"))
                {
                    //Enumerate each file and delete
                    foreach (var file in Directory.EnumerateFiles(FileFinder.TEMP_DIRECTORY_PATH + Path.DirectorySeparatorChar + "FileFinderUpdater"))
                    {
                        Logger.LogToFile(1, $"Deleting {file}", Logger.UrgencyLevel.Info);
                        File.Delete(file);
                    }
                    //Enumerate each directory and delete
                    foreach (var directory in Directory.EnumerateDirectories(FileFinder.TEMP_DIRECTORY_PATH + Path.DirectorySeparatorChar + "FileFinderUpdater"))
                    {
                        Logger.LogToFile(1, $"Deleting {directory}", Logger.UrgencyLevel.Info);
                        Directory.Delete(directory);
                    }
                    Logger.LogToFile(1, "Temp directory deleted", Logger.UrgencyLevel.Info);
                    Directory.Delete(FileFinder.TEMP_DIRECTORY_PATH + Path.DirectorySeparatorChar + "FileFinderUpdater");
                }
            }
            catch (Exception caughtException)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Logger.LogToFile(1, "Temp directory could not be deleted", Logger.UrgencyLevel.Critical);
                Logger.LogToFile(1, "Exception: " + caughtException.Message, Logger.UrgencyLevel.Info);
                Console.WriteLine($"Temp directory could not be deleted. Delete \"{FileFinder.TEMP_DIRECTORY_PATH + Path.DirectorySeparatorChar}FileFinderUpdater\" and try again. Check logs for more info.");
                throw new QuitRequestedException("Temp deletion method");
            }
        }
        /// <summary>
        /// This method uses the Github API to get a JSON containing all releases, deserializes it and stores the JSON in the shared variable.
        /// </summary>
        /// <param name="initData"></param>
        public void FetchUpdates(ref FFInitData InitData)
        {
            HttpWebRequest WebReq = WebRequest.Create(UpdaterData.GitHubURL) as HttpWebRequest;

            WebReq.ContentType = "application/json";
            WebReq.UserAgent = "Nothing";

            using (Stream WebStream = WebReq.GetResponse().GetResponseStream())
            {
                using (StreamReader WebStreamReader = new StreamReader(WebStream))
                {
                    //Convert the answer into a string, then convert it into usable data
                    string Answer = WebStreamReader.ReadToEnd();
                    UpdaterData.Releases = JsonSerializer.Deserialize<List<ReleaseData>>(Answer);
                    
                    //Free up memory
                    WebStreamReader.Close();
                    WebStreamReader.Dispose();
                }
            }
        }
        /// <summary>
        /// This method downloads the newest release
        /// </summary>
        /// <param name="initData"></param>
        public void DownloadUpdate(ref FFInitData InitData)
        {
            //Create the updater directory if it doesn't exist
            if (!Directory.Exists(UpdaterData.UpdaterPath))
            {
                Logger.LogToFile(1, "Temp directory created", Logger.UrgencyLevel.Info);
                Directory.CreateDirectory(UpdaterData.UpdaterPath);
            }
            
            Logger.LogToFile(1, "Downloading updater", Logger.UrgencyLevel.Info);
            long fileSize = 0;
            int bufferSize = 1024;
            bufferSize *= 1000;
            long existLen = 0;
            
            System.IO.FileStream saveFileStream;
            if (System.IO.File.Exists(UpdaterData.UpdaterPath + Path.DirectorySeparatorChar + UpdaterData.UpdaterFileName))
            {
                System.IO.FileInfo destinationFileInfo = new System.IO.FileInfo(UpdaterData.UpdaterPath + Path.DirectorySeparatorChar + UpdaterData.UpdaterFileName);
                existLen = destinationFileInfo.Length;
            }

            if (existLen > 0)
                saveFileStream = new System.IO.FileStream(UpdaterData.UpdaterPath + Path.DirectorySeparatorChar + UpdaterData.UpdaterFileName, System.IO.FileMode.Append, System.IO.FileAccess.Write, System.IO.FileShare.ReadWrite);
            else
                saveFileStream = new System.IO.FileStream(UpdaterData.UpdaterPath + Path.DirectorySeparatorChar + UpdaterData.UpdaterFileName, System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.ReadWrite);
        
            System.Net.HttpWebRequest httpReq;
            System.Net.HttpWebResponse httpRes;
            httpReq = (System.Net.HttpWebRequest) System.Net.HttpWebRequest.Create(UpdaterData.Releases[UpdaterData.SelectedRelease].ReleaseAssets.Find(a => a.AssetDownloadURL.Contains(FileFinder.APP_EXTENSION)).AssetDownloadURL);
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

            Console.ForegroundColor = ConsoleColor.DarkGray;

            //The following step is neccessary for UNIX machines because files require "execute permissions"
            if (FileFinder.IS_UNIX)
            {
                Logger.LogToFile(1, "Marking new file as executable", Logger.UrgencyLevel.Info);
                RunInBash("chmod +x " + UpdaterData.UpdaterPath + Path.DirectorySeparatorChar + UpdaterData.UpdaterFileName);
            }

            //Now duplicate the executable to avoid a headache later
            Logger.LogToFile(1, "Duplicating updater", Logger.UrgencyLevel.Info);
            File.Copy(UpdaterData.UpdaterPath + Path.DirectorySeparatorChar + UpdaterData.UpdaterFileName, UpdaterData.UpdaterPath + Path.DirectorySeparatorChar + FileFinder.APP_NAME + FileFinder.APP_EXTENSION);

            Console.WriteLine("Updater: Starting updater: " + UpdaterData.UpdaterPath + Path.DirectorySeparatorChar + UpdaterData.UpdaterFileName);
            Console.ResetColor();
            
            //Now start a thread which will replace the main thread.
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = UpdaterData.UpdaterPath + Path.DirectorySeparatorChar + UpdaterData.UpdaterFileName;
            //This is scuffed, but it'll do
            startInfo.Arguments = Directory.GetCurrentDirectory().Replace(" ", "─");
            startInfo.WorkingDirectory = UpdaterData.UpdaterPath + Path.DirectorySeparatorChar;
            startInfo.CreateNoWindow = false;
            Logger.LogToFile(1, "Starting updater", Logger.UrgencyLevel.Info);
            Process.Start(startInfo);
            
            throw new QuitRequestedException();
        }
        /// <summary>
        /// This method compares app versions with eachanother
        /// </summary>
        /// <param name="initData"></param>
        public void CompareUpdates(ref FFInitData InitData)
        {
            ReleaseData cutReleaseData = UpdaterData.Releases[UpdaterData.SelectedRelease];
            string currentVersion = FileFinder.APP_VERSION;
            string newVersion = cutReleaseData.ReleaseTag;
            
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
                UpdaterData.UpdateLevel = 3;
            }
            //compare minor
            else if (firstSplit[0] == secondSplit[0] && firstSplit[1] < secondSplit[1])
            {
                UpdaterData.UpdateLevel = 2;
            }
            //compare patch
            else if (firstSplit[0] == secondSplit[0] && firstSplit[1] == secondSplit[1] && firstSplit[2] < secondSplit[2])
            {
                UpdaterData.UpdateLevel = 1;
            }
            //check if they are equal
            else if (firstSplit[0] == secondSplit[0] && firstSplit[1] == secondSplit[1] && firstSplit[2] == secondSplit[2])
            {
                UpdaterData.UpdateLevel = 0;
            }
            else
            {
                UpdaterData.UpdateLevel = -1;
            }
        }
        /// <summary>
        /// This method shows a menu that lets the user choose whether or not to update
        /// </summary>
        /// <param name="initData"></param>
        public void ShowUpdateMenu(ref FFInitData InitData)
        {
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
                foreach (var release in UpdaterData.Releases)
                {
                    //Used to mark stuff
                    string prefix = "";
                    if (release.IsPreRelease)
                    {
                        prefix += "(Pre-release) ";
                    }
                    versionTags.Add(prefix + release.ReleaseTag);
                }
                UpdaterData.SelectedRelease = pr.SelectionPrompt("Select version", "", versionTags.ToArray(), true, UpdaterData.SelectedRelease);

                //If the user chose a version
                if (UpdaterData.SelectedRelease > 0)
                {
                    break;
                }
            } while (action == 2);

            if (action == 0)
                return;
            else if (action == 1)
                UpdaterData.SelectedRelease = 0;
            
            ReleaseData cutReleaseData = UpdaterData.Releases[UpdaterData.SelectedRelease];
            //The code below executes if an update has been found. In other words: If the current version is older than the newer one...
            Console.ForegroundColor = ConsoleColor.DarkGray;
            
            switch (UpdaterData.UpdateLevel)
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
            Console.WriteLine(FileFinder.APP_VERSION + " >> " + cutReleaseData.ReleaseTag);
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

            DownloadUpdate(ref InitData);
        }
        /// <summary>
        /// This method should only be run by itself. No other methods should be called besides this.
        /// </summary>
        /// <param name="initData"></param>
        public void UpdateApp(ref FFInitData InitData)
        {
            
        }

        /// <summary>
        /// Runs commands in the bash shell
        /// </summary>
        /// <param name="cmd">Command</param>
        public void RunInBash(string cmd)
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
    }

    /// <summary>
    /// This class contains the most critical methods, hence it's name. It contains methods that:
    /// <para>
    /// - Show a menu with settings a user can change<para />
    /// - A method that enumerates files recursively<para />
    /// - A method that copies files<para />
    /// - A method that logs the results to the logfiles<para />
    /// </para>
    /// </summary>
    public class FFCore
    {
        public FFPreferences Preferences = new FFPreferences { };
        public FFCoreData CoreData = new FFCoreData { };
        public void LoadPreferences(ref FFInitData InitData)
        {
            LoadPreferences:
            if (File.Exists(FileFinder.PREFS_FILE_PATH))
            {
                try
                {
                    string PreferencesJson = File.ReadAllText(FileFinder.PREFS_FILE_PATH);
                    Preferences = JsonSerializer.Deserialize<FFPreferences>(PreferencesJson);
                    Logger.LogToFile(2, "Preferences have been set from preferences file", Logger.UrgencyLevel.Success);
                }
                catch (JsonException)
                {
                    Logger.LogToFile(2, "Preferences file contains invalid Json", Logger.UrgencyLevel.Error);
                    File.Delete(FileFinder.PREFS_FILE_PATH);
                    Logger.LogToFile(2, "Deleted preferences file", Logger.UrgencyLevel.Success);
                    goto LoadPreferences;
                }
                catch (Exception) {
                    throw;
                }
            }
            else
            {
                FileStream PreferencesFileStream = File.Create(FileFinder.PREFS_FILE_PATH);
                PreferencesFileStream.Close();
                PreferencesFileStream.Dispose();
                Logger.LogToFile(2, "Preferences file was created", Logger.UrgencyLevel.Success);
            }
        }
        public void SavePreferences(ref FFInitData InitData)
        {
            SavePreferences:
            if (File.Exists(FileFinder.PREFS_FILE_PATH))
            {
                string PreferencesJson = JsonSerializer.Serialize<FFPreferences>(Preferences);
                File.WriteAllText(FileFinder.PREFS_FILE_PATH, PreferencesJson);
                Logger.LogToFile(2, "Preferences have been written to preferences file", Logger.UrgencyLevel.Success);
            }
            else
            {
                FileStream PreferencesFileStream = File.Create(FileFinder.PREFS_FILE_PATH);
                PreferencesFileStream.Close();
                PreferencesFileStream.Dispose();
                Logger.LogToFile(2, "Preferences file has been created", Logger.UrgencyLevel.Success);
                goto SavePreferences;
            }
        }
        public void Settings(ref FFInitData InitData)
        {
            Console.WriteLine("Settings: If you don't see a menu appear, restart the app");
                
            Logger.LogToFile(2, "Instantiated \"Settingsmenu\" class", Logger.UrgencyLevel.Success);
            SettingsUI SettingsMenu = new SettingsUI { };

            //Load user preferences
            LoadPreferences(ref InitData);

            #region Settings

            //paths
            SettingsMenu.Settings.Add(
                new SettingsEntry(
                    "Source path", 
                    SettingsEntry.InteractionType.selectableAndInteractable, 
                    new List<string> { "$Path", ""}, 
                    0, 
                    "Path to the files you want sorted i.e. \"Family_pictures\""
                    )
                );
            SettingsMenu.Settings.Add(
                new SettingsEntry(
                    "Destination path", 
                    SettingsEntry.InteractionType.selectableAndInteractable, 
                    new List<string> { "$Path", "" }, 
                    0, 
                    "Path to the destination folder i.e. \"Family_pictures_sorted\""
                    )
                );
            //options
            SettingsMenu.Settings.Add(
                new SettingsEntry(
                    "", 
                    SettingsEntry.InteractionType.nonSelectableAndNonInteractable, 
                    new List<string> { "" }, 
                    0
                    )
                );
            SettingsMenu.Settings.Add(
                new SettingsEntry(
                    "Should filenames be changed?", 
                    SettingsEntry.InteractionType.selectableAndInteractable, 
                    new List<string> { "No, do not change the name", "Add date only", "Add date and iterator" }, 
                    Preferences.FileNameType
                    )
                );
            SettingsMenu.Settings.Add(
                new SettingsEntry(
                    "Should files be sorted?", 
                    SettingsEntry.InteractionType.selectableAndInteractable, 
                    new List<string> { "No", "Yes" }, 
                    Preferences.SortingEnabled ? 1 : 0
                    )
                );
            List<string> values0to100 = new List<string> { };
            for (var i = 0; i <= 100; i++)
            {
                values0to100.Add(i.ToString());
            }
            SettingsMenu.Settings.Add(
                new SettingsEntry {
                    StrLabel = "Recursive search depth", 
                    EnumInteractable = SettingsEntry.InteractionType.selectableAndInteractable, 
                    StrValueLabels = values0to100,
                    IntSelection = Math.Clamp(Preferences.MaxRecursionCount, 0, 100),
                    StrDescription = "How deep down should files be searched?"
                });
            SettingsMenu.Settings.Add(
                new SettingsEntry(
                    "Should duplicates be overwritten?", 
                    SettingsEntry.InteractionType.selectableAndInteractable, 
                    new List<string> { "No, always keep the duplicate", "Only overwrite if source file is newer", "Only overwrite if source file is older", "Yes, always overwrite the duplicate" }, 
                    Preferences.OverwriteType
                    )
                );
            SettingsMenu.Settings.Add(
                new SettingsEntry(
                    "", 
                    SettingsEntry.InteractionType.nonSelectableAndNonInteractable, 
                    new List<string> { "" }, 
                    0
                    )
                );
            SettingsMenu.Settings.Add(
                new SettingsEntry(
                    "", 
                    SettingsEntry.InteractionType.selectableAndInteractable, 
                    new List<string> { "$Done" }, 
                    0
                    )
                );
            
            #endregion

            //Loop until the user presses "DONE"
            Logger.LogToFile(2, "Showing menu", Logger.UrgencyLevel.Info);
            while (!SettingsMenu.DrawSettings("FileFinder settings manager " + FileFinder.APP_VERSION)) { }
            
            //Other variables
            List<char> ValidIntParseChars = new List<char> { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };

            //Variables that depend on the settings manager
            CoreData.SourcePath = SettingsMenu.Settings[0].StrValueLabels[1];
            CoreData.DestinationPath = SettingsMenu.Settings[1].StrValueLabels[1];
            Preferences.FileNameType = SettingsMenu.Settings[3].IntSelection;
            Preferences.SortingEnabled = SettingsMenu.Settings[4].IntSelection == 1;
            Preferences.OverwriteType = SettingsMenu.Settings[6].IntSelection;
            Preferences.MaxRecursionCount = int.Parse(SettingsMenu.Settings[5].StrValueLabels[SettingsMenu.Settings[5].IntSelection]);

            //Save preferences
            SavePreferences(ref InitData);
        }
        public void FindFiles(ref FFInitData InitData)
        {
            //Get a list of all files
            CoreData.FilePaths = TryGetAllDirectories(CoreData.SourcePath, 0);
            Logger.LogToFile(2, $"{CoreData.FilePaths.Count} media file(s) were found", Logger.UrgencyLevel.Info);
            foreach (var item in CoreData.FilePaths)
            {
                Logger.LogToFile(1, $"Found file \"{item}\"", Logger.UrgencyLevel.Info);
            }

            List<string> TryGetAllDirectories(string path, int recursionLevel)
            {
                //Create a empty list
                List<string> newdirs = new List<string> { };
                List<string> output = new List<string> { };
                
                //Get a list of subdirectories
                try
                {
                    newdirs.AddRange(Directory.GetDirectories(path));
                }
                catch
                {
                    if (recursionLevel > 0)
                    {
                        return newdirs;
                    }
                }
                
                //If the recursion is too deep, return
                if (recursionLevel > Preferences.MaxRecursionCount)
                {
                    return newdirs;
                }
                
                //Call this function recursively with each having a unique path
                newdirs.ForEach(
                    subdir => { 
                        output.AddRange(TryGetAllDirectories(subdir, recursionLevel + 1));
                    }
                );
                output.AddRange(newdirs);
                return output;
            }
        }
        public void CopyFiles(ref FFInitData InitData)
        {

        }
        public void FinalizeResults(ref FFInitData InitData)
        {
            Logger.LogToFile(2, $"Found {CoreData.FilePaths.Count} files, out of which {Math.Round((CoreData.UnsortedCount / CoreData.FilePaths.Count) * 10d) / 10d}% were sorted using the fallback method", Logger.UrgencyLevel.Info);
            if (InitData.CaughtExceptions.Count == 0)
                return;
            if (InitData.CaughtExceptions.Count == 1)
                Logger.LogToFile(2, $"The following {InitData.CaughtExceptions.Count} Exception was caught: ", Logger.UrgencyLevel.Info);
            else
                Logger.LogToFile(2, $"The following {InitData.CaughtExceptions.Count} Exceptions were caught: ", Logger.UrgencyLevel.Info);
            foreach (var excep in InitData.CaughtExceptions)
            {
                Logger.LogToFile(2, excep.Message, Logger.UrgencyLevel.Info);
                Logger.LogToFile(2, excep.TargetSite.Name, Logger.UrgencyLevel.Info);
                Logger.LogToFile(2, excep.StackTrace, Logger.UrgencyLevel.Info);
            }
        }
    }

    public class Logger
    {
        public static int MessageID = 0;
        public static Dictionary<int, StreamWriter> LogFiles = new Dictionary<int, StreamWriter>();
        public static void CreateLog(int ID, string SourcePath)
        {
            LogFiles.Add(ID, new StreamWriter(SourcePath));
            LogFiles.TryGetValue(ID, out StreamWriter writer);
            writer.WriteLine($"Log file created with ID {ID}");
            writer.WriteLine($"Local time is {DateTime.Now}");
        }
        public static void LogToFile(int ID, string Message, UrgencyLevel Urgency)
        {
            StreamWriter writer;
            LogFiles.TryGetValue(ID, out writer);
            switch (Urgency)
            {
                case UrgencyLevel.Info:
                    writer.WriteLine($"[{MessageID} INFO] " + Message);
                    break;
                
                case UrgencyLevel.Success:
                    writer.WriteLine($"[{MessageID} SUCCESS] " + Message);
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
            Info,
            Success,
            Warn,
            Error,
            Critical
        }
    }
    
    #endregion
    
    #region Data classes
    
    public class FFPreferences
    {
        [JsonPropertyName("FileNameType")]
        public int FileNameType { get; set; } = 2;
        [JsonPropertyName("SortingEnabled")]
        public bool SortingEnabled { get; set; } = true;
        [JsonPropertyName("OverwriteType")]
        public int OverwriteType { get; set; } = 1;
        [JsonPropertyName("MaxRecursionCount")]
        public int MaxRecursionCount { get; set; } = 10;
    }
    public class FFCoreData
    {
        public List<string> FilePaths = new List<string> { };
        public int UnsortedCount = 0;
        public string SourcePath = "";
        public string DestinationPath = "";
    }
    public class FFUpdaterData
    {
        public List<ReleaseData> Releases = new List<ReleaseData> { };
        public int SelectedRelease = 0;
        public string UpdaterPath = FileFinder.TEMP_DIRECTORY_PATH + Path.DirectorySeparatorChar + "FileFinderUpdater";
        public string UpdaterFileName = FileFinder.APP_NAME + "_Updater" + FileFinder.APP_EXTENSION;
        public string GitHubURL = "https://api.github.com/repos/GermanBread/FileFinderGit/releases";
        public int UpdateLevel = 0;
    }
    public class FFInitData
    {
        public List<Exception> CaughtExceptions = new List<Exception> { };
    }

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

        public class ReleaseAssetsData
        {
            [JsonPropertyName("name")]
            public string AssetName { get; set; }
            [JsonPropertyName("browser_download_url")]
            public string AssetDownloadURL { get; set; }
        }
    }
    
    #endregion

    #region Exceptions

    [Serializable]
    public class QuitRequestedException : Exception
    {
        public QuitRequestedException() { }
        public QuitRequestedException(string message) : base(message) { }
        public QuitRequestedException(string message, System.Exception inner) : base(message, inner) { }
        protected QuitRequestedException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    #endregion
}