using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
namespace Phramacy_Product.DataModel
{
    public class SaleDetail : INotifyPropertyChanged
    {
        public int SaleID { get; set; }
        public int SrNo { get; set; }
        public String BillNumber { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? BillDate { get; set; }
        public String CreatedBy { get; set; }
        public String CustomerName { get; set; }
        public String PatientName { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public String PaymentStatus { get; set; }
        public String BillPath { get; set; }
        private int CustomerNumber { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        public string CombinedBillPayment => $"{TotalAmount:C2} ({PaymentStatus})";
    }
}