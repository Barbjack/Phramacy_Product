using System;
namespace Phramacy_Product.DataModel
{
    internal class PharmacyProfile
    {
        public int profileId { get; set; }
        public object pharmacy_name { get; set; }
        public object pharmacist_name { get; set; }
        public object gstin {  get; set; }
        public object panno { get; set; }
        public object dlno { get; set; }
        public object mobile { get; set; }
        public object email { get; set; }
        public object address { get; set; }
        public object address2 { get; set; }
        public object area { get; set; }
        public object pincode { get; set; }
        public object city { get; set; }
        public object state { get; set; }
        public object billPath { get; set; }
        public byte[] company_logo { get; set; }
        public byte[] signature { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
    }
}