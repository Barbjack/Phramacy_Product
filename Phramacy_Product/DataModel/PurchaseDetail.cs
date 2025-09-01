using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Phramacy_Product.DataModel
{
    public class PurchaseDetail : INotifyPropertyChanged
    {
    public int SrNo { get; set; }
        public int PurchaseID { get; set; }
    public string DistributorName { get; set; }
    public string BillNumber { get; set; }
    public DateTime? BillDate { get; set; }
    public DateTime? DueDate { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal TotalAmount { get; set; }
        public decimal? PendingAmount { get; set; }
    public decimal? ReturnAmount { get; set; }

    public string CreatedBy { get; set; }
    public string PaymentType { get; set; }
    public string PayName { get; set; }
    public string TsNum { get; set; }
    public string PaymentStatus { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }



        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
