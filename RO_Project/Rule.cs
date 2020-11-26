using System;
using System.Collections.Generic;
using System.Drawing;
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
        public abstract int Update(PrimalSymbol symbol);
        public abstract string GetMeaning();
        public abstract void ClearStates();
    }
    
    public abstract class IConstructRule
    {
        public enum Result
        {
            NotBelong = 0,
            Belong = 1,
            End = 2
        }
        public abstract int Update(RealSymbol symbol, List<RealSymbol> realSymbols);
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

        public override int Update(PrimalSymbol symbol)
        {
            int result = -1;
            //если по правилу
            if(symbol.GetMeaning() == marksList[currentState])
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

        public override void ClearStates()
        {
            currentState = 0;
        }
    }

    public class IndexRule : IRule
    {
        string meaning;
        private PrimalSymbol mainSymbol;
        int currentState;
        Rectangle rectangle;

        public IndexRule()
        {
            meaning = "";
            currentState = 0;
        }

        public override void ClearStates()
        {
            currentState = 0;
        }

        public override string GetMeaning()
        {
            return meaning;
        }

        public override int Update(PrimalSymbol symbol)
        {
            //все математические символы не могут быть индексами
            if(symbol.GetType() == "math")
            {
                return (int)Result.NotBelong;
            }
            Rectangle symbolRectangle = symbol.GetRealBoundaries();
            int result = -1;
            if (currentState == 0)
            {
                currentState = 1;
                rectangle = new Rectangle(symbolRectangle.X, symbolRectangle.Y, symbolRectangle.Width, symbolRectangle.Height);
                mainSymbol = symbol;
                result = (int)Result.Belong;
            }
            else if (currentState == 1)
            {
                currentState = 0;
                int left = Math.Min(symbolRectangle.Left, rectangle.Left);
                int right = Math.Max(symbolRectangle.Right, rectangle.Right);
                int top = Math.Min(symbolRectangle.Top, rectangle.Top);
                int bottom = Math.Max(symbolRectangle.Bottom, rectangle.Bottom);
                rectangle = new Rectangle(left, top, right - left + 1, bottom - top + 1);

                Rectangle mainSymbolRectangle = mainSymbol.GetRealBoundaries();
                Point rectCenter = new Point(rectangle.Left + rectangle.Width / 2, rectangle.Top + rectangle.Height / 2);
             
                //если это 2 индекса на одном уровне (они в любом случае сначала будут распознаны в своём, отедльном квадате)
                if(mainSymbolRectangle.Left >= symbolRectangle.Left && mainSymbolRectangle.Left <= symbolRectangle.Right ||
                   mainSymbolRectangle.Right >= symbolRectangle.Left && mainSymbolRectangle.Right <= symbolRectangle.Right)
                {
                    if (mainSymbolRectangle.Top > symbolRectangle.Top &&
                        mainSymbolRectangle.Top > symbolRectangle.Bottom &&
                        mainSymbolRectangle.Top > symbolRectangle.Top &&
                        mainSymbolRectangle.Top > symbolRectangle.Bottom ||
                        symbolRectangle.Top > mainSymbolRectangle.Top &&
                        symbolRectangle.Top > mainSymbolRectangle.Bottom &&
                        symbolRectangle.Top > mainSymbolRectangle.Top &&
                        symbolRectangle.Top > mainSymbolRectangle.Bottom
                       )
                    {
                        if (mainSymbolRectangle.Top > symbolRectangle.Top)
                        {
                            meaning = mainSymbol.GetMeaning() + "-ый в степени " + symbol.GetMeaning();
                        }
                        else
                        {
                            meaning = symbol.GetMeaning() + "-ый в степени " + mainSymbol.GetMeaning();
                        }

                        result = (int)Result.End;
                    }
                    else
                    {
                        result = (int)Result.NotBelong;
                    }
                }
                //но когда мы просто идём по правилам, важно понимать, что там тоже могут быть ндексы(степени). И их над приписать
                else
                {
                    //если первый символ должен  выше центра прямоугольника всего выражения, а второй ниже его, то это индекс 
                    if (symbolRectangle.Top < mainSymbolRectangle.Bottom &&
                        symbolRectangle.Top > mainSymbolRectangle.Top &&
                        symbolRectangle.Bottom > mainSymbolRectangle.Bottom)
                    {
                        meaning = mainSymbol.GetMeaning() + " " + symbol.GetMeaning() + "-ый";
                        result = (int)Result.End;
                    }
                    //иначе,
                    else 
                    if (symbolRectangle.Bottom < mainSymbolRectangle.Bottom &&
                        symbolRectangle.Bottom > mainSymbolRectangle.Top &&
                        symbolRectangle.Bottom < mainSymbolRectangle.Bottom &&
                        symbolRectangle.Top < mainSymbolRectangle.Top
                        )
                    {
                        meaning = mainSymbol.GetMeaning() + " в степени " + symbol.GetMeaning();
                        result = (int)Result.End;
                    }
                    else
                    {
                        result = (int)Result.NotBelong;
                    }
                }
               

            }
            return result;
        }
    }

    //public class ConstructIJ : IConstructRule
    //{
    //    string meaning;
    //    int state;

    //    public ConstructIJ()
    //    {
    //        state = 0;
    //        meaning = "";
    //    }

    //    public override string GetMeaning()
    //    {
    //        return meaning;
    //    }

    //    public override int Update(RealSymbol symbol)
    //    {
    //        int result = -1;
    //        if()

    //        List<RealSymbol> otherSymbols = new List<RealSymbol>();
    //        otherSymbols.Remove(symbol);
    //        otherSymbols.Reverse();
    //        Rectangle symbolRectangle = symbol.GetRealBounds();
    //        //будем искать мою часть, но искать с минимальной высостой до неё. 
    //        RealSymbol myParticle = null;
    //        int heightToMyParticle = int.MaxValue;
    //        //цикл j для поиска particle (точки для 'i')
    //        for (int j = otherSymbols.Count - 1; j >= 0; --j)
    //        {
    //            Rectangle particleRectangle = otherSymbols[j].GetRealBounds();
    //            Point particleRectCenter = new Point(particleRectangle.Left + particleRectangle.Width / 2, particleRectangle.Top + particleRectangle.Height / 2);
    //            if (symbol != otherSymbols[j])
    //            {
    //                //точка сверху над телом i или j, или же вторая палка знака '=' лежит в моих границах 
    //                if (particleRectCenter.X > symbolRectangle.Left && particleRectCenter.X < symbolRectangle.Left + symbolRectangle.Width - 1 &&
    //                    //ищу так, чтобы моя координата по Y была ниже, чем то, что я ищу. Т.е. ищем для тела 'i' верхнюю точку, для '=' верхнюю палку и т.д.
    //                    symbolRectangle.Top > particleRectangle.Top &&
    //                    heightToMyParticle > (symbolRectangle.Top - particleRectangle.Top + particleRectangle.Height - 1))
    //                {
    //                    myParticle = otherSymbols[j];
    //                    heightToMyParticle = (symbolRectangle.Top - particleRectangle.Top + particleRectangle.Height - 1);

    //                }
    //            }
    //        }
    //        if (myParticle != null)
    //        {
    //            //а потом добавим её реальную биткарту к своей реальной биткарте
    //            realSymbols[i] = (RealSymbol.SumSymbols(realSymbols[i], myParticle));
    //            done.Add(realSymbols[i]);
    //            //найденную частичку удалим из списка реальных символов
    //            realSymbols.Remove(myParticle);
    //        }

    //        otherSymbols.Reverse();
    //        return result;
    //    }
    //}
}
