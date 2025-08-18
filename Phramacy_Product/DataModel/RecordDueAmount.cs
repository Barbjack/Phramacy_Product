using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Phramacy_Product.DataModel
{
    public class RecordDueAmount
    {
        public string CustomerName { get; set; }
        public string MobileNumber { get; set; }
        public string DueAmount { get; set; }

        public string Initial => !string.IsNullOrEmpty(CustomerName)
            ? CustomerName.Trim()[0].ToString().ToUpper()
            : "?";

        public Brush AvatarColor
        {
            get
            {
                switch (Initial)
                {
                    case "A":
                    case "B":
                        return Brushes.Red;
                    case "C":
                    case "D":
                        return Brushes.Blue;
                    case "E":
                    case "F":
                        return Brushes.Green;
                    case "G":
                    case "H":
                        return Brushes.Orange;
                    case "I":
                    case "J":
                        return Brushes.Purple;
                    case "K":
                    case "L":
                        return Brushes.Teal;
                    default:
                        return Brushes.Gray;
                }
            }
        }
    }


}
