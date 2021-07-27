using System;
using System.Collections.Generic;
using System.Text;

namespace Processor
{
    public class SzamlaTetel
    {
        public string SzamlaSorszam { get; set; }

        public decimal BruttoAr { get; set; }

        public decimal NettoAr { get; set; }

        public double Afakulcs { get; set; }

        public DateTime? IdoszakTol { get; set; }

        public DateTime? IdoszakIg { get; set; }
    }
}
