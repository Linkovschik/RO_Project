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
    public class MyTextRecognizer {

        //список с матрицами(эталонами) для каждого символа
        List<EtalonSymbol> etalonSymbols;
        //список правил
        List<IRule> rules;

        private string etalonArraysTxtPath;
        private string rulesDirectoryPath;

        public static int ResizeWidth = 32;
        public static int ResizeHeight = 32;

        //результат распознавания
        private string recognitionResult;

        //конструктор
        public MyTextRecognizer(string _etalonArraysTxtPath, string _rulesDirectoryPath) {

            etalonArraysTxtPath = _etalonArraysTxtPath;
            rulesDirectoryPath = _rulesDirectoryPath;
            

            recognitionResult = "";
        }

        public void Start(Bitmap imageBitmap)
        {
            ReadEtalons(etalonArraysTxtPath);
            ReadRules(rulesDirectoryPath);
            List<PrimalSymbol> primalSymbols = PrimalSymbol.GetPrimalSymbols(imageBitmap);
            Console.WriteLine("Символы: ======================");
            foreach(var primalSymbol in primalSymbols)
            {
                primalSymbol.PrintRealBitMap();
                primalSymbol.SetMeaning(Recognize(primalSymbol));
            }
            recognitionResult += RuleChecking(primalSymbols);
        }

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


        private static double GetDelta(EtalonSymbol etalon, RealSymbol symbol)
        {
            byte[,] array = symbol.GetByteArray();
            double delta = 0;

            for (int j = 0; j < array.GetLength(0); j++)
                for (int i = 0; i < array.GetLength(1); i++)
                    delta += Math.Abs(array[j, i] - etalon.array[j, i]);

            return delta;
        }
        //распознать символ (сравнение с эталоном)
        public string RecognizeSymbol(RealSymbol symbol)
        {
            string result = "";
            //минимальное отклонение от эталона
            double minDelta = int.MaxValue;
            foreach (EtalonSymbol etalonSymbol in etalonSymbols)
            {
                double delta = GetDelta(etalonSymbol, symbol);
                if (delta < minDelta)
                {
                    minDelta = delta;
                    result = etalonSymbol.Mark;
                }
            }
            if (ResizeWidth * ResizeHeight / 4.0 <= minDelta)
                result = "";
            return result;
        }

        private static bool isWhiteColor(Color currColor)
        {
            return (currColor.R > 215 && currColor.G > 215 && currColor.B > 215);
        }

        public string RuleChecking(List<PrimalSymbol> primalSymbols)
        {
            string result = "";
            //составили массив: i-ый элемент которого содержит r -номер правила, на котором остановился этот i-ый символ, изначально массив инициализирован 0-ми
            int[] indexCorrelation = new int[primalSymbols.Count];
            //активное правило - правило, которое сейчас пытается "собраться"
            IRule activeRule = null;
            //lastSavedSymbolIndex - индекс первого "обработанного не по правилу символа"
            int lastSavedSymbolIndex = 0;

            for (int currentSymbolIndex = 0; currentSymbolIndex < primalSymbols.Count; ++currentSymbolIndex)
            {
                bool writeMyMark = true;
                if (activeRule == null)
                {
                    for (int currentRuleIndex = indexCorrelation[currentSymbolIndex]; currentRuleIndex < rules.Count; ++currentRuleIndex)
                    {

                        int updateResult = rules[currentRuleIndex].Update(primalSymbols[currentSymbolIndex]);
                        switch (updateResult)
                        {
                            //если нет никаких правил с этим символом
                            case (int)IRule.Result.NotBelong:
                                break;
                            case (int)IRule.Result.Belong:
                                if (currentSymbolIndex != primalSymbols.Count - 1)
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
                        result += primalSymbols[currentSymbolIndex].GetMeaning() + ((currentSymbolIndex!= primalSymbols.Count-1)?" ":"");
                    }

                }
                //сюда прихожу только тогда, когда нашлось активное правило
                else
                {
                    int updateResult = activeRule.Update(primalSymbols[currentSymbolIndex]);
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
                            if (currentSymbolIndex != primalSymbols.Count - 1)
                            {
                                //тут как бы ничего не делаем, поскольку просто прошагиваем очередной символ в функции (шли по cat, прошли 'c', 'a,' и идём дальше... (всё делается в update())
                            }
                            break;
                        //если мы дошли до конца актиивного правила, то нужно активное правило "сбросить", а в результат добавить его интерпретацию
                        case (int)IRule.Result.End:
                            result += activeRule.GetMeaning() + ((currentSymbolIndex != primalSymbols.Count - 1) ? " " : "");
                            activeRule = null;
                            break;

                    }
                }
            }
            foreach(var rule in rules)
            {
                rule.ClearStates();
            }
            return result;
        }

        private void SortPrimalSymbols(ref List<PrimalSymbol> listToSort, Rectangle primalSymbolRectangle)
        {
            PrimalSymbolComparer<PrimalSymbol> primalSymbolComparer = new PrimalSymbolComparer<PrimalSymbol>(primalSymbolRectangle);
            listToSort.Sort(primalSymbolComparer);
        }
        //получить всевозможные символы внутри символа
        private delegate Tuple<bool, string> BodyConditions(RealSymbol particleSymbol, RealSymbol symbol);
        public List<PrimalSymbol> ProcessAndUniteTwoParticles(PrimalSymbol primalSymbol)
        {
            Tuple<bool, string> i_BodyCondition(RealSymbol particleSymbol, RealSymbol symbol)
            {

                bool boolRes =
                (symbol.GetMeaning() == "i_body" ||
                symbol.GetMeaning() == "I" ||
                symbol.GetMeaning() == "l")
                &&
                particleSymbol.GetMeaning() == "dot";
                return new Tuple<bool, string>(boolRes, "i");

            }

            Tuple<bool, string> j_BodyCondition(RealSymbol particleSymbol, RealSymbol symbol)
            {
                bool boolRes =
                (symbol.GetMeaning() == "j_body" ||
                symbol.GetMeaning() == "J")
                &&
                particleSymbol.GetMeaning() == "dot";
                return new Tuple<bool, string>(boolRes, "j");
            }

            Tuple<bool, string> twoDot_BodyCondition(RealSymbol particleSymbol, RealSymbol symbol)
            {
                bool boolRes =
                symbol.GetMeaning() == "dot"
                &&
                particleSymbol.GetMeaning() == "dot";
                return new Tuple<bool, string>(boolRes, "twoDot");
            }

            Tuple<bool, string> equation_BodyCondition(RealSymbol particleSymbol, RealSymbol symbol)
            {
                bool boolRes =
                symbol.GetMeaning() == "minus"
                &&
                particleSymbol.GetMeaning() == "minus";
                return new Tuple<bool, string>(boolRes, "equation");
            }

            BodyConditions[] bodyConditions = new BodyConditions[4];
            bodyConditions[0] = i_BodyCondition;
            bodyConditions[1] = j_BodyCondition;
            bodyConditions[2] = twoDot_BodyCondition;
            bodyConditions[3] = equation_BodyCondition;

            List<PrimalSymbol> result = new List<PrimalSymbol>();
            List<RealSymbol> realInSymbols = primalSymbol.GetRealSymbols();

            List<RealSymbol> realSymbols = new List<RealSymbol>(realInSymbols);
           
            realSymbols.Reverse();

            //done - количество обработанных символов
            int doneCounter = realSymbols.Count;
            //пока не пройдём все символы
            while (doneCounter > 0)
            {
                for (int i = realSymbols.Count - 1; i >= 0; --i)
                {
                    //будем искать мою часть, но искать с минимальной высостой до неё. 
                    Rectangle symbolRectangle = realSymbols[i].GetRealBounds();
                    RealSymbol myParticle = null;
                    RealSymbol bodyParticle = realSymbols[i];

                    string newMeaning = "";
                    int heightToMyParticle = int.MaxValue;

                    //цикл j для поиска particle (точки для 'i')
                    for (int j = realSymbols.Count - 1; j >= 0; --j)
                    {
                        Rectangle particleRectangle = realSymbols[j].GetRealBounds();
                        Point particleRectCenter = new Point(particleRectangle.Left + particleRectangle.Width / 2, particleRectangle.Top + particleRectangle.Height / 2);
                        if (i != j)
                        {
                            //точка сверху над телом i или j, или же вторая палка знака '=' лежит в моих границах 
                            if ((particleRectangle.Left > symbolRectangle.Left && particleRectangle.Left < symbolRectangle.Right ||
                                particleRectangle.Right > symbolRectangle.Left && particleRectangle.Right < symbolRectangle.Right) &&
                                symbolRectangle.Top > particleRectangle.Top &&
                                heightToMyParticle > (symbolRectangle.Top - particleRectangle.Top + particleRectangle.Height - 1))
                            {
                                bool found = false;
                                for (int bRuleIndex = 0; bRuleIndex < bodyConditions.Count(); ++bRuleIndex)
                                {
                                    Tuple<bool, string> res = bodyConditions[bRuleIndex](realSymbols[j], realSymbols[i]);
                                    found |= res.Item1;
                                    if (res.Item1)
                                        newMeaning = res.Item2;
                                }

                                if (found)
                                {
                                    myParticle = realSymbols[j];
                                    heightToMyParticle = (symbolRectangle.Top - particleRectangle.Top + particleRectangle.Height - 1);
                                }
                            }
                        }
                    }
                    

                    if (myParticle != null)
                    {
                       
                        result.Add(new PrimalSymbol(RealSymbol.SumSymbols(bodyParticle, myParticle), newMeaning));
                        //найденную частичку и своё тело удалим из списка реальных символов
                        realSymbols.Remove(bodyParticle);
                        realSymbols.Remove(myParticle);
                        doneCounter -= 2;
                        //выходим из цикла по i
                        break;
                    }
                    else
                    {
                        result.Add(new PrimalSymbol(bodyParticle, bodyParticle.GetMeaning()));
                        realSymbols.Remove(bodyParticle);
                        doneCounter -= 1;
                    }

                }
            }
          
            SortPrimalSymbols(ref result, primalSymbol.GetRealBoundaries());
            return result;
        }

        public string Recognize(PrimalSymbol primalSymbol)
        {
            string recognitionResult = "";

            List<RealSymbol> realSymbols = new List<RealSymbol>(primalSymbol.GetRealSymbols());

            //присваиваем всем символам внутри "символа" метки
            foreach (var realSymbol in realSymbols)
            {
                realSymbol.SetMeaning(RecognizeSymbol(realSymbol));
            }

            if (primalSymbol.GetRealSymbols().Count > 1) {
                List<PrimalSymbol> myInnerSymbols = ProcessAndUniteTwoParticles(primalSymbol);
                recognitionResult += RuleChecking(myInnerSymbols);
            }
            else if (primalSymbol.GetRealSymbols().Count == 1)
            {
                recognitionResult += primalSymbol.GetRealSymbols()[0].GetMeaning();
            }
            else if (primalSymbol.GetRealSymbols().Count == 0)
            {
                recognitionResult += "ERROR ";
            }

            return recognitionResult;
        }
    }
}
