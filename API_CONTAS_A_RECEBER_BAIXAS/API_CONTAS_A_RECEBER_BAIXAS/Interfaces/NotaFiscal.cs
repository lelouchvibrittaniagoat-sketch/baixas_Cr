using Microsoft.Data.Analysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace API_CONTAS_A_RECEBER_BAIXAS.Interfaces
{
    public  class NotaFiscal
    {
        public virtual object NotaFiscalAnalisadaBanco { get; set; }
        public DataFrameRow NotaFiscalComposicao { get; set; } // Assuming this is a placeholder for the actual DataFrame or similar structure
        public bool SaldoEstaValido { get; set; }
        public bool EParceiroConsolidado { get; set; }

        public bool NotaValidaParaLancamento { get; set; }
        public List<String> Problemas = new List<String>();
        public Dictionary<String, List<String>> ProblemasJson = new Dictionary<String, List<String>>();



        public string GetListaDeErrosString()
        {
            if (SaldoEstaValido == false)
            {
                Problemas.Add($"NRO DO DOC-{NotaFiscalComposicao["NUMERO INTERNO"]}:O saldo em aberto da nota fiscal não está válido. Verique a nota no SAP.");
                ProblemasJson.TryAdd(NotaFiscalComposicao["NUMERO INTERNO"].ToString(), new List<String>());
                ProblemasJson[NotaFiscalComposicao["NUMERO INTERNO"].ToString()].Add("O saldo em aberto da nota fiscal não está válido. Verique a nota no SAP.");
            }
            if (NotaFiscalAnalisadaBanco!= null)
            {
                Problemas.Add($"NRO DO DOC-{NotaFiscalComposicao["NUMERO INTERNO"]}:Nota fiscal de saída não foi encontrada no banco de dados. Verificar nf.");
                ProblemasJson.TryAdd(NotaFiscalComposicao["NUMERO INTERNO"].ToString(), new List<String>());
                ProblemasJson[NotaFiscalComposicao["NUMERO INTERNO"].ToString()].Add("Nota fiscal de saída não foi encontrada no banco de dados. Verificar nf.");


            }


            return string.Join(',', Problemas);
        }
        public bool NotasEstaApta()
        {
            if(SaldoEstaValido && NotaFiscalAnalisadaBanco != null)
            {
                return true;
            }
            return false;
        }
    }
}
