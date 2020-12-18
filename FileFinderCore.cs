//System
using System;
using System.IO;
using System.Net;
using System.Linq;
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
        
        public static string LOGFILE_BASE_PATH = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + Path.DirectorySeparatorChar + "FileFinderData" + Path.DirectorySeparatorChar /*+ "FileFinder_Logs" + Path.DirectorySeparatorChar*/;
        public static string PREFS_FILE_PATH = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + Path.DirectorySeparatorChar + "FileFinderData" + Path.DirectorySeparatorChar + "Preferences.json";
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
            if (!Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + Path.DirectorySeparatorChar + "FileFinderData"))
                Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + Path.DirectorySeparatorChar + "FileFinderData");
            #if !DEBUG
            Logger.CreateLog(0, LOGFILE_BASE_PATH + "Init.log");
            Logger.CreateLog(1, LOGFILE_BASE_PATH + "Updater.log");
            Logger.CreateLog(2, LOGFILE_BASE_PATH + "Runtime.log");
            #else
            Logger.CreateLog(0, "Init.log");
            Logger.CreateLog(2, "Runtime.log");
            #endif
            Logger.LogToFile(0, $"OS: " + Environment.OSVersion + (Environment.Is64BitOperatingSystem ? "-64bit" : "-32bit"), Logger.UrgencyLevel.Info);
            Logger.LogToFile(0, $"Thread count: {Environment.ProcessorCount} threads", Logger.UrgencyLevel.Info);
            Logger.LogToFile(0, $"Uptime: {Environment.TickCount64} milliseconds", Logger.UrgencyLevel.Info);
            
            //Variables
            Logger.LogToFile(0, "Instantiated classes", Logger.UrgencyLevel.Success);
            FFInitData InitData = new FFInitData { ConsoleArgs = args };
            FFUpdater FFUpdater = new FFUpdater { };
            FFCore FFMain = new FFCore { };

            //Hide the cursor
            Console.CursorVisible = false;

            //Methods
            try
            {
                if (IS_IN_TEMP)
                {
                    FFUpdater.UpdateApp(ref InitData);
                }
                
                //Check for updates and install
                #if !DEBUG
                FFUpdater.DeleteTemp(ref InitData);
                FFUpdater.FetchUpdates(ref InitData);
                FFUpdater.UpdaterData.UpdateLevel = FFUpdater.CompareUpdates(APP_VERSION, FFUpdater.UpdaterData.Releases[0].ReleaseTag);
                if (FFUpdater.UpdaterData.UpdateLevel > 0 || args.Contains("-u"))
                {
                    FFUpdater.ShowUpdateMenu(ref InitData);
                }
                #endif
                
                //Proceed to the main part
                FFMain.Settings(ref InitData);
                FFMain.FindFiles(ref InitData);
                if (FFMain.CoreData.FilePaths.Count > 0)
                    FFMain.CopyFiles(ref InitData);
                FFMain.FinalizeResults(ref InitData);
                Console.WriteLine("All operations completed successfully. Check the log files if needed");
            }
            catch (QuitRequestedException excep)
            {
                //Write results to file
                Logger.LogToFile(0, "A method requested an application exit", Logger.UrgencyLevel.Info);
                if (excep.Message != null && excep.Message.Length > 0)
                    Logger.LogToFile(0, "Message: " + excep.Message, Logger.UrgencyLevel.Info);
            }
            catch (Exception excep) {
                //Write results to file
                Logger.LogToFile(0, "A method threw an exception", Logger.UrgencyLevel.Critical);
                Logger.LogToFile(0, $"Exception: {excep}", Logger.UrgencyLevel.Info);
                Console.WriteLine($"The app ran into an error. View logs in {LOGFILE_BASE_PATH} for more details.");
            }
            
            //Save log files
            foreach (int key in Logger.LogFiles.Keys)
            {
                Logger.SaveLog(key);
            }
            
            //The cursor was hidden, now show it again
            Console.CursorVisible = false;

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
                Logger.LogToFile(1, "Temp directory could not be deleted", Logger.UrgencyLevel.Critical);
                Logger.LogToFile(1, "Exception: " + caughtException.Message, Logger.UrgencyLevel.Info);
                Console.WriteLine($"Temp directory could not be deleted. Delete \"{FileFinder.TEMP_DIRECTORY_PATH + Path.DirectorySeparatorChar}FileFinderUpdater\" and try again");
                throw;
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

            StartUpdater();
        }
        /// <summary>
        /// This method starts the updater, which will be responsible for replacing the main executable
        /// </summary>
        public void StartUpdater()
        {
            Console.WriteLine("Starting updater: " + UpdaterData.UpdaterPath + Path.DirectorySeparatorChar + UpdaterData.UpdaterFileName);
            Console.ResetColor();
            
            //Now start a thread which will replace the main thread.
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = UpdaterData.UpdaterPath + Path.DirectorySeparatorChar + UpdaterData.UpdaterFileName;
            //This is scuffed, but it'll do
            if (CompareUpdates(FileFinder.APP_VERSION, "v2.1.0") >= 0)
                //New update method
                startInfo.Arguments = Directory.GetCurrentDirectory() + FileFinder.APP_NAME + FileFinder.APP_EXTENSION;
            else
                //For legacy reasons, will be stripped out sooner or later
                startInfo.Arguments = Directory.GetCurrentDirectory().Replace(" ", "─");
            startInfo.WorkingDirectory = UpdaterData.UpdaterPath;
            startInfo.CreateNoWindow = false;
            Logger.LogToFile(1, "Starting updater", Logger.UrgencyLevel.Info);
            Process.Start(startInfo);
            
            throw new QuitRequestedException("Updater started");
        }
        /// <summary>
        /// This method compares app versions with eachanother
        /// </summary>
        /// <param name="initData"></param>
        public int CompareUpdates(string currentVersion, string newVersion)
        {
            ReleaseData cutReleaseData = UpdaterData.Releases[UpdaterData.SelectedRelease];
            
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
                return 3;
            }
            //compare minor
            else if (firstSplit[0] == secondSplit[0] && firstSplit[1] < secondSplit[1])
            {
                return 2;
            }
            //compare patch
            else if (firstSplit[0] == secondSplit[0] && firstSplit[1] == secondSplit[1] && firstSplit[2] < secondSplit[2])
            {
                return 1;
            }
            //check if they are equal
            else if (firstSplit[0] == secondSplit[0] && firstSplit[1] == secondSplit[1] && firstSplit[2] == secondSplit[2])
            {
                return 0;
            }
            else
            {
                return -1;
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
            else if (action == 2)
                UpdaterData.SelectedRelease--;
            
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

            //Before we download the updater, check if it even exists
            if (UpdaterData.Releases[UpdaterData.SelectedRelease]?.ReleaseAssets?.Find(a => a.AssetDownloadURL.Contains(FileFinder.APP_EXTENSION))?.AssetDownloadURL == null)
                //No release was found. Throw an exception, call it a day, and let Init() handle the rest
                throw new FileNotFoundException($"No release was found. Was it deleted?");
            DownloadUpdate(ref InitData);
        }
        /// <summary>
        /// This method should only be run by itself. No other methods should be called besides this.
        /// </summary>
        /// <param name="initData"></param>
        public void UpdateApp(ref FFInitData InitData)
        {
            string AppPath = InitData.ConsoleArgs[0].Replace("─", " ");
            
            //Since my older builds don't pass the executable's name alongside the path
            if (!File.Exists(InitData.ConsoleArgs[0]))
                //The executable's name will be hard coded to ensure backwards-compatibility
                AppPath = InitData.ConsoleArgs[0].Replace("─", " ") + "FileFinder" + FileFinder.APP_EXTENSION;

            File.Delete(AppPath);
            Logger.LogToFile(1, "Deleted executable", Logger.UrgencyLevel.Info);
            File.Copy(UpdaterData.UpdaterPath + FileFinder.APP_NAME + FileFinder.APP_EXTENSION, AppPath);
            Logger.LogToFile(1, "Copied new executable", Logger.UrgencyLevel.Info);
            Console.WriteLine("Update to version {0} completed sucessfully", FileFinder.APP_VERSION);

            throw new QuitRequestedException("Update complete");
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
            /*SettingsMenu.Settings.Add(
                new SettingsEntry(
                    "Allow unsafe paths?", 
                    SettingsEntry.InteractionType.selectableAndInteractable, 
                    new List<string> { "No", "Yes" }, 
                    Preferences.AllowUnsafePaths ? 1 : 0,
                    "A path is considered unsafe when the app does not have write permissions to said path"
                    )
                );*/
            SettingsMenu.Settings.Add(
                new SettingsEntry(
                    "Should duplicates be overwritten?", 
                    SettingsEntry.InteractionType.selectableAndInteractable, 
                    new List<string> { "No, always keep the duplicate", "Only overwrite if source file is newer", "Only overwrite if source file is older", "Yes, always overwrite the duplicate" }, 
                    (int)Preferences.CopyOption
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
            Preferences.MaxRecursionCount = int.Parse(SettingsMenu.Settings[5].StrValueLabels[SettingsMenu.Settings[5].IntSelection]);
            //Preferences.AllowUnsafePaths = SettingsMenu.Settings[6].IntSelection == 1;
            Preferences.CopyOption = (FFPreferences.CopyMode)SettingsMenu.Settings[6].IntSelection;

            //Save preferences
            SavePreferences(ref InitData);
        }
        /// <summary>
        /// This method recursively enumerates all files starting at a directory. It has a recursion limit since there are sometimes symlinks causing an infinite loop.
        /// </summary>
        public void FindFiles(ref FFInitData InitData)
        {
            //Clear the console and notify the user
            Console.Clear();
            Console.WriteLine("Searching for files in {0}. This will take a while", CoreData.SourcePath);
            
            //Get a list of all files
            List<string> UnfilteredFilePaths = GetAllDirectories(CoreData.SourcePath, 0);
            
            //Before continuing, remove every path that the user doesn't have permissions to
            for (int i = 0; i < UnfilteredFilePaths.Count; i++)
            {
                Console.Clear();
                Console.WriteLine("Retrieving files [{0} out of {1} directories processed]", i, UnfilteredFilePaths.Count - 1);
                Console.WriteLine(BarGraph(i, UnfilteredFilePaths.Count - 1, Console.WindowWidth));
                
                try
                {
                    foreach (string file in Directory.GetFiles(UnfilteredFilePaths[i], "*", SearchOption.TopDirectoryOnly).Where(
                        a => a.EndsWith(".mp4")
                         || a.EndsWith(".mov")
                          || a.EndsWith(".png")
                           || a.EndsWith(".jpg")
                            || a.EndsWith(".jpeg")
                             || a.EndsWith(".img")
                              || a.EndsWith(".avi")
                               || a.EndsWith(".webm")))
                    {
                        CoreData.FilePaths.Add(file);
                    }
                }
                catch { }
            }
            
            Logger.LogToFile(2, $"{CoreData.FilePaths.Count} media file(s) were found", Logger.UrgencyLevel.Info);
            foreach (var item in CoreData.FilePaths)
            {
                Logger.LogToFile(2, $"Found file \"{item}\"", Logger.UrgencyLevel.Info);
            }

            List<string> GetAllDirectories(string path, int recursionLevel)
            {
                //Check if the path provided is in the blacklist
                //foreach (string blackListedPathWord in CoreData.PathBlacklist)
                    //if (path.Contains(blackListedPathWord))
                        //return new List<string> { };
                
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
                        output.AddRange(GetAllDirectories(subdir, recursionLevel + 1));
                    }
                );
                output.AddRange(newdirs);
                return output;
            }
        }
        /// <summary>
        /// This method uses the files found to sort them.
        /// </summary>
        public void CopyFiles(ref FFInitData InitData)
        {
            Console.Clear();
            
            List<string> Log = new List<string> { };
            
            for (int i = 0; i < CoreData.FilePaths.Count; i++)
            {
                Console.SetCursorPosition(0, 0);
                Console.WriteLine("Copying [{0} out of {1} files copied]", i, CoreData.FilePaths.Count - 1);
                Console.WriteLine(BarGraph(i, CoreData.FilePaths.Count - 1, Console.WindowWidth));

                //Generate new filenames
                string newFileName = GenerateFileName(CoreData.FilePaths[i], i, ref InitData);
                string newDirectoryPath = GenerateDirectoryName(CoreData.FilePaths[i], ref InitData);
                
                //To make the code easier to maintain
                string oldFilePath = CoreData.FilePaths[i];
                string newFilePath = newDirectoryPath + Path.DirectorySeparatorChar + newFileName;
                
                //Create the target directory if it doesn't exist
                if (!Directory.Exists(newDirectoryPath)) {
                    Directory.CreateDirectory(newDirectoryPath);
                    Log.Insert(0, "Created a new directory");
                }
                
                try
                {
                    //Time to copy the files
                    switch (Preferences.CopyOption)
                    {
                        case FFPreferences.CopyMode.AlwaysKeep:
                            //The file at the target destination does not exist, copy the source file
                            if (!File.Exists(newFilePath)) {
                                File.Copy(oldFilePath, newFilePath);
                                Log.Insert(0, $"$Copied {oldFilePath} to {newFilePath}");
                            }
                            break;
                        
                        case FFPreferences.CopyMode.OverwriteIfOlder:
                            if (File.Exists(newFilePath) && GetDate(oldFilePath, ref InitData) > GetDate(newFilePath, ref InitData)) {
                                //Delete the file if it exists
                                File.Delete(newFilePath);
                                //Copy the source file
                                File.Copy(oldFilePath, newFilePath);
                                Log.Insert(0, $"$Replaced {newFilePath}");
                            }
                            else {
                                //The file at the target destination does not exist, copy the source file
                                File.Copy(oldFilePath, newFilePath);
                                Log.Insert(0, $"$Copied {oldFilePath} to {newFilePath}");
                            }
                            break;
                        
                        case FFPreferences.CopyMode.OverwriteIfNewer:
                            if (File.Exists(newFilePath) && GetDate(oldFilePath, ref InitData) < GetDate(newFilePath, ref InitData)) {
                                //Delete the file if it exists
                                File.Delete(newFilePath);
                                //Copy the source file
                                File.Copy(oldFilePath, newFilePath);
                                Log.Insert(0, $"$Replaced {newFilePath}");
                            }
                            else {
                                //The file at the target destination does not exist, copy the source file
                                File.Copy(oldFilePath, newFilePath);
                                Log.Insert(0, $"$Copied {oldFilePath} to {newFilePath}");
                            }
                            break;
                        
                        case FFPreferences.CopyMode.AlwaysOverwrite:
                            //The same file exists at the target directory, delete it
                            if (File.Exists(newFilePath)) {
                                File.Delete(newFilePath);
                                //Copy the source file
                                File.Copy(oldFilePath, newFilePath);
                                Log.Insert(0, $"$Replaced {newFilePath}");
                            }
                            else {
                                //Copy the source file
                                File.Copy(oldFilePath, newFilePath);
                                Log.Insert(0, $"$Copied {oldFilePath} to {newFilePath}");
                            }
                            break;
                    }
                    Logger.LogToFile(2, $"$Copied {oldFilePath} to {newFilePath}", Logger.UrgencyLevel.Info);
                }
                catch (Exception excep)
                {
                    InitData.CaughtExceptions.Add(excep);
                    Logger.LogToFile(2, $"File {oldFilePath} caused an error", Logger.UrgencyLevel.Warn);
                    Log.Insert(0, $"&File {oldFilePath} caused an error");
                }
                
                //Just to show the user what the app is currently doing
                Console.SetCursorPosition(0, 4);
                Console.WriteLine("Log");
                while (Log.Count > Console.WindowHeight / 2)
                    Log.RemoveAt(Log.Count - 1);
                for (int l = 0; l < Log.Count; l++) {
                    Console.SetCursorPosition(0, 5 + l);
                    Console.ForegroundColor = Log[l].StartsWith("%") ? ConsoleColor.Yellow : ConsoleColor.Blue;
                    Console.WriteLine(new string(Log[l].Skip(1).Take(Console.WindowWidth).ToArray()));
                    Console.ResetColor();
                }
                //Write a really long string
                Console.WriteLine("                                                                                                                                                                                                     ");
            }

            /// <summary>
            /// This method uses the MetadataExtractor librairy to extract the date from the media files
            /// </summary>
            /// <param name="filePath">The file whose metadata should be extracted</param>
            /// <returns>A point in time that can be used</returns>
            DateTime GetDate(string filePath, ref FFInitData InitData)
            {
                string FileDate;
                try
                {
                    IEnumerable<MetadataExtractor.Directory> FileMetadata = MetadataExtractor.ImageMetadataReader.ReadMetadata(filePath);
                    ExifSubIfdDirectory ExifMetadata = FileMetadata?.OfType<ExifSubIfdDirectory>().FirstOrDefault();
                    XmpDirectory XmpMetadata = FileMetadata?.OfType<XmpDirectory>().FirstOrDefault();
                    
                    FileDate = ExifMetadata?.GetDescription(ExifDirectoryBase.TagDateTimeOriginal);
                    if (FileDate == null)
                    {
                        XmpMetadata?.GetXmpProperties().TryGetValue("MetadataDate", out FileDate);
                        if (FileDate == null)
                        {
                            //If all of the above fail, use the last write time as a reference
                            FileDate = File.GetLastWriteTimeUtc(filePath).ToString();
                            CoreData.UnsortedCount++;
                        }
                    }
                }
                catch (Exception caughtException)
                {
                    //If all of the above fail, use the last write time as a reference
                    FileDate = File.GetLastWriteTimeUtc(filePath).ToString();
                    Logger.LogToFile(2, $"{filePath} was sorted using the last write time", Logger.UrgencyLevel.Warn);
                    CoreData.UnsortedCount++;
                    InitData.CaughtExceptions.Add(caughtException);
                }
                DateTime Result;
                try {
                    //Try parsing the date
                    Result = DateTime.ParseExact(FileDate, "dd/MM/yyyy HH:mm:ss", null);
                }
                catch (FormatException) {
                    //That one failed, try again with a different format
                    Result = DateTime.ParseExact(FileDate, "yyyy:MM:dd HH:mm:ss", null);
                }
                catch (Exception excep) {
                    //This should never execute
                    Result = DateTime.Now;
                    Console.WriteLine($"Failed to parse date of {filePath}!");
                    Console.WriteLine(excep);
                    Console.WriteLine($"Press enter to continue");
                    InitData.CaughtExceptions.Add(excep);
                    CoreData.UnsortedCount++;
                    Console.ReadLine();
                }
                return Result;
            }
            
            /// <summary>
            /// This method uses the "GetDate" method to create a new filename
            /// </summary>
            /// <param name="filePath">The file to use</param>
            /// <param name="iteratorValue">The current iterator's value</param>
            /// <returns></returns>
            string GenerateFileName(string filePath, int iteratorValue, ref FFInitData InitData) {
                DateTime FileDate = GetDate(filePath, ref InitData);
                string output;
                output = Path.GetFileNameWithoutExtension(filePath);
                if (Preferences.FileNameType == 1)
                    output += "_" + FileDate.Day + "+" + FileDate.Month + "+" + FileDate.Year;
                else if (Preferences.FileNameType == 2)
                    output += "_" + FileDate.Day + "+" + FileDate.Month + "+" + FileDate.Year + "_" + iteratorValue;
                output += Path.GetExtension(filePath);
                return output;
            }
            /// <summary>
            /// This method uses the "GetDate" method to create a new directoryname
            /// </summary>
            /// <param name="filePath">The file to use</param>
            /// <param name="iteratorValue">The current iterator's value</param>
            /// <returns></returns>
            string GenerateDirectoryName(string filePath, ref FFInitData InitData)
            {
                DateTime FileDate = GetDate(filePath, ref InitData);
                string output = $"{CoreData.DestinationPath}";
                if (Preferences.SortingEnabled)
                    output += $"{Path.DirectorySeparatorChar}{FileDate.Year}_{FileDate.Month}_{FileDate.Day}";
                return output;
            }
        }
        public void FinalizeResults(ref FFInitData InitData)
        {
            try
            {
                Logger.LogToFile(2, $"Found {CoreData.FilePaths.Count} files, out of which {Math.Round((CoreData.UnsortedCount / CoreData.FilePaths.Count) * 10d) / 10d}% were sorted using the fallback method", Logger.UrgencyLevel.Info);
            } catch { }
            if (InitData.CaughtExceptions.Count == 0)
                return;
            if (InitData.CaughtExceptions.Count == 1)
                Logger.LogToFile(2, $"The following {InitData.CaughtExceptions.Count} Exception was caught: ", Logger.UrgencyLevel.Info);
            else
                Logger.LogToFile(2, $"The following {InitData.CaughtExceptions.Count} Exceptions were caught: ", Logger.UrgencyLevel.Info);
            foreach (var excep in InitData.CaughtExceptions)
            {
                //Should be good enough
                Logger.LogToFile(2, excep.ToString(), Logger.UrgencyLevel.Info);
            }
        }

        private string BarGraph(int value, int maxValue, int width)
        {
            string output = "";
            string format = System.Environment.OSVersion.Platform == PlatformID.Win32NT ? "█" : "▏▎▍▌▋▊▉█";

            float ratio = (float)value / (float)maxValue;
            for (float i = 0; i < width * ratio; i++)
            {
                output += format[(int)Math.Clamp(((ratio * format.Length) * width) - (i * format.Length), 0, format.Length - 1)];
            }
            return output;
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
        public CopyMode CopyOption { get; set; } = CopyMode.OverwriteIfOlder;
        [JsonPropertyName("MaxRecursionCount")]
        public int MaxRecursionCount { get; set; } = 5;
        [JsonPropertyName("UnsafePaths")]
        public bool AllowUnsafePaths { get; set; } = false;

        public enum CopyMode
        {
            AlwaysKeep = 0,
            OverwriteIfOlder = 1,
            OverwriteIfNewer = 2,
            AlwaysOverwrite = 3
        }
    }
    public class FFCoreData
    {
        public List<string> FilePaths = new List<string> { };
        public int UnsortedCount = 0;
        public string SourcePath = "";
        public string DestinationPath = "";
        public List<string> PathBlacklist = new List<string> { "dosdevices" };
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
        public string[] ConsoleArgs;
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