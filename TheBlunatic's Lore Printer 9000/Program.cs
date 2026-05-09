using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading.Tasks;
using System.IO;

namespace TheBlunatic_s_Lore_Printer_9000
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Directory.CreateDirectory("put images here");
            Directory.CreateDirectory("output will be sent here");
            Console.WriteLine("What image would you like to convert?");
            try
            {
                string fileName = Console.ReadLine();
                string path = $"put images here\\{fileName}";
                using (Bitmap bmp = new Bitmap(path))
                {
                    List<string> lines = new List<string>();
                    for (int y = 0; y < bmp.Height; y++)
                    {
                        string line = "/customize lore add <i:false>";
                        string lastColor = null;
                        for (int x = 0; x < bmp.Width; x++)
                        {
                            string colorCode = Blunatic.Parsing.Hex.GetString(bmp.GetPixel(x, y));
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
                        lines.Add(line);
                    }
                    File.WriteAllLines($"output will be sent here\\{fileName.Split('.').First()}.txt", lines.ToArray());
                }
                Console.WriteLine($"Commands have been added to {fileName.Split('.').First()}.txt!");
            }
            catch (FileNotFoundException e)
            {
                Console.WriteLine($"Hmmmm... I'm not seeing a \"{e.FileName}\" here. Are you sure it exists?");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Something has gone wrong and it's probably TheBl's fault!!! Send this to him:\nMESSAGE\n{e.Message}\nSTACKTRACE\n{e.StackTrace}");
            }
            Console.ReadLine();
        }
    }
}
