using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading.Tasks;
using System.IO;
using TextCopy;

namespace LoreApp
{
    internal class Program
    {
        const string INPUT_FOLDER = "put images here";
        const string OUTPUT_FOLDER = "output will be sent here";

        const string LINE_PREFIX = "/customize lore add <i:false>";

        static Dictionary<string, (Action, string)> COMMANDS = new Dictionary<string, (Action, string)>
        {
            { "quit", (null, "Exit the program") },
            { "help", (DoHelp, "View a list of available functions") },
            { "convert", (DoConvert, "Convert an image into minecraft commands") },
            { "batch", (DoBatch, "Runs convert on every file in the input directory") },
            { "copy", (DoCopy, "Sequentially copy commands to the clipboard") },
            { "clear", (null, "Clear the console") },
        };

        static string RemovePrefix(string path) => path.Substring(path.IndexOf('\\'));
        static string RemovePostfix(string path) => path.Substring(0, path.LastIndexOf('.'));
        static string GetDirectory(string path) => path.Substring(0, path.LastIndexOf('\\'));
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
        static void ConvertFile(string inputPath, string outputPath)
        {
            string[] commands;
            using (Bitmap bmp = new Bitmap(inputPath))
            {
                commands = GetCommandsFor(bmp);
            }
            Directory.CreateDirectory(GetDirectory(outputPath));
            File.WriteAllLines(outputPath, commands);
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
                    ConvertFile(file, $"{OUTPUT_FOLDER}{RemovePrefix(RemovePostfix(file))}.txt");
                    WriteLine($"Converted {file}", ConsoleColor.Gray);
                    count.Item1++;
                }
                catch (Exception e)
                {
                    WriteLine($"Failed to convert {file} ({e.GetType().FullName})", ConsoleColor.Red);
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
            Console.WriteLine("What image would you like to convert? (include the file extension)");

            string fileName = GetInput();

            string fileOutput = $"{OUTPUT_FOLDER}\\{RemovePostfix(fileName)}.txt";
            string fileInput = $"{INPUT_FOLDER}\\{fileName}";

            try
            {
                ConvertFile(fileInput, fileOutput);
            }
            catch (FileNotFoundException e)
            {
                WriteLine($"Cannot find a file named '{fileName}'.", ConsoleColor.DarkRed);
                return;
            }
            catch (ArgumentException e)
            {
                WriteLine($"The given file name '{fileName}' is not valid.", ConsoleColor.DarkRed);
                return;
            }

            Console.WriteLine($"Commands have been added to {fileOutput}!");
        }
        static void DoCopy()
        {
            Console.WriteLine("What is the name of the output file you would like to copy? (include .txt)");

            string path = $"{OUTPUT_FOLDER}\\{GetInput()}";

            string[] lines;
            try
            {
                lines = File.ReadAllLines(path);
            }
            catch (FileNotFoundException e)
            {
                if (path.EndsWith(".txt"))
                {
                    WriteLine("File not found.", ConsoleColor.DarkRed);
                }
                else
                {
                    WriteLine("File not found. Did you end with the correct file extension (.txt)?", ConsoleColor.DarkRed);
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
            (int, int) counts = ConvertAllInDirectory(INPUT_FOLDER);
            Console.WriteLine($"Converted {counts.Item1}/{counts.Item2} encountered files.");
        }

        static void Main(string[] args)
        {
            Directory.CreateDirectory(INPUT_FOLDER);
            Directory.CreateDirectory(OUTPUT_FOLDER);

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
