using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RO_Project {

    class BinarizedImage {

        //размерность массива
        int N, M;

        //массив, соответствующий бинаризированному представлению картинки
        byte[,] binarizedImageArray;

        //конструктор
        public BinarizedImage(Bitmap image) {

            if (image == null) {
                Console.WriteLine("BinarizedImage: image was null");
                return;
            }

            N = image.Size.Width;
            M = image.Size.Height;

            //создадим массив по размеру исходной картинки
            binarizedImageArray = new byte[N, M];

            for (int i = 0; i < N; i++)
                for (int j = 0; j < M; j++) {

                    //получаем цвет пикселя изображения
                    Color pixelColor = image.GetPixel(i, j);

                    //если близко к белому, сделаем белым
                    if (pixelColor.R > 200 && pixelColor.G > 200 && pixelColor.B > 200)
                        binarizedImageArray[i, j] = 0;
                    //иначе, сделаем черным
                    else
                        binarizedImageArray[i, j] = 1;
                }
        }

        public BinarizedImage(BinarizedImage image, int xLeft, int xRight, int yTop, int yBottom) {

            N = xRight - xLeft + 1;
            M = yBottom - yTop + 1;

            Console.WriteLine("Создается биткарта размерами "+N+" x "+M);

            binarizedImageArray = new byte[N, M];

            for (int i = xLeft; i <= xRight; i++)
                for (int j = yTop; j <= yBottom; j++)
                    binarizedImageArray[i - xLeft, j - yTop] = image.GetArray()[i, j];
        }

        //получить массив, соответствующий бинаризованному изображению
        public byte[,] GetArray() {

            byte[,] binarizedImageArrayCopy = new byte[N, M];
            for (int i = 0; i < N; i++)
                for (int j = 0; j < M; j++)
                    binarizedImageArrayCopy[i, j] = binarizedImageArray[i, j];

            return binarizedImageArrayCopy;
        }

        //получить бинаризованное изображение
        public Bitmap GetBitmap() {

            Bitmap result = new Bitmap(N, M);

            for (int i = 0; i < N; i++)
                for (int j = 0; j < M; j++) {

                    //если близко к белому, сделаем белым
                    if (binarizedImageArray[i,j]==1)
                        result.SetPixel(i, j, Color.Black);
                    //иначе, сделаем черным
                    else
                        result.SetPixel(i, j, Color.White);
                }

            return result;
        }

        //получить ширину картинки
        public int GetWidth() {
            return N;
        }

        //получить высоту картинки
        public int GetHeight() {
            return M;
        }
    }
}
