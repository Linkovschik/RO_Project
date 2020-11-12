using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RO_Project {
    interface IRule {

        int Update(Symbol symbol);

        void Reset();
    }

    public class TrigonomRule : IRule {

        List<string> marksList;

        string meaning;

        int state;

        Queue<string> queue;

        public TrigonomRule(string filePath) {
            marksList = new List<string>();
            meaning = "NaN";
            state = 0;

            queue = new Queue<string>();

            using (StreamReader sr = new StreamReader(filePath)) {

                int countSymbols = Int32.Parse(sr.ReadLine());

                for (int i = 0; i < countSymbols; i++) {

                    marksList.Add(sr.ReadLine());
                }

                meaning = sr.ReadLine();
            }
        }

        public int Update(Symbol symbol) {
            return 2;
            
        }

        public void Reset() {

            
        }
    }
}
