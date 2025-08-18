using System.ComponentModel;
using System.Runtime.CompilerServices;
namespace Phramacy_Product.DataModel
{
    public class SaleItemReturn : INotifyPropertyChanged
    {
        private bool isSelected;
        private int returnQty;

        public int SaleItemID { get; set; }
        public int SaleID { get; set; }
        public string ItemName { get; set; }
        public string BillNumber { get; set; }
        public string Batch { get; set; }
        public string Expiry { get; set; }
        public int FullQty { get; set; }
        public int LooseQty { get; set; }
        public decimal MRP { get; set; }
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
