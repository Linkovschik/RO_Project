﻿using System;
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

            

            recognitionResult = "";
        }

        public void Start(Bitmap imageBitmap)
        {
            ReadEtalons(etalonArraysTxtPath);
            ReadRules(rulesDirectoryPath);
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

        //считать правила
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

        //распознать символ (сравнение с эталоном)
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

        public List<Symbol> GetSymbols(Bitmap imageBitMap)
        {
            //сначала получим настоящие символы, т.е. с реальными биткартами и границами
            List<RealSymbol> realSymbols = new List<RealSymbol>();
            for(int i=0; i< imageBitMap.Width; ++i)
            {
                for(int j=0; j< imageBitMap.Height; ++j)
                {
                    Point pixel = new Point(i, j);
                    if(!isWhiteColor(imageBitMap.GetPixel(i,j)) && !IsPixelBelongsToSymbol(realSymbols, pixel))
                    {
                        RealSymbol realSymbol = new RealSymbol(pixel, imageBitMap);
                        realSymbols.Add(realSymbol);
                    }
                }
            }

            //достройка символов i,j,=,; и т.п.
            RealSymbol.UniteRealSymbols(ref realSymbols);

            //теперь можно на основе реальных данных создать символы для распознавания
            List<Symbol> result = new List<Symbol>();
            foreach (RealSymbol realSymbol in realSymbols)
            {
                result.Add(new Symbol(realSymbol, ResizeWidth, ResizeHeight));
            }
            return result;
        }

        private bool IsPixelBelongsToSymbol(List<RealSymbol> symbols, Point pixel)
        {
            foreach (var symbol in symbols)
            {
                if (symbol.GetRealBounds().Contains(pixel))
                    return true;
            }
            return false;
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
            double[,] result = new double[arrays[0].GetLength(0), arrays[0].GetLength(1)];

            //записываем сумму в результат
            foreach (var array in arrays)
            {

                for (int i = 0; i < array.GetLength(0); ++i)
                {
                    for (int j = 0; j < array.GetLength(1); ++j)
                    {
                        result[i, j] += array[i, j];
                        //Console.Write(array[i, j] + " ");
                    }
                    //Console.Write("\n");
                }

            }

            //делим на количество
            for (int i = 0; i < result.GetLength(0); ++i)
                for (int j = 0; j < result.GetLength(1); ++j)
                {
                    result[i, j] /= (double)arrays.Count;
                }

            return result;
        }
    }
}
