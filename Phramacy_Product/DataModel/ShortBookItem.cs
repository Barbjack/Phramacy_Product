using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Phramacy_Product.DataModel
{   
    public class ShortBookItem
    {
        public string itemName { get; set; }
         public string manufacturer { get; set; }
        public string priority { get; set; }
        public int currentStock { get; set; }
    }
}
