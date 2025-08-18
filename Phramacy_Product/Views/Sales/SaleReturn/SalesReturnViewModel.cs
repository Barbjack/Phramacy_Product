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
        public List<SaleItemReturn> AllSaleItems;
        private SaleDetail currentSale;
        private string txtBillNumber;
        private decimal returnTotal;
        private SaleItemReturn selectedSaleItem;
        private ObservableCollection<SaleItemReturn> pagedSaleItems = new ObservableCollection<SaleItemReturn>();

        public ObservableCollection<SaleItemReturn> PagedSaleItems
        {
            get => pagedSaleItems;
            set
            {
                pagedSaleItems = value;
                OnPropertyChanged();
            }
        }

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
                        // Clear the grid when search box is cleared.
                        PagedSaleItems.Clear();
                        CurrentSale = null;
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

        public SaleItemReturn SelectedSaleItem
        {
            get => selectedSaleItem;
            set
            {
                selectedSaleItem = value;
                OnPropertyChanged();
                UpdateDetailsFromSelectedItem();
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
            LoadInitialData();
            // Subscribe to the CollectionChanged event to handle item additions/removals
            PagedSaleItems.CollectionChanged += PagedSaleItems_CollectionChanged;
        }

        private void PagedSaleItems_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            // Unsubscribe from old items
            if (e.OldItems != null)
            {
                foreach (SaleItemReturn item in e.OldItems)
                {
                    item.PropertyChanged -= Item_PropertyChanged;
                }
            }

            // Subscribe to new items
            if (e.NewItems != null)
            {
                foreach (SaleItemReturn item in e.NewItems)
                {
                    item.PropertyChanged += Item_PropertyChanged;
                }
            }
        }

        private void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Recalculate total when a property of an item changes
            if (e.PropertyName == nameof(SaleItemReturn.IsSelected) || e.PropertyName == nameof(SaleItemReturn.ReturnQty))
            {
                CalculateReturnTotal();
            }
        }


        public void LoadInitialData()
        {
            AllSaleItems = DbService.GetAllSaleItems();
            System.Diagnostics.Debug.WriteLine($"Total items loaded: {AllSaleItems?.Count ?? 0}");
        }

        public void SearchByBillNumber()
        {
            // Clear details from previous selections
            CurrentSale = null;
            SelectedSaleItem = null;
            PagedSaleItems.Clear();

            // Filter the local list based on the bill number
            if (string.IsNullOrEmpty(TxtBillNumber))
            {
                MessageBox.Show("Please enter a Bill Number to search.");
                return;
            }

            var filteredList = AllSaleItems.Where(i => i.BillNumber == TxtBillNumber).ToList();
            if (filteredList.Any())
            {
                PagedSaleItems = new ObservableCollection<SaleItemReturn>(filteredList);
                UpdateDetailsFromSelectedItem();
            }
            else
            {
                MessageBox.Show($"No records found for Bill Number: {TxtBillNumber}");
            }
            CalculateReturnTotal();
        }


        public void UpdateDetailsFromSelectedItem()
        {
            if (SelectedSaleItem != null)
            {
                CurrentSale = DbService.GetSaleDetailBySaleId(SelectedSaleItem.SaleID);
            }
            else
            {
                CurrentSale = null;
            }
        }

        public void CalculateReturnTotal()
        {
            ReturnTotal = PagedSaleItems.Where(i => i.IsSelected)
                                         .Sum(i => i.ReturnQty * (i.MRP - (i.MRP * i.Discount / 100)));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}