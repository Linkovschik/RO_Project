using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RO_Project
{
    static class MyEtalonLoader
    {

        public static List<Bitmap> GetEtalonBitmaps(Bitmap imageBitMap)
        {
            //сначала получим настоящие символы, т.е. с реальными биткартами и границами
            List<RealSymbol> realSymbols = RealSymbol.GetRealSymbols(imageBitMap);

            //достройка символов i,j,=,; и т.п.
            //RealSymbol.UniteRealSymbols(ref realSymbols);

            //теперь можно на основе реальных данных создать символы для распознавания
            List<Bitmap> result = new List<Bitmap>();
            foreach (RealSymbol realSymbol in realSymbols)
            {
                result.Add(Symbol.BitmapResize(Symbol.MakeASquareBitmap(realSymbol.GetRealBitMap()),MyTextRecognizer.ResizeWidth, MyTextRecognizer.ResizeHeight));
            }
            return result;
        }


        private static bool isWhiteColor(Color currColor)
        {
            return (currColor.R > 215 && currColor.G > 215 && currColor.B > 215);
        }

        //создать матрицу 16*16 для картинки с конкретным символом - эталоном
        //тип double, поскольку нужно получить усреднённый эталон
        public static double[,] CreateDoubleMatrixForEtalon(Bitmap bitmap)
        {
            //bitmap = Symbol.BitmapResize(bitmap, ResizeWidth, ResizeHeight);
            double[,] result = new double[bitmap.Height, bitmap.Width];
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
            for (int i = 0; i < bitmap.Height; ++i)
            {
                for (int j = 0; j < bitmap.Width; ++j)
                {
                    Console.Write(result[i, j] + " ");
                }
                Console.Write("\n");
            }
            return result;

        }

        //функция для определение средней матрицы эталонных изображений одного символа(используется только для этого пока что)
        public static double[,] GetAverageArrayForEtalon(List<double[,]> arrays)
        {
            double[,] result;
            try
            {
                result = new double[arrays[0].GetLength(0), arrays[0].GetLength(1)];

                //записываем сумму в результат
                foreach (var array in arrays)
                {

                    for (int i = 0; i < array.GetLength(0); ++i)
                    {
                        for (int j = 0; j < array.GetLength(1); ++j)
                        {
                            result[i, j] += array[i, j];
                        }
                    }

                }

                //делим на количество
                for (int i = 0; i < result.GetLength(0); ++i)
                    for (int j = 0; j < result.GetLength(1); ++j)
                    {
                        result[i, j] /= (double)arrays.Count;
                    }
            }
            catch(Exception e)
            {
                result = new double[32, 32];
            }
            return result;
        }
    }
}
