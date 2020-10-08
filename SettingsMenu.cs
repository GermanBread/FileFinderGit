using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace User_Interface
{
    public class SettingsEntry
    {
        public enum InteractionType
        {
            selectableAndInteractable = 0,
            nonInteractable = 1,
            nonSelectableAndNonInteractable = 2,
        }
        public string StrLabel { get; private set; }
        public List<string> StrValueLabels { get; private set; }
        public int IntSelection { get; set; }
        public InteractionType EnumInteractable { get; private set; }
        public string StrDescription { get; private set; }
        public SettingsEntry(string Label, InteractionType Interactable, List<string> ValueLabels, int DefaultSelection, string Description = null)
        {
            StrLabel = Label;
            StrValueLabels = ValueLabels;
            IntSelection = DefaultSelection;
            EnumInteractable = Interactable;
            StrDescription = Description;
        }
    }
    
    public class SettingsUI
    {
        public List<SettingsEntry> settings = new List<SettingsEntry>();
        int selectedSetting;
        public SettingsUI()
        {
            selectedSetting = 0;
        }
        
        public bool DrawSettings(string title)
        {
            ConsoleKey pressedKey = ConsoleKey.NoName;
            bool isDone = false;
            int i;

            //make sure "selectedSetting" int doesn't go out of bounds
            selectedSetting = Math.Clamp(selectedSetting, 0, settings.Count);

            //clear console
            Console.Clear();

            //draw title
            //jump to the position
            Console.SetCursorPosition(5, 5);
            Console.WriteLine(title + " [Arrow Keys, Enter]");

            //while (settings[selectedSetting].EnumInteractable == SettingsEntry.InteractionType.nonSelectableAndNonInteractable)
            //{
            //    selectedSetting++;
            //} TODO: fix
            
            for (i = 0; i < settings.Count; i++)
            {
                SettingsEntry settingsElement = settings[i];

                settingsElement.IntSelection = Math.Clamp(settingsElement.IntSelection, 0, settingsElement.StrValueLabels.Count - 1);
                
                //jump to the position
                Console.SetCursorPosition(5, 5 + i + 2);
                
                //invert colors if the setting is selected
                Console.ForegroundColor = selectedSetting == i ? ConsoleColor.Black : ConsoleColor.White;
                Console.BackgroundColor = selectedSetting == i ? ConsoleColor.White : ConsoleColor.Black;

                //write the settings properties
                Console.Write(settingsElement.StrLabel);

                //reset color for space and insert spacer
                Console.ResetColor();
                if (settingsElement.StrLabel.Length > 0 && settingsElement.StrValueLabels[0].Length > 0)
                {
                    Console.Write(" - ");
                }

                //invert colors if the setting is selected
                Console.ForegroundColor = selectedSetting == i ? ConsoleColor.Black : ConsoleColor.White;
                Console.BackgroundColor = selectedSetting == i ? ConsoleColor.White : ConsoleColor.Black;
                
                //write the selected option to console
                if (settingsElement.EnumInteractable == SettingsEntry.InteractionType.selectableAndInteractable)
                {
                    //statement for handling "buttons"
                    if (settingsElement.StrValueLabels[0] == "$Done")
                    {
                        Console.Write("<Done>");
                    }
                    else if (settingsElement.StrValueLabels[0] == "$Input")
                    {
                        if (settingsElement.StrValueLabels[1].Length > 0)
                        {
                            Console.Write("<" + settingsElement.StrValueLabels[1].ToString() + ">");
                        }
                        else
                        {
                            Console.Write("<Text field>");
                        }
                    }
                    else if (settingsElement.StrValueLabels[0] == "$Path")
                    {
                        if (settingsElement.StrValueLabels[1].Length > 0)
                        {
                            Console.Write("<" + settingsElement.StrValueLabels[1].ToString() + ">");
                        }
                        else
                        {
                            Console.Write("<Select path>");
                        }
                    }
                    else
                    {
                        Console.Write("<" + settingsElement.StrValueLabels[settingsElement.IntSelection].ToString() + ">");
                    }
                }
                else
                {
                    Console.Write(settingsElement.StrValueLabels[settingsElement.IntSelection].ToString());
                }
                
                //reset the color even if the setting was not highlighted
                Console.ResetColor();
                
                //show description of selected element
                if (settingsElement.StrDescription != null && settingsElement == settings[selectedSetting])
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine(" Description: " + settingsElement.StrDescription);
                    Console.ResetColor();
                }
            }

            //catch keystroke
            pressedKey = Console.ReadKey().Key;
            
            //decide what to do with the keys
            try 
            {
                SettingsEntry currentSetting = settings[selectedSetting];
                bool isSettingInteractable = currentSetting.EnumInteractable == SettingsEntry.InteractionType.selectableAndInteractable;
                
                switch (pressedKey)
                {
                    case ConsoleKey.UpArrow:
                        selectedSetting--;
                        while (settings[selectedSetting].EnumInteractable == SettingsEntry.InteractionType.nonSelectableAndNonInteractable)
                        {
                            selectedSetting--;
                        }
                        break;

                    case ConsoleKey.DownArrow:
                        selectedSetting++;
                        while (settings[selectedSetting].EnumInteractable == SettingsEntry.InteractionType.nonSelectableAndNonInteractable)
                        {
                            selectedSetting++;
                        }
                        break;

                    case ConsoleKey.LeftArrow:
                        if (isSettingInteractable)
                        {
                            currentSetting.IntSelection--;
                        }
                        break;

                    case ConsoleKey.RightArrow:
                        if (isSettingInteractable)
                        {
                            currentSetting.IntSelection++;
                        }
                        break;

                    case ConsoleKey.Enter:
                        if (currentSetting.StrValueLabels[0] == "$Done" && isSettingInteractable)
                        {
                            isDone = true;
                        }
                        else if (currentSetting.StrValueLabels[0] == "$Input" && isSettingInteractable)
                        {
                            currentSetting.StrValueLabels[1] = UserInput();
                        }
                        else if (currentSetting.StrValueLabels[0] == "$Path" && isSettingInteractable)
                        {
                            FileExplorer fe = new FileExplorer();
                            while (!fe.FileExplorerWindow());
                            settings[selectedSetting].StrValueLabels[1] = fe.path;
                            if (fe.path.Length == 0)
                            {
                                settings[selectedSetting].StrValueLabels[1] = "undefined";
                            }
                        }
                        break;
                }
            }
            catch (System.Exception)
            {
            }
            //clamp "selected setting" just in case :)
            selectedSetting = Math.Clamp(selectedSetting, 0, settings.Count - 1);
            
            //return if the done button has been pressed
            return isDone;
        }

        private string UserInput()
        {
            Console.Clear();
            Console.WriteLine("Enter value");
            Console.WriteLine("-----------");
            Console.Write("Input> ");
            return Console.ReadLine();
        }

        class FileExplorer
        {
            public string path;
            private int selection;
            //get a filtered list of all drives
            private List<DriveInfo> drives = new List<DriveInfo>();
            private List<string> paths = new List<string>();
            public FileExplorer()
            {
                //set the drives
                drives = DriveInfo.GetDrives().ToList().Where(a => 
                a.DriveType.Equals(DriveType.Fixed)
                 || a.DriveType.Equals(DriveType.Removable)
                  || a.DriveType.Equals(DriveType.Network)
                   || a.DriveType.Equals(DriveType.CDRom)).ToList();
                //add drives to "paths" for first setup
                foreach (var drive in drives)
                {
                    paths.Add(drive.Name);
                }
            }
            public bool FileExplorerWindow()
            {
                
                //variables
                bool isDone = false;

                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("[ESC] quit and save");
                Console.WriteLine("[Enter] down");
                Console.WriteLine("[Backspace] list drives");
                Console.WriteLine("[Arrow keys] move up and down");
                Console.ResetColor();
                Console.WriteLine();
                Console.WriteLine("Select path");
                Console.WriteLine();
                //remove drives

                //add option to go up
                if (!paths.Contains("[Go to top]"))
                {
                    paths.Add("[Go to top]");
                }
                
                //list all the drives
                for (int i = 0; i < paths.Count; i++)
                {
                    if (i < selection - 2)
                    {
                        continue;
                    }
                    else if (i > selection + 2)
                    {
                        continue;
                    }
                    
                    try
                    {
                        if (i == selection)
                        {
                            Console.ForegroundColor = ConsoleColor.Black;
                            Console.BackgroundColor = ConsoleColor.White;
                        }
                        Console.WriteLine(paths[i]);
                        Console.ResetColor();
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        try
                        {
                            if (i == selection)
                            {
                                string[] subDirectories = Directory.GetDirectories(paths[i]);
                                string[] tempSubDirectories = subDirectories.Take(4).ToArray();
                                //add "..." to show that there are more subdirectories
                                if (subDirectories.Length > 4)
                                {
                                    tempSubDirectories = tempSubDirectories.Append("...").ToArray();
                                }
                                //list subdirectories
                                foreach (var directory in tempSubDirectories)
                                {
                                    Console.WriteLine(" " + directory);
                                }
                            }
                        }
                        catch (UnauthorizedAccessException)
                        {
                            Console.WriteLine(" Unable to get directories due to missing permissions");
                        }
                        catch (PathTooLongException)
                        {
                            Console.WriteLine(" Unable to get directories due to a path that is too long");
                        }
                        catch (Exception)
                        {
                            if (paths[selection] == "[Go to top]")
                            {
                                Console.WriteLine(" Go to drive selection");
                            }
                            else
                            {
                                Console.WriteLine(" Unable to get directories");
                            }
                        }
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Unable to get drive name");
                    }
                    Console.ResetColor();
                }
                
                switch (Console.ReadKey().Key)
                {
                    case ConsoleKey.DownArrow:
                        selection++;
                        break;

                    case ConsoleKey.UpArrow:
                        selection--;
                        break;

                    case ConsoleKey.Enter:
                        if (paths[selection] == "[Go to top]")
                        {
                            //clear paths and list the disks
                            paths.Clear();
                            foreach (var drive in drives)
                            {
                                paths.Add(drive.Name);
                            }
                        }
                        else
                        {
                            try
                            {
                                paths = Directory.GetDirectories(paths[selection]).ToList();
                            }
                            catch (Exception)
                            {
                            }
                        }
                        selection = 0;
                        break;

                    case ConsoleKey.Backspace:
                        //clear paths and list the disks
                        paths.Clear();
                        foreach (var drive in drives)
                        {
                            paths.Add(drive.Name);
                        }
                        break;

                    case ConsoleKey.Escape:
                        path = paths[selection];
                        //if the selected setting ends up being "[Go up]"
                        if (path == "[Go to top]")
                        {
                            path = "";
                        }
                        isDone = true;
                        break;
                    
                }
                //clamp selection
                try
                {
                    selection = Math.Clamp(selection, 0, paths.Count - 1);
                }
                catch (System.Exception)
                {
                    selection = 0;
                }
                return isDone;
            }
        }
    }
}