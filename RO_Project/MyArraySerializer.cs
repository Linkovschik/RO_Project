using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RO_Project {
    class MyArraySerializer {

        public static void SerializeArray(byte[,] array, int N, int M, StreamWriter sw) {

            for (int i = 0; i < N; i++) {
                for (int j = 0; j < M; j++) {
                    sw.Write(array[i, j]);
                    if (j != M - 1)
                        sw.Write(" ");
                }
                sw.WriteLine();
            }

            sw.Close();
        }

        public static void SerializeDoubleArray(double[,] array, int N, int M, StreamWriter sw)
        {

            for (int i = 0; i < N; i++)
            {
                for (int j = 0; j < M; j++)
                {
                    sw.Write(Math.Round(array[i, j],5));
                    if (j != M - 1)
                        sw.Write(" ");
                }
                sw.WriteLine();
            }

            sw.Close();
        }

        public static byte[,] DeserializeArray(int N, int M, StreamReader sr ) {

            byte[,] deserializedArray = new byte[N, M];

            for (int i = 0; i < N; i++) {

                string str = sr.ReadLine();
                string[] strSplitted = str.Split(' ');

                for (int j = 0; j < M; j++) {
                    deserializedArray[i,j] = Byte.Parse(strSplitted[j]);
                }
            }

            return deserializedArray;
        }

        public static double[,] DeserializeDoubleArray(int N, int M, StreamReader sr )
        {

            double[,] deserializedArray = new double[N, M];

            for (int i = 0; i < N; i++)
            {

                string str = sr.ReadLine();
                string[] strSplitted = str.Split(' ');

                for (int j = 0; j < M; j++)
                {
                    deserializedArray[i, j] = Double.Parse(strSplitted[j]);
                }
            }

            return deserializedArray;
        }
    }
}
