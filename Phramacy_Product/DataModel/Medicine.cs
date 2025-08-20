using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Phramacy_Product.DataModel
{
    public class Medicine
    {
        public int ItemId { get; set; }
        public string ProductName { get; set; }
        public string CompanyName { get; set; }
        public string StripInfo { get; set; }
        public decimal MRP { get; set; }
        public int Stock { get; set; }
        public String BatchNumber { get; set; }
        public DateTime Expiry { get; set; }
        public String qtyType { get; set; }
        public int QtyF { get; set; }
        public int QtyL { get; set; }
        public decimal Discount { get; set; }
        public decimal GST { get; set; }
        public decimal Total { get; set; }
        public string PaymentApp { get; set; }
        public string TransactionNumber { get; set; }
    }
}
