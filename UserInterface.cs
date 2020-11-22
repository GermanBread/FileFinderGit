using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace UserInterface
{
    /// <summary>
    /// This class defines how the <c>drawSettings()</c> method sould treat this setting
    /// </summary>
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
    
    /// <summary>
    /// This class contains both the menu itself
    /// </summary>
    public class SettingsUI
    {
        public List<SettingsEntry> settings = new List<SettingsEntry>();
        int selectedSetting;
        public SettingsUI()
        {
            selectedSetting = 0;
        }
        
        /// <summary>
        /// This method uses the list <c>settings</c> to draw menu items
        /// </summary>
        /// <param name="title">Text shown at top</param>
        /// <returns>A bool indicating if the user has pressed <c>DONE</c></returns>
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
                        //if the variable is not empty, display it
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
                        //if the variable is not empty, display it
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
                
                //Reset the color even if the setting was not highlighted
                Console.ResetColor();

                //Show a separator for the description
                Console.SetCursorPosition(0, Console.WindowHeight - 3);
                Console.Write(LoopString("-", Console.WindowWidth) + "\n");                               

                //show description of selected element
                if (settingsElement.StrDescription != null && settingsElement == settings[selectedSetting])
                {
                    Console.Write("Description: " + settingsElement.StrDescription);
                }
            }

            //Catch keystroke
            pressedKey = Console.ReadKey().Key;
            
            //Decide what to do with the keys
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
                            FileExplorer fe = new FileExplorer(currentSetting.StrValueLabels[1]);
                            while (!fe.FileExplorerWindow()) {}
                            currentSetting.StrValueLabels[1] = fe.selectedPath;
                            if (fe.selectedPath.Length == 0)
                            {
                                settings[selectedSetting].StrValueLabels[1] = "";
                            }
                        }
                        break;
                }
            }
            catch (Exception)
            {
            }
            //Clamp "selected setting" just in case :)
            selectedSetting = Math.Clamp(selectedSetting, 0, settings.Count - 1);
            
            //Return if the done button has been pressed
            return isDone;
        }

        private string LoopString(string input, int loopAmount)
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
            public string selectedPath;
            private int selection;
            private bool showHidden = false;
            //get a filtered list of all drives
            private List<DriveInfo> drives = new List<DriveInfo>();
            private List<string> paths = new List<string>();
            public FileExplorer(string startingPath = "")
            {              
                //get a list of drives that are NOT ramdisks
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

                //if a path has been provided, use it
                if (startingPath.Length > 0)
                {
                    try
                    {
                        selectedPath = Directory.GetParent(startingPath).FullName;
                        paths = Directory.GetDirectories(selectedPath).ToList();
                    }
                    catch (Exception)
                    {
                        //get a list of drives that are NOT ramdisks
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
                }
            }
            /// <summary>
            /// This method draws the main window. Must run in a while loop
            /// </summary>
            /// <returns>Returns true if the user pressed ESC</returns>
            public bool FileExplorerWindow()
            {
                //Variables
                bool isDone = false;

                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("[Arrow keys] move up and down");
                Console.WriteLine("[Enter] go down a directory");
                Console.WriteLine("[Backspace] go up a directory");
                //Console.WriteLine("[F12] toggle \"show hidden directories\"");
                Console.WriteLine("[Insert] create a directory");
                Console.WriteLine("[Delete] delete a directory");
                Console.WriteLine("[ESC] quit and save path");
                Console.ResetColor();
                Console.WriteLine();
                if (paths.Count > 0)
                {
                    Console.WriteLine("Select path:");
                }
                else
                {
                    Console.WriteLine("There seems to be nothing here");
                }
                try
                {
                    Console.BackgroundColor = ConsoleColor.White;
                    Console.ForegroundColor = ConsoleColor.Black;
                    Console.Write(paths[selection]);
                    Console.ResetColor();
                    Console.WriteLine(" will be selected");
                }
                catch (Exception)
                {
                    Console.ResetColor();
                    Console.WriteLine("No path will be selected");
                }
                Console.WriteLine();

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
                    //drawing the elements
                    try
                    {
                        //split the path into sections (used for coloring)
                        string[] splitpath = paths[i].Split(Path.DirectorySeparatorChar);
                        //highlight the selected object
                        if (i == selection)
                        {
                            Console.ForegroundColor = ConsoleColor.Black;
                            Console.BackgroundColor = ConsoleColor.DarkGray;
                        }
                        //write every path up until the element before the last one
                        for (int j = 0; j < splitpath.Length - 1; j++)
                        {
                            if (j > 0)
                            {
                                Console.Write(Path.DirectorySeparatorChar);
                            }
                            Console.Write(splitpath[j]);
                        }
                        //change the color
                        if (i == selection) 
                        { 
                            Console.BackgroundColor = ConsoleColor.White; 
                        }
                        Console.WriteLine(Path.DirectorySeparatorChar + splitpath[splitpath.Length - 1]);
                        Console.ResetColor();
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        //what this does is display a list of subdirectories under the selected directory
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
                        //however, if this fails, display this
                        catch (UnauthorizedAccessException)
                        {
                            Console.WriteLine(" Unable to get directories due to missing permissions");
                        }
                        catch (Exception)
                        {
                            Console.WriteLine(" Unable to get subdirectories");
                        }
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Unable to get directories");
                    }
                    Console.ResetColor();
                }
                Console.ResetColor();
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
                        try
                        {
                            selectedPath = paths[selection];
                            paths = Directory.GetDirectories(paths[selection]).ToList();
                        }
                        catch (Exception)
                        {
                        }
                        selection = 0;
                        break;

                    case ConsoleKey.Backspace:
                        //clear paths and list the directories
                        try
                        {
                            paths = Directory.GetDirectories(Directory.GetParent(selectedPath).FullName).ToList();
                            selectedPath = Directory.GetParent(selectedPath).FullName;
                            //try to select the element the path in the new list
                            selection = paths.FindIndex(element => element.Equals(selectedPath));
                        }
                        catch
                        {
                            paths.Clear();
                            foreach (var drive in drives)
                            {
                                paths.Add(drive.Name);
                            }
                            selection = 0;
                        }
                        break;

                    case ConsoleKey.Insert:
                        //If the user chose to create a folder
                        SettingsUI SUI = new SettingsUI();
                        Prompts PR = new Prompts();
                        string directoryName = PR.UserInput("Enter directory name", selectedPath + Path.DirectorySeparatorChar);
                        if (PR.SelectionPrompt("Confirm creation of " + directoryName, "", new string[] { "No", "Yes" }, false) == 1)
                        {
                            try
                            {
                                Directory.CreateDirectory(selectedPath + Path.DirectorySeparatorChar + directoryName);
                            }
                            catch (UnauthorizedAccessException)
                            {
                                Console.Clear();
                                Console.WriteLine("Creation: Missing permissions!");
                                System.Threading.Thread.Sleep(1000);
                            }
                            catch (Exception a)
                            {
                                Console.Clear();
                                Console.WriteLine("Creation: Unknown error");
                                Console.WriteLine(a.Message);
                                System.Threading.Thread.Sleep(1000);
                            }
                        }
                        //rebuild list
                        paths = Directory.GetDirectories(selectedPath).ToList();
                        break;

                    case ConsoleKey.Delete:
                        //check passed, now execute this
                        Prompts PRdelete = new Prompts();
                        if (PRdelete.SelectionPrompt("Confirm deletion of " + paths[selection], Directory.GetFiles(paths[selection], "*", SearchOption.AllDirectories).Length > 0 ? "WARNING: This directory contains files!" : "", new string[] { "No", "Yes" }, false) == 1)
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
                                paths = Directory.GetDirectories(selectedPath).ToList();
                            }
                            catch (UnauthorizedAccessException)
                            {
                                Console.Clear();
                                Console.WriteLine("Deletion: Missing permissions!");
                                System.Threading.Thread.Sleep(1000);
                            }
                            catch (Exception a)
                            {
                                Console.Clear();
                                Console.WriteLine("Deletion: Unknown error");
                                Console.WriteLine(a.Message);
                                System.Threading.Thread.Sleep(1000);
                            }
                        }
                        break;

                    case ConsoleKey.Escape:
                        try
                        {
                            selectedPath = paths[selection];
                        }
                        catch (System.Exception)
                        {
                            selectedPath = "";
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
        
        public int SelectionPrompt(string Title, string Description, string[] Options, bool VerticalList, int DefaultSelection = 0)
        {
            int selection = DefaultSelection;
            bool isDone = false;
            Console.CursorVisible = false;
            while (!isDone)
            {
                Console.Clear();
                Console.SetCursorPosition(5, 5);
                Console.Write(Title);
                Console.SetCursorPosition(6, 6);
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write(Description);
                Console.ResetColor();
                Console.SetCursorPosition(8, 8);
                for (int i = 0; i < Options.Length; i++)
                {
                    string option = Options[i];
                    Console.BackgroundColor = i == selection ? ConsoleColor.White : ConsoleColor.Black;
                    Console.ForegroundColor = i == selection ? ConsoleColor.Black : ConsoleColor.White;
                    Console.Write(option);
                    Console.ResetColor();
                    if (i < Options.Length - 1 && !VerticalList)
                    {
                        Console.Write(" - ");
                    }
                    else if (VerticalList)
                    {
                        Console.WriteLine();
                        Console.SetCursorPosition(8, Console.CursorTop);
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

                    case ConsoleKey.UpArrow:
                        selection--;
                        break;

                    case ConsoleKey.DownArrow:
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
            Console.Clear();
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

    /// <summary>
    /// This class contains methods that, when called, show a notification at a position set in a variable, inside the console's window. (Similiar to the BDD plugin "InAppNotifications")
    /// </summary>
    public static class NotificationsManager
    {
        /*public enum NotificationPosition
        {
            TopLeft = 0,
            TopRight = 1,
            BottomLeft = 2,
            BottomRight = 3
        }*/
        private static Dictionary<int, TaskData> notifTasks = new Dictionary<int, TaskData>();
        /// <summary>
        /// Summons a notification
        /// </summary>
        /// <param name="title">The title that appears at the top</param>
        /// <param name="message">The message to be displayed</param>
        /// <param name="duration">The duration in milliseconds. 0 makes the message stay</param>
        /// <param name="hideProgress">Hides the progress and displays a little animation instead</param>
        /// <param name="primaryColor">Primary progress bar color to be used</param>
        /// <param name="secondaryColor">Secondary progress bar color to be used</param>
        /// <returns>A unique message ID</returns>
        public static int ShowMessage(string title, string message, int duration, bool hideProgress, ConsoleColor primaryColor, ConsoleColor secondaryColor)
        {
            int maxWidth = 40;
            int maxHeight = 15;
            int calculatedWidth = Math.Clamp(message.Length + 2, title.Length + 4, maxWidth);
            int calculatedHeight = (int)Math.Clamp(Math.Floor(message.Length / (float)maxWidth) + 3, 3, maxHeight);
            int maxTextLength = (maxWidth - 2) * (calculatedHeight - 2);
            string trimmedMessage = message.Substring(0, Math.Clamp(maxTextLength, 0, message.Length));
            //Messagebox method
            int key = notifTasks.Count;
            var tokenSource = new CancellationTokenSource();
            CancellationToken ct = tokenSource.Token;
            Task task = Task.Run(() => MessageBox(Console.WindowWidth - calculatedWidth - 4, 2, calculatedWidth, calculatedHeight, title, trimmedMessage, duration, hideProgress, primaryColor, secondaryColor, key), ct);
            notifTasks.Add(key, new TaskData{task = task, cancellationToken = ct, CancellationTokenSource = tokenSource});
            return key;
        }
        /// <summary>
        /// Summons a notification that is as big as the console's window
        /// </summary>
        /// <param name="title">The title that appears at the top</param>
        /// <param name="message">The message to be displayed</param>
        /// <param name="duration">The duration in milliseconds. 0 makes the message stay</param>
        /// <param name="hideProgress">Hides the progress and displays a little animation instead</param>
        /// <param name="primaryColor">Primary progress bar color to be used</param>
        /// <param name="secondaryColor">Secondary progress bar color to be used</param>
        /// <returns>A unique message ID</returns>
        public static int ShowFullscreenMessage(string title, string message, int duration, bool hideProgress, ConsoleColor primaryColor, ConsoleColor secondaryColor)
        {
            int calculatedWidth = Console.WindowWidth;
            int calculatedHeight = Console.WindowHeight;
            int maxTextLength = (calculatedWidth - 2) * (calculatedHeight - 2);
            string trimmedMessage = message.Substring(0, Math.Clamp(maxTextLength, 0, message.Length));
            //Messagebox method
            int key = notifTasks.Count;
            var tokenSource = new CancellationTokenSource();
            CancellationToken ct = tokenSource.Token;
            Task task = Task.Run(() => MessageBox(0, 0, calculatedWidth, calculatedHeight, title, trimmedMessage, duration, hideProgress, primaryColor, secondaryColor, key), ct);
            notifTasks.Add(key, new TaskData{task = task, cancellationToken = ct, CancellationTokenSource = tokenSource});
            return key;
        }
        /// <summary>
        /// Waits for the message to disappear
        /// </summary>
        /// <param name="ID"></param>
        public static void AwaitMessage(int ID)
        {
            notifTasks[ID].task.Wait();
            notifTasks.Remove(ID);
        }
        /// <summary>
        /// This is the function that takes care of drawing the notification
        /// </summary>
        /// <param name="top"></param>
        /// <param name="left"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="title"></param>
        /// <param name="message"></param>
        /// <param name="duration"></param>
        /// <param name="inProgress"></param>
        /// <param name="minimal"></param>
        /// <param name="barColor"></param>
        /// <param name="altBarColor"></param>
        private static void MessageBox(int left, int top, int width, int height, string title, string message, int duration, bool hideProgress, ConsoleColor barColor, ConsoleColor altBarColor, int key)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            int loopCount = duration == 0 ? 999999999 : duration / 10;
            for (int repeat = 0; repeat < loopCount; repeat++)
            {   
                //If a cancellation is requested
                if (notifTasks[key].cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                
                //Restart thee stopwatch
                sw.Restart();
                //Change the window color to gray to add some animation
                ConsoleColor windowColor = repeat > 10 && repeat < loopCount - 10 ? ConsoleColor.White : ConsoleColor.DarkGray;
                //int verticalOffset = (height * notificationsCount + 1);
                int verticalOffset = notifTasks.Keys.ToList().IndexOf(key) * (height + 3);
                
                //UNIX and UNIX-alike systems use box drawing characters
                if (System.Environment.OSVersion.Platform.Equals(System.PlatformID.Unix))
                {
                    Console.ForegroundColor = windowColor;
                    Console.SetCursorPosition(left, top + verticalOffset);
                    //This draws the top part
                    Console.Write("┌" + LoopString("─", (int)Math.Floor(width / 2f - title.Length / 2f) - 2) + $"╴{title}╶" + LoopString("─", (int)Math.Ceiling(width / 2f - title.Length / 2f) - 2) + "┐");
                    //This draws the body
                    for (var i = 1; i < height - 1; i++)
                    {
                        Console.SetCursorPosition(left, top + i + verticalOffset);
                        Console.Write("│" + LoopString(" ", width - 2) + "│");
                    }
                    //This splits the string into an array by index so that it can be put in place more easily
                    string[] formattedMessage;
                    try
                    {
                        formattedMessage = SplitByLength(message, width - 2).ToArray();
                    }
                    catch
                    {
                        formattedMessage = new string[]{message};
                    }
                    for (int l = 0; l < formattedMessage.Length; l++)
                    {
                        Console.SetCursorPosition(left + 1, top + 1 + l + verticalOffset);
                        Console.Write(formattedMessage[l]);
                    }
                    //This is for the bottom part
                    for (int elapsed = 0; elapsed < 10; elapsed++)
                    {
                        //Clone the msec value from the stopwatch
                        int msec = (int)sw.ElapsedMilliseconds;
                        Console.SetCursorPosition(left, top + height - 1 + verticalOffset);
                        Console.Write("└╴");
                        //Show a progress bar
                        if (!hideProgress && duration > 0)
                        {
                            Console.ForegroundColor = barColor;
                            Console.BackgroundColor = ConsoleColor.DarkGray;
                            Console.Write(BarGraph(repeat * 10 + elapsed, duration, width - 4));
                            Console.ResetColor();
                            Console.ForegroundColor = windowColor;
                        }
                        //Don't show the progress
                        else if (hideProgress)
                        {
                            Console.ForegroundColor = repeat % 100 < 50 ? barColor : altBarColor;
                            Console.BackgroundColor = repeat % 100 >= 50 ? barColor : altBarColor;
                            Console.Write(BarGraph(repeat % 50, 50, width - 4));
                            Console.ResetColor();
                            Console.ForegroundColor = windowColor;
                        }
                        //Just blink
                        else
                        {
                            Console.BackgroundColor = repeat % 100 >= 50 ? barColor : altBarColor;
                            Console.Write(LoopString(" ", width - 4));
                            Console.ResetColor();
                            Console.ForegroundColor = windowColor;
                        }
                        Console.Write("╶┘");
                        //Line break at the end
                        Thread.Sleep(Math.Clamp(1 - (msec - (int)sw.ElapsedMilliseconds), 0, 1000));
                    }
                }
                //Since Windows doesn't support fancy characters
                else
                {
                    Console.ForegroundColor = windowColor;
                    Console.SetCursorPosition(left, top + verticalOffset);
                    //This draws the top part
                    Console.Write("#" + LoopString("-", (int)Math.Floor(width / 2f - title.Length / 2f) - 2) + $"-{title}-" + LoopString("-", (int)Math.Ceiling(width / 2f - title.Length / 2f) - 2) + "#");
                    //This draws the body
                    for (var i = 1; i < height - 1; i++)
                    {
                        Console.SetCursorPosition(left, top + i + verticalOffset);
                        Console.Write("|" + LoopString(" ", width - 2) + "|");
                    }
                    //This splits the string into an array by index so that it can be put in place more easily
                    string[] formattedMessage;
                    try
                    {
                        formattedMessage = SplitByLength(message, width - 2).ToArray();
                    }
                    catch
                    {
                        formattedMessage = new string[]{message};
                    }
                    for (int l = 0; l < formattedMessage.Length; l++)
                    {
                        Console.SetCursorPosition(left + 1, top + 1 + l + verticalOffset);
                        Console.Write(formattedMessage[l]);
                    }
                    //This is for the bottom part
                    for (int elapsed = 0; elapsed < 10; elapsed++)
                    {
                        //Clone the msec value from the stopwatch
                        int msec = (int)sw.ElapsedMilliseconds;
                        Console.SetCursorPosition(left, top + height - 1 + verticalOffset);
                        Console.Write("#-");
                        //Show a progress bar
                        if (!hideProgress && duration > 0)
                        {
                            Console.ForegroundColor = barColor;
                            Console.BackgroundColor = ConsoleColor.DarkGray;
                            Console.Write(BarGraph(repeat * 10 + elapsed, duration, width - 4));
                            Console.ResetColor();
                            Console.ForegroundColor = windowColor;
                        }
                        //Don't show the progress
                        else if (hideProgress)
                        {
                            Console.ForegroundColor = repeat % 100 < 50 ? barColor : altBarColor;
                            Console.BackgroundColor = repeat % 100 >= 50 ? barColor : altBarColor;
                            Console.Write(BarGraph(repeat % 50, 50, width - 4));
                            Console.ResetColor();
                            Console.ForegroundColor = windowColor;
                        }
                        //Just blink
                        else
                        {
                            Console.BackgroundColor = repeat % 100 >= 50 ? barColor : altBarColor;
                            Console.Write(LoopString(" ", width - 4));
                            Console.ResetColor();
                            Console.ForegroundColor = windowColor;
                        }
                        Console.Write("-#");
                        //Line break at the end
                        Console.WriteLine();
                    }
                }
                //Sleep one millisecond to keep the progress bar consistent
                Thread.Sleep(Math.Clamp(1 - (int)sw.ElapsedMilliseconds, 0, 1000));
            }
            sw.Stop();
            for (var i = 0; i < height; i++)
            {
                Console.SetCursorPosition(left, top + i);
                Console.WriteLine(LoopString(" ", width));
            }
            //Before returning, start a new Task that deletes the current on
            Task.Factory.StartNew(() => {
                Thread.Sleep(100);
                notifTasks.Remove(key);
            });
            //Now return
            return;
        }
        /// <summary>
        /// Sends a cancellation request to the notification task.
        /// </summary>
        /// <param name="key"></param>
        public static void CloseMessage(int ID)
        {
            notifTasks[ID].CancellationTokenSource.Cancel();
            notifTasks[ID].task.Wait();
            notifTasks[ID].CancellationTokenSource.Dispose();
            notifTasks[ID].task.Dispose();
            notifTasks.Remove(ID);
        }
        private static string BarGraph(int value, int maxValue, int width)
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
        private static string LoopString(string input, int loopAmount)
        {
            string outputString = "";
            for (int i = 0; i < loopAmount; i++)
            {
                outputString += input;
            }
            return outputString;
        }
        private static IEnumerable<string> SplitByLength(this string input, int maxLength)
        {
            for (int index = 0; index < input.Length; index += maxLength)
            {
                yield return input.Substring(index, Math.Min(maxLength, input.Length - index));
            }
        }
        private class TaskData
        {
            public Task task = null;
            public CancellationToken cancellationToken = new CancellationToken();
            public CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();
        }
    }
}