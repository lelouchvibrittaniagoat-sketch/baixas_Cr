using DocumentFormat.OpenXml;
using API_CONTAS_A_RECEBER_BAIXAS.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace API_CONTAS_A_RECEBER_BAIXAS
{
    public  class IncomingPayments
    {
        public string CardCode { get; set; }
        public DateOnly DocDate { get; set; }
        public string TransferAccount { get; set; }
        public double TransferSum { get; set; }
        public DateOnly TransferDate { get; set; }
        public string Remarks { get; set; }
        public DateOnly TaxDate { get; set; }
        public double Series { get; set; }
        public int BPLID { get; set; }
        public List<PaymentInvoices> PaymentInvoices { get; set; }
        public string U_ContaContabilDeOrigem { get; set; }
        public double UnderOverpaymentdifference { get;set; }
        public List<CashFlowAssignments> CashFlowAssignments { get; set; }
    }
}
