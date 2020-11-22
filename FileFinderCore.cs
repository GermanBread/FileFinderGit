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
        
        public static string LOGFILE_PATH = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar/* + "FileFinder_Logs" + Path.DirectorySeparatorChar*/;
        public static string TEMP_DIRECTORY_PATH = Path.TrimEndingDirectorySeparator(Path.GetTempPath());
        public static bool IS_IN_TEMP = Directory.GetCurrentDirectory().Contains(TEMP_DIRECTORY_PATH);
        public static bool IS_UNIX = System.Environment.OSVersion.Platform.Equals(System.PlatformID.Unix);
        public static string APP_NAME = "FileFinder";
        public static string APP_EXTENTION = IS_UNIX ? ".x86-64" : ".exe";
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
            //Create a log file
            Logger.CreateLog(0, LOGFILE_PATH + "Init.log");
            
            //Variables used
            FFInitData initData = new FFInitData{};
            FFUpdater FFUpdater = new FFUpdater{};
            FFCore FFMain = new FFCore{};

            //Check for updates
            FFUpdater.FetchUpdates(ref initData);
            
            //Save the log file
            Logger.SaveLog(0);
            //Now return
            return initData;
        }
    }

    #region Functional classes
    
    /// <summary>
    /// This class contains methods that:
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
        FFUpdaterData updaterData = new FFUpdaterData{};
        /// <summary>
        /// This methods uses the Github API to get a JSON containing all releases, deserializes it and stores the JSON in the shared variable.
        /// </summary>
        /// <param name="initData"></param>
        public void FetchUpdates(ref FFInitData initData)
        {
             
        }
        public void CompareUpdates(ref FFInitData initData)
        {
            
        }
        /// <summary>
        /// Show a menu if the currently running app is outdated and requires an update
        /// </summary>
        public void ShowUpdateMenu(ref FFInitData initData)
        {
            
        }
        public void UpdateApp(ref FFInitData initData)
        {

        }
    }

    /// <summary>
    /// This class contains the most critical methods, hence it's name. 
    /// <para>
    /// - A<para />
    /// - B<para />
    /// - C<para />
    /// - D<para />
    /// </para>
    /// </summary>
    public class FFCore
    {
        public FFCoreData coreData = new FFCoreData{};
        public void Settings(ref FFInitData initData)
        {
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
            while (!settingsMenu.DrawSettings("FileFinder settings manager " + FileFinder.APP_VERSION)) {}
            
            //variables that depend on the settings manager
            coreData.sourcePath = settingsMenu.settings[0].StrValueLabels[1];
            coreData.destinationPath = settingsMenu.settings[1].StrValueLabels[1];
            coreData.fileNameType = settingsMenu.settings[3].IntSelection;
            coreData.sortingEnabled = settingsMenu.settings[4].IntSelection == 1;
            coreData.overwriteType = settingsMenu.settings[5].IntSelection;
        }
        public void FindFiles(ref FFInitData initData)
        {
            
        }
        public void CopyFiles(ref FFInitData initData)
        {

        }
        public void FinalizeResults(ref FFInitData initData)
        {

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
    
    #region Data classes
    
    public class FFCoreData
    {
        public List<string> FilePaths = new List<string>();
        public string sourcePath = "";
        public string destinationPath = "";
        public int fileNameType = 0;
        public bool sortingEnabled = true;
        public int overwriteType = 0;
    }
    public class FFUpdaterData
    {
        
    }
    public class FFInitData
    {
        public List<Exception> CaughtExceptions = new List<Exception>();
    }
    
    #endregion
}