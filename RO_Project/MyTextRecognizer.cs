using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RO_Project {
    class MyTextRecognizer {

        //эталонные массивы 16*16
        //----------------------------------------------
        private static byte[,] a1 = new byte[16, 16]{
    { 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0  },
    { 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0  },
    { 0, 0, 1, 1, 1, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 0  },
    { 0, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 0  },
    { 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 0  },
    { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 0  },
    { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 0  },
    { 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0  },
    { 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 1, 1, 1, 0  },
    { 0, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 0  },
    { 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 0  },
    { 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 0  },
    { 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 0  },
    { 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 0  },
    { 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 1, 1, 0  },
    { 0, 0, 0, 0, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 1, 0  },
};

        private static byte[,] a2 = new byte[16, 16] {
    { 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0  },
    { 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0  },
    { 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0  },
    { 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0  },
    { 1, 1, 1, 0, 0, 0, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0  },
    { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0  },
    { 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 0  },
    { 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 0  },
    { 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0  },
    { 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1 },
    { 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1  },
    { 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0  },
    { 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 0  },
    { 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 0  },
    { 1, 1, 1, 1, 1, 1, 0, 0, 0, 1, 1, 1, 1, 0, 0, 0  },
    { 1, 1, 1, 0, 0, 0, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0  },
};
        //----------------------------------------------

        static string etalonArraysTxtPath = "C:\\Users\\1\\Desktop\\etalons\\txt\\";
        static string rulesDirectoryPath = "C:\\Users\\1\\Desktop\\rules\\";

        //список с матрицами(эталонами) для каждого символа
        List<Symbol> etalonSymbols;

        //список правил
        List<IRule> rules;


        //результат распознавания
        public static string recognitionResult;

        //конструктор
        public MyTextRecognizer() {

            recognitionResult = "";

            string[] directories = Directory.GetDirectories(etalonArraysTxtPath);

            etalonSymbols = new List<Symbol>();

            foreach(string directory in directories) {

                string[] files = Directory.GetFiles(directory);

                foreach (string filePath in files) {

                    byte[,] array = MyArraySerializer.DeserializeArray(new StreamReader(filePath));

                    etalonSymbols.Add(new Symbol(array, Path.GetFileName(filePath).Replace(".txt","")));

                    Console.WriteLine(filePath);
                }
            }

            ReadRules(rulesDirectoryPath);
        }

        private void ReadRules(string rulesDirectoryPath) {

            string[] directories = Directory.GetDirectories(rulesDirectoryPath);

            rules = new List<IRule>();

            foreach (string directory in directories) {


                switch (Path.GetDirectoryName(directory)) {
                    case "Trigonom":

                        string[] files = Directory.GetFiles(directory);

                        foreach (string filePath in files) {

                            TrigonomRule trigonomRule = new TrigonomRule(filePath);

                            rules.Add(trigonomRule);

                            Console.WriteLine("rule: "+ filePath);
                        }

                        break;
                }

                
            }
        }

        //распознать символ 
        public void RecognizeSymbol(Symbol symbol) {

            //минимальное отклонение от эталона
            int minDelta = int.MaxValue;

            string result = "nomark";

            foreach (Symbol etalonSymbol in etalonSymbols) {

                int delta = etalonSymbol.GetDelta(symbol);

                if (delta < minDelta) {
                    minDelta = delta;
                    result = etalonSymbol.Mark;
                }
            }

            List<IRule> activeRules = new List<IRule>(rules);

            IRule activeRule = null;
            Queue<Symbol> activeQueue = new Queue<Symbol>();

            bool flag = false;
            string tmp = "";

            if (activeRule == null) {
                foreach (IRule rule in rules) {

                    string strResult = "";

                    int index = rule.Update(symbol);

                    switch (index) {
                        //символ не принадлежит правилу
                        case 0:

                            break;
                        //символ входит в правило
                        case 1:

                            activeRule = rule;
                            activeQueue.Enqueue(symbol);
                            break;

                        //символ является завершающим в правиле
                        case 2:

                            break;
                    }    
                }
            }
            else {

                int index = activeRule.Update(symbol);

                switch (index) {
                    //символ не принадлежит правилу
                    case 0:

                        activeRules.Remove(activeRule);
                        break;
                    //символ входит в правило
                    case 1:

                        activeQueue.Enqueue(symbol);
                        break;

                    //символ является завершающим в правиле
                    case 2:
                        
                        activeQueue.Clear();
                        break;
                }   
            }
                if (flag)
                    recognitionResult += tmp;  
        }

        //создать матрицу 16*16 для картинки с конкретным символом
        public static byte[,] CreateMatrix_16X16(BinarizedImage binarizedImage) {

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

            for (int j = yTop; j <= yBottom; j++)
                for (int i = xLeft; i <= xRight; i++)
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

            //ширина прямоугольника вокруг изображения
            int symbolWidth = xRight - xLeft;

            int[,] XY = new int[17, 2];
            XY[0, 0] = 0;
            XY[16, 0] = symbolWidth;
            XY[8, 0] = XY[16, 0] / 2;
            XY[4, 0] = XY[8, 0] / 2;
            XY[2, 0] = XY[4, 0] / 2;
            XY[1, 0] = XY[2, 0] / 2;
            XY[3, 0] = (XY[4, 0] + XY[2, 0]) / 2;
            XY[6, 0] = (XY[8, 0] + XY[4, 0]) / 2;
            XY[5, 0] = (XY[6, 0] + XY[4, 0]) / 2;
            XY[7, 0] = (XY[8, 0] + XY[6, 0]) / 2;
            XY[12, 0] = (XY[16, 0] + XY[8, 0]) / 2;
            XY[10, 0] = (XY[12, 0] + XY[8, 0]) / 2;
            XY[14, 0] = (XY[16, 0] + XY[12, 0]) / 2;
            XY[9, 0] = (XY[10, 0] + XY[8, 0]) / 2;
            XY[11, 0] = (XY[12, 0] + XY[10, 0]) / 2;
            XY[13, 0] = (XY[14, 0] + XY[12, 0]) / 2;
            XY[15, 0] = (XY[16, 0] + XY[14, 0]) / 2;

            //высота образа
            int symbolHeight = yBottom - yTop;
            XY[0, 1] = 0;
            XY[16, 1] = symbolHeight;
            XY[8, 1] = XY[16, 1] / 2;
            XY[4, 1] = XY[8, 1] / 2;
            XY[2, 1] = XY[4, 1] / 2;
            XY[1, 1] = XY[2, 1] / 2;
            XY[3, 1] = (XY[4, 1] + XY[2, 1]) / 2;
            XY[6, 1] = (XY[8, 1] + XY[4, 1]) / 2;
            XY[5, 1] = (XY[6, 1] + XY[4, 1]) / 2;
            XY[7, 1] = (XY[8, 1] + XY[6, 1]) / 2;
            XY[12, 1] = (XY[16, 1] + XY[8, 1]) / 2;
            XY[10, 1] = (XY[12, 1] + XY[8, 1]) / 2;
            XY[14, 1] = (XY[16, 1] + XY[12, 1]) / 2;
            XY[9, 1] = (XY[10, 1] + XY[8, 1]) / 2;
            XY[11, 1] = (XY[12, 1] + XY[10, 1]) / 2;
            XY[13, 1] = (XY[14, 1] + XY[12, 1]) / 2;
            XY[15, 1] = (XY[16, 1] + XY[14, 1]) / 2;
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
                            if (binarizedImageArray[i, j] == 1)
                                nSymbol += 1;

                    // если отношение кол-ва знач. пикселей к общему кол-ву в ящейке > характерного процента заполнения то = 1 иначе = 0
                    if (nSymbol / Math.Max(1, ((XY[ki + 1, 0] - XY[ki, 0]) * (XY[kj + 1, 1] - XY[kj, 1]))) > percent) 
                        result[kj, ki] = 1;
                    else
                        result[kj, ki] = 0;
                }
            }
            //==========================================================

            return result;
        }

        //получить границы символов на изображении с множеством символов
        private static List<Rectangle> GetSymbolsBoundaries(BinarizedImage binarizedImage) {

            List<Rectangle> symbolsBoundaries = new List<Rectangle>();

            byte[,] imageArray = binarizedImage.GetArray();
            int N = binarizedImage.GetWidth();
            int M = binarizedImage.GetHeight();

            //первый разделитель в промежтуке
            bool previousOnlyWhite = false;

            int Left = -1, Right = -1, Top = -1, Bottom =-1, Counter = 0; 

            for (int i = 0; i < N; ++i) {

                bool currentOnlyWhite = true;

                for (int j = 0; j < M; ++j) {

                    currentOnlyWhite &= imageArray[i, j] == 0;
                }
                //переход с белого на чёрный
                if (currentOnlyWhite == false) {

                    /*if (i == 0 || i == N - 1) {
                        resultDivivderIndexes.Add(i);
                    }*/
                    if (previousOnlyWhite == true) {
                        Counter += 1;
                        Left = i - 1;
                    }
                }
                //переход с чёрного на белый
                else {
                    //если первый столбец пикселей белый, значит мы не нашли изображение... пропускаем этот столбец
                    if (previousOnlyWhite == false && i != 0) {
                        Counter += 1;
                        Right = i;
                    }
                }
                previousOnlyWhite = currentOnlyWhite;

                if (Counter == 2) {

                    

                    Top = binarizedImage.GetHeight();
                    Bottom = -1;

                    for (int k = Left; k <= Right; ++k) {

                        for (int l = 0; l < binarizedImage.GetHeight(); ++l) {

                            if (imageArray[k, l] == 1) {
                                if (Top > l)
                                    Top = l;
                                if (Bottom < l)
                                    Bottom = l;
                            }
                        }
                    }

                    Counter = 0;

                    Console.WriteLine("l: "+Left+", \tr: "+Right+", \tt: "+Top+", \tb: "+Bottom);

                    symbolsBoundaries.Add(new Rectangle(Left, Top - 1, Right - Left + 1, Bottom - Top + 2));
                }
            }

            return symbolsBoundaries;
        }

        //устарело
        //-----------------------------------------------
        //получит список всех символов на изображении
        private static List<int> GetDividersX(BinarizedImage binarizedImage) {

            List<int> resultDivivderIndexes = new List<int>();

            byte[,] imageArray = binarizedImage.GetArray();
            int N = binarizedImage.GetWidth();
            int M = binarizedImage.GetHeight();

            //первый разделитель в промежтуке
            bool previousOnlyWhite = false;

            for (int i = 0; i < N; ++i) {

                bool currentOnlyWhite = true;

                for (int j = 0; j < M; ++j) {

                    currentOnlyWhite &= imageArray[i, j] == 0;
                }
                //переход с белого на чёрный
                if (currentOnlyWhite == false) {
                    /*if (i == 0 || i == N - 1) {
                        resultDivivderIndexes.Add(i);
                    }*/
                    if (previousOnlyWhite == true) {
                        resultDivivderIndexes.Add(i - 1);
                    }
                }
                //переход с чёрного на белый
                else {
                    //если первый столбец пикселей белый, значит мы не нашли изображение... пропускаем этот столбец
                    if (previousOnlyWhite == false && i != 0) {
                        resultDivivderIndexes.Add(i);
                    }
                }
                previousOnlyWhite = currentOnlyWhite;
            }
            return resultDivivderIndexes;
        }

        private static Tuple<int, int> GetDividersY(BinarizedImage binarizedImage, Tuple<int, int> xBoundaries) {

            byte[,] imageArray = binarizedImage.GetArray();

            int yStart = binarizedImage.GetHeight(), yEnd = -1;

            for (int i = xBoundaries.Item1; i <= xBoundaries.Item2; ++i) {

                for (int j = 0; j < binarizedImage.GetHeight(); ++j) {

                    if (imageArray[i, j] == 1) {
                        if (yStart > j)
                            yStart = j;
                        if (yEnd < j)
                            yEnd = j;
                    }
                }
            }

            return new Tuple<int, int>(yStart - 1, yEnd + 1);
            //return new Tuple<int, int>(0, ImageBitmap.Height - 1);
        }
        //-----------------------------------------------

        //получить список символов на картинке
        public static List<Symbol> GetSymbols(BinarizedImage binarizedImage) {

            List<Symbol> symbols = new List<Symbol>();

            List<Rectangle> symbolsBoundaries = GetSymbolsBoundaries(binarizedImage);

            foreach(Rectangle rect in symbolsBoundaries) {

                //Console.WriteLine("DividerX: "+dividerX.Item1+", "+dividerX.Item2);
                //Console.WriteLine("DividerY: " + dividerY.Item1 + ", " + dividerY.Item2);

                BinarizedImage image = new BinarizedImage(binarizedImage, rect.Left, rect.Right-1, rect.Top, rect.Bottom);

                Symbol symbol = new Symbol(rect.Left, rect.Top, rect.Width, rect.Height, CreateMatrix_16X16(image));

                symbols.Add(symbol);
            }

            return symbols;
        }
    }
}
