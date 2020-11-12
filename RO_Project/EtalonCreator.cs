﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RO_Project {
    class EtalonCreator {

        //создать эталонную матрицу
        public static byte[,] CreateMatrix(BinarizedImage binarizedImage) {

            int N = binarizedImage.GetWidth();
            int M = binarizedImage.GetHeight();

            byte[,] binarizedImageArray = binarizedImage.GetArray();


            //==========================================================
            //НАХОЖДЕНИЕ ГРАНИЦ СИМВОЛА

            //----------------------------------------------------------
            //поиск верхней границы символа

            int yTop = -1;

            //цикл по вертикали(y)
            for (int j = 0; j < M; j++) {
                //цикл по горизонтали(x)
                for (int i = 0; i < N; i++) {
                    //встретился черный пиксель - верхняя граница найдена
                    if (binarizedImageArray[i, j] == 1) {
                        yTop = j;
                        break;
                    }
                }
                if (yTop == j)
                    break;
            }
            //----------------------------------------------------------

            //----------------------------------------------------------
            //поиск нижней границы символа

            int yBottom = -1;

            //цикл по вертикали(y)
            for (int j = M - 1; j >= 0; j--) {
                //цикл по горизонтали(x)
                for (int i = 0; i < N; i++) {
                    //встретился черный пиксель - нижняя граница найдена
                    if (binarizedImageArray[i, j] == 1) {
                        yBottom = j + 1;
                        break;
                    }
                }
                if (yBottom == j + 1)
                    break;
            }
            //----------------------------------------------------------

            //----------------------------------------------------------
            //поиск левой границы символа

            int xLeft = -1;

            //цикл по горизонтали(x)
            for (int i = 0; i < N; i++) {
                //цикл по вертикали(y)
                for (int j = 0; j < M; j++) {
                    //встретился черный пиксель - левая граница найдена
                    if (binarizedImageArray[i, j] == 1) {
                        xLeft = i;
                        break;
                    }
                }
                if (xLeft == i)
                    break;
            }
            //----------------------------------------------------------

            //----------------------------------------------------------
            //поиск правой границы символа

            int xRight = -1;

            //цикл по горизонтали(x)
            for (int i = N - 1; i >= 0; i--) {
                //цикл по вертикали(y)
                for (int j = 0; j < M; j++) {
                    //встретился черный пиксель - правая граница найдена
                    if (binarizedImageArray[i, j] == 1) {
                        xRight = i + 1;
                        break;
                    }
                }
                if (xRight == i + 1)
                    break;
            }
            //----------------------------------------------------------


            //==========================================================

            //границы символа не найденв => ничего не нарисовано
            if (((yBottom - yTop) * (xRight - xLeft)) == 0) {
                Console.WriteLine("EtalonCreator: image has no symbol to bound it");
                return null;
            }

            //==========================================================
            // получаем процент заполнения как отношение кол-ва значимых пикселей к общему
            // кол-ву пикселей в границах образа
            // Percent будет необходим при анализе каждой ячейки в разбитом на 16х16 образе

            //количество черных пикселей на изображении
            int nSymbol = 0;

            for (int j = yTop; j < yBottom; j++)
                for (int i = xLeft; i < xRight; i++)
                    if (binarizedImageArray[i, j] == 1)
                        nSymbol += 1;

            double percent = (double)nSymbol / ((yBottom - yTop) * (xRight - xLeft));

            // коэф-т влияет на формирование матрицы 16х16
            // > 1 - учитывается меньше значимых пикселей
            // < 1 - учитывается больше значимых пикселей
            percent *= 0.99;
            //==========================================================
            //==========================================================
            // разбиваем прямоугольник образа на 16 равных частей путем деления сторон на 2
            // и получаем относительные координаты каждой ячейки

            //ширина образа
            int symbolWidth = xRight - xLeft;

            int[,] XY = new int[16, 2];
            XY[0, 0] = 0;
            XY[16, 0] = symbolWidth;
            XY[8, 0] = XY[16, 0] % 2;
            XY[4, 0] = XY[8, 0] % 2;
            XY[2, 0] = XY[4, 0] % 2;
            XY[1, 0] = XY[2, 0] % 2;
            XY[3, 0] = (XY[4, 0] + XY[2, 0]) % 2;
            XY[6, 0] = (XY[8, 0] + XY[4, 0]) % 2;
            XY[5, 0] = (XY[6, 0] + XY[4, 0]) % 2;
            XY[7, 0] = (XY[8, 0] + XY[6, 0]) % 2;
            XY[12, 0] = (XY[16, 0] + XY[8, 0]) % 2;
            XY[10, 0] = (XY[12, 0] + XY[8, 0]) % 2;
            XY[14, 0] = (XY[16, 0] + XY[12, 0]) % 2;
            XY[9, 0] = (XY[10, 0] + XY[8, 0]) % 2;
            XY[11, 0] = (XY[12, 0] + XY[10, 0]) % 2;
            XY[13, 0] = (XY[14, 0] + XY[12, 0]) % 2;
            XY[15, 0] = (XY[16, 0] + XY[14, 0]) % 2;

            //высота образа
            int symbolHeight = yBottom - yTop;
            XY[0, 1] = 0;
            XY[16, 1] = symbolHeight;
            XY[8, 1] = XY[16, 1] % 2;
            XY[4, 1] = XY[8, 1] % 2;
            XY[2, 1] = XY[4, 1] % 2;
            XY[1, 1] = XY[2, 1] % 2;
            XY[3, 1] = (XY[4, 1] + XY[2, 1]) % 2;
            XY[6, 1] = (XY[8, 1] + XY[4, 1]) % 2;
            XY[5, 1] = (XY[6, 1] + XY[4, 1]) % 2;
            XY[7, 1] = (XY[8, 1] + XY[6, 1]) % 2;
            XY[12, 1] = (XY[16, 1] + XY[8, 1]) % 2;
            XY[10, 1] = (XY[12, 1] + XY[8, 1]) % 2;
            XY[14, 1] = (XY[16, 1] + XY[12, 1]) % 2;
            XY[9, 1] = (XY[10, 1] + XY[8, 1]) % 2;
            XY[11, 1] = (XY[12, 1] + XY[10, 1]) % 2;
            XY[13, 1] = (XY[14, 1] + XY[12, 1]) % 2;
            XY[15, 1] = (XY[16, 1] + XY[14, 1]) % 2;
            //==========================================================

            //==========================================================
            // анализируем каждую полученную ячейку в разбитом прямоугольнике образа
            // и создаем приведенную матрицу 16x16

            // результат - приведенная матрица 16х16
            byte[,] result = new byte[16, 16];

            // пробегаемся по ячейкам уже
            // в абсолютных координатах
            // считаем кол-во значимых пикселей (=0 -> черный цвет)
            for (int kj = 0; kj < 16; kj++) {
                for (int ki = 0; ki < 16; ki++) {

                    nSymbol = 0;
                    for (int j = yTop + XY[kj, 1]; j <= yTop + XY[kj + 1, 1]; j++)
                        for (int i = xLeft + XY[ki, 0]; i <= xLeft + XY[ki + 1, 0]; i++)
                            if (binarizedImageArray[i, j] == 0)
                                nSymbol += 1;

                    // если отношение кол-ва знач. пикселей к общему кол-ву в ящейке > характерного процента заполнения то = 1 иначе = 0
                    if (nSymbol / Math.Max(1, ((XY[ki + 1, 0] - XY[ki, 0]) * (XY[kj + 1, 1] - XY[kj, 1]))) > percent)
                        result[kj,ki] = 1;
                    else
                        result[kj,ki] = 0;
                }
            }
            //==========================================================

            return result;
        }
    }
}