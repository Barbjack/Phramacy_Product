using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Phramacy_Product.DataModel
{
    public class SalePdfInvoice
    {
        public string CustomerName { get; set; }
        public string Mobile { get; set; }
        public string BillNo { get; set; }
        public DateTime Date { get; set; }
        public string PaymentType { get; set; }
        public decimal Discount { get; set; }
        public decimal GST { get; set; }
        public decimal NetAmount { get; set; }
        public decimal CashPaid { get; set; }
        public decimal Balance => NetAmount - CashPaid;
    }
}
