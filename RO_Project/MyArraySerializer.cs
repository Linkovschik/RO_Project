using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RO_Project {
    class MyArraySerializer {

        public static void SerializeArray(byte[,] array, StreamWriter sw) {

            int N = 16, M = 16;

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

        public static byte[,] DeserializeArray(StreamReader sr) {

            int N = 16, M = 16;

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
    }
}
