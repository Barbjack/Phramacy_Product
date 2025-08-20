using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using Phramacy_Product.DataModel;

namespace Phramacy_Product.Views.Sales
{
    public class SaleReturnViewModel : INotifyPropertyChanged
    {
        public readonly DatabaseService DbService = new DatabaseService();
        private SaleDetail currentSale;
        private string txtBillNumber;
        private decimal returnTotal;
        public ObservableCollection<SaleItemReturn> PagedSaleItems { get; private set; } = new ObservableCollection<SaleItemReturn>();

        public string TxtBillNumber
        {
            get => txtBillNumber;
            set
            {
                if (txtBillNumber != value)
                {
                    txtBillNumber = value;
                    OnPropertyChanged();
                    if (string.IsNullOrEmpty(value))
                    {
                        PagedSaleItems.Clear();
                        CurrentSale = null;
                        ReturnTotal = 0;
                    }
                }
            }
        }

        public SaleDetail CurrentSale
        {
            get => currentSale;
            set
            {
                currentSale = value;
                OnPropertyChanged();
            }
        }

        public decimal ReturnTotal
        {
            get => returnTotal;
            set
            {
                returnTotal = value;
                OnPropertyChanged();
            }
        }

        public SaleReturnViewModel()
        {
            PagedSaleItems.CollectionChanged += PagedSaleItems_CollectionChanged;
        }

        private void PagedSaleItems_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (SaleItemReturn item in e.OldItems.OfType<SaleItemReturn>())
                {
                    item.PropertyChanged -= Item_PropertyChanged;
                }
            }
            if (e.NewItems != null)
            {
                foreach (SaleItemReturn item in e.NewItems.OfType<SaleItemReturn>())
                {
                    item.PropertyChanged += Item_PropertyChanged;
                }
            }
        }

        private void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SaleItemReturn.IsSelected) || e.PropertyName == nameof(SaleItemReturn.ReturnQty))
            {
                CalculateReturnTotal();
            }
        }

        public void SearchByBillNumber()
        {
            if (string.IsNullOrEmpty(TxtBillNumber))
            {
                MessageBox.Show("Please enter a Bill Number to search.");
                return;
            }

            var saleDetail = DbService.GetSaleDetailByBillNumber(TxtBillNumber);

            if (saleDetail != null)
            {
                var saleItems = DbService.GetSaleItemsBySaleId(saleDetail.SaleID);
                if (saleItems.Any())
                {
                    CurrentSale = saleDetail;
                    PagedSaleItems.Clear();
                    foreach (var item in saleItems)
                    {

                        PagedSaleItems.Add(item);

                    }
                }
                else
                {
                    MessageBox.Show($"No items found for Bill Number: {TxtBillNumber}");
                    PagedSaleItems.Clear();
                    CurrentSale = null;
                }
            }
            else
            {
                MessageBox.Show($"No records found for Bill Number: {TxtBillNumber}");
                PagedSaleItems.Clear();
                CurrentSale = null;
            }
            CalculateReturnTotal();
        }

        public void CalculateReturnTotal()
        {
            ReturnTotal = PagedSaleItems.Where(i => i.IsSelected && i.ReturnQty > 0)
                                        .Sum(i =>
                                        {
                                            decimal priceAfterDiscount = i.MRP - (i.MRP * i.Discount / 100);
                                            return i.ReturnQty * (priceAfterDiscount + (priceAfterDiscount * i.GST / 100));
                                        });
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}