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
            int tempCount = 0;
            foreach (Symbol symbol in symbols)
            {
                RecognizeSymbol(symbol);
                tempCount += 1;
                for (int i=0; i<symbol.array.GetLength(0) && tempCount==2; ++i)
                {
                    for(int j=0; j<symbol.array.GetLength(1); ++j)
                    {
                        Console.Write(symbol.array[i,j] + " ");
                    }
                    Console.Write("\n");
                }
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
                                if(currentSymbolIndex != symbols.Count-1)
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
                    double[,] array = MyArraySerializer.DeserializeDoubleArray(ResizeWidth, ResizeHeight, new StreamReader(filePath));
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
                string[] files = Directory.GetFiles(directory);
                foreach (string filePath in files)
                {
                    switch (Path.GetFileName(directory))
                    {
                        case "Trigonom":
                            TrigonomRule trigonomRule = new TrigonomRule(filePath);
                            rules.Add(trigonomRule);
                            break;
                    }
                    Console.WriteLine("rule: " + filePath);
                }
            }
            rules.Add(new IndexRule());
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
            if(ResizeWidth * ResizeHeight / 4.0 > minDelta)
                symbol.SetMark(result);
        }

        private bool correlation(Color currColor)
        {
            return (currColor.R > 215 && currColor.G > 215 && currColor.B > 215);
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

        //запуск поиска в ширину для бнаружения границ символа
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
                    symbolBitmap.SetPixel(current.X-rect.Left, current.Y - rect.Top, bitMap.GetPixel(current.X, current.Y));
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

        public List<Symbol> GetSymbols(Bitmap imageBitmap)
        {
            List<Symbol> symbols = new List<Symbol>();
            for(int i=0; i<imageBitmap.Width; ++i)
            {
                for(int j=0; j< imageBitmap.Height; ++j)
                {
                    Point pixel = new Point(i, j);
                    if(!correlation(imageBitmap.GetPixel(i,j)) && !IsPixelBelongsToSymbol(symbols,pixel))
                    {
                        Rectangle symbolBounds = StartBFSFromPixel(pixel, imageBitmap);
                        //Rectangle symbolBounds_1 = StartBFSFromPixel(pixel, imageBitmap);
                        Bitmap symbolBitMap = GetImagePart(pixel, imageBitmap, symbolBounds);
                        symbols.Add(new Symbol(symbolBounds, CreateByteMatrix_16X16(symbolBitMap), symbolBitMap));
                        Console.WriteLine("Мы вышли");
                    }
                }
            }
            return symbols;
        }

        private bool IsPixelBelongsToSymbol(List<Symbol> symbols, Point pixel)
        {
            foreach (var symbol in symbols)
            {
                if (symbol.GetRectangle().Contains(pixel))
                    return true;
            }
            return false;
        }

        public int ResizeWidth = 32;
        public int ResizeHeight = 32;

        //создать матрицу 16*16 для картинки с конкретным символом
        public byte[,] CreateByteMatrix_16X16(Bitmap bitmap)
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

        public double[,] CreateDoubleMatrix_16X16(Bitmap bitmap)
        {
            double[,] result = new double[ResizeWidth, ResizeHeight];
            Bitmap resizedBitmap;
            if (bitmap.Width!=ResizeWidth || bitmap.Height!=ResizeHeight)
            {
                resizedBitmap = BitmapResize(bitmap, ResizeWidth, ResizeHeight);
            }
            else
            {
                resizedBitmap = bitmap;
            }

            
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
