using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Phramacy_Product.DataModel
{
    public class PharmaMedicine
    {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Batch { get; set; }
            public decimal Price { get; set; }
            public DateTime? Expiry { get; set; }
            public int Quantity { get; set; }
            public int QtyInLoose { get; set; }
            public bool IsDiscontinued { get; set; }
            public string ManufacturerName { get; set; }
            public string Type { get; set; }
            public string PackSizeLabel { get; set; }
            public string ShortComposition1 { get; set; }
            public string ShortComposition2 { get; set; }
            public DateTime UpdatedAt { get; set; }
            public bool IsDeleted { get; set; }
            public decimal Discount { get; set; }
            public decimal GST { get; set; }
        }
    }

