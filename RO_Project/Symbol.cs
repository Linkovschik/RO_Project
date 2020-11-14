using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RO_Project {
    public class Symbol {

        //биткарта, сооьтветствующая символу
        byte[,] array;

        Rectangle rectangle;

        public string Mark { get; private set; }

        //конструктор
        public Symbol(int Left, int Top, int Width, int Height, byte[,] _array) {

            rectangle = new Rectangle(Left, Top, Width, Height);
            array = new byte[_array.GetLength(0), _array.GetLength(1)];
            Array.Copy(_array, array, _array.Length);
        }

        public Symbol(byte[,] _array, string _mark) {
            rectangle = new Rectangle(0, 0, 0, 0);
            array = new byte[_array.GetLength(0), _array.GetLength(1)];
            Array.Copy(_array, array, _array.Length);
            Mark = _mark;
        }

        public void SetMark(string _mark)
        {
            Mark = _mark;
        }

        public int GetDelta(Symbol etalon) {

            int delta = 0;

            for (int j = 0; j < array.GetLength(0); j++)
                for (int i = 0; i < array.GetLength(1); i++)
                    delta += (byte)(array[j, i] ^ etalon.array[j, i]);

            return delta;
        }

       public void Print() {
            for (int i = 0; i < array.GetLength(0); i++) {
                for (int j = 0; j < array.GetLength(1); j++)
                    Console.Write(array[i, j] + " ");
                Console.WriteLine();
            }
        }
    }
}
