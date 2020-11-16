using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RO_Project {

    public class Symbol {

        //биткарта, сооьтветствующая символу
        public byte[,] array;

        //битмап
        private Bitmap bitMap;

        private Rectangle realSymbolBounds;

        public string Mark { get; private set; }

        //конструктор
        public Symbol(RealSymbol realSymbol) {
            realSymbolBounds = realSymbol.GetRealBounds();
            bitMap = realSymbol.GetBitMap();
            array = CreateByteMatrix_16X16(bitMap);
        }

        public void SetMark(string _mark)
        {
            Mark = _mark;
        }

        public double GetDelta(EtalonSymbol etalon) {

            double delta = 0;

            for (int j = 0; j < array.GetLength(0); j++)
                for (int i = 0; i < array.GetLength(1); i++)
                    delta += Math.Abs(array[j, i] - etalon.array[j, i]);

            return delta;
        }

        public Rectangle GetRectangle()
        {
            return realSymbolBounds;
        }

        public void Print() {
            for (int i = 0; i < array.GetLength(0); i++) {
                for (int j = 0; j < array.GetLength(1); j++)
                    Console.Write(array[i, j] + " ");
                Console.WriteLine();
            }
        }

        public Bitmap GetBitMap() {
            return bitMap;
        }

        private bool correlation(Color currColor)
        {
            return (currColor.R > 215 && currColor.G > 215 && currColor.B > 215);
        }

        //создать матрицу 16*16 для картинки с конкретным символом
        private byte[,] CreateByteMatrix_16X16(Bitmap bitmap)
        {
            byte[,] result = new byte[bitmap.Width, bitmap.Height];
            for (int i = 0; i < bitmap.Width; ++i)
                for (int j = 0; j < bitmap.Height; ++j)
                {
                    if (correlation(bitmap.GetPixel(i, j)))
                    {
                        result[j, i] = 0;
                    }
                    else
                    {
                        result[j, i] = 1;
                    }
                }
            return result;

        }

        private double[,] CreateDoubleMatrix_16X16(Bitmap bitmap)
        {
            double[,] result = new double[bitmap.Width, bitmap.Height];

            for (int i = 0; i < bitmap.Width; ++i)
                for (int j = 0; j < bitmap.Height; ++j)
                {
                    if (correlation(bitmap.GetPixel(i, j)))
                    {
                        result[j, i] = 0;
                    }
                    else
                    {
                        result[j, i] = 1;
                    }
                }
            return result;

        }

        private Bitmap BitmapResize(Bitmap image, int Width, int Height)
        {
            int sourceWidth = image.Width;
            int sourceHeight = image.Height;
            int sourceX = 0;
            int sourceY = 0;
            int destX = 0;
            int destY = 0;

            float nPercent = 0;
            float nPercentW = 0;
            float nPercentH = 0;

            nPercentW = ((float)Width / (float)sourceWidth);
            nPercentH = ((float)Height / (float)sourceHeight);
            if (nPercentH < nPercentW)
            {
                nPercent = nPercentH;
                destX = System.Convert.ToInt16((Width -
                                (sourceWidth * nPercent)) / 2);
            }
            else
            {
                nPercent = nPercentW;
                destY = System.Convert.ToInt16((Height - (sourceHeight * nPercent)) / 2);
            }

            int destWidth = (int)(sourceWidth * nPercent);
            int destHeight = (int)(sourceHeight * nPercent);

            Bitmap bmPhoto = new Bitmap(Width, Height, PixelFormat.Format24bppRgb);
            bmPhoto.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            Graphics grPhoto = Graphics.FromImage(bmPhoto);
            grPhoto.Clear(Color.White);
            grPhoto.InterpolationMode = InterpolationMode.HighQualityBicubic;

            grPhoto.DrawImage(image,
                new Rectangle(destX, destY, destWidth, destHeight),
                new Rectangle(sourceX, sourceY, sourceWidth, sourceHeight),
                GraphicsUnit.Pixel);

            grPhoto.Dispose();
            return bmPhoto;

        }

        private Bitmap MakeASquareBitmap(Bitmap bitMap)
        {
            int Size = Math.Max(bitMap.Width, bitMap.Height);
            int shift_X = (Size - bitMap.Width) / 2;
            int shift_Y = (Size - bitMap.Height) / 2;
            Bitmap squareBitmap = new Bitmap(Size, Size);
            for (int i = 0; i < Size; ++i)
            {
                for (int j = 0; j < Size; ++j)
                {
                    squareBitmap.SetPixel(i, j, Color.FromArgb(255, 255, 255));
                }
            }

            for (int i = shift_X; i < shift_X + bitMap.Width; ++i)
            {
                for (int j = shift_Y; j < shift_Y + bitMap.Height; ++j)
                {
                    squareBitmap.SetPixel(i, j, bitMap.GetPixel(i - shift_X, j - shift_Y));
                }
            }
            return squareBitmap;
        }
    }

    public class EtalonSymbol
    {
        //биткарта, сооьтветствующая символу
        public double[,] array;
        public string Mark { get; private set; }

        public EtalonSymbol(double[,] _array, string _mark)
        {
            array = new double[_array.GetLength(0), _array.GetLength(1)];
            Array.Copy(_array, array, _array.Length);
            Mark = _mark;
        }

        public void SetMark(string _mark)
        {
            Mark = _mark;
        }

        public void Print()
        {
            for (int i = 0; i < array.GetLength(0); i++)
            {
                for (int j = 0; j < array.GetLength(1); j++)
                    Console.Write(array[i, j] + " ");
                Console.WriteLine();
            }
        }
    }

    public class RealSymbol
    {
        private Bitmap bitMap;
        private Rectangle realBoundaries;

        public RealSymbol(Point pixel, Bitmap imageBitMap)
        {
            realBoundaries = StartBFSFromPixel(pixel, imageBitMap);
            bitMap = GetImagePart(pixel, imageBitMap, realBoundaries);
        }

        private RealSymbol(Bitmap symbolRealBitMap)
        {
            bitMap = symbolRealBitMap;
        }

        public Rectangle GetRealBounds()
        {
            return realBoundaries;
        }

        public Bitmap GetBitMap()
        {
            return bitMap;
        }

        private Rectangle StartBFSFromPixel(Point pixel, Bitmap bitMap)
        {

            //очередь посещаемых пикселей
            Queue<Point> toVisitList = new Queue<Point>();

            //помещаем в очередь первый пиксель(стартовый)
            toVisitList.Enqueue(pixel);

            //список посещенных пикселей
            List<Point> visitedList = new List<Point>();

            int left = bitMap.Width;
            int top = bitMap.Height;
            int right = -1;
            int bottom = -1;

            //итерационный процесс
            while (toVisitList.Count != 0)
            {

                //извлекаем из очереди пиксель
                Point current = toVisitList.Dequeue();

                if (!Visited(current))
                {
                    visitedList.Add(current);
                    if (current.X < left)
                        left = current.X;
                    if (current.X > right)
                        right = current.X;
                    if (current.Y < top)
                        top = current.Y;
                    if (current.Y > bottom)
                        bottom = current.Y;
                }
                else
                    continue;

                //помещаем в Q все пикскли, смежные с current
                //смежными являются черные пиксели во всех 8-ми напрвалениях
                //которые еще не были посещены и находятся в пределах изображения 
                List<Point> newPixels = new List<Point>();

                newPixels.Add(new Point(current.X - 1, current.Y));
                newPixels.Add(new Point(current.X + 1, current.Y));
                newPixels.Add(new Point(current.X, current.Y + 1));
                newPixels.Add(new Point(current.X, current.Y - 1));
                newPixels.Add(new Point(current.X - 1, current.Y - 1));
                newPixels.Add(new Point(current.X - 1, current.Y + 1));
                newPixels.Add(new Point(current.X + 1, current.Y + 1));
                newPixels.Add(new Point(current.X + 1, current.Y - 1));

                foreach (Point newPixel in newPixels)
                {
                    if (newPixel.X < bitMap.Width &&
                       newPixel.X >= 0 &&
                       newPixel.Y < bitMap.Height &&
                       newPixel.Y >= 0 &&
                       !correlation(bitMap.GetPixel(newPixel.X, newPixel.Y)) &&
                       !Visited(newPixel))
                        toVisitList.Enqueue(newPixel);
                }

            }

            return new Rectangle(left, top, right - left + 1, bottom - top + 1);

            //локальная функция для проверки списка L на наличие рассматриваемого пикселя
            bool Visited(Point pixelToCheck)
            {

                foreach (Point pixelFromL in visitedList)
                    if (pixelFromL.X == pixelToCheck.X && pixelFromL.Y == pixelToCheck.Y)
                        return true;

                return false;
            }
        }

        private Bitmap GetImagePart(Point pixel, Bitmap bitMap, Rectangle rect)
        {
            Bitmap symbolBitmap = new Bitmap(rect.Width, rect.Height);
            for (int i = 0; i < symbolBitmap.Width; ++i)
            {
                for (int j = 0; j < symbolBitmap.Height; ++j)
                {
                    symbolBitmap.SetPixel(i, j, Color.FromArgb(255, 255, 255));
                }
            }
            //очередь посещаемых пикселей
            Queue<Point> toVisitList = new Queue<Point>();

            //помещаем в очередь первый пиксель(стартовый)
            toVisitList.Enqueue(pixel);

            //список посещенных пикселей
            List<Point> visitedList = new List<Point>();

            //итерационный процесс
            while (toVisitList.Count != 0)
            {

                //извлекаем из очереди пиксель
                Point current = toVisitList.Dequeue();

                if (!Visited(current))
                {
                    visitedList.Add(current);
                    symbolBitmap.SetPixel(current.X - rect.Left, current.Y - rect.Top, bitMap.GetPixel(current.X, current.Y));
                }
                else
                    continue;

                //помещаем в Q все пикскли, смежные с current
                //смежными являются черные пиксели во всех 8-ми напрвалениях
                //которые еще не были посещены и находятся в пределах изображения 
                List<Point> newPixels = new List<Point>();

                newPixels.Add(new Point(current.X - 1, current.Y));
                newPixels.Add(new Point(current.X + 1, current.Y));
                newPixels.Add(new Point(current.X, current.Y + 1));
                newPixels.Add(new Point(current.X, current.Y - 1));
                newPixels.Add(new Point(current.X - 1, current.Y - 1));
                newPixels.Add(new Point(current.X - 1, current.Y + 1));
                newPixels.Add(new Point(current.X + 1, current.Y + 1));
                newPixels.Add(new Point(current.X + 1, current.Y - 1));

                foreach (Point newPixel in newPixels)
                {
                    if (newPixel.X < bitMap.Width &&
                       newPixel.X >= 0 &&
                       newPixel.Y < bitMap.Height &&
                       newPixel.Y >= 0 &&
                       !correlation(bitMap.GetPixel(newPixel.X, newPixel.Y)) &&
                       !Visited(newPixel))
                        toVisitList.Enqueue(newPixel);
                }

            }

            return symbolBitmap;

            //локальная функция для проверки списка L на наличие рассматриваемого пикселя
            bool Visited(Point pixelToCheck)
            {

                foreach (Point pixelFromL in visitedList)
                    if (pixelFromL.X == pixelToCheck.X && pixelFromL.Y == pixelToCheck.Y)
                        return true;

                return false;
            }
        }

        public static RealSymbol SumSymbols(RealSymbol bodySymbol, RealSymbol particleSymbol)
        {
            int width = Math.Max(particleSymbol.realBoundaries.Left + particleSymbol.realBoundaries.Width, bodySymbol.realBoundaries.Left);
            int height = bodySymbol.realBoundaries.Top + bodySymbol.realBoundaries.Height - particleSymbol.realBoundaries.Top;
            Bitmap result = new Bitmap(width,
                                       bodySymbol.bitMap.Height + particleSymbol.bitMap.Height);

            for(int )
            return result;
        }
    }
}
