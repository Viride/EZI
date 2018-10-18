using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EZI.Model
{
    public class StemmedDocument
    {
        public int Id { get; set; }
        public List<string> Title { get; set; }
        public List<string> Contents { get; set; }
        public List<Dictionary<string, double>> BagOfWords { get; set; }
        public double Vector { get; set; }
        public double Similarity { get; set; }
    }
}
