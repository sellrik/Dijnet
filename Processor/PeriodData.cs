using System;
using System.Collections.Generic;
using System.Text;

namespace Processor
{
    public class PeriodData
    {
        public string InvoiceNumber { get; set; }

        public DateTime OriginalPeriodFrom { get; set; }

        public DateTime OriginalPeriodTo { get; set; }

        public decimal OriginalAmount { get; set; }

        public DateTime PeriodFrom { get; set; }

        public DateTime PeriodTo { get; set; }

        public decimal Amount { get; set; }
    }
}
