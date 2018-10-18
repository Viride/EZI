using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EZI.Model
{
    public class BagOfWords
    {
        public Dictionary<int, Dictionary<string, double>> BagOfWord { get; set; } 
        public Dictionary<int, double> Vectors { get; set; }
    }
}
