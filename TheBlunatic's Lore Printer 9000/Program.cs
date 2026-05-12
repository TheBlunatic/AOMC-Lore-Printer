using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading.Tasks;
using System.IO;
using TextCopy;
using System.Diagnostics;

namespace LoreApp
{
    public struct SplitPath
    {
        public string MasterDirectory { get; set; }
        public string SubDirectory { get; set; }
        public string FileName { get; set; }
        public string FileExtension { get; set; }
        public SplitPath(string md, string sd, string fn, string fe)
        {
            MasterDirectory = md;
            SubDirectory = sd;
            FileName = fn;
            FileExtension = fe;
        }

        public SplitPath SetMasterDirectory(string to)
        {
            return new SplitPath(to, SubDirectory, FileName, FileExtension);
        }
        public SplitPath SetSubDirectory(string to)
        {
            return new SplitPath(MasterDirectory, to, FileName, FileExtension);
        }
        public SplitPath SetFileName(string to)
        {
            return new SplitPath(MasterDirectory, SubDirectory, to, FileExtension);
        }
        public SplitPath SetFileExtension(string to)
        {
            return new SplitPath(MasterDirectory, SubDirectory, FileName, to);
        }
        public SplitPath RemoveMasterDirectory()
        {
            return new SplitPath(string.Empty, SubDirectory, FileName, FileExtension);
        }
        public SplitPath RemoveSubDirectory()
        {
            return new SplitPath(MasterDirectory, string.Empty, FileName, FileExtension);
        }
        public SplitPath RemoveFileName()
        {
            return new SplitPath(MasterDirectory, SubDirectory, string.Empty, FileExtension);
        }
        public SplitPath RemoveFileExtension()
        {
            return new SplitPath(MasterDirectory, SubDirectory, FileName, string.Empty);
        }

        public SplitPath ShiftFileDirectoriesToSubDirectory()
        {
            int index = FileName.LastIndexOf('\\');
            if (index == -1) return new SplitPath(MasterDirectory, SubDirectory, FileName, FileExtension);
            string newSubDirectory = $"{SubDirectory}{FileName.Substring(0, index + 1)}";
            string newFileName = FileName.Substring(index + 1);
            return new SplitPath(MasterDirectory, newSubDirectory, newFileName, FileExtension);
        }

        public static implicit operator string(SplitPath splitPath) => splitPath.ToString();
        public override string ToString()
        {
            return $"{MasterDirectory}{SubDirectory}{FileName}{FileExtension}";
        }
    }
    internal class Program
    {
        const string CONFIG_FOLDER = "config";
        const string CONFIG_PATH = CONFIG_FOLDER + "\\config.txt";

        const string LINE_PREFIX = "/customize lore add <i:false>";

        static string ACTIVE_BASE_DIRECTORY;

        static Dictionary<string, (Action, string)> COMMANDS = new Dictionary<string, (Action, string)>
        {
            { "quit", (null, "Exit the program") },
            { "help", (DoHelp, "View a list of available functions") },
            { "convert", (DoConvert, "Convert an image into minecraft commands") },
            { "batch", (DoBatch, "Runs convert on every file in the input directory") },
            { "copy", (DoCopy, "Sequentially copy commands to the clipboard") },
            { "clear", (null, "Clear the console") },
            { "reload", (DoConfigReload, "Reloads config files") },
            { "config", (DoDisplayConfig, "Displays loaded configuration") },
            { "whereami", (DoShowActiveBaseDirectory, "Displays the base directory this application is currently active in") },
        };

        static Config CONFIG;

        static SplitPath GetInputFolder()
        {
            return new SplitPath(ACTIVE_BASE_DIRECTORY, $"{CONFIG.InputFolder}\\", "", "");
        }
        static SplitPath GetOutputFolder()
        {
            return new SplitPath(ACTIVE_BASE_DIRECTORY, $"{CONFIG.OutputFolder}\\", "", "");
        }
        static SplitPath GetConfigFolder()
        {
            return new SplitPath(ACTIVE_BASE_DIRECTORY, $"{CONFIG_FOLDER}\\", "", "");
        }
        static SplitPath GetConfigFile()
        {
            return new SplitPath(ACTIVE_BASE_DIRECTORY, $"{CONFIG_FOLDER}\\", "config", ".txt");
        }
        static SplitPath ConstructSplitPathFromString(string path)
        {
            string md = ACTIVE_BASE_DIRECTORY;
            string rest = path.Substring(ACTIVE_BASE_DIRECTORY.Length);
            int lastIndexOf = rest.LastIndexOf('\\');
            string sd = string.Empty;
            if (lastIndexOf != -1)
            {
                sd = rest.Substring(0, lastIndexOf + 1);
                rest = rest.Substring(lastIndexOf + 1);
            }
            lastIndexOf = rest.LastIndexOf('.');
            return new SplitPath(md, sd, rest.Substring(0, lastIndexOf), rest.Substring(lastIndexOf));
        }

        static string GetInput()
        {
            ConsoleColor old = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("> ");
            string read = Console.ReadLine();
            Console.ForegroundColor = old;
            return read;
        }
        static string[] GetCommandsFor(Bitmap bmp)
        {
            string[] lines = new string[bmp.Height];
            for (int y = 0; y < bmp.Height; y++)
            {
                string line = LINE_PREFIX;

                string lastColor = null;

                for (int x = 0; x < bmp.Width; x++)
                {
                    Color color = bmp.GetPixel(x, y);

                    if (color.A > 0)
                    {
                        string colorCode = Blunatic.Parsing.Hex.GetString(color);

                        if (lastColor == colorCode)
                        {
                            line += '\u2588';
                            continue;
                        }
                        else
                        {
                            line += $"<#{colorCode}>\u2588";
                            lastColor = colorCode;
                        }
                    }
                    else
                    {
                        line += $"<b> </b> ";
                    }
                }
                lines[y] = line;
            }
            return lines;
        }
        static string GetChoice(params string[] choices)
        {
            while (true)
            {
                string input = GetInput().ToLower();
                if (choices.Contains(input)) return input;
                Console.WriteLine("Not a valid input!");
            }
        }
        static void WriteLine(string line, ConsoleColor color)
        {
            ConsoleColor old = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(line);
            Console.ForegroundColor = old;
        }
        static SplitPath ConvertFile(SplitPath inputPath)
        {
            SplitPath outputPath = inputPath.SetSubDirectory(GetOutputFolder().SubDirectory).SetFileExtension(".txt");
            string[] commands;
            using (Bitmap bmp = new Bitmap(inputPath))
            {
                commands = GetCommandsFor(bmp);
            }
            Directory.CreateDirectory(outputPath.ShiftFileDirectoriesToSubDirectory().RemoveFileName().RemoveFileExtension());
            File.WriteAllLines(outputPath, commands);
            return outputPath;
        }
        static (int, int) ConvertAllInDirectory(string directoryPath)
        {
            (int, int) count = (0, 0);
            string[] directories = Directory.GetDirectories(directoryPath);
            string[] files = Directory.GetFiles(directoryPath);
            foreach (string file in files)
            {
                count.Item2++;
                try
                {
                    ConvertFile(ConstructSplitPathFromString(file));
                    WriteLine($"Converted '{file}'", ConsoleColor.Gray);
                    count.Item1++;
                }
                catch (Exception e)
                {
                    WriteLine($"Failed to convert '{file}' ({e.GetType().FullName})", ConsoleColor.Red);
                }
            }
            foreach (string directory in directories)
            {
                (int, int) countHere = ConvertAllInDirectory(directory);
                count.Item1 += countHere.Item1;
                count.Item2 += countHere.Item2;
            }
            return count;
        }

        static bool Do(Action action)
        {
            try
            {
                action();
                return true;
            }
            catch (Exception e)
            {
                WriteLine($"ERROR", ConsoleColor.Yellow);
                WriteLine($"Something has gone wrong and it's probably TheBlunatic's fault!!! Send this to him:", ConsoleColor.DarkRed);
                WriteLine($"TYPE", ConsoleColor.Yellow);
                WriteLine(e.GetType().FullName, ConsoleColor.DarkRed);
                WriteLine($"MESSAGE", ConsoleColor.Yellow);
                WriteLine(e.Message, ConsoleColor.DarkRed);
                WriteLine($"STACK TRACE", ConsoleColor.Yellow);
                WriteLine(e.StackTrace, ConsoleColor.DarkRed);

                Console.WriteLine("This program will close on the next keypress.");
                
                Console.ReadKey(true);
                return false;
            }
        }

        static void DoShowActiveBaseDirectory()
        {
            Console.WriteLine($"Currently active in '{ACTIVE_BASE_DIRECTORY}'");
        }
        static void DoConfigReload()
        {
            ReloadConfig();
            Console.WriteLine("Config reloaded!");
        }
        static void DoDisplayConfig()
        {
            Console.WriteLine("CONFIGURATION:");
            foreach (IConfigParameter configParam in CONFIG.ParameterDictionary.Values)
            {
                ConsoleColor originColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.Write($"{configParam.ID}");
                Console.ForegroundColor = originColor;
                Console.Write(" = ");
                Console.ForegroundColor = configParam.GetMostSuitableValueColor();
                Console.WriteLine(configParam.ToString());
                Console.ForegroundColor = originColor;
            }
        }
        static void DoHelp()
        {
            Console.WriteLine($"COMMANDS:");
            foreach (KeyValuePair<string, (Action, string)> kvp in COMMANDS)
            {
                Console.WriteLine($"'{kvp.Key}' - {kvp.Value.Item2}");
            }
        }
        static void DoConvert()
        {
            Console.WriteLine("What image would you like to convert? (DON'T include the file extension)");

            string fileName = GetInput();

            SplitPath fileInput = GetInputFolder().SetFileName(fileName).SetFileExtension(".png");
            SplitPath outputPath;

            try
            {
                outputPath = ConvertFile(fileInput);
            }
            catch (FileNotFoundException)
            {
                WriteLine($"Cannot find a file named '{fileName}'.", ConsoleColor.DarkRed);
                return;
            }
            catch (ArgumentException)
            {
                WriteLine($"The given file name '{fileName}' is not valid.", ConsoleColor.DarkRed);
                return;
            }

            Console.WriteLine($"Commands have been added to '{outputPath}'!");
        }
        static void DoCopy()
        {
            Console.WriteLine("What is the name of the output file you would like to copy? (DON'T include the file extension)");

            SplitPath path = GetOutputFolder().SetFileName(GetInput()).SetFileExtension(".txt");

            string[] lines;
            try
            {
                lines = File.ReadAllLines(path);
            }
            catch (FileNotFoundException)
            {
                if (path.FileName.EndsWith(".txt"))
                {
                    WriteLine("File not found. This version of loreprinter requires that you DON'T put a file extension, this may be your issue.", ConsoleColor.DarkRed);
                }
                else
                {
                    WriteLine("File not found.", ConsoleColor.DarkRed);
                }
                return;
            }

            Console.WriteLine($"When you next press enter, copying will begin. Press enter to continue, enter 'back' to go back one line, and enter 'reset' to return to line 1.");

            int lineIndex = -1;

            while (true)
            {
                switch (GetChoice("", "back", "reset", "end"))
                {
                    case "reset":
                        lineIndex = -1;
                        goto case "";
                    case "back":
                        lineIndex = Math.Max(lineIndex - 2, -1);
                        goto case "";
                    case "":
                        if (lineIndex == lines.Length - 1)
                        {
                            Console.WriteLine("You have reached the end of the command list. Type 'end' to finish.");
                        }
                        else
                        {
                            lineIndex++;
                            Console.WriteLine($"Copied line {lineIndex+1} of {lines.Length}.");
                            ClipboardService.SetText(lines[lineIndex]);
                        }
                        break;
                    case "end":
                        return;
                }
            }
        }
        static void DoBatch()
        {
            (int, int) counts = ConvertAllInDirectory(GetInputFolder());
            Console.WriteLine($"Converted {counts.Item1}/{counts.Item2} encountered files.");
        }

        static void SaveConfig(Config config)
        {
            File.Delete(GetConfigFile());
            File.WriteAllLines(GetConfigFile(), config.GetFileContents());
        }
        static void InvalidateConfig()
        {
            void ensureDirectory(string directory)
            {
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    WriteLine($"WARNING: Missing directory was created: '{directory}'", ConsoleColor.Cyan);
                }
            }

            ensureDirectory(GetInputFolder());
            ensureDirectory(GetOutputFolder());
        }
        static void ReloadConfig()
        {
            Config returner;

            Directory.CreateDirectory(GetConfigFolder());

            try
            {
                string[] configFile = File.ReadAllLines(GetConfigFile());
                try
                {
                    returner = new Config(configFile);
                }
                catch
                {
                    File.WriteAllLines(GetConfigFile().SetFileName("old_config"), configFile);
                    returner = new Config();
                    SaveConfig(returner);
                    WriteLine($"WARNING: Configuration file was invalid. It has been copied to '{GetConfigFile().SetFileName("old_config")}' and a new default config file has been created: '{GetConfigFile()}'.", ConsoleColor.Cyan);
                }
            }
            catch
            {
                returner = new Config();
                SaveConfig(returner);
                WriteLine($"WARNING: Could not find configuration file. A new default config file has been created: '{GetConfigFile()}'.", ConsoleColor.Cyan);
            }

            CONFIG = returner;
            InvalidateConfig();
        }
        static void Main(string[] args)
        {
            ACTIVE_BASE_DIRECTORY = System.Reflection.Assembly.GetEntryAssembly().Location.Substring(0, System.Reflection.Assembly.GetEntryAssembly().Location.LastIndexOf('\\') + 1);
            
            ACTIVE_BASE_DIRECTORY = $"{Environment.CurrentDirectory}\\";

            Console.ForegroundColor = ConsoleColor.Gray;
            if (!Do(ReloadConfig)) return;

            if (args.Length == 0)
            {
                while (true)
                {
                    WriteLine($"What would you like to do? (type 'help' for a list of commands)", ConsoleColor.Green);

                    string choice = GetChoice(COMMANDS.Keys.ToArray());

                    if (choice == "clear")
                    {
                        Console.Clear();
                        continue;
                    }
                    else if (choice == "quit" || !Do(COMMANDS[choice].Item1))
                    {
                        return;
                    }

                    WriteLine("\nPress any key to continue.", ConsoleColor.Yellow);
                    Console.ReadKey(true);
                }
            }
        }
    }
}
