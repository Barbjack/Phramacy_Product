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
    public class PurchaseMedicine : INotifyPropertyChanged
    {       
            public decimal qtFTotal { get; set; }
            public decimal QtFTotal
            {
                get => qtFTotal;
                set
                {
                    if (qtFTotal != value)
                    {
                        qtFTotal = value;
                        OnPropertyChanged();
                        RecalculateTotal();
                    }
                }
            }
            public decimal qtLTotal { get; set; }
            public decimal QtLTotal
            {
                get => qtLTotal;
                set
                {
                    if (qtLTotal != value)
                    {
                        qtLTotal = value;
                        OnPropertyChanged();
                        RecalculateTotal();
                    }
                }
            }
            public int ItemId { get; set; }
            public decimal pTR { get; set; }
            public decimal baseAmt { get; set; }
        public decimal BaseAmt
        {
            get => baseAmt;
            set
            {
                if (baseAmt != value)
                {
                    baseAmt = value;
                    OnPropertyChanged();
                    RecalculateTotal(); 
                }
            }
        }
        public string ProductName { get; set; }
            public string CompanyName { get; set; }
            public string medicineType { get; set; }
            public string saltComposition1 { get; set; }
            public string saltComposition2 { get; set; }
            public string StripInfo { get; set; }
            public decimal mRP { get; set; }
            public int Stock { get; set; }
            public string BillNumber { get; set; }
            public DateTime? BillDate { get; set; }
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
            public decimal schAmt { get; set; }
            public decimal SchAmt
           {
            get => schAmt;
            set
            {
                if(schAmt != value)
                {
                    schAmt = value;
                    OnPropertyChanged();
                    RecalculateTotal();
                }
            }
           }
            public bool IsLoose { get; set; }
            public string TransactionNumber { get; set; }
            public string DistributorName { get; set; }

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
            public decimal PTR
            {
                get => pTR;
                set
                {
                    if (pTR != value)
                    {
                        pTR = value;
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
                    if (mRP != value)
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
                decimal unitPriceF = 0;
                decimal priceAfterDiscountF = 0;
                decimal priceWithGSTF = 0;
                decimal unitPriceL = 0;
                decimal priceAfterDiscountL = 0;
                decimal priceWithGSTL = 0;
               
                if (QtyF > 0)
                {
                    unitPriceF = PTR;
                    priceAfterDiscountF = unitPriceF - (unitPriceF * Discount / 100);
                    
                    priceWithGSTF = priceAfterDiscountF + (priceAfterDiscountF * GST / 100);
                    QtFTotal = QtyF * priceWithGSTF;
                }
                else
                {
                    QtFTotal = 0.0m;
                }
                if (QtyL > 0)
                {
                    decimal number = 0;
                    Match match = Regex.Match(StripInfo, @"\d+");
                    if (match.Success)
                    {
                        number = Convert.ToDecimal(match.Value);
                    }

                    if (number > 0)
                    {
                        unitPriceL = PTR / number;
                        priceAfterDiscountL = unitPriceL - (unitPriceL * Discount / 100);
                        SchAmt = QtyL * priceAfterDiscountL;
                        priceWithGSTL = priceAfterDiscountL + (priceAfterDiscountL * GST / 100);
                        QtLTotal = QtyL * priceWithGSTL;
                    }
                    else
                    {
                        QtLTotal = 0.0m;
                    }
                }
                else
                {
                    QtLTotal = 0.0m;
                }

                Total = QtFTotal + QtLTotal;
            }
        }
    }

