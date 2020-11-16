using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RO_Project {
    class MyTextRecognizer {

        //список с матрицами(эталонами) для каждого символа
        List<EtalonSymbol> etalonSymbols;
        //список правил
        List<IRule> rules;

        private string etalonArraysTxtPath;
        private string rulesDirectoryPath;

        //результат распознавания
        private string recognitionResult;

        //конструктор
        public MyTextRecognizer(string _etalonArraysTxtPath, string _rulesDirectoryPath) {

            etalonArraysTxtPath = _etalonArraysTxtPath;
            rulesDirectoryPath = _rulesDirectoryPath;
            
            ReadEtalons(etalonArraysTxtPath);
            ReadRules(rulesDirectoryPath);

            recognitionResult = "";
        }

        public void Start(Bitmap imageBitmap)
        {
            string result = "";
            //получили все символы
            List<Symbol> symbols = GetSymbols(imageBitmap);

            //теперь присвоим всем символам их "метки"
            foreach (Symbol symbol in symbols)
            {
                RecognizeSymbol(symbol);
            }

            //и, наконец, приступим к обработке по правилам:

            //составили массив: i-ый элемент которого содержит r -номер правила, на котором остановился этот i-ый символ, изначально массив инициализирован 0-ми
            int[] indexCorrelation = new int[symbols.Count];
            //активное правило - правило, которое сейчас пытается "собраться"
            IRule activeRule = null;
            //lastSavedSymbolIndex - индекс первого "обработанного не по правилу символа"
            int lastSavedSymbolIndex = 0;

            for (int currentSymbolIndex = 0; currentSymbolIndex < symbols.Count; ++currentSymbolIndex)
            {
                bool writeMyMark = true;
                if (activeRule == null)
                {
                    for (int currentRuleIndex = indexCorrelation[currentSymbolIndex]; currentRuleIndex < rules.Count; ++currentRuleIndex)
                    {

                        int updateResult = rules[currentRuleIndex].Update(symbols[currentSymbolIndex]);
                        switch (updateResult)
                        {
                            //если нет никаких правил с этим символом
                            case (int)IRule.Result.NotBelong:
                                break;
                            case (int)IRule.Result.Belong:
                                if(currentSymbolIndex!= symbols.Count-1)
                                {
                                    //запишем ссылку на активное правило
                                    activeRule = rules[currentRuleIndex];
                                    //сохраним индекс места, куда нужно будет вернуться в случае неудачи (на 1 символ раньше, поскольку всё равно в цикле перешагнём к следующему) 
                                    lastSavedSymbolIndex = currentSymbolIndex - 1;
                                    //сохраним индекс правила, к которму вернёмся для этого символа в случае неудачи
                                    indexCorrelation[currentSymbolIndex] = currentRuleIndex + 1;
                                }
                                break;
                            //случай когда  в функции 1 символ - маловероятен, но, если его допустить, то нужно сразу дать ответ и идти дальше
                            case (int)IRule.Result.End:
                                activeRule = rules[currentRuleIndex];
                                //раз символ один, значит и правило выполнилось, вернём тогда результат, чтобы записать, но сделаем это в следующий раз
                                indexCorrelation[currentSymbolIndex] = currentRuleIndex + 1;
                                currentSymbolIndex -= 1;
                                break;


                        }
                        //если нашлось правило, то будем идти по нему, другие нам не нужны, т.е. покидаем цикл по правилам
                        if (activeRule != null)
                            break;
                    }
                    if (activeRule == null)
                    {
                        //если я прошёл последнее правило и ни одно не сработало, то мне надо вывести свою метку
                        //выводим метку
                        result += symbols[currentSymbolIndex].Mark + " ";
                    }

                }
                //сюда прихожу только тогда, когда нашлось активное правило
                else
                {
                    int updateResult = activeRule.Update(symbols[currentSymbolIndex]);
                    switch (updateResult)
                    {
                        //и вот она, неудача, неполучилось распознать функцию
                        case (int)IRule.Result.NotBelong:
                            //откатываем индекс текущего символа на начало функции (шли по cat, подбирали cos, в итоге после 'c' откатываемся к 'c' снова, но на
                            //сей раз по правилам перешагиваем cos благодаря indexCorrelation
                            currentSymbolIndex = lastSavedSymbolIndex;
                            activeRule = null;
                            break;
                        case (int)IRule.Result.Belong:
                            if (currentSymbolIndex != symbols.Count - 1)
                            {
                                //тут как бы ничего не делаем, поскольку просто прошагиваем очередной символ в функции (шли по cat, прошли 'c', 'a,' и идём дальше... (всё делается в update())
                            }
                            break;
                        //если мы дошли до конца актиивного правила, то нужно активное правило "сбросить", а в результат добавить его интерпретацию
                        case (int)IRule.Result.End:
                            result += activeRule.GetMeaning() + " ";
                            activeRule = null;
                            break;

                    }
                }
            }

            recognitionResult += result;
        }

        //получить результат
        public string GetResult()
        {
            return recognitionResult;
        }

        //очистить результат
        public void ClearResult()
        {
            recognitionResult = "";
        }

        //считать эталоны
        private void ReadEtalons(string etalonArraysTxtPath)
        {
            etalonSymbols = new List<EtalonSymbol>();
            string[] directories = Directory.GetDirectories(etalonArraysTxtPath);
            foreach (string directory in directories)
            {
                string[] files = Directory.GetFiles(directory);
                foreach (string filePath in files)
                {
                    double[,] array = MyArraySerializer.DeserializeDoubleArray(new StreamReader(filePath), MyTextRecognizer.ResizeWidth, MyTextRecognizer.ResizeWidth);
                    etalonSymbols.Add(new EtalonSymbol(array, Path.GetFileName(filePath).Replace(".txt", "")));
                    Console.WriteLine(filePath);
                }
            }
        }

        //считать
        private void ReadRules(string rulesDirectoryPath) {

            string[] directories = Directory.GetDirectories(rulesDirectoryPath);
            rules = new List<IRule>();
            foreach (string directory in directories) {

                switch (Path.GetFileName(directory)) {
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

        //получить границы символов на изображении с множеством символов

        public static List<Symbol> GetSymbols(Bitmap imageBitmap)
        {

            List<Symbol> symbols = new List<Symbol>();

            List<Rectangle> symbolsBoundaries = GetSymbolsBoundaries(imageBitmap);

            foreach (Rectangle rect in symbolsBoundaries)
            {
                Symbol symbol = new Symbol(rect, CreateByteMatrix_16X16(GetImagePart(imageBitmap,rect)));

                symbols.Add(symbol);
            }

            return symbols;
        }

        private void RecognizeSymbol(Symbol symbol)
        {
            //минимальное отклонение от эталона
            double minDelta = int.MaxValue;
            string result = "";
            foreach (EtalonSymbol etalonSymbol in etalonSymbols)
            {
                double delta = symbol.GetDelta(etalonSymbol);
                if (delta < minDelta)
                {
                    minDelta = delta;
                    result = etalonSymbol.Mark;
                }
            }
            symbol.SetMark(result);
        }

        private static bool correlation(Color currColor)
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
                    currentOnlyWhite &= correlation(imageBitmap.GetPixel(i, j));
                }
                //переход с белого на чёрный
                if (currentOnlyWhite == false)
                {

                    if (i == 0 || i == N - 1)
                    {
                        Counter += 1;
                        Left = i;
                    }
                    if (previousOnlyWhite == true)
                    {
                        Counter += 1;
                        Left = i;
                    }
                }
                //переход с чёрного на белый
                else
                {
                    //если первый столбец пикселей белый, значит мы не нашли изображение... пропускаем этот столбец
                    if (previousOnlyWhite == false && i != 0)
                    {
                        Counter += 1;
                        Right = i;
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
                            if (!correlation(imageBitmap.GetPixel(k, l)))
                            {
                                if (Top > l)
                                    Top = l;
                                if (Bottom < l)
                                    Bottom = l;
                            }
                        }
                    }
                    Counter = 0;
                    Console.WriteLine("l: " + Left + ", \tr: " + Right + ", \tt: " + Top + ", \tb: " + Bottom);
                    symbolsBoundaries.Add(new Rectangle(Left, Top, Right - Left + 1, Bottom - Top + 1));
                }
            }
            return symbolsBoundaries;
        }

        private static Bitmap BitmapResize(Bitmap image, int Width, int Height)
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
            bmPhoto.SetResolution(image.HorizontalResolution,image.VerticalResolution);

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

        public static List<Bitmap> GetImageBitmaps(Bitmap imageBitmap)
        {
            List<Bitmap> segmantationResult = new List<Bitmap>();
            List<Rectangle> symbolsBoundaries = GetSymbolsBoundaries(imageBitmap);
            foreach (var rect in symbolsBoundaries)
            {
                segmantationResult.Add(GetImagePart(imageBitmap, rect));
            }
            return segmantationResult;
        }

        //создать матрицу 16*16 для картинки с конкретным символом
        public static byte[,] CreateByteMatrix_16X16(Bitmap bitmap)
        {
            byte[,] result = new byte[ResizeWidth, ResizeHeight];
            Bitmap resizedBitmap = BitmapResize(bitmap, ResizeWidth, ResizeHeight);
            for(int i=0; i< resizedBitmap.Width; ++i)
                for(int j=0; j<resizedBitmap.Height; ++j)
                {
                    if (correlation(resizedBitmap.GetPixel(i,j)))
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

        public static int ResizeWidth = 32;
        public static int ResizeHeight = 32;

        public static double[,] CreateDoubleMatrix_16X16(Bitmap bitmap)
        {
            double[,] result = new double[ResizeWidth, ResizeHeight];
            Bitmap resizedBitmap = BitmapResize(bitmap, ResizeWidth, ResizeHeight);

            for (int i = 0; i < resizedBitmap.Width; ++i)
                for (int j = 0; j < resizedBitmap.Height; ++j)
                {
                    if (correlation(resizedBitmap.GetPixel(i, j)))
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

    }
}
