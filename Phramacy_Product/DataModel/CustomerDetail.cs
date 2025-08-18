using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Phramacy_Product.DataModel
{
    public class CustomerDetail
    {
        public String CustomerName { get; set; }
        public String CustomerNumber { get; set; }
        public decimal PendingAmount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

    }
}
