using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RO_Project {

    public class PrimalSymbolComparer<T> : IComparer<T>
      where T : PrimalSymbol
    {
        private int y_center;
        public PrimalSymbolComparer(Rectangle _fatherRectangle)
        {
            y_center = _fatherRectangle.Top + _fatherRectangle.Height/2;
        }
        public int Compare(T x, T y)
        {
            PrimalSymbol x_symbol = x as PrimalSymbol;
            PrimalSymbol y_symbol = y as PrimalSymbol;

            if (x_symbol.GetRealBoundaries().Bottom > y_symbol.GetRealBoundaries().Bottom)
                return 1;
            if (x_symbol.GetRealBoundaries().Bottom < y_symbol.GetRealBoundaries().Bottom)
                return -1;
            else
            {
                if (x_symbol.GetRealBoundaries().Left > y_symbol.GetRealBoundaries().Left)
                    return 1;
                if (x_symbol.GetRealBoundaries().Left < y_symbol.GetRealBoundaries().Left)
                    return -1;
                else return 0;
            }
            //if (x_symbol.GetRealBoundaries().Top > y_center && y_symbol.GetRealBoundaries().Top < y_center)
            //    return 1;
            //if (x_symbol.GetRealBoundaries().Top < y_center && y_symbol.GetRealBoundaries().Top > y_center)
            //    return -1;
            //else
            //{
            //    if (x_symbol.GetRealBoundaries().Left > y_symbol.GetRealBoundaries().Left)
            //        return 1;
            //    if (x_symbol.GetRealBoundaries().Left < y_symbol.GetRealBoundaries().Left)
            //        return -1;
            //    else return 0;
            //}
            //if (x_symbol.GetRealBoundaries().Left > y_symbol.GetRealBoundaries().Left)
            //    return 1;
            //if (x_symbol.GetRealBoundaries().Left < y_symbol.GetRealBoundaries().Left)
            //    return -1;
            //else return 0;
        }
    }

    public class LeftPrimalSymbolComparer<T> : IComparer<T>
      where T : PrimalSymbol
    {
        private int y_center;
        public int Compare(T x, T y)
        {
            PrimalSymbol x_symbol = x as PrimalSymbol;
            PrimalSymbol y_symbol = y as PrimalSymbol;

            if (x_symbol.GetRealBoundaries().Left > y_symbol.GetRealBoundaries().Left)
                return 1;
            if (x_symbol.GetRealBoundaries().Left < y_symbol.GetRealBoundaries().Left)
                return -1;
            else return 0;
            
        }
    }

    public static class Symbol {



        public static int ResizeWidth = 32;
        public static int ResizeHeight = 32;

        private static bool isWhiteColor(Color currColor)
        {
            return (currColor.R > 215 && currColor.G > 215 && currColor.B > 215);
        }

        //создать матрицу 16*16 для картинки с конкретным символом
        public static byte[,] CreateByteMatrix(Bitmap bitmap)
        {
            byte[,] result = new byte[bitmap.Height, bitmap.Width];
            for (int i = 0; i < bitmap.Height; ++i)
            {
                for (int j = 0; j < bitmap.Width; ++j)
                {
                    if (isWhiteColor(bitmap.GetPixel(j, i)))
                    {
                        result[i, j] = 0;
                    }
                    else
                    {
                        result[i, j] = 1;
                    }
                }
            }
            return result;
        }

        public static Bitmap BitmapResize(Bitmap image, int Width, int Height)
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

        public static Bitmap MakeASquareBitmap(Bitmap bitMap)
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

        private bool correlation(Color currColor)
        {
            return (currColor.R > 215 && currColor.G > 215 && currColor.B > 215);
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
        private Bitmap realBitMap;
        private Rectangle realBoundaries;
        private string meaning;
        private byte[,] array;

        private RealSymbol(Point pixel, Bitmap imageBitMap)
        {
            //сначала найдём граница поиском в ширину
            realBoundaries = StartBFSFromPixel(pixel, imageBitMap);
            //потом создадим свою бит-карту для символа
            realBitMap = GetImagePart(pixel, imageBitMap, realBoundaries);
            array = Symbol.CreateByteMatrix(Symbol.BitmapResize(Symbol.MakeASquareBitmap(realBitMap), Symbol.ResizeWidth, Symbol.ResizeHeight));
            PrintArray();
        }

        private RealSymbol(Rectangle _realBoundaries, Bitmap _bitMap)
        {
            realBoundaries = new Rectangle(_realBoundaries.X,_realBoundaries.Y, _realBoundaries.Width, _realBoundaries.Height);
            realBitMap = _bitMap;
            array = Symbol.CreateByteMatrix(Symbol.BitmapResize(Symbol.MakeASquareBitmap(realBitMap), Symbol.ResizeWidth, Symbol.ResizeHeight));
        }

        public byte[,] GetByteArray()
        {
            return array;
        }

        public void SetMeaning(string _meaning)
        {
            meaning = _meaning;
        }
        public string GetMeaning()
        {
            return meaning;
        }

        public void PrintRealBitMap()
        {
            for (int i = 0; i < realBitMap.Height; i++)
            {
                for (int j = 0; j < realBitMap.Width; j++)
                    Console.Write((isWhiteColor(realBitMap.GetPixel(j,i))?0:1).ToString() + " ");
                Console.WriteLine();
            }
        }
        public void PrintArray()
        {
            for (int i = 0; i < array.GetLength(0); i++)
            {
                for (int j = 0; j < array.GetLength(1); j++)
                    Console.Write(array[i,j].ToString() + " ");
                Console.WriteLine();
            }
        }

        public Rectangle GetRealBounds()
        {
            return realBoundaries;
        }

        public Bitmap GetRealBitMap()
        {
            return realBitMap;
        }

        private static bool IsPixelBelongsToSymbol(List<RealSymbol> symbols, Point pixel)
        {
            foreach (var symbol in symbols)
            {
                if (symbol.GetRealBounds().Contains(pixel))
                    return true;
            }
            return false;
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
                       !isWhiteColor(bitMap.GetPixel(newPixel.X, newPixel.Y)) &&
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
                       !isWhiteColor(bitMap.GetPixel(newPixel.X, newPixel.Y)) &&
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

        public static List<RealSymbol> UniteRealSymbols(List<RealSymbol> _realSymbols)
        {
            List<RealSymbol> realSymbols = new List<RealSymbol>(_realSymbols);
            List<RealSymbol> done = new List<RealSymbol>();
            realSymbols.Reverse();
            //достройка i,j,=,: 
            for (int i = realSymbols.Count - 1; i >= 0; --i)
            {
                if (done.Contains(realSymbols[i]))
                    continue;
                Rectangle symbolRectangle = realSymbols[i].GetRealBounds();
                //будем искать мою часть, но искать с минимальной высостой до неё. 
                RealSymbol myParticle = null;
                int heightToMyParticle = int.MaxValue;
                //цикл j для поиска particle (точки для 'i')
                for (int j = realSymbols.Count - 1; j >= 0; --j)
                {
                    Rectangle particleRectangle = realSymbols[j].GetRealBounds();
                    Point particleRectCenter = new Point(particleRectangle.Left + particleRectangle.Width / 2, particleRectangle.Top + particleRectangle.Height / 2);
                    if (i != j)
                    {
                        //точка сверху над телом i или j, или же вторая палка знака '=' лежит в моих границах 
                        if (particleRectCenter.X > symbolRectangle.Left && particleRectCenter.X < symbolRectangle.Left + symbolRectangle.Width - 1 &&
                            //ищу так, чтобы моя координата по Y была ниже, чем то, что я ищу. Т.е. ищем для тела 'i' верхнюю точку, для '=' верхнюю палку и т.д.
                            symbolRectangle.Top > particleRectangle.Top &&
                            heightToMyParticle > (symbolRectangle.Top - particleRectangle.Top + particleRectangle.Height - 1))
                        {
                            myParticle = realSymbols[j];
                            heightToMyParticle = (symbolRectangle.Top - particleRectangle.Top + particleRectangle.Height - 1);

                        }
                    }
                }
                if (myParticle != null)
                {
                    //а потом добавим её реальную биткарту к своей реальной биткарте
                    realSymbols[i] = (RealSymbol.SumSymbols(realSymbols[i], myParticle));
                    done.Add(realSymbols[i]);
                    //найденную частичку удалим из списка реальных символов
                    realSymbols.Remove(myParticle);
                }



            }
            realSymbols.Reverse();
            return realSymbols;
        }

        public static RealSymbol SumSymbols(RealSymbol bodySymbol, RealSymbol particleSymbol)
        {
            int left = Math.Min(particleSymbol.realBoundaries.Left, bodySymbol.realBoundaries.Left);
            int top = Math.Min(particleSymbol.realBoundaries.Top, bodySymbol.realBoundaries.Top);
            int bottom = Math.Max(particleSymbol.realBoundaries.Bottom, bodySymbol.realBoundaries.Bottom);
            int right = Math.Max(particleSymbol.realBoundaries.Right, bodySymbol.realBoundaries.Right);
            int width = right - left + 1;
            int height = bottom - top + 1;
            Bitmap result = new Bitmap(width, height);
            Rectangle resRect = new Rectangle(left, top, width, height);

            for (int i = left; i < width + left; ++i)
            {
                for (int j = top; j < height + top; ++j)
                {
                    if (i >= particleSymbol.realBoundaries.Left && i <= particleSymbol.realBoundaries.Left + particleSymbol.realBoundaries.Width - 1 &&
                        j >= particleSymbol.realBoundaries.Top && j <= particleSymbol.realBoundaries.Top + particleSymbol.realBoundaries.Height - 1)
                        result.SetPixel(i - left, j - top, particleSymbol.realBitMap.GetPixel(i - particleSymbol.realBoundaries.Left, j - particleSymbol.realBoundaries.Top));
                    else if (i >= bodySymbol.realBoundaries.Left && i <= bodySymbol.realBoundaries.Left + bodySymbol.realBoundaries.Width - 1 &&
                        j >= bodySymbol.realBoundaries.Top && j <= bodySymbol.realBoundaries.Top + bodySymbol.realBoundaries.Height - 1)
                        result.SetPixel(i - left, j - top, bodySymbol.realBitMap.GetPixel(i - bodySymbol.realBoundaries.Left, j - bodySymbol.realBoundaries.Top));
                    else
                    {
                        result.SetPixel(i - left, j - top, Color.FromArgb(255, 255, 255));
                    }
                }
            }
            return new RealSymbol(resRect, result);
        }

        private static bool isWhiteColor(Color currColor)
        {
            return (currColor.R > 215 && currColor.G > 215 && currColor.B > 215);
        }

        public static List<RealSymbol> GetRealSymbols(Bitmap imageBitMap)
        {
            List<RealSymbol> realSymbols = new List<RealSymbol>();
            for (int i = 0; i < imageBitMap.Width; ++i)
            {
                for (int j = 0; j < imageBitMap.Height; ++j)
                {
                    Point pixel = new Point(i, j);
                    if (!isWhiteColor(imageBitMap.GetPixel(i, j)) && !IsPixelBelongsToSymbol(realSymbols, pixel))
                    {
                        RealSymbol realSymbol = new RealSymbol(pixel, imageBitMap);
                        realSymbols.Add(realSymbol);
                    }
                }
            }
            return realSymbols;
        }
    }

    public class PrimalSymbol
    {
        private string meaning;
        private Rectangle primalSymbolBoundaries;
        private Bitmap primalSymbolBitmap;
        private List<PrimalSymbol> primalSymbols;
        List<RealSymbol> realInSymbols;

        public PrimalSymbol(RealSymbol _realSymbol, string _meaning)
        {
            primalSymbolBoundaries = _realSymbol.GetRealBounds();
            primalSymbolBitmap = _realSymbol.GetRealBitMap();
            realInSymbols = new List<RealSymbol>() { _realSymbol };
            meaning = _meaning;
        }

        public PrimalSymbol(RealSymbol _realSymbol)
        {
            primalSymbolBoundaries = _realSymbol.GetRealBounds();
            primalSymbolBitmap = _realSymbol.GetRealBitMap();
            realInSymbols = RealSymbol.GetRealSymbols(primalSymbolBitmap);
            meaning = "";
        }

        private PrimalSymbol(Bitmap _primalSymbolBitmap, Rectangle _primalSymbolBoundaries)
        {
            primalSymbolBoundaries = _primalSymbolBoundaries;
            primalSymbolBitmap = _primalSymbolBitmap;
            realInSymbols = RealSymbol.GetRealSymbols(primalSymbolBitmap);
            meaning = "";
        }

        public void PrintRealBitMap()
        {
            for (int i = 0; i < primalSymbolBitmap.Height; i++)
            {
                for (int j = 0; j < primalSymbolBitmap.Width; j++)
                    Console.Write((isWhiteColor(primalSymbolBitmap.GetPixel(j, i)) ? 0 : 1).ToString() + " ");
                Console.WriteLine();
            }
        }

        public void SetMeaning(string _meaning)
        {
            meaning = _meaning;
        }

        public string GetMeaning()
        {
            return meaning;
        }

        public Rectangle GetRealBoundaries()
        {
            return primalSymbolBoundaries;
        }

        public List<RealSymbol> GetRealSymbols()
        {
            return new List<RealSymbol>(realInSymbols);
        }

        private static bool isWhiteColor(Color currColor)
        {
            return (currColor.R > 215 && currColor.G > 215 && currColor.B > 215);
        }

        private static List<Rectangle> GetSymbolsBoundaries(Bitmap imageBitmap)
        {
            List<Rectangle> symbolsBoundaries = new List<Rectangle>();

            int N = imageBitmap.Width;
            int M = imageBitmap.Height;

            //первый разделитель в промежтуке
            bool previousOnlyWhite = false;
            int Left = -1, Right = -1, Top = -1, Bottom = -1, Counter = 0;
            for (int i = 0; i < N; ++i)
            {

                bool currentOnlyWhite = true;
                for (int j = 0; j < M; ++j)
                {
                    currentOnlyWhite &= isWhiteColor(imageBitmap.GetPixel(i, j));
                }

                //если есть чёрный символ в этой вертикальной линии i
                if (currentOnlyWhite == false)
                {

                    if (i == 0)
                    {
                        Counter += 1;
                        Left = i;
                    }
                    else if (i == N - 1)
                    {
                        Counter += 1;
                        Right = i;
                    }
                    //переход с белого на чёрный
                    else if (previousOnlyWhite == true)
                    {
                        Counter += 1;
                        Left = i;
                    }
                }

                else
                {
                    //переход с чёрного на белый
                    if (previousOnlyWhite == false && i != 0)
                    {
                        Counter += 1;
                        Right = i - 1;
                    }
                }
                previousOnlyWhite = currentOnlyWhite;
                if (Counter == 2)
                {
                    Top = M;
                    Bottom = -1;
                    for (int k = Left; k <= Right; ++k)
                    {
                        for (int l = 0; l < M; ++l)
                        {
                            if (!isWhiteColor(imageBitmap.GetPixel(k, l)))
                            {
                                if (Top > l)
                                    Top = l;
                                if (Bottom < l)
                                    Bottom = l;
                            }
                        }
                    }
                    Counter = 0;
                    symbolsBoundaries.Add(new Rectangle(Left, Top, Right - Left + 1, Bottom - Top + 1));
                }
            }
            return symbolsBoundaries;
        }

        private static Bitmap GetImagePart(Bitmap imageBitmap, Rectangle rect)
        {
            int N = rect.Width;
            int M = rect.Height;

            Console.WriteLine("Создается биткарта размерами " + N + " x " + M);

            Bitmap partBitmap = new Bitmap(N + 1, M + 1);

            for (int i = rect.Left; i <= rect.Right; i++)
                for (int j = rect.Top; j <= rect.Bottom; j++)
                    partBitmap.SetPixel(i - rect.Left, j - rect.Top, imageBitmap.GetPixel(i, j));

            return partBitmap;
        }

        private delegate Tuple<bool> ExtraSymbolChec(RealSymbol particleSymbol, RealSymbol symbol);
        public static List<PrimalSymbol> GetPrimalSymbols(Bitmap imageBitmap)
        {
            
            List<PrimalSymbol> result = new List<PrimalSymbol>();
            List<Rectangle> rects = GetSymbolsBoundaries(imageBitmap);
            foreach (var rect in rects)
            {
                result.Add(new PrimalSymbol(GetImagePart(imageBitmap, rect), rect));
            }
            return result;
        }
        
    }
}
