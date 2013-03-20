/*
 * Copyright (c) 2013 Nick Winters (sphereinabox)
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy of
 * this software and associated documentation files (the "Software"), to deal in
 * the Software without restriction, including without limitation the rights to 
 * use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies 
 * of the Software, and to permit persons to whom the Software is furnished to do
 * so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all 
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Text.RegularExpressions;

namespace ConvertPhotosynthToCubeTexture
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            var size = 1024; // -size 1234, defaults to 1024. Specifies the width and height of the result PNG file

            // To process a single panorama:
            var singlePanoramaCubeManifestTxt = string.Empty; // -file "folder\panorama\02134124-FEA6-FEF6-BBB5-BA323423DB4C\deepzoom\CubeManifest.txt"
            var singlePanoramaOutputFile = string.Empty; // -out "example.png"

            // For processing all panorama files in a directory:
            var panoramaDirectory = string.Empty; // -dir "panorama"
            var outputDirectory = string.Empty; // -out "outputfiles"

            var argumentError = false;

            // Parameter Parsing
            for (int currentArgIndex = 0;
                 currentArgIndex + 1 < args.Length && !argumentError;
                 currentArgIndex += 2)
            {
                var argName = args[currentArgIndex];
                var nextArg = args[currentArgIndex + 1];
                if (argName.StartsWith("-") || argName.StartsWith("/"))
                {
                    argName = argName.Substring(1).ToLowerInvariant().Trim();
                    if (argName == "file" || argName == "f")
                    {
                        singlePanoramaCubeManifestTxt = nextArg;
                        if (!File.Exists(singlePanoramaCubeManifestTxt))
                        {
                            argumentError = true;
                            Console.WriteLine("Error: -file argument '{0}' does not exist",
                                              singlePanoramaCubeManifestTxt);
                        }
                    }
                    else if (argName == "dir" || argName == "d")
                    {
                        panoramaDirectory = nextArg;
                        if (!Directory.Exists(panoramaDirectory))
                        {
                            argumentError = true;
                            Console.WriteLine("Error: -dir argument '{0}' does not exist", panoramaDirectory);
                        }
                    }
                    else if (argName == "out" || argName == "o")
                    {
                        outputDirectory = singlePanoramaOutputFile = nextArg;
                    }
                    else if (argName == "size" || argName == "s")
                    {
                        if (!int.TryParse(nextArg, out size))
                        {
                            argumentError = true;
                            Console.WriteLine("Error: -size argument '{0}' is not an integer", nextArg);
                        }
                        if (size <= 1)
                        {
                            argumentError = true;
                            Console.WriteLine("Error: -size argument '{0}' must be greater than 1", size);
                        }
                    }
                    else if (argName == "help" || argName == "-help" || argName == "h" || argName == "?")
                    {
                        argumentError = true;
                        // Will print help below. Don't need to also say that I don't recognize -help option.
                    }
                    else
                    {
                        argumentError = true;
                        Console.WriteLine("Unrecognized Argument '{0}'", argName);
                    }
                }
                else
                {
                    argumentError = true;
                    Console.WriteLine("Unrecognized Argument '{0}'", argName);
                }
            }

            // Extra parameter validation
            if (!argumentError &&
                !string.IsNullOrEmpty(panoramaDirectory) &&
                string.IsNullOrEmpty(outputDirectory))
            {
                argumentError = true;
                Console.WriteLine("Error: When -dir is specified, the output directory must be specified with the -out argument.");
            }
            else if (!argumentError &&
                     !string.IsNullOrEmpty(singlePanoramaCubeManifestTxt) &&
                     string.IsNullOrEmpty(singlePanoramaOutputFile))
            {
                argumentError = true;
                Console.WriteLine("Error: When -file is specified, the output .png file must be specified with the -out argument.");
            }
            
            if (!argumentError && 
                !string.IsNullOrEmpty(panoramaDirectory) && 
                !string.IsNullOrEmpty(outputDirectory))
            {
                CompositeAllPanoramasInDirectory(size/4, panoramaDirectory, outputDirectory);
            }
            else if (!argumentError &&
                     !string.IsNullOrEmpty(singlePanoramaCubeManifestTxt) &&
                     !string.IsNullOrEmpty(singlePanoramaOutputFile))
            {
                var cubeManifest = ParseCubeManifest(singlePanoramaCubeManifestTxt);
                CompositeCubemapIntoSingleImage(cubeManifest, size/4, singlePanoramaOutputFile);
            }
            else
            {
                Console.WriteLine(
                    @"Process a Photosynth for iOS panorama into one .png file for all 6 cube faces.

Usage: ConvertPhotosynthToCubeTexture.exe -file CubeManifest.txt -out result.png
or ConvertPhotosynthToCubeTexture.exe -dir panoramas -out outputFilesDirectory

Options:
-file   Process a single panorama. Specify the deepzoom/CubeManifest.txt for the 
        panorama and the images will be found relative to the specified file.
-dir    Process all panoramas in directories named with GUIDs underneath the 
        specified directory.
-out    Specify either the output .png file (when using -file) or output 
        directory (when using -dir).
-size   Dimension of one side of the resulting .png file. Defaults to 1024 for
        a 1024x1024 png file.
");
            }
        }

        private static void CompositeAllPanoramasInDirectory(int faceSize, string panoramaDirectory, string outputDirectory)
        {
            // Process all panoramas:
            var processedPanoramas = 0;
            foreach (string panoramaGuidDirectory in Directory.GetDirectories(panoramaDirectory))
            {
                var guidDirectoryName = Path.GetFileName(panoramaGuidDirectory);
                // Folder names must be GUIDs
                if (
                    !Regex.IsMatch(guidDirectoryName,
                                   "[0-9A-Fa-f]{8}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{12}"))
                {
                    continue;
                }

                Console.WriteLine("Processing Panorama in {0}", panoramaGuidDirectory);
                var deepzoomPath = Path.Combine(panoramaGuidDirectory, "deepzoom");
                var cubeManifestPath = Path.Combine(deepzoomPath, "CubeManifest.txt");
                var cubeManifest = ParseCubeManifest(cubeManifestPath);
                CompositeCubemapIntoSingleImage(cubeManifest, faceSize, Path.Combine(outputDirectory, guidDirectoryName + ".png"));
                processedPanoramas += 1;
            }
            Console.WriteLine("Processed {0} panoramas underneath directory", processedPanoramas);
        }

        private static void CompositeCubemapIntoSingleImage(CubeManifest cubeManifest, int faceSize, string synthOutputCubemapPath)
        {
            string deepzoomroot = Path.GetDirectoryName(cubeManifest.CubeManifestPath);
            
            // Combine images into single .png
            using (var bmp = new Bitmap(faceSize * 4, faceSize * 4))
            {
                using (var graphics = Graphics.FromImage(bmp))
                {
                    ProcessFace(graphics,
                                Path.Combine(deepzoomroot, "left_files"),
                                new Rectangle(0, faceSize, faceSize, faceSize),
                                cubeManifest.LargestFaceSize);
                    ProcessFace(graphics,
                            Path.Combine(deepzoomroot, "front_files"),
                            new Rectangle(faceSize, faceSize, faceSize, faceSize),
                            cubeManifest.LargestFaceSize);
                    ProcessFace(graphics,
                            Path.Combine(deepzoomroot, "right_files"),
                            new Rectangle(2 * faceSize, faceSize, faceSize, faceSize),
                            cubeManifest.LargestFaceSize);
                    ProcessFace(graphics,
                            Path.Combine(deepzoomroot, "back_files"),
                            new Rectangle(3 * faceSize, faceSize, faceSize, faceSize),
                            cubeManifest.LargestFaceSize);
                    ProcessFace(graphics,
                            Path.Combine(deepzoomroot, "top_files"),
                            new Rectangle(faceSize, 0, faceSize, faceSize),
                            cubeManifest.LargestFaceSize);
                    ProcessFace(graphics,
                            Path.Combine(deepzoomroot, "bottom_files"),
                            new Rectangle(faceSize, 2 * faceSize, faceSize, faceSize),
                            cubeManifest.LargestFaceSize);
                }
                bmp.Save(synthOutputCubemapPath, ImageFormat.Png);
            }
        }

        private static CubeManifest ParseCubeManifest(string cubeManifestPath)
        {
            var manifest = new CubeManifest();
            manifest.CubeManifestPath = cubeManifestPath;
            var cubeManifestLines = File.ReadAllLines(cubeManifestPath);
            // Expected format:
            // comment                  Sample File
            // Unknown                  19
            // Largest Size             1040
            // Angular Bounds           -180,180,-58.8321,40.5767
            // Image coords per face    0,front,0,0,0,934,1040,934,1040,0
            //                          1,right,0,0,0,934,1040,934,1040,0
            //                          2,back,0,0,0,1040,1040,1040,1040,0
            //                          3,left,0,0,0,1040,1040,1040,1040,0
            //                          4,top,0,0,0,1040,1040,1040,1040,0
            //                          5,bottom,0,832,0,1040,199,1040,199,832

            // Note that not all faces of the cube are always included.

            if (cubeManifestLines.Length < 4)
            {
                throw new ArgumentException(
                    string.Format(
                        "Expected CubeManifest file of at least 4 lines (one cube face), got {0} lines for file '{1}'",
                        cubeManifestLines.Length, cubeManifestPath));
            }
            var line0 = cubeManifestLines[0];
            var maxSizeLine = cubeManifestLines[1];
            if (!int.TryParse(maxSizeLine, out manifest.LargestFaceSize))
            {
                throw new ArgumentException(
                    string.Format(
                        "Unable to parse line as integer line in file '{0}'({1}), got '{2}'",
                        cubeManifestPath, 2, maxSizeLine));
            }
            var panoramaAngularBounds = cubeManifestPath[2];
            // We don't need the information stored for each face, don't bother parsing it.
            //for (int lineIndex = 3; lineIndex < cubeManifestLines.Length; ++lineIndex)
            //{
            //    var faceLine = cubeManifestLines[lineIndex];
            //    var faceLineParts = faceLine.Split(',');
            //    if (faceLineParts.Length != 10)
            //    {
            //        throw new ArgumentException(
            //            string.Format(
            //                "Invalid face line in file '{0}'({1}), expected 10 parts when split by comma, got {2}. Line Looks like '{3}'",
            //                cubeManifestPath, lineIndex + 1, faceLineParts.Length, faceLine));
            //    }
            //}

            return manifest;
        }

        class CubeManifest
        {
            public string CubeManifestPath;
            public int LargestFaceSize;
        }

        private static void ProcessFace(Graphics graphics, string faceDirectory, Rectangle faceRectangle, int levelSize)
        {
            if (!Directory.Exists(faceDirectory))
            {
                // No imges on this face of the cube.
                return;
            }

            // When images get larger they are split so the resulting image is 256px wide
            // but 1px on each side is a copy of the adjacent image so that texture filtering works.
            const int maxImageSize = 256-2;

            // Find the corresponding level for the image size
            // the level is the power of two. Level 8 is up to (including) 256x256 pixels of real image data (does not count the duplicate pixels for edges)
            // level 9 is up to 512x512 and so-on
            int faceLevel = -999;
            for (int tempFaceLevel = 1, tempFaceLevelSize = 2;
                tempFaceLevel < 20;
                tempFaceLevel += 1, tempFaceLevelSize *= 2)
            {
                if (tempFaceLevelSize / 2 < levelSize && levelSize <= tempFaceLevelSize)
                {
                    faceLevel = tempFaceLevel;
                    break;
                }
            }

            var numImagesPerRow = (int)Math.Ceiling((double)levelSize / (maxImageSize-2));

            var faceLevelDirectory = Path.Combine(faceDirectory, faceLevel.ToString(CultureInfo.InvariantCulture));
            if (!Directory.Exists(faceLevelDirectory))
            {
                return;
            }

            // Copy images to bitmap:
            for (int row = 0; row < numImagesPerRow; row++)
            {
                for (int col = 0; col < numImagesPerRow; col++)
                {
                    var imagePath = Path.Combine(
                        faceLevelDirectory,
                        string.Format("{0}_{1}.jpg", col, row));
                    if (File.Exists(imagePath))
                    {
                        using (var faceImage = new Bitmap(imagePath))
                        {
                            var destRectangle =
                                new Rectangle(
                                    faceRectangle.Left + faceRectangle.Width * (maxImageSize * col) / levelSize,
                                    faceRectangle.Top + faceRectangle.Height * (maxImageSize * row) / levelSize,
                                    faceRectangle.Width * (maxImageSize * (col+1)) / levelSize
                                        - faceRectangle.Width * (maxImageSize * col) / levelSize,
                                    faceRectangle.Height * (maxImageSize * (row + 1)) / levelSize
                                        - faceRectangle.Height * (maxImageSize * row) / levelSize);
                            // The last row/column might not be the same size. Fill the remainder:
                            if (col == numImagesPerRow - 1)
                            {
                                destRectangle.Width = faceRectangle.Width - destRectangle.Left + faceRectangle.Left;
                            }
                            if (row == numImagesPerRow - 1)
                            {
                                destRectangle.Height = faceRectangle.Height - destRectangle.Top + faceRectangle.Top;
                            }
                            graphics.DrawImage(
                                faceImage,
                                destRectangle,
                                // Crop off the outer 1px, it is a copy of the adjacent images so that scaled image has no visible seams.
                                1, 1, faceImage.Width - 2, faceImage.Height - 2,
                                GraphicsUnit.Pixel);
                        }
                    }
                }
            }
        }
    }
}
