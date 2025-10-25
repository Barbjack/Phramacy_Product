using Phramacy_Product.DataModel;
using Phramacy_Product.Views.Purchase.PurchaseReturn;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Phramacy_Product.Views.Purchase.PurchaseReturn
{
    public class PurchaseReturnViewModel : INotifyPropertyChanged
    {
            public readonly DatabaseServices DbService = new DatabaseServices();
            private PurchaseDetail currentPurchase;
            private string txtBillNumber;
            private decimal returnTotal;
            public ObservableCollection<PurchaseItemReturn> PagedPurchaseItems { get; private set; } = new ObservableCollection<PurchaseItemReturn>();
            private string selectedMember;
            public string SelectedMember
            {
            get { return selectedMember; }
            set
             {
                selectedMember = value;

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
                            PagedPurchaseItems.Clear();
                            currentPurchase = null;
                            ReturnTotal = 0;
                        }
                    }
                }
            }

            public PurchaseDetail CurrentPurchase
            {
                get => currentPurchase;
                set
                {
                    currentPurchase = value;
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

            public PurchaseReturnViewModel()
            {
                PagedPurchaseItems.CollectionChanged += PagedPurchaseItems_CollectionChanged;
            }

            private void PagedPurchaseItems_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
            {
                if (e.OldItems != null)
                {
                    foreach (PurchaseItemReturn item in e.OldItems.OfType<PurchaseItemReturn>())
                    {
                        item.PropertyChanged -= Item_PropertyChanged;
                    }
                }
                if (e.NewItems != null)
                {
                    foreach (PurchaseItemReturn item in e.NewItems.OfType<PurchaseItemReturn>())
                    {
                        item.PropertyChanged += Item_PropertyChanged;
                    }
                }
            }

            private void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
            {
                if (e.PropertyName == nameof(PurchaseItemReturn.IsSelected) || e.PropertyName == nameof(PurchaseItemReturn.ReturnQty))
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

                var purchaseDetail = DbService.GetPurchaseDetailByBillNumber(TxtBillNumber);

                if (purchaseDetail != null)
                {
                    var purchaseItems = DbService.GetPurchaseItemsBySaleId(purchaseDetail.PurchaseID);
                    if (purchaseItems.Any())
                    {
                        CurrentPurchase = purchaseDetail;
                        PagedPurchaseItems.Clear();
                        foreach (var item in purchaseItems)
                        {
                            PagedPurchaseItems.Add(item);
                        }
                    }
                    else
                    {
                        MessageBox.Show($"No items found for Bill Number: {TxtBillNumber}");
                        PagedPurchaseItems.Clear();
                        currentPurchase = null;
                    }
                }
                else
                {
                    MessageBox.Show($"No records found for Bill Number: {TxtBillNumber}");
                    PagedPurchaseItems.Clear();
                    currentPurchase = null;
                }
                CalculateReturnTotal();
            }

            public void CalculateReturnTotal()
            {
                ReturnTotal = PagedPurchaseItems.Where(i => i.IsSelected && i.ReturnQty > 0)
                                            .Sum(i =>
                                            {
                                                decimal priceAfterDiscount = i.PTR - (i.PTR * i.Discount / 100);
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
