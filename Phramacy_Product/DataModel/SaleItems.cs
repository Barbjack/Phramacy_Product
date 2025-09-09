using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Phramacy_Product.DataModel
{
        public class SaleItems : INotifyPropertyChanged
        {
        
            public int SaleItemID { get; set; }
            public int ItemId { get; set; }
            public int SaleID { get; set; }
            public string ItemName { get; set; }
            public string Pack { get; set; }
            public string Batch { get; set; }
            public string Expiry { get; set; }
            public int FullQty { get; set; }
            public int LooseQty { get; set; }
            public decimal MRP { get; set; }
            public decimal Discount { get; set; }
            public decimal GST { get; set; }
            public string NetAmount { get; set; }
            public bool Is_Loose { get; set; }
            public bool Is_Returned { get; set; }

            public event PropertyChangedEventHandler PropertyChanged;
            public void OnPropertyChanged([CallerMemberName] string name = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }
        }
    }


