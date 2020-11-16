using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RO_Project {
    public class Symbol {

        //биткарта, сооьтветствующая символу
        public byte[,] array;

        //битмап
        private Bitmap bitMap;

        private Rectangle rectangle;

        public string Mark { get; private set; }

        //конструктор
        public Symbol(Rectangle _rect, byte[,] _array, Bitmap _bitMap) {
            rectangle = new Rectangle(_rect.Left, _rect.Top, _rect.Width, _rect.Height);
            array = new byte[_array.GetLength(0), _array.GetLength(1)];
            Array.Copy(_array, array, _array.Length);
            bitMap = _bitMap;
        }

        public void SetMark(string _mark)
        {
            Mark = _mark;
        }

        public double GetDelta(EtalonSymbol etalon) {

            double delta = 0;

            for (int j = 0; j < array.GetLength(0); j++)
                for (int i = 0; i < array.GetLength(1); i++)
                    delta += Math.Abs(array[j, i] - etalon.array[j, i]);

            return delta;
        }

        public Rectangle GetRectangle()
        {
            return rectangle;
        }

        public void Print() {
            for (int i = 0; i < array.GetLength(0); i++) {
                for (int j = 0; j < array.GetLength(1); j++)
                    Console.Write(array[i, j] + " ");
                Console.WriteLine();
            }
        }

        public Bitmap GetBitMap() {
            return bitMap;
        }
        
    }

    public class EtalonSymbol
    {
        //биткарта, сооьтветствующая символу
        public double[,] array;
        public string Mark { get; private set; }

        public EtalonSymbol(double[,] _array, string _mark)
        {
            array = new double[_array.GetLength(0), _array.GetLength(1)];
            Array.Copy(_array, array, _array.Length);
            Mark = _mark;
        }

        public void SetMark(string _mark)
        {
            Mark = _mark;
        }

        public void Print()
        {
            for (int i = 0; i < array.GetLength(0); i++)
            {
                for (int j = 0; j < array.GetLength(1); j++)
                    Console.Write(array[i, j] + " ");
                Console.WriteLine();
            }
        }
    }
}
