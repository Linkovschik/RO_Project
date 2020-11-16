using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RO_Project {
    public abstract class IRule {
        public enum Result
        {
            NotBelong = 0,
            Belong = 1,
            End = 2
        }
        public abstract int Update(Symbol symbol);
        public abstract string GetMeaning();
    }

    public class TrigonomRule : IRule
    {

        List<string> marksList;

        string meaning;

        int currentState;
        int countSymbols;

        public TrigonomRule(string filePath) {
            marksList = new List<string>();
            meaning = "NaN";
            currentState = 0;

            using (StreamReader sr = new StreamReader(filePath)) {

                countSymbols = Int32.Parse(sr.ReadLine());

                for (int i = 0; i < countSymbols; i++) {

                    marksList.Add(sr.ReadLine());
                }

                meaning = sr.ReadLine();
            }
        }

        public override int Update(Symbol symbol)
        {
            int result = -1;
            //если по правилу
            if(symbol.Mark == marksList[currentState])
            {
                currentState += 1;
                result = (int)Result.Belong;

                //если правило собралось и это конец
                if (currentState == marksList.Count)
                {
                    result = (int)Result.End;
                    //возвращаем правило в начсальное состояние
                    currentState = 0;
                }
            }
            //если не по правилу
            else
            {
                result = (int)Result.NotBelong;
                //возвращаем правило в начальное состояние
                currentState = 0;
            }
            return result;
        }

        public override string GetMeaning()
        {
            return meaning;
        }

    }

    public class IndexRule : IRule
    {
        string meaning;
        private Symbol mainSymbol;
        int currentState;

        public IndexRule()
        {
            meaning = "-ый";
            currentState = 0;
        }


        public override string GetMeaning()
        {
            return meaning;
        }

        public override int Update(Symbol symbol)
        {
            int result = -1;
            if(currentState==0)
            {
                mainSymbol = symbol;
                currentState += 1;
                result = (int)Result.Belong;
            }
            else if(currentState==1)
            {
                if (mainSymbol.GetRectangle().Top + mainSymbol.GetRectangle().Height / 2 <= symbol.GetRectangle().Top)
                {
                    meaning = mainSymbol.Mark + " " + symbol.Mark + meaning;
                    currentState = 0;
                    result = (int)Result.End;
                }
                else
                {
                    currentState = 0;
                    result = (int)Result.NotBelong;
                }
                     
            }
            return result;
        }
    }
}
