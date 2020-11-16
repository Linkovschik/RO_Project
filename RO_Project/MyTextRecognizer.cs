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

        public int ResizeWidth { get; private set; }
        public int ResizeHeight { get; private set; }

        //результат распознавания
        private string recognitionResult;

        //конструктор
        public MyTextRecognizer(string _etalonArraysTxtPath, string _rulesDirectoryPath, int _ResizeWidth, int _ResizeHeight) {

            etalonArraysTxtPath = _etalonArraysTxtPath;
            rulesDirectoryPath = _rulesDirectoryPath;

            ResizeWidth = _ResizeWidth;
            ResizeHeight = _ResizeHeight;

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
                                if (currentSymbolIndex != symbols.Count - 1)
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
            if (ResizeWidth * ResizeHeight / 4.0 > minDelta)
                symbol.SetMark(result);
        }

        private bool correlation(Color currColor)
        {
            return (currColor.R > 215 && currColor.G > 215 && currColor.B > 215);
        }



        //запуск поиска в ширину для бнаружения границ символа
        



        

        

        private Symbol SumSymbols(Symbol bodySymbol, Symbol particleSymbol)
        {

        }

        public List<Symbol> GetSymbols(Bitmap imageBitMap)
        {
            List<RealSymbol> realSymbols = new List<RealSymbol>();
            for(int i=0; i< imageBitMap.Width; ++i)
            {
                for(int j=0; j< imageBitMap.Height; ++j)
                {
                    Point pixel = new Point(i, j);
                    if(!correlation(imageBitMap.GetPixel(i,j)) && !IsPixelBelongsToSymbol(realSymbols, pixel))
                    {
                        RealSymbol realSymbol = new RealSymbol(pixel, imageBitMap);
                        realSymbols.Add(realSymbol);
                        Bitmap symbolBitMap = GetImagePart(pixel, imageBitmap, realSymbolBounds);
                        symbolBitMap = MakeASquareBitmap(symbolBitMap);
                        symbolBitMap = BitmapResize(symbolBitMap, ResizeWidth, ResizeHeight);
                        symbols.Add(new Symbol(realSymbolBounds, symbolBitMap));
                    }
                }
            }


            //достройка i,j,=,: 
            List<int> indexesOfParticlesToClear = new List<int>();
            for (int i = 0; i < symbols.Count; ++i)
            {
                Rectangle symbolRectangle = symbols[i].GetRectangle();
                Point rectCenter = new Point(symbolRectangle.Left + symbolRectangle.Width / 2, symbolRectangle.Top + symbolRectangle.Height / 2);
                //цикл j для поиска particle (точки для 'i')
                for(int j=0; j< symbols.Count; ++j)
                {
                    Rectangle particleRectangle = symbols[j].GetRectangle();
                    if (i!=j)
                    {
                        //точка сверху над телом i или j, или же вторая палка знака '=' лежит в моих границах 
                        if (rectCenter.X> particleRectangle.Left && rectCenter.X < particleRectangle.Left + particleRectangle.Width-1 &&
                            //ищу так, чтобы моя координата по Y была ниже, чем то, что я ищу. Т.е. ищем для тела 'i' верхнюю точку, для '=' верхнюю палку и т.д.
                            symbolRectangle.Top> particleRectangle.Top)
                        {
                            indexesOfParticlesToClear.Add(j);
                            symbols[i] = SumSymbols(symbols[i], symbols[j]);
                        }
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



        //создать матрицу 16*16 для картинки с конкретным символом
        public byte[,] CreateByteMatrix_16X16(Bitmap bitmap)
        {
            byte[,] result = new byte[ResizeWidth, ResizeHeight];
            for(int i=0; i< bitmap.Width; ++i)
                for(int j=0; j< bitmap.Height; ++j)
                {
                    if (correlation(bitmap.GetPixel(i,j)))
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

        

    }
}
