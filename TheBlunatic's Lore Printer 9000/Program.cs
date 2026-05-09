using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading.Tasks;
using System.IO;
using TextCopy;

namespace TheBlunatic_s_Lore_Printer_9000
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
            { "copy", (DoCopy, "Sequentially copy commands to the clipboard") },
        };

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

                    if (color.A == 255)
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
                string input = Console.ReadLine().ToLower();
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

        static bool Do(Action action)
        {
            try
            {
                action();
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Something has gone wrong and it's probably TheBl's fault!!! Send this to him:\nMESSAGE\n{e.Message}\nSTACKTRACE\n{e.StackTrace}");
                Console.WriteLine("This program will close on the next keypress.");
                Console.ReadLine();
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

            string fileName = Console.ReadLine();
            string fileOutput = $"{fileName.Split('.').First()}.txt";

            string path = $"{INPUT_FOLDER}\\{fileName}";

            string[] commands;

            using (Bitmap bmp = new Bitmap(path))
            {
                commands = GetCommandsFor(bmp);
            }

            File.WriteAllLines($"{OUTPUT_FOLDER}\\{fileOutput}", commands);
            Console.WriteLine($"Commands have been added to {fileOutput}!");
        }
        static void DoCopy()
        {
            Console.WriteLine("What is the name of the output file you would like to copy? (include .txt)");

            string path = $"{OUTPUT_FOLDER}\\{Console.ReadLine()}";

            string[] lines = File.ReadAllLines(path);

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

        static void Main(string[] args)
        {
            Directory.CreateDirectory(INPUT_FOLDER);
            Directory.CreateDirectory(OUTPUT_FOLDER);

                while (true)
                {
                WriteLine($"What would you like to do? (type 'help' for a list of commands)", ConsoleColor.Green);

                string choice = GetChoice(COMMANDS.Keys.ToArray());

                if (choice == "quit") return;

                Do(COMMANDS[choice].Item1);

                WriteLine("\nPress any key to continue.", ConsoleColor.Yellow);
                Console.ReadKey(true);
            }

            

            
        }
    }
}
