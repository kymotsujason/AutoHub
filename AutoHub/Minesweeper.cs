using AForge;
using AForge.Imaging;
using AForge.Imaging.Filters;
using AForge.Math.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace AutoHub
{
    class Minesweeper
    {
        private static Macro macro = new Macro();
        private static List<int> visibleCoord = new List<int>();
        private static List<int> hiddenCoord = new List<int>();
        private static List<SweeperGrid> grid = new List<SweeperGrid>();
        private static System.Drawing.Point p1 = new System.Drawing.Point();
        private static List<bool> hiddenImage;
        private static List<bool> blank;
        private static List<bool> hiddenImage2;
        private static List<bool> bomb;
        private static List<bool> num1;
        private static List<bool> num2;
        private static List<bool> num3;
        private static List<bool> num4;
        private static List<bool> num5;
        private static List<bool> num6;
        private static bool win10 = false;
        private static bool clicked = false;
        private static bool bombDetected = false;
        private static bool finishedProcessing = false;
        private static bool guess = false;
        private static int rowCount = 0;
        private static int current = 0;

        public void Init7()
        {
            win10 = false;
            hiddenImage = GetHash(new Bitmap(Properties.Resources.hidden));
            blank = GetHash(new Bitmap(Properties.Resources.blank));
            hiddenImage2 = GetHash(new Bitmap(Properties.Resources.testblank));
            bomb = GetHash(new Bitmap(Properties.Resources.bomb));
            num1 = GetHash(new Bitmap(Properties.Resources._1));
            num2 = GetHash(new Bitmap(Properties.Resources._2));
            num3 = GetHash(new Bitmap(Properties.Resources._3));
            num4 = GetHash(new Bitmap(Properties.Resources._4));
            num5 = GetHash(new Bitmap(Properties.Resources._5));
            num6 = GetHash(new Bitmap(Properties.Resources._6));
        }

        public void Init10()
        {
            win10 = true;
            hiddenImage = GetHash(new Bitmap(Properties.Resources.hidden1));
            blank = GetHash(new Bitmap(Properties.Resources.blank1));
            bomb = GetHash(new Bitmap(Properties.Resources.bomb1));
            num1 = GetHash(new Bitmap(Properties.Resources._11));
            num2 = GetHash(new Bitmap(Properties.Resources._21));
            num3 = GetHash(new Bitmap(Properties.Resources._31));
            num4 = GetHash(new Bitmap(Properties.Resources._41));
            num5 = GetHash(new Bitmap(Properties.Resources._51));
            num6 = GetHash(new Bitmap(Properties.Resources._61));
        }

        public void Begin(IProgress<string> progress)
        {
            clicked = false;
            bombDetected = false;
            finishedProcessing = false;
            guess = false;
            current = 0;
            visibleCoord.Clear();
            hiddenCoord.Clear();
            grid.Clear();
            rowCount = 0;
            ProcessMove(GetHandle(), progress);
        }

        // Process image
        private void ProcessImage(Bitmap bitmap)
        {
            // lock image
            BitmapData bitmapData = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadWrite, bitmap.PixelFormat);

            // step 1 - turn background to black
            ColorFiltering colorFilter = new ColorFiltering
            {
                Blue = new IntRange(0, 64),
                FillOutsideRange = false
            };

            colorFilter.ApplyInPlace(bitmapData);

            // step 2 - locating objects
            BlobCounter blobCounter = new BlobCounter
            {
                FilterBlobs = true,
                MinHeight = 35,
                MinWidth = 35
            };
            blobCounter.ProcessImage(bitmapData);
            Blob[] blobs = blobCounter.GetObjectsInformation();
            bitmap.UnlockBits(bitmapData);

            // step 3 - check objects' type and highlight
            SimpleShapeChecker shapeChecker = new SimpleShapeChecker();
            //Pen colorPen = new Pen(Color.Yellow, 2);   // quadrilateral with known sub-type
            using (Graphics g = Graphics.FromImage(bitmap)) // SourceImage is a Bitmap object
            {
                for (int i = 0, n = blobs.Length; i < n; i++)
                {
                    List<IntPoint> edgePoints = blobCounter.GetBlobsEdgePoints(blobs[i]);

                    // is triangle or quadrilateral
                    if (shapeChecker.IsConvexPolygon(edgePoints, out List<IntPoint> corners))
                    {
                        // get sub-type
                        PolygonSubType subType = shapeChecker.CheckPolygonSubType(corners);

                        if (subType != PolygonSubType.Unknown)
                        {
                            if (corners.Count == 4)
                            {
                                // ignore the application window itself
                                if (corners[0].X <= 15)
                                {
                                    continue;
                                }
                                else
                                {
                                    //g.DrawPolygon(colorPen, ToPointsArray(corners));
                                    int right = corners[0].X, left = corners[0].X, top = corners[0].Y, bottom = corners[0].Y;
                                    for (int j = 0; j < corners.Count; j++)
                                    {
                                        if (corners[j].X > right)
                                        {
                                            right = corners[j].X;
                                        }
                                        if (corners[j].X < left)
                                        {
                                            left = corners[j].X;
                                        }
                                        if (corners[j].Y > bottom)
                                        {
                                            bottom = corners[j].Y;
                                        }
                                        if (corners[j].Y < top)
                                        {
                                            top = corners[j].Y;
                                        }
                                    }
                                    Rectangle section = new Rectangle(new System.Drawing.Point(left, top), new Size(right - left, bottom - top));
                                    IntPoint center = new IntPoint(((right - left) / 2) + left, ((top - bottom) / 2) + top);
                                    grid.Add(DetectStatus(CropImage(bitmap, section), center, section));
                                }
                            }
                        }
                    }
                }
            }
            //colorPen.Dispose();
            //grid[9].BMP.Save(@".\image2.png");

            // put new image to clipboard
            //bitmap.Save(@".\image.png");
        }

        private void UpdateImage(Bitmap bmp)
        {
            for (int i = hiddenCoord.Count - 1; i >= 0; i--)
            {
                SweeperGrid tmpSquare = DetectStatus(CropImage(bmp, grid[hiddenCoord[i]].Rect), grid[hiddenCoord[i]].Center, grid[hiddenCoord[i]].Rect);
                if (tmpSquare.Hidden != grid[hiddenCoord[i]].Hidden)
                {
                    finishedProcessing = false;
                    clicked = false;
                    grid[hiddenCoord[i]] = tmpSquare;
                    visibleCoord.Add(hiddenCoord[i]);
                    hiddenCoord.RemoveAt(i);
                }
            }
        }

        // Conver list of AForge.NET's points to array of .NET points
        private System.Drawing.Point[] ToPointsArray(List<IntPoint> points)
        {
            System.Drawing.Point[] array = new System.Drawing.Point[points.Count];

            for (int i = 0, n = points.Count; i < n; i++)
            {
                array[i] = new System.Drawing.Point(points[i].X, points[i].Y);
            }

            return array;
        }

        private static List<bool> GetHash(Bitmap bmpSource)
        {
            List<bool> lResult = new List<bool>();
            //create new image with 16x16 pixel
            Bitmap bmpMin = new Bitmap(bmpSource, new Size(16, 16));
            for (int j = 0; j < bmpMin.Height; j++)
            {
                for (int i = 0; i < bmpMin.Width; i++)
                {
                    //reduce colors to true / false                
                    lResult.Add(bmpMin.GetPixel(i, j).GetBrightness() < 0.5f);
                }
            }
            return lResult;
        }

        private SweeperGrid DetectStatus(Bitmap bmp, IntPoint center, Rectangle rect)
        {
            List<bool> imageInfo = GetHash(new Bitmap(bmp));

            //determine the number of equal pixel (x of 256)
            int pixelHidden = imageInfo.Zip(hiddenImage, (j, k) => j == k).Count(eq => eq);
            int pixelHidden2 = imageInfo.Zip(hiddenImage2, (j, k) => j == k).Count(eq => eq);
            int pixelBlank = imageInfo.Zip(blank, (j, k) => j == k).Count(eq => eq);
            int pixelBomb = imageInfo.Zip(bomb, (j, k) => j == k).Count(eq => eq);
            int pixelNum1 = imageInfo.Zip(num1, (j, k) => j == k).Count(eq => eq);
            int pixelNum2 = imageInfo.Zip(num2, (j, k) => j == k).Count(eq => eq);
            int pixelNum3 = imageInfo.Zip(num3, (j, k) => j == k).Count(eq => eq);
            int pixelNum4 = imageInfo.Zip(num4, (j, k) => j == k).Count(eq => eq);
            int pixelNum5 = imageInfo.Zip(num5, (j, k) => j == k).Count(eq => eq);
            int pixelNum6 = imageInfo.Zip(num6, (j, k) => j == k).Count(eq => eq);
            //int pixelNum8 = imageInfo.Zip(num8, (j, k) => j == k).Count(eq => eq);
            int pixelNum7 = 0, pixelNum8 = 0;

            if (pixelHidden > pixelBlank && pixelHidden > pixelNum1 && pixelHidden > pixelNum2 && pixelHidden > pixelNum3 && pixelHidden > pixelNum4 &&
                pixelHidden > pixelNum5 && pixelHidden > pixelNum6 && pixelHidden > pixelNum7 && pixelHidden > pixelNum8 && pixelHidden > pixelBomb)
            {
                return new SweeperGrid(bmp, rect, true, center, 10, false, false, false);
            }
            if (!win10 && pixelHidden2 > pixelBlank && pixelHidden2 > pixelNum1 && pixelHidden2 > pixelNum2 && pixelHidden2 > pixelNum3 && pixelHidden2 > pixelNum4 &&
                pixelHidden2 > pixelNum5 && pixelHidden2 > pixelNum6 && pixelHidden2 > pixelNum7 && pixelHidden2 > pixelNum8 && pixelHidden2 > pixelBomb)
            {
                return new SweeperGrid(bmp, rect, true, center, 10, false, false, false);
            }
            else if (pixelBlank > pixelHidden && pixelBlank > pixelNum1 && pixelBlank > pixelNum2 && pixelBlank > pixelNum3 && pixelBlank > pixelNum4 &&
                pixelBlank > pixelNum5 && pixelBlank > pixelNum6 && pixelBlank > pixelNum7 && pixelBlank > pixelNum8 && pixelBlank > pixelBomb)
            {
                return new SweeperGrid(bmp, rect, false, center, 0, false, false, true);
            }
            else if (pixelNum1 > pixelHidden && pixelNum1 > pixelBlank && pixelNum1 > pixelNum2 && pixelNum1 > pixelNum3 && pixelNum1 > pixelNum4 &&
                pixelNum1 > pixelNum5 && pixelNum1 > pixelNum6 && pixelNum1 > pixelNum7 && pixelNum1 > pixelNum8 && pixelNum1 > pixelBomb)
            {
                return new SweeperGrid(bmp, rect, false, center, 1, false, false, false);
            }
            else if (pixelNum2 > pixelHidden && pixelNum2 > pixelBlank && pixelNum2 > pixelNum1 && (pixelNum2 + 15) > pixelNum3 && pixelNum2 > pixelNum4 &&
                pixelNum2 > pixelNum5 && pixelNum2 > pixelNum6 && pixelNum2 > pixelNum7 && pixelNum2 > pixelNum8 && pixelNum2 > pixelBomb)
            {
                return new SweeperGrid(bmp, rect, false, center, 2, false, false, false);
            }
            else if (pixelNum3 > pixelHidden && pixelNum3 > pixelBlank && pixelNum3 > pixelNum1 && pixelNum3 > (pixelNum2 + 15) && pixelNum3 > pixelNum4 &&
                pixelNum3 > pixelNum5 && pixelNum3 > pixelNum6 && pixelNum3 > pixelNum7 && pixelNum3 > pixelNum8 && pixelNum3 > pixelBomb)
            {
                return new SweeperGrid(bmp, rect, false, center, 3, false, false, false);
            }
            else if (pixelNum4 > pixelHidden && pixelNum4 > pixelBlank && pixelNum4 > pixelNum1 && pixelNum4 > pixelNum2 && pixelNum4 > pixelNum3 &&
                pixelNum4 > pixelNum5 && pixelNum4 > pixelNum6 && pixelNum4 > pixelNum7 && pixelNum4 > pixelNum8 && pixelNum4 > pixelBomb)
            {
                return new SweeperGrid(bmp, rect, false, center, 4, false, false, false);
            }
            else if (pixelNum5 > pixelHidden && pixelNum5 > pixelBlank && pixelNum5 > pixelNum1 && pixelNum5 > pixelNum2 && pixelNum5 > pixelNum4 &&
                pixelNum5 > pixelNum3 && pixelNum5 > pixelNum6 && pixelNum5 > pixelNum7 && pixelNum5 > pixelNum8 && pixelNum5 > pixelBomb)
            {
                return new SweeperGrid(bmp, rect, false, center, 5, false, false, false);
            }
            else if (pixelNum6 > pixelHidden && pixelNum6 > pixelBlank && pixelNum6 > pixelNum1 && pixelNum6 > pixelNum2 && pixelNum6 > pixelNum4 &&
                pixelNum6 > pixelNum5 && pixelNum6 > pixelNum3 && pixelNum6 > pixelNum7 && pixelNum6 > pixelNum8 && pixelNum6 > pixelBomb)
            {
                return new SweeperGrid(bmp, rect, false, center, 6, false, false, false);
            }
            /*else if (pixelNum7 > pixelHidden && pixelNum7 > pixelBlank && pixelNum7 > pixelNum1 && pixelNum7 > pixelNum2 && pixelNum7 > pixelNum4 &&
                pixelNum7 > pixelNum5 && pixelNum7 > pixelNum6 && pixelNum7 > pixelNum3 && pixelNum7 > pixelNum8 && pixelNum7 > pixelBomb)
            {
                SweeperGrid sweeperInfo = new SweeperGrid(bmp, rect, false, center, 7, false, false, false);
                return sweeperInfo;
            }
            else if (pixelNum8 > pixelHidden && pixelNum8 > pixelBlank && pixelNum8 > pixelNum1 && pixelNum8 > pixelNum2 && pixelNum8 > pixelNum4 &&
                pixelNum8 > pixelNum5 && pixelNum8 > pixelNum6 && pixelNum8 > pixelNum7 && pixelNum8 > pixelNum3 && pixelNum8 > pixelBomb)
            {
                SweeperGrid sweeperInfo = new SweeperGrid(bmp, rect, false, center, 8, false, false, false);
                return sweeperInfo;
            }*/
            else if (pixelBomb > pixelHidden && pixelBomb > pixelBlank && pixelBomb > pixelNum1 && pixelBomb > pixelNum2 && pixelBomb > pixelNum4 &&
                pixelBomb > pixelNum5 && pixelBomb > pixelNum6 && pixelBomb > pixelNum7 && pixelBomb > pixelNum3 && pixelBomb > pixelNum8)
            {
                bombDetected = true;
                return new SweeperGrid(bmp, rect, false, center, 9, true, true, true);
            }
            else
            {
                return new SweeperGrid(bmp, rect, true, center, 10, false, false, false);
            }
        }

        private Bitmap CropImage(Bitmap source, Rectangle section)
        {
            // An empty bitmap which will hold the cropped image
            Bitmap bmp = new Bitmap(section.Width, section.Height);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                // Draw the given area (section) of the source image
                // at location 0,0 on the empty bitmap (bmp)
                g.DrawImage(source, 0, 0, section, GraphicsUnit.Pixel);
            }
            return RemoveColor(bmp);
        }

        // remove color for better detection (probably don't need but beneficial to try out)
        private Bitmap RemoveColor(Bitmap source)
        {
            using (Graphics gr = Graphics.FromImage(source)) // SourceImage is a Bitmap object
            {
                var gray_matrix = new float[][] {
                new float[] { 0.299f, 0.299f, 0.299f, 0, 0 },
                new float[] { 0.587f, 0.587f, 0.587f, 0, 0 },
                new float[] { 0.114f, 0.114f, 0.114f, 0, 0 },
                new float[] { 0,      0,      0,      1, 0 },
                new float[] { 0,      0,      0,      0, 1 }
            };

                var ia = new System.Drawing.Imaging.ImageAttributes();
                ia.SetColorMatrix(new System.Drawing.Imaging.ColorMatrix(gray_matrix));
                ia.SetThreshold((float)0.70); // Change this threshold as needed
                var rc = new Rectangle(0, 0, source.Width, source.Height);
                gr.DrawImage(source, rc, 0, 0, source.Width, source.Height, GraphicsUnit.Pixel, ia);
            }
            return source;
        }

        // use quicksort to ensure grid in the proper order
        private void OrganizeGrid()
        {
            QuickSortY(0, grid.Count - 1);
            int coordY = grid[0].Center.Y;
            int left = 0;
            for (int i = 0; i < grid.Count; i++)
            {
                if (Math.Abs(grid[i].Center.Y - coordY) > 10)
                {
                    if (rowCount == 0)
                    {
                        rowCount = i;
                    }
                    //Console.WriteLine("Sorting at: " + i);
                    QuickSortX(left, i - 1);
                    coordY = grid[i].Center.Y;
                    left = i;
                }
                // the algorithm seems to miss the last row, this forces it to sort the last row
                if (i == (grid.Count - 1))
                {
                    QuickSortX(left, i);
                }
            }
        }

        private static void QuickSortY(int left, int right)
        {
            if (left < right)
            {
                int pivot = PartitionY(left, right);

                if (pivot > 1)
                {
                    QuickSortY(left, pivot - 1);
                }
                if (pivot + 1 < right)
                {
                    QuickSortY(pivot + 1, right);
                }
            }
        }

        private static int PartitionY(int left, int right)
        {
            int pivot = grid[left].Center.Y;
            while (true)
            {

                while (grid[left].Center.Y < pivot)
                {
                    left++;
                }

                while (grid[right].Center.Y > pivot)
                {
                    right--;
                }

                if (left < right)
                {
                    if (grid[left].Center.Y == grid[right].Center.Y) return right;

                    SweeperGrid temp = grid[left];
                    grid[left] = grid[right];
                    grid[right] = temp;
                }
                else
                {
                    return right;
                }
            }
        }

        private static void QuickSortX(int left, int right)
        {
            if (left < right)
            {
                int pivot = PartitionX(left, right);

                if (pivot > 1)
                {
                    QuickSortX(left, pivot - 1);
                }
                if (pivot + 1 < right)
                {
                    QuickSortX(pivot + 1, right);
                }
            }

        }

        private static int PartitionX(int left, int right)
        {
            int pivot = grid[left].Center.X;
            while (true)
            {

                while (grid[left].Center.X < pivot)
                {
                    left++;
                }

                while (grid[right].Center.X > pivot)
                {
                    right--;
                }

                if (left < right)
                {
                    if (grid[left].Center.X == grid[right].Center.X) return right;

                    SweeperGrid temp = grid[left];
                    grid[left] = grid[right];
                    grid[right] = temp;
                }
                else
                {
                    return right;
                }
            }
        }

        private static void InsertionSort()
        {
            for (int i = 0; i < visibleCoord.Count - 1; i++)
            {
                for (int j = i + 1; j > 0; j--)
                {
                    if (grid[visibleCoord[j - 1]].Num < grid[visibleCoord[j]].Num)
                    {
                        int temp = visibleCoord[j - 1];
                        visibleCoord[j - 1] = visibleCoord[j];
                        visibleCoord[j] = temp;
                    }
                }
            }
        }

        private void PrintGrid()
        {
            int coordY = grid[0].Center.Y;
            for (int i = 0; i < grid.Count; i++)
            {
                if ((grid[i].Center.Y - coordY) > 10)
                {
                    Console.WriteLine("");
                    coordY = grid[i].Center.Y;
                }
                if (grid[i].Num < 9)
                {
                    if (grid[i].Processed)
                    {
                        Console.Write("{" + grid[i].Num + "}");
                    }
                    else
                    {
                        Console.Write("[" + grid[i].Num + "]");
                    }
                }
                else if (grid[i].Num == 9)
                {
                    if (grid[i].Processed)
                    {
                        Console.Write("{" + "X" + "}");
                    }
                    else
                    {
                        Console.Write("[" + "X" + "]");
                    }
                }
                else
                {
                    if (grid[i].Processed)
                    {
                        Console.Write("{" + "~" + "}");
                    }
                    else
                    {
                        Console.Write("[" + "~" + "]");
                    }
                }
            }
            Console.WriteLine("");
            Console.WriteLine("---------------------------");
        }

        private void SaveGrid()
        {
            if (!Directory.Exists(@".\grid"))
            {
                Directory.CreateDirectory(@".\grid");
            }
            for (int i = 0; i < grid.Count; i++)
            {
                grid[i].BMP.Save(@".\grid\grid" + (i + 1) + ".png");
            }
        }

        private IntPtr GetHandle()
        {
            Process[] processlist = Process.GetProcesses();
            String[] process = null;
            process = new string[200];
            int i = 0;
            IntPtr handle = IntPtr.Zero;
            foreach (Process theprocess in processlist)
            {
                process[i] = theprocess.MainWindowTitle;
                if (process[i].Equals("Minesweeper"))
                {
                    handle = theprocess.MainWindowHandle;
                    break;
                }
                i++;
            }
            return handle;
        }

        private void InitializeGrid(IProgress<string> progress, IntPtr handle)
        {
            progress.Report("Initializing grid");
            for (int i = 0; i < grid.Count; i++)
            {
                if (grid[i].Hidden)
                {
                    hiddenCoord.Add(i);
                }
                else if (grid[i].Num > 0 && grid[i].Num < 9)
                {
                    visibleCoord.Add(i);
                }
            }
            if (visibleCoord.Count == 0)
            {
                int clickCenter = (((grid.Count / rowCount) / 2) * rowCount) + (rowCount / 2);
                p1.X = grid[clickCenter].Center.X;
                p1.Y = grid[clickCenter].Center.Y;
                macro.ClickOnPoint(handle, p1);
                UpdateImage(macro.GetWindowBitmap(handle));
            }
        }

        private void FindBestMove(IProgress<string> progress)
        {
            InsertionSort();
            for (int i = visibleCoord.Count - 1; i >= 0; i--)
            {
                List<int> surroundingCoord = new List<int>();
                int coordY = grid[visibleCoord[i]].Center.Y;
                int possibleBomb = 0;
                int confirmedBomb = 0;
                // check left
                if ((visibleCoord[i] - 1) >= 0)
                {
                    if (Math.Abs(coordY - grid[visibleCoord[i] - 1].Center.Y) < 15)
                    {
                        if (grid[visibleCoord[i] - 1].Hidden)
                        {
                            possibleBomb++;
                            if (grid[visibleCoord[i] - 1].IsBomb)
                            {
                                confirmedBomb++;
                            }
                            else
                            {
                                surroundingCoord.Add(visibleCoord[i] - 1);
                            }
                        }
                    }
                }
                // check right
                if ((visibleCoord[i] + 1) < grid.Count)
                {
                    if (Math.Abs(coordY - grid[visibleCoord[i] + 1].Center.Y) < 15)
                    {
                        if (grid[visibleCoord[i] + 1].Hidden)
                        {
                            possibleBomb++;
                            if (grid[visibleCoord[i] + 1].IsBomb)
                            {
                                confirmedBomb++;
                            }
                            else
                            {
                                surroundingCoord.Add(visibleCoord[i] + 1);
                            }
                        }
                    }
                }
                // check top
                if ((visibleCoord[i] - rowCount) >= 0)
                {
                    if (grid[visibleCoord[i] - rowCount].Hidden)
                    {
                        possibleBomb++;
                        if (grid[visibleCoord[i] - rowCount].IsBomb)
                        {
                            confirmedBomb++;
                        }
                        else
                        {
                            surroundingCoord.Add(visibleCoord[i] - rowCount);
                        }
                    }
                }
                // check bottom
                if ((visibleCoord[i] + rowCount) < grid.Count)
                {
                    if (grid[visibleCoord[i] + rowCount].Hidden)
                    {
                        possibleBomb++;
                        if (grid[visibleCoord[i] + rowCount].IsBomb)
                        {
                            confirmedBomb++;
                        }
                        else
                        {
                            surroundingCoord.Add(visibleCoord[i] + rowCount);
                        }
                    }
                }
                // check top left
                if ((visibleCoord[i] - rowCount - 1) >= 0)
                {
                    if (Math.Abs(grid[visibleCoord[i] - rowCount].Center.Y - grid[visibleCoord[i] - rowCount - 1].Center.Y) < 15)
                    {
                        if (grid[visibleCoord[i] - rowCount - 1].Hidden)
                        {
                            possibleBomb++;
                            if (grid[visibleCoord[i] - rowCount - 1].IsBomb)
                            {
                                confirmedBomb++;
                            }
                            else
                            {
                                surroundingCoord.Add(visibleCoord[i] - rowCount - 1);
                            }
                        }
                    }
                }
                // check top right
                if ((visibleCoord[i] - rowCount + 1) > 0)
                {
                    if (Math.Abs(grid[visibleCoord[i] - rowCount].Center.Y - grid[visibleCoord[i] - rowCount + 1].Center.Y) < 15)
                    {
                        if (grid[visibleCoord[i] - rowCount + 1].Hidden)
                        {
                            possibleBomb++;
                            if (grid[visibleCoord[i] - rowCount + 1].IsBomb)
                            {
                                confirmedBomb++;
                            }
                            else
                            {
                                surroundingCoord.Add(visibleCoord[i] - rowCount + 1);
                            }
                        }
                    }
                }
                // check bottom left
                if ((visibleCoord[i] + rowCount - 1) < grid.Count - 1)
                {
                    if (Math.Abs(grid[visibleCoord[i] + rowCount].Center.Y - grid[visibleCoord[i] + rowCount - 1].Center.Y) < 15)
                    {
                        if (grid[visibleCoord[i] + rowCount - 1].Hidden)
                        {
                            possibleBomb++;
                            if (grid[visibleCoord[i] + rowCount - 1].IsBomb)
                            {
                                confirmedBomb++;
                            }
                            else
                            {
                                surroundingCoord.Add(visibleCoord[i] + rowCount - 1);
                            }
                        }
                    }
                }
                // check bottom right
                if ((visibleCoord[i] + rowCount + 1) < grid.Count)
                {
                    if (Math.Abs(grid[visibleCoord[i] + rowCount].Center.Y - grid[visibleCoord[i] + rowCount + 1].Center.Y) < 15)
                    {
                        if (grid[visibleCoord[i] + rowCount + 1].Hidden)
                        {
                            possibleBomb++;
                            if (grid[visibleCoord[i] + rowCount + 1].IsBomb)
                            {
                                confirmedBomb++;
                            }
                            else
                            {
                                surroundingCoord.Add(visibleCoord[i] + rowCount + 1);
                            }
                        }
                    }
                }
                if (guess)
                {
                    for (int j = 0; j < surroundingCoord.Count; j++)
                    {
                        if (!grid[surroundingCoord[j]].IsBomb)
                        {
                            clicked = true;
                            p1.X = grid[surroundingCoord[j]].Center.X;
                            p1.Y = grid[surroundingCoord[j]].Center.Y;
                            break;
                        }
                    }
                    guess = false;
                    break;
                }
                //Console.WriteLine(grid[visibleCoord[i]].Num + " at grid: " + (visibleCoord[i] + 1) + " has " + confirmedBomb + "/" + possibleBomb + " hidden");
                // detect if the number at this coord = the number of hidden tiles, means those are definitely bombs
                if (possibleBomb == grid[visibleCoord[i]].Num && !finishedProcessing)
                {
                    for (int j = 0; j < surroundingCoord.Count; j++)
                    {
                        grid[surroundingCoord[j]] = new SweeperGrid(grid[surroundingCoord[j]].BMP, grid[surroundingCoord[j]].Rect, true, grid[surroundingCoord[j]].Center, 9, false, true, true);
                    }
                    grid[visibleCoord[i]] = new SweeperGrid(grid[visibleCoord[i]].BMP, grid[visibleCoord[i]].Rect, false, grid[visibleCoord[i]].Center, grid[visibleCoord[i]].Num, false, false, true);
                    current = visibleCoord[i] + 1;
                    progress.Report("Processing node " + current);
                    visibleCoord.RemoveAt(i);
                }
                // after we finish processing everything, start looking at what we can click
                else if (possibleBomb > grid[visibleCoord[i]].Num && confirmedBomb == grid[visibleCoord[i]].Num && finishedProcessing)
                {
                    //Console.WriteLine(grid[visibleCoord[i]].Num + " at grid: " + (visibleCoord[i] + 1) + " has " + confirmedBomb + "/" + possibleBomb + " hidden");
                    clicked = true;
                    p1.X = grid[surroundingCoord[0]].Center.X;
                    p1.Y = grid[surroundingCoord[0]].Center.Y;
                    break;
                }
                if (i == 0 && !finishedProcessing)
                {
                    finishedProcessing = true;
                }
                else if (i == 0 && finishedProcessing)
                {
                    //Console.WriteLine("Guessing");
                    progress.Report("Guessing");
                    guess = true;
                }
            }
        }

        private void ProcessMove(IntPtr handle, IProgress<string> progress)
        {
            if (handle == IntPtr.Zero)
            {
                progress.Report("Process not found");
                return;
            }
            progress.Report("Process found");
            ProcessImage(macro.GetWindowBitmap(handle));
            OrganizeGrid();
            InitializeGrid(progress, handle);
            //SaveGrid();
            //PrintGrid();
            int processing = 0;
            while (true)
            {
                if (processing == grid.Count || bombDetected)
                {
                    progress.Report("Bomb detected");
                    break;
                }
                if (clicked)
                {
                    macro.ClickOnPoint(handle, p1);
                    UpdateImage(macro.GetWindowBitmap(handle));
                }
                FindBestMove(progress);
                //PrintGrid();
                processing++;
            }
        }

        private struct SweeperGrid
        {
            private Bitmap _bmp;
            private Rectangle _rect;
            private bool _hidden;
            private IntPoint _center;
            private int _num;
            private bool _bombvisible;
            private bool _isbomb;
            private bool _processed;

            public SweeperGrid(Bitmap bmp, Rectangle rect, bool hidden, IntPoint center, int num, bool bombvisible, bool isbomb, bool processed)
            {
                _bmp = bmp;
                _rect = rect;
                _hidden = hidden;
                _center = center;
                _num = num;
                _bombvisible = bombvisible;
                _isbomb = isbomb;
                _processed = processed;
            }

            public Bitmap BMP
            {
                get { return _bmp; }
                set { _bmp = value; }
            }

            public Rectangle Rect
            {
                get { return _rect; }
                set { _rect = value; }
            }

            public bool Hidden
            {
                get { return _hidden; }
                set { _hidden = value; }
            }

            public IntPoint Center
            {
                get { return _center; }
                set { _center = value; }
            }

            public int Num
            {
                get { return _num; }
                set { _num = value; }
            }

            public bool BombVisible
            {
                get { return _bombvisible; }
                set { _bombvisible = value; }
            }

            public bool IsBomb
            {
                get { return _isbomb; }
                set { _isbomb = value; }
            }

            public bool Processed
            {
                get { return _processed; }
                set { _processed = value; }
            }
        }
    }
}
