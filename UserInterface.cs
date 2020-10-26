using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace UserInterface
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

                //show a separator for the description
                Console.SetCursorPosition(0, Console.WindowHeight - 3);
                Console.Write(loopString("-", Console.WindowWidth) + "\n");                               

                //show description of selected element
                if (settingsElement.StrDescription != null && settingsElement == settings[selectedSetting])
                {
                    Console.Write("Description: " + settingsElement.StrDescription);
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
                            currentSetting.StrValueLabels[1] = new Prompts().UserInput();
                        }
                        else if (currentSetting.StrValueLabels[0] == "$Path" && isSettingInteractable)
                        {
                            FileExplorer fe = new FileExplorer();
                            while (!fe.FileExplorerWindow());
                            settings[selectedSetting].StrValueLabels[1] = fe.path;
                            if (fe.path.Length == 0)
                            {
                                settings[selectedSetting].StrValueLabels[1] = "";
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

        private string loopString(string input, int loopAmount)
        {
            string outputString = "";
            for (int i = 0; i < loopAmount; i++)
            {
                outputString += input;
            }
            return outputString;
        }

        public class FileExplorer
        {
            public string path;
            private int selection;
            private bool showHidden = false;
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
                Console.WriteLine("[Enter] go down a directory");
                Console.WriteLine("[Backspace] go up a directory");
                Console.WriteLine("[Arrow keys] move up and down");
                Console.WriteLine("[Insert] create a directory");
                Console.WriteLine("[Delete] delete a directory");
                //Console.WriteLine("[F12] toggle \"show hidden\"");
                Console.WriteLine("[ESC] quit and save path");
                Console.ResetColor();
                Console.WriteLine();
                Console.BackgroundColor = ConsoleColor.White;
                Console.ForegroundColor = ConsoleColor.Black;
                Console.Write(paths[selection]);
                Console.ResetColor();
                Console.WriteLine(" will be the path selected");
                Console.WriteLine();
                Console.WriteLine("Select path:");
                
                //remove hidden directories
                if (!showHidden)
                {
                    //paths = paths.Where(a => new DirectoryInfo(a).Attributes != FileAttributes.Hidden).ToList();
                }
                //add option to go up
                if (!paths.Contains("[Go up]"))
                {
                    paths.Add("[Go up]");
                }
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
                        //highlight the selected object
                        if (i == selection)
                        {
                            Console.ForegroundColor = ConsoleColor.Black;
                            Console.BackgroundColor = ConsoleColor.DarkGray;
                        }
                        string[] splitpath = paths[i].Split(Path.DirectorySeparatorChar);
                        //write every path up until the element before the last one
                        for (int j = 0; j < splitpath.Length - 1; j++)
                        {
                            Console.Write(splitpath[j] + Path.DirectorySeparatorChar);
                        }
                        //change the color
                        if (i == selection) 
                        { 
                            Console.BackgroundColor = ConsoleColor.White; 
                        }
                        Console.WriteLine(splitpath[splitpath.Length - 1]);
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
                                    string dirname = directory.Split(Path.DirectorySeparatorChar).TakeLast(1).ToArray()[0];
                                    Console.WriteLine(" " + dirname);
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
                            else if (paths[selection] == "[Go up]")
                            {
                                Console.WriteLine(" Go up a directory");
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

                    case ConsoleKey.Home:
                        selection = 0;
                        break;
                    
                    case ConsoleKey.End:
                        selection = paths.Count - 1;
                        break;
                    
                    case ConsoleKey.F12:
                        showHidden = !showHidden;
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
                        else if (paths[selection] == "[Go up]")
                        {
                            //clear paths and list the directories
                            try
                            {
                                paths = Directory.GetDirectories(Directory.GetParent(path).FullName).ToList();
                                path = Directory.GetParent(path).FullName;
                            }
                            catch
                            {
                                paths.Clear();
                                foreach (var drive in drives)
                                {
                                    paths.Add(drive.Name);
                                }
                            }
                        }
                        else
                        {
                            try
                            {
                                path = paths[selection];
                                paths = Directory.GetDirectories(paths[selection]).ToList();
                            }
                            catch (Exception)
                            {
                            }
                        }
                        selection = 0;
                        break;

                    case ConsoleKey.Backspace:
                        //clear paths and list the directories
                        try
                        {
                            paths = Directory.GetDirectories(Directory.GetParent(path).FullName).ToList();
                            path = Directory.GetParent(path).FullName;
                        }
                        catch
                        {
                            paths.Clear();
                            foreach (var drive in drives)
                            {
                                paths.Add(drive.Name);
                            }
                        }
                        break;

                    case ConsoleKey.Insert:
                        //if the user chose to create a folder
                        SettingsUI SUI = new SettingsUI();
                        Prompts PR = new Prompts();
                        string directoryName = PR.UserInput("Enter directory name", path + Path.DirectorySeparatorChar);
                        if (PR.SelectionPrompt("Confirm creation of " + directoryName, "", new string[] { "No", "Yes" }) == 1)
                        {
                            try
                            {
                                Directory.CreateDirectory(path + Path.DirectorySeparatorChar + directoryName);
                            }
                            catch (UnauthorizedAccessException)
                            {
                                Console.Clear();
                                Console.WriteLine("Creation: Missing permissions!");
                                System.Threading.Thread.Sleep(1000);
                            }
                            catch (Exception)
                            {
                                Console.Clear();
                                Console.WriteLine("Creation: Unknown error");
                                System.Threading.Thread.Sleep(1000);
                            }
                        }
                        //rebuild list
                        paths = Directory.GetDirectories(path).ToList();
                        break;

                    case ConsoleKey.Delete:
                        //before anything gets executed, check if the path is valid in the first place
                        if (paths[selection] == "[Go to top]" || paths[selection] == "[Go up]")
                        {
                            break;
                        }
                        //check passed, now execute this
                        Prompts PRdelete = new Prompts();
                        if (PRdelete.SelectionPrompt("Confirm deletion of " + paths[selection], Directory.GetFiles(paths[selection], "*", SearchOption.AllDirectories).Length > 0 ? "WARNING: This directory contains files!" : "", new string[] { "No", "Yes" }) == 1)
                        {
                            try
                            {
                                //delete files
                                foreach (var file in Directory.GetFiles(paths[selection], "*", SearchOption.AllDirectories))
                                {
                                    File.Delete(file);
                                }
                                //delete directories
                                foreach (var directory in Directory.GetDirectories(paths[selection], "*", SearchOption.AllDirectories))
                                {
                                    Directory.Delete(directory);
                                }
                                Directory.Delete(paths[selection]);
                                paths = Directory.GetDirectories(path).ToList();
                            }
                            catch (UnauthorizedAccessException)
                            {
                                Console.Clear();
                                Console.WriteLine("Deletion: Missing permissions!");
                                System.Threading.Thread.Sleep(1000);
                            }
                            catch (Exception)
                            {
                                Console.Clear();
                                Console.WriteLine("Deletion: Unknown error");
                                System.Threading.Thread.Sleep(1000);
                            }
                        }
                        break;

                    case ConsoleKey.Escape:
                        path = paths[selection];
                        //if the selected setting ends up being "[Go up]"
                        if (path == "[Go to top]" || path == "[Go up]")
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

    public class Prompts
    {
        public string UserInput(string Title = "Enter value", string InputFieldStyle = "Input> ")
        {
            Console.Clear();
            Console.WriteLine(Title);
            Console.WriteLine(loopString("-", Console.WindowWidth));
            Console.Write(InputFieldStyle);
            return Console.ReadLine();
        }
        
        public int SelectionPrompt(string Title, string Description, string[] Options, int DefaultSelection = 0)
        {
            int selection = DefaultSelection;
            bool isDone = false;
            while (!isDone)
            {
                Console.Clear();
                Console.SetCursorPosition(5, 5);
                Console.Write(Title);
                Console.SetCursorPosition(6, 6);
                Console.Write(Description);
                Console.SetCursorPosition(8, 8);
                for (int i = 0; i < Options.Length; i++)
                {
                    string option = Options[i];
                    Console.BackgroundColor = i == selection ? ConsoleColor.White : ConsoleColor.Black;
                    Console.ForegroundColor = i == selection ? ConsoleColor.Black : ConsoleColor.White;
                    Console.Write(option);
                    Console.ResetColor();
                    if (i < Options.Length - 1)
                    {
                        Console.Write(" - ");
                    }
                }
                switch (Console.ReadKey().Key)
                {
                    case ConsoleKey.LeftArrow:
                        selection--;
                        break;

                    case ConsoleKey.RightArrow:
                        selection++;
                        break;

                    case ConsoleKey.Enter:
                        isDone = true;
                        break;

                    default:
                        break;
                }
                //Because "selection %= Options.Lenght" works completely different than what I expect
                if (selection >= Options.Length)
                {
                    selection = 0;
                }
                else if (selection < 0)
                {
                    selection = Options.Length - 1;
                }
            }
            return selection;
        }

        private string loopString(string input, int loopAmount)
        {
            string outputString = "";
            for (int i = 0; i < loopAmount; i++)
            {
                outputString += input;
            }
            return outputString;
        }
    }
}