
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace API_CONTAS_A_RECEBER_BAIXAS
{
    public class PaymentInvoices
    {
        public int DocEntry { get; set; }

        //Campo utilizadop para aplicar saldo na NF.
        public decimal SumApplied { get; set; }
        //public double AppliedSys { set; get; }
        public string InvoiceType { get; set; }
        //public double DiscountPercent { get; set; }
        public double TotalDiscount { get; set; }   
    }
}
