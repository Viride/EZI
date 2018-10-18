using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EZI.Model
{
    public class Keyword
    {
        public string key { get; set; }
        public int Id { get; set; }
        public double Idf { get; set; }
        public int DocWithKeyCount { get; set; }
    }
}
