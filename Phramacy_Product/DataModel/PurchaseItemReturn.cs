using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Phramacy_Product.DataModel
{
    public class PurchaseItemReturn : INotifyPropertyChanged
    {
            private bool isSelected;
            private int returnQty;
            public int ItemID { get; set; }
            public int MedId { get; set; }
            public int PurchaseID { get; set; }
            public string ItemName { get; set; }
            public string BillNumber { get; set; }
            public string Pack { get; set; }
            public string Batch { get; set; }
            public string Expiry { get; set; }
            public int FullQty { get; set; }
            public int LooseQty { get; set; }
            public decimal MRP { get; set; }
            public decimal PTR { get; set; }
            public decimal Discount { get; set; }
            public decimal GST { get; set; }
            public decimal NetAmount { get; set; }
            public bool Is_Loose { get; set; }
            public bool Is_Returned { get; set; }

            public bool IsSelected
            {
                get => isSelected;
                set
                {
                    if (isSelected != value)
                    {
                        isSelected = value;
                        OnPropertyChanged();
                    }
                }
            }

            public int ReturnQty
            {
                get => returnQty;
                set
                {
                    if (returnQty != value)
                    {
                        returnQty = value;
                        OnPropertyChanged();
                    }
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;
            public void OnPropertyChanged([CallerMemberName] string name = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }
        }
    }

