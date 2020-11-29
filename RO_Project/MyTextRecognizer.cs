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
    public class MyTextRecognizer {

        string[] upCaseLetters = new string[]
        {
            "A","B","C","D","E","F","G","H","I","J","K","L","M","N","O","P","Q","R","S","T","U","V","W","X","Y","Z"
        };
        string[] downCaseLetters = new string[]
        {
            "a","b","c","d","e","f","g","h","i","j","k","l","m","n","o","p","q","r","s","t","u","v","w","x","y","z"
        };
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

        //типы символов (название папок, в которых лежат эталоны)
        public enum SymbolTypes
        {
            expression = 0,
            greek = 1,
            lowerLetters = 2,
            upperLetters = 3,
            math = 4
        }

        //конструктор
        public MyTextRecognizer(string _etalonArraysTxtPath, string _rulesDirectoryPath) {

            etalonArraysTxtPath = _etalonArraysTxtPath;
            rulesDirectoryPath = _rulesDirectoryPath;
            

            recognitionResult = "";
        }

        private bool ExtraSymbolChecker(RealSymbol realSymbol)
        {
            string meaning = realSymbol.GetMeaning();
            return (meaning == "summator" || meaning == "integral");
        }
        private void UniteWithExtraSymbol(ref List<RealSymbol> _realSymbols)
        {
            List<RealSymbol> extraSymbols = new List<RealSymbol>();
            List<RealSymbol> preparedSymbols = new List<RealSymbol>();
            for (int i = _realSymbols.Count - 1; i >= 0; --i)
            {
                _realSymbols[i].SetMeaning(RecognizeSymbol(_realSymbols[i]).Item1, RecognizeSymbol(_realSymbols[i]).Item2);
                if (ExtraSymbolChecker(_realSymbols[i]))
                {
                    extraSymbols.Add(_realSymbols[i]);
                    _realSymbols.RemoveAt(i);
                }
            }
            _realSymbols.Reverse();
            Dictionary<int, List<RealSymbol>> extrSymbolDictionary = new Dictionary<int, List<RealSymbol>>();
            for(int i=0; i< extraSymbols.Count; ++i)
            {
                extrSymbolDictionary.Add(i, new List<RealSymbol>());
            }
            for (int i = _realSymbols.Count - 1; i >= 0; --i)
            {
                Rectangle particleBounds = _realSymbols[i].GetRealBounds();
                int myFatherSymbolIndex = -1;
                double minDistance = double.MaxValue;
                for (int j = extraSymbols.Count - 1; j >= 0; --j)
                {
                    Rectangle extraSymbolBounds = extraSymbols[j].GetRealBounds();
                    //что-то над сумматором или под ним
                    if (particleBounds.Bottom < extraSymbolBounds.Top && particleBounds.Top < extraSymbolBounds.Top || particleBounds.Top > extraSymbolBounds.Bottom && particleBounds.Bottom > extraSymbolBounds.Bottom)
                    {
                        double distnace = Math.Sqrt(Math.Pow((extraSymbolBounds.Top - particleBounds.Top), 2) + Math.Pow((extraSymbolBounds.Left - particleBounds.Left), 2));
                        if (distnace < minDistance)
                        {
                            minDistance = distnace;
                            myFatherSymbolIndex = j;
                        }
                    }
                }

                if (myFatherSymbolIndex != -1)
                {
                    extrSymbolDictionary[myFatherSymbolIndex].Add(_realSymbols[i]);
                    _realSymbols.RemoveAt(i);
                }
            }
            for(int i=0; i<extraSymbols.Count; ++i)
            {
                foreach (var realSymbol in extrSymbolDictionary[i] )
                {
                    extraSymbols[i] = RealSymbol.SumSymbols(extraSymbols[i], realSymbol);
                }
                
            }
            //объединяем всё, что осталось с тем, что относилось к сумматору
            _realSymbols.AddRange(extraSymbols);
        }

        private void UniteWithInetrsectingSymbol (ref List<RealSymbol> _realSymbols)
        {
            for (int i = _realSymbols.Count - 1; i >= 0; --i)
            {
                List<int> intersectingIndexes = new List<int>();
                double minDistance = double.MaxValue;
                for (int j = _realSymbols.Count - 1; j >= 0; --j)
                {
                    if(i!=j)
                    {
                        Rectangle second = _realSymbols[j].GetRealBounds();
                        //что-то над сумматором или под ним
                        if (_realSymbols[i].GetRealBounds().Left <= second.Right && _realSymbols[i].GetRealBounds().Left >= second.Left ||
                            _realSymbols[i].GetRealBounds().Right <= second.Right && _realSymbols[i].GetRealBounds().Right >= second.Left ||
                            _realSymbols[i].GetRealBounds().Left <= second.Left && _realSymbols[i].GetRealBounds().Right >= second.Right ||
                            _realSymbols[i].GetRealBounds().Left >= second.Left && _realSymbols[i].GetRealBounds().Right <= second.Right)
                        {
                            _realSymbols[i] = RealSymbol.SumSymbols(_realSymbols[i], _realSymbols[j]);
                            _realSymbols.RemoveAt(j);
                            break;
                        }
                    }
                  
                }
            }
        }

        public void Start(Bitmap imageBitmap)
        {
            ReadEtalons(etalonArraysTxtPath);
            ReadRules(rulesDirectoryPath);
            List<RealSymbol> realSymbols = RealSymbol.GetRealSymbols(imageBitmap);
            foreach(var realSymbol in realSymbols)
            {
                realSymbol.SetMeaning(RecognizeSymbol(realSymbol).Item1, RecognizeSymbol(realSymbol).Item2);
            }
            UniteWithExtraSymbol(ref realSymbols);
            UniteWithInetrsectingSymbol(ref realSymbols);

            List<PrimalSymbol> primalSymbols = new List<PrimalSymbol>();
            foreach (var realSymbol in realSymbols)
            {
                primalSymbols.Add(new PrimalSymbol(realSymbol));
            }
           SortPrimalSymbols(ref primalSymbols);

            foreach(var primalSymbol in primalSymbols)
            {
                Tuple<string,string> recognizeResult = Recognize(primalSymbol);
                primalSymbol.SetMeaning(recognizeResult.Item1, recognizeResult.Item2);
            }
            recognitionResult += RuleChecking(primalSymbols).Item1;
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
                    etalonSymbols.Add(new EtalonSymbol(array, Path.GetFileName(filePath).Replace(".txt", ""), Path.GetFileName(directory)));
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
        public Tuple<string,string> RecognizeSymbol(RealSymbol symbol)
        {
            //минимальное отклонение от эталона
            double minDelta = int.MaxValue;
            string resultItem1 = "[ERROR_MARK]";
            string resultItem2 = "[ERROR_TYPE]";
            foreach (EtalonSymbol etalonSymbol in etalonSymbols)
            {
                double delta = GetDelta(etalonSymbol, symbol);
                if (delta < minDelta)
                {
                    minDelta = delta;
                    resultItem1 = etalonSymbol.Mark;
                    resultItem2 = etalonSymbol.Type;
                }
            }
            for(int i=0; i< upCaseLetters.Length; ++i)
            {
                if(resultItem1 == upCaseLetters[i])
                {
                    resultItem1 = downCaseLetters[i];
                    break;
;               }
            }
            //if (ResizeWidth * ResizeHeight / 4.0 <= minDelta)
              //  result = "";
            return new Tuple<string, string>(resultItem1, resultItem2); ;
        }

        private static bool isWhiteColor(Color currColor)
        {
            return (currColor.R > 215 && currColor.G > 215 && currColor.B > 215);
        }

        public Tuple<string,string> RuleChecking(List<PrimalSymbol> primalSymbols)
        {
            string result = "";
            string resultType = "expression";
            if (primalSymbols.Count==1)
            {
                resultType = primalSymbols[0].GetType();
            }
           
            //составили массив: i-ый элемент которого содержит r -номер правила, на котором остановился этот i-ый символ, изначально массив инициализирован 0-ми
            int[] indexCorrelation = new int[primalSymbols.Count];
            //активное правило - правило, которое сейчас пытается "собраться"
            IRule activeRule = null;
            //lastSavedSymbolIndex - индекс первого "обработанного не по правилу символа"
            int lastSavedSymbolIndex = 0;

            bool succesfulRecognition = false;
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
                        succesfulRecognition = true;
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
                            succesfulRecognition = true;
                            activeRule = null;
                            break;

                    }
                }
                if(succesfulRecognition)
                {
                    foreach (var rule in rules)
                    {
                        rule.ClearStates();
                    }
                    succesfulRecognition = false;
                }
            }

            foreach (var rule in rules)
            {
                rule.ClearStates();
            }
            return new Tuple<string, string>(result, resultType);
        }

        private void SortPrimalSymbols(ref List<PrimalSymbol> listToSort)
        {
            LeftPrimalSymbolComparer<PrimalSymbol> primalSymbolComparer = new LeftPrimalSymbolComparer<PrimalSymbol>();
            listToSort.Sort(primalSymbolComparer);
        }
        //получить всевозможные символы внутри символа
        private delegate Tuple<bool, string,string> BodyConditions(RealSymbol particleSymbol, RealSymbol symbol);
        public List<PrimalSymbol> ProcessAndUniteTwoParticles(PrimalSymbol primalSymbol)
        {
            Tuple<bool, string, string> i_BodyCondition(RealSymbol particleSymbol, RealSymbol symbol)
            {

                bool boolRes =
                (symbol.GetMeaning() == "i_body" ||
                symbol.GetMeaning() == "I" ||
                symbol.GetMeaning() == "i")
                &&
                particleSymbol.GetMeaning() == "dot";
                return new Tuple<bool, string, string>(boolRes, "i", "lowerLetters");

            }

            Tuple<bool, string, string> j_BodyCondition(RealSymbol particleSymbol, RealSymbol symbol)
            {
                bool boolRes =
                (symbol.GetMeaning() == "j_body" ||
                symbol.GetMeaning() == "J" ||
                symbol.GetMeaning() == "j")
                &&
                particleSymbol.GetMeaning() == "dot";
                return new Tuple<bool, string, string>(boolRes, "j", "lowerLetters");
            }

            Tuple<bool, string, string> twoDot_BodyCondition(RealSymbol particleSymbol, RealSymbol symbol)
            {
                bool boolRes =
                symbol.GetMeaning() == "dot"
                &&
                particleSymbol.GetMeaning() == "dot";
                return new Tuple<bool, string, string>(boolRes, "twoDot", "math");
            }

            Tuple<bool, string, string> equation_BodyCondition(RealSymbol particleSymbol, RealSymbol symbol)
            {
                bool boolRes =
                symbol.GetMeaning() == "minus"
                &&
                particleSymbol.GetMeaning() == "minus";
                return new Tuple<bool, string, string>(boolRes, "equation", "math");
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
                    string newType = "";
                    int heightToMyParticle = int.MaxValue;

                    //цикл j для поиска particle (точки для 'i')
                    for (int j = realSymbols.Count - 1; j >= 0; --j)
                    {
                        Rectangle particleRectangle = realSymbols[j].GetRealBounds();
                        Point particleRectCenter = new Point(particleRectangle.Left + particleRectangle.Width / 2, particleRectangle.Top + particleRectangle.Height / 2);
                        if (i != j)
                        {
                            //точка сверху над телом i или j, или же вторая палка знака '=' лежит в моих границах 
                            if ((particleRectangle.Left >= symbolRectangle.Left && particleRectangle.Left <= symbolRectangle.Right ||
                                particleRectangle.Right >= symbolRectangle.Left && particleRectangle.Right <= symbolRectangle.Right) &&
                                heightToMyParticle > Math.Abs(symbolRectangle.Top - particleRectangle.Top + particleRectangle.Height - 1))
                            {
                                bool found = false;
                                for (int bRuleIndex = 0; bRuleIndex < bodyConditions.Count(); ++bRuleIndex)
                                {
                                    Tuple<bool, string, string> res = bodyConditions[bRuleIndex](realSymbols[j], realSymbols[i]);
                                    found |= res.Item1;
                                    if (res.Item1)
                                    {
                                        newMeaning = res.Item2;
                                        newType = res.Item3;
                                    }
                                        
                                }

                                if (found)
                                {
                                    myParticle = realSymbols[j];
                                    heightToMyParticle = Math.Abs(symbolRectangle.Top - particleRectangle.Top + particleRectangle.Height - 1);
                                }
                            }
                        }
                    }
                    

                    if (myParticle != null)
                    {
                       
                        result.Add(new PrimalSymbol(RealSymbol.SumSymbols(bodyParticle, myParticle), newMeaning, newType));
                        //найденную частичку и своё тело удалим из списка реальных символов
                        realSymbols.Remove(bodyParticle);
                        realSymbols.Remove(myParticle);
                        doneCounter -= 2;
                        //выходим из цикла по i
                        break;
                    }
                    else
                    {
                        result.Add(new PrimalSymbol(bodyParticle, bodyParticle.GetMeaning(), bodyParticle.GetType()));
                        realSymbols.Remove(bodyParticle);
                        doneCounter -= 1;
                    }

                }
            }
          
            SortPrimalSymbols(ref result);
            return result;
        }


        public Tuple<string,string> Recognize(PrimalSymbol primalSymbol)
        {
            string recognitionResult = "";
            string typeResult = "expression";

            List<RealSymbol> realSymbols = new List<RealSymbol>(primalSymbol.GetRealSymbols());

            RealSymbol extrsSymbol = null;
            //присваиваем всем символам внутри "символа" метки
            foreach (var realSymbol in realSymbols)
            {
                realSymbol.SetMeaning(RecognizeSymbol(realSymbol).Item1, RecognizeSymbol(realSymbol).Item2);
                if (ExtraSymbolChecker(realSymbol))
                {
                    extrsSymbol = realSymbol;
                }
            }
            realSymbols.Remove(extrsSymbol);

            if (extrsSymbol == null) {
                if (primalSymbol.GetRealSymbols().Count > 1) {
                    List<PrimalSymbol> myInnerSymbols = ProcessAndUniteTwoParticles(primalSymbol);
                    Tuple<string, string> res = RuleChecking(myInnerSymbols);
                    recognitionResult += res.Item1;
                    typeResult = res.Item2;
                }
                else if (primalSymbol.GetRealSymbols().Count == 1) {
                    recognitionResult += primalSymbol.GetRealSymbols()[0].GetMeaning();
                    typeResult = primalSymbol.GetRealSymbols()[0].GetType();
                }
                else if (primalSymbol.GetRealSymbols().Count == 0) {
                    recognitionResult += "ERROR_MEANING";
                    typeResult = "ERROR_TYPE";
                }
            }
            else {
                
                    List<RealSymbol> Up = new List<RealSymbol>();
                    List<RealSymbol> Down = new List<RealSymbol>();
                    foreach (var symbol in realSymbols) {
                        if (symbol.GetRealBounds().Bottom < extrsSymbol.GetRealBounds().Top) {
                            Up.Add(symbol);
                        }
                        else {
                            Down.Add(symbol);
                        }
                    }
                    if (Up.Count == 0 || Down.Count == 0) {
                        recognitionResult += "нераспознаваемый сумматор, нет либо верхней, либо нижней границы";
                        //return new Tuple<string, string>(recognitionResult,typeResult);
                    }
                if (Up.Count > 0 && Down.Count > 0) {

                    RealSymbol upRes = Up[0];
                    for (int i = 1; i < Up.Count; ++i) {
                        upRes = RealSymbol.SumSymbols(upRes, Up[i]);
                    }
                    RealSymbol downRes = Down[0];
                    for (int i = 1; i < Down.Count; ++i) {
                        downRes = RealSymbol.SumSymbols(downRes, Down[i]);
                    }
                    switch (extrsSymbol.GetMeaning()) {
                        case "summator":
                            recognitionResult += "сумма с " + Recognize(new PrimalSymbol(downRes)).Item1 + " по " + Recognize(new PrimalSymbol(upRes)).Item1 + " для ";
                            break;
                        case "integral":
                            recognitionResult += "интеграл в интервале с " + Recognize(new PrimalSymbol(downRes)).Item1 + " до " + Recognize(new PrimalSymbol(upRes)).Item1;
                            break;
                    }

                }
            }

            return new Tuple<string, string>(recognitionResult, typeResult);
        }
    }
}
