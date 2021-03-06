﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.IO;
using NDesk.Options;
using System.Globalization;

namespace Image_Comparer
{
    class Rect
    {
        public int x, y, w, h;
        public Rect(int x, int y, int w, int h)
        {
            this.x = x;
            this.y = y;
            this.w = w;
            this.h = h;
        }
    }
    class Options
    {
        public string ReferenceImage { get; set; }
        public Rect ReferenceRectangle { get; set; }
        public string ComparisonFolder { get; set; }
        public string OutputFolder { get; set; }
        public string OutputFilename { get; set; }
        public float MatchTreshold { get; set; }
        public bool Copy { get; set; }
    }
    class Program
    {
        static void Main(string[] args)
        {
            Options options = new Options();
            bool show_help = false;
            OptionSet p = new OptionSet()
            {
                {
                    "r|Reference-Image=", "The reference all the other images are compared to.",
                    v => options.ReferenceImage = v
                },
                {
                    "R|Rectangle-Area=", "The rectangular area to use when comparing the images.\n" +
                    "Format: X:Y:W:H\n" +
                    "Example usage: --Rectangle-Area 10:20:40:50",
                    v =>
                    {
                        string[] s = v.Split(':');
                        options.ReferenceRectangle = new Rect(int.Parse(s[0]), int.Parse(s[1]),
                            int.Parse(s[2]), int.Parse(s[3]));
                    }
                },
                {
                    "f|Comparison-Folder=", "The folder of images to compare to the reference.",
                    v => options.ComparisonFolder = v
                },
                {
                    "o|Output-Folder=", "The output folder for all images above the match% treshold.",
                    v => options.OutputFolder = v
                },
                {
                    "O|Output-Filename=", "The output filename.\n" +
                    "Variables: \n<o> - Original filename, minus extension\n" +
                    "<e> - Original extension\n" +
                    "Example usage: \"Joe - <o><e>\"",
                    v => options.OutputFilename = v
                },
                {
                    "m|Match-Treshold=", "A {FLOAT} value representing the percentage treshold before a picture is considered a match.",
                    v => options.MatchTreshold = (float.Parse(v, CultureInfo.InvariantCulture) / 100f)
                },
                {
                    "c|Copy", "A {BOOL} value. If set copies matches instead of moving.",
                    v => options.Copy = v != null
                },
                {
                    "h|help", "Show this message and exit.",
                    v => show_help = v != null
                }
            };

            try
            {
                p.Parse(args);
            }
            catch (OptionException e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("Try --help for more information.");
                Environment.Exit(0);
            }

            if (options.ReferenceImage == null) { MissingArgument("Reference-Image", p); };
            if (options.ComparisonFolder == null) { MissingArgument("Comparison-Folder", p); };
            Bitmap referenceBitmap = (Bitmap)Image.FromFile(options.ReferenceImage);
            string[] comparisonFilePaths = Directory.GetFiles(options.ComparisonFolder);
            for (int i = 0; i < comparisonFilePaths.Length; i++)
            {
                Console.WriteLine(new String('-', Console.WindowWidth - 1));
                Bitmap image;
                string path = comparisonFilePaths[i];
                try
                {
                    // https://stackoverflow.com/a/1105330
                    image = (Bitmap)Image.FromStream(new MemoryStream(File.ReadAllBytes(path)));
                    Console.WriteLine("File found: " +
                        Path.GetFileName(path));
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Ignoring: " +
                        Path.GetFileName(path));
                    continue;
                    // ( ͡° ͜ʖ ͡°)
                }

                int matches = 0, total = 0;

                Rect bounds;
                if (options.ReferenceRectangle != null)
                {
                    bounds = options.ReferenceRectangle;
                }
                else
                {
                    bounds = new Rect(0, 0,
                    image.Width,
                    image.Height);
                }
                for (int y = bounds.y; y < bounds.h; y++)
                {
                    for (int x = bounds.x; x < bounds.w; x++)
                    {
                        if (Color.Equals(referenceBitmap.GetPixel(x, y),
                            image.GetPixel(x, y)))
                        {
                            matches++;
                        }
                        total++;
                    }
                }
                float matchRatio = ((float)matches / (float)total);
                string fileName = Path.GetFileName(path);
                Console.WriteLine("Match %: " + matchRatio * 100f);
                if (options.OutputFolder != null && options.MatchTreshold != 0f && matchRatio >= options.MatchTreshold)
                {
                    Console.WriteLine("Match found! Image is being moved to output folder.");
                    string outputFilename;
                    if (options.OutputFilename == null)
                    {
                        outputFilename = fileName;
                    }
                    else
                    {
                        outputFilename = options.OutputFilename;
                        outputFilename = outputFilename.Replace("<o>",
                            Path.GetFileNameWithoutExtension(path));
                        outputFilename = outputFilename.Replace("<e>",
                            Path.GetExtension(path));
                    }
                    try
                    {
                        if (options.Copy)
                        {
                            File.Copy(path,
                                Path.Combine(options.OutputFolder, outputFilename));
                        }
                        else
                        {
                            File.Move(path,
                                Path.Combine(options.OutputFolder, outputFilename));
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Something went wrong when moving the file.");
                        Console.WriteLine(ex.Message);
                    }
                }
            }
#if DEBUG
            Console.ReadKey();
#endif
        }

        static void MissingArgument (string arg, OptionSet p)
        {
            Console.WriteLine($"Missing argument. You need to specify {arg}.");
            Console.WriteLine();
            ShowHelp(p);
            Environment.Exit(0);
        }
        static void ShowHelp (OptionSet p)
        {
            p.WriteOptionDescriptions(Console.Out);
        }
    }
}
