using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Phramacy_Product.DataModel
{
    public class ExpiringItem
    {
        public string Name { get; set; }
        public DateTime ExpiryDate { get; set; }
        public String Quantity { get; set; }
    }
}
