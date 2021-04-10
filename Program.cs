using System;
using System.Text;
using System.IO;
using System.Drawing;
using System.Globalization;
using System.Diagnostics;
using System.Reflection;

namespace V2S
{
    class Program
    {
        private static float threshold;

        private static void NewMain()
        {
            Environment.CurrentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            Console.Write("Input file: ");
            string filename = Console.ReadLine().Replace("\"", "");
            Console.Write("FPS: ");
            int framesPerSecond = Convert.ToInt32(Console.ReadLine());
            float frameInterval = 1f / framesPerSecond;
            Console.Write("Threshold (float): ");
            threshold = float.Parse(Console.ReadLine().Replace(',', '.'), CultureInfo.InvariantCulture);

            Console.WriteLine("Step 1 / 6: Cleaning up...");
            if(!Directory.Exists("tmp"))
            {
                Directory.CreateDirectory("tmp");
            }
            if (Directory.Exists("tmp\\frames\\"))
            {
                Console.WriteLine("Deleting frames...");
                Directory.Delete("tmp\\frames\\", true);
            }
            Directory.CreateDirectory("tmp\\frames\\");
            if (File.Exists("tmp\\audio.mp3"))
            {
                Console.WriteLine("Deleting audio...");
                File.Delete("tmp\\audio.mp3");
            }
            if (File.Exists("output.mp4"))
            {
                File.Delete("output.mp4");
            }
            if(File.Exists("output.srt"))
            {
                File.Delete("output.srt");
            }

            Console.WriteLine("Step 2 / 6: Extracting frames...");
            Process ffmpegProc = new Process();
            ffmpegProc.StartInfo.FileName = "tools\\ffmpeg.exe";
            ffmpegProc.StartInfo.Arguments = "-i \"" + filename + "\" -vf \"scale=44:27\" tmp\\frames\\%0d.bmp";
            ffmpegProc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            ffmpegProc.Start();
            ffmpegProc.WaitForExit();

            Console.WriteLine("Step 3 / 6: Extracing audio...");
            ffmpegProc = new Process();
            ffmpegProc.StartInfo.FileName = "tools\\ffmpeg.exe";
            ffmpegProc.StartInfo.Arguments = "-i \"" + filename + "\" tmp\\audio.mp3";
            ffmpegProc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            ffmpegProc.Start();
            ffmpegProc.WaitForExit();

            Console.WriteLine("Step 4 / 6: Creating blank video...");
            ffmpegProc = new Process();
            ffmpegProc.StartInfo.FileName = "tools\\ffmpeg.exe";
            ffmpegProc.StartInfo.Arguments = "-loop 1 -i tools\\black.png -i tmp\\audio.mp3 -c:v libx264 -tune stillimage -shortest output.mp4";
            ffmpegProc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            ffmpegProc.Start();
            ffmpegProc.WaitForExit();

            Console.WriteLine("Step 5 / 5: Generating subtitles...");
            int frameIndex = 0;
            StreamWriter outputFileWriter = new StreamWriter("output.srt");
            Stopwatch reportProgress = new Stopwatch();
            reportProgress.Start();

            while(true)
            {
                if (!File.Exists("tmp\\frames\\" + (frameIndex+1).ToString() + ".bmp"))
                {
                    break;
                }

                TimeSpan currentFrameTime = TimeSpan.FromSeconds(frameIndex * frameInterval);
                TimeSpan nextFrameTime = TimeSpan.FromSeconds((frameIndex + 1) * frameInterval);

                if (reportProgress.Elapsed.TotalSeconds > 1f)
                {
                    Console.WriteLine($"--> Frame {frameIndex.ToString()}, Time: {currentFrameTime.Hours.ToString("00")}:" +
                                                                            $"{currentFrameTime.Minutes.ToString("00")}:" +
                                                                            $"{currentFrameTime.Seconds.ToString("00")}," +
                                                                            $"{currentFrameTime.Milliseconds.ToString("000")}");
                    reportProgress.Restart();
                }


                outputFileWriter.WriteLine(frameIndex.ToString());
                outputFileWriter.WriteLine($"{currentFrameTime.Hours.ToString("00")}:" +
                                            $"{currentFrameTime.Minutes.ToString("00")}:" +
                                            $"{currentFrameTime.Seconds.ToString("00")}," +
                                            $"{currentFrameTime.Milliseconds.ToString("000")} --> " +
                                            $"{nextFrameTime.Hours.ToString("00")}:" +
                                            $"{nextFrameTime.Minutes.ToString("00")}:" +
                                            $"{nextFrameTime.Seconds.ToString("00")}," +
                                            $"{nextFrameTime.Milliseconds.ToString("000")}");


                using (Bitmap bmp = new Bitmap("tmp\\frames\\" + (frameIndex + 1).ToString() + ".bmp"))
                {
                    outputFileWriter.WriteLine(ImageToString(bmp));
                }

                frameIndex++;
            }
            outputFileWriter.Close();
            Console.WriteLine("\nDone!");
            Console.ReadLine();
        }

        static void Main(string[] args)
        {
            Console.WriteLine("V2S\n" +
                 "Converts image sequence to braille subtitles (SRT)\n" +
                 "Input resolution: 44x27\n" +
                 "\n" +
                 "(C) Copyright 2021, samplefx" +
                 "\n" +
                 "--------------------------------" +
                 "\n");

            NewMain();        
        }

        private static string ImageToString(Bitmap input)
        {
            StringBuilder buildImage = new StringBuilder();

            for(int y = 0; y < input.Height; y += 3)
            {
                for(int x = 0; x < input.Width; x += 2)
                {
                    buildImage.Append(ToBraille(input.GetPixel(x, y).GetBrightness() > threshold, 
                                                input.GetPixel(x + 1, y).GetBrightness() > threshold,
                                                input.GetPixel(x, y + 1).GetBrightness() > threshold,
                                                input.GetPixel(x + 1, y + 1).GetBrightness() > threshold,
                                                input.GetPixel(x, y + 2).GetBrightness() > threshold,
                                                input.GetPixel(x + 1, y + 2).GetBrightness() > threshold));
                }

                buildImage.Append("\n");
            }

            return buildImage.ToString();
        }

        private static char ToBraille(bool topLeft, bool topRight, bool middleLeft, bool middleRight, bool bottomLeft, bool bottomRight)
        {
            if(topLeft)
            {
                if(topRight)
                {
                    if(middleLeft)
                    {
                        if(middleRight)
                        {
                            if(bottomLeft)
                            {
                                if(bottomRight)
                                {
                                    return '⠿';
                                }
                                else
                                {
                                    return '⠟';
                                }
                            }
                            else
                            {
                                if (bottomRight)
                                {
                                    return '⠻';
                                }
                                else
                                {
                                    return '⠛';
                                }
                            }
                        }
                        else
                        {
                            if (bottomLeft)
                            {
                                if (bottomRight)
                                {
                                    return '⠯';
                                }
                                else
                                {
                                    return '⠏';
                                }
                            }
                            else
                            {
                                if (bottomRight)
                                {
                                    return '⠫';
                                }
                                else
                                {
                                    return '⠋';
                                }
                            }
                        }
                    }
                    else
                    {
                        if (middleRight)
                        {
                            if (bottomLeft)
                            {
                                if (bottomRight)
                                {
                                    return '⠽';
                                }
                                else
                                {
                                    return '⠝';
                                }
                            }
                            else
                            {
                                if (bottomRight)
                                {
                                    return '⠹';
                                }
                                else
                                {
                                    return '⠙';
                                }
                            }
                        }
                        else
                        {
                            if (bottomLeft)
                            {
                                if (bottomRight)
                                {
                                    return '⠭';
                                }
                                else
                                {
                                    return '⠍';
                                }
                            }
                            else
                            {
                                if (bottomRight)
                                {
                                    return '⠩';
                                }
                                else
                                {
                                    return '⠉';
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (middleLeft)
                    {
                        if (middleRight)
                        {
                            if (bottomLeft)
                            {
                                if (bottomRight)
                                {
                                    return '⠷';
                                }
                                else
                                {
                                    return '⠗';
                                }
                            }
                            else
                            {
                                if (bottomRight)
                                {
                                    return '⠳';
                                }
                                else
                                {
                                    return '⠓';
                                }
                            }
                        }
                        else
                        {
                            if (bottomLeft)
                            {
                                if (bottomRight)
                                {
                                    return '⠧';
                                }
                                else
                                {
                                    return '⠇';
                                }
                            }
                            else
                            {
                                if (bottomRight)
                                {
                                    return '⠣';
                                }
                                else
                                {
                                    return '⠃';
                                }
                            }
                        }
                    }
                    else
                    {
                        if (middleRight)
                        {
                            if (bottomLeft)
                            {
                                if (bottomRight)
                                {
                                    return '⠵';
                                }
                                else
                                {
                                    return '⠕';
                                }
                            }
                            else
                            {
                                if (bottomRight)
                                {
                                    return '⠱';
                                }
                                else
                                {
                                    return '⠑';
                                }
                            }
                        }
                        else
                        {
                            if (bottomLeft)
                            {
                                if (bottomRight)
                                {
                                    return '⠥';
                                }
                                else
                                {
                                    return '⠅';
                                }
                            }
                            else
                            {
                                if (bottomRight)
                                {
                                    return '⠡';
                                }
                                else
                                {
                                    return '⠁';
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                if (topRight)
                {
                    if (middleLeft)
                    {
                        if (middleRight)
                        {
                            if (bottomLeft)
                            {
                                if (bottomRight)
                                {
                                    return '⠾';
                                }
                                else
                                {
                                    return '⠞';
                                }
                            }
                            else
                            {
                                if (bottomRight)
                                {
                                    return '⠺';
                                }
                                else
                                {
                                    return '⠚';
                                }
                            }
                        }
                        else
                        {
                            if (bottomLeft)
                            {
                                if (bottomRight)
                                {
                                    return '⠮';
                                }
                                else
                                {
                                    return '⠎';
                                }
                            }
                            else
                            {
                                if (bottomRight)
                                {
                                    return '⠪';
                                }
                                else
                                {
                                    return '⠊';
                                }
                            }
                        }
                    }
                    else
                    {
                        if (middleRight)
                        {
                            if (bottomLeft)
                            {
                                if (bottomRight)
                                {
                                    return '⠼';
                                }
                                else
                                {
                                    return '⠜';
                                }
                            }
                            else
                            {
                                if (bottomRight)
                                {
                                    return '⠸';
                                }
                                else
                                {
                                    return '⠘';
                                }
                            }
                        }
                        else
                        {
                            if (bottomLeft)
                            {
                                if (bottomRight)
                                {
                                    return '⠬';
                                }
                                else
                                {
                                    return '⠌';
                                }
                            }
                            else
                            {
                                if (bottomRight)
                                {
                                    return '⠨';
                                }
                                else
                                {
                                    return '⠈';
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (middleLeft)
                    {
                        if (middleRight)
                        {
                            if (bottomLeft)
                            {
                                if (bottomRight)
                                {
                                    return '⠶';
                                }
                                else
                                {
                                    return '⠖';
                                }
                            }
                            else
                            {
                                if (bottomRight)
                                {
                                    return '⠲';
                                }
                                else
                                {
                                    return '⠒';
                                }
                            }
                        }
                        else
                        {
                            if (bottomLeft)
                            {
                                if (bottomRight)
                                {
                                    return '⠦';
                                }
                                else
                                {
                                    return '⠆';
                                }
                            }
                            else
                            {
                                if (bottomRight)
                                {
                                    return '⠢';
                                }
                                else
                                {
                                    return '⠂';
                                }
                            }
                        }
                    }
                    else
                    {
                        if (middleRight)
                        {
                            if (bottomLeft)
                            {
                                if (bottomRight)
                                {
                                    return '⠴';
                                }
                                else
                                {
                                    return '⠔';
                                }
                            }
                            else
                            {
                                if (bottomRight)
                                {
                                    return '⠰';
                                }
                                else
                                {
                                    return '⠐';
                                }
                            }
                        }
                        else
                        {
                            if (bottomLeft)
                            {
                                if (bottomRight)
                                {
                                    return '⠤';
                                }
                                else
                                {
                                    return '⠄';
                                }
                            }
                            else
                            {
                                if (bottomRight)
                                {
                                    return '⠠';
                                }
                                else
                                {
                                    return '⠁';
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
