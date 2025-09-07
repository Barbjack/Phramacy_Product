using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Phramacy_Product.DataModel
{
    public class Medicine : INotifyPropertyChanged
    {
        public int ItemId { get; set; }
        public string ProductName { get; set; }
        public string CompanyName { get; set; }
        public string StripInfo { get; set; }
        public decimal mRP { get; set; }
        public int Stock { get; set; }
        public String BatchNumber { get; set; }
        public DateTime Expiry { get; set; }
        public string expiryMedicine { get; set; }
        public String qtyType { get; set; }
        public int qtyF { get; set; }
        public int qtyL { get; set; }
        public decimal discount { get; set; }
        public decimal gST { get; set; }
        public decimal total { get; set; }
        public string PaymentApp { get; set; }
        public  decimal SchAmt { get; set; }
        public bool IsLoose { get; set; }
        public string TransactionNumber { get; set; }

        public int QtyF
        {
            get => qtyF;
            set
            {
                if (qtyF != value)
                {
                    qtyF = value;
                    OnPropertyChanged();
                    RecalculateTotal();
                }
            }
        }

        public int QtyL
        {
            get => qtyL;
            set
            {
                if (qtyL != value)
                {
                    qtyL = value;
                    OnPropertyChanged();
                    RecalculateTotal();
                }
            }
        }

        public decimal MRP
        {
            get => mRP;
            set
            {
                if(mRP != value)
                {
                    mRP = value;
                    OnPropertyChanged();
                    RecalculateTotal();
                }
            }
        }
        public decimal Discount
        {
            get => discount;
            set
            {
                if (discount != value)
                {
                    discount = value;
                    OnPropertyChanged();
                    RecalculateTotal();
                }
            }
        }

        public decimal GST
        {
            get => gST;
            set
            {
                if (gST != value)
                {
                    gST = value;
                    OnPropertyChanged();
                    RecalculateTotal();
                }
            }
        }

        public decimal Total
        {
            get => total;
            set
            {
                if (total != value)
                {
                    total = value;
                    OnPropertyChanged();

                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void RecalculateTotal()
        {
            decimal priceAfterDiscount;
            decimal priceWithGST;
            decimal unitPrice;

            if (QtyF > 0)
            {
                unitPrice = MRP;
                priceAfterDiscount = unitPrice - (unitPrice * Discount / 100);
                priceWithGST = priceAfterDiscount + (priceAfterDiscount * GST / 100);
                Total = QtyF * priceWithGST;

            }
            else if (QtyL > 0)
            {
                decimal number = 0;
                Match match = Regex.Match(StripInfo, @"\d+");
                if (match.Success)
                {
                    number = Convert.ToDecimal(match.Value);
                }
                if (number > 0)
                {
                    unitPrice = MRP / number;
                    priceAfterDiscount = unitPrice - (unitPrice * Discount / 100);
                    priceWithGST = priceAfterDiscount + (priceAfterDiscount * GST / 100);
                    Total = QtyL * priceWithGST;
                }
            }
            else
            {
                Total = 0.0m;
            }
        }
    }
}

