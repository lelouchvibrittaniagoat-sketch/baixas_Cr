using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using API_CONTAS_A_RECEBER_BAIXAS.Interfaces;

namespace API_CONTAS_A_RECEBER_BAIXAS.DTOS
{
    public class NotasASeremBaixadas
    {
        public String Cl { get; set; }
        public List<NotaFiscalDeDevolucaoDTOHandler> NotasFiscaisDevolucao { get; set; }
        public List<NotasFiscaisDeSaidaDTOHandler> NotasFiscaisSaida { get; set; }
        public List<NotaFiscalDeDevolucaoDTOHandler> NotasFiscaisDeDevolucaoComProblemas { get; set; }
        public List<NotasFiscaisDeSaidaDTOHandler> NotasFiscaisSaidaComProblemas { get; set; }


        public List<NotaFiscal> GetNotasFiscaisComProblemas()
        {
            List<NotaFiscal> strings = new List<NotaFiscal>();
            strings.AddRange(NotasFiscaisDeDevolucaoComProblemas);
            strings.AddRange(NotasFiscaisSaidaComProblemas);
            return strings;
        }
        public double GetValorLiquidoNotasSaidas()
        {
            return NotasFiscaisSaida
                .Where(x => x.NotaFiscalComposicao["CL"]?.ToString() == Cl)
                .Sum(x => Convert.ToDouble(x.NotaFiscalComposicao["VALOR LIQUIDO"]));
        }

        public double GetValorLiquidoNotasDevolucoes()
        {
            return NotasFiscaisDevolucao
                .Where(x => x.NotaFiscalComposicao["CL"]?.ToString() == Cl)
                .Sum(x => Convert.ToDouble(x.NotaFiscalComposicao["VALOR LIQUIDO"]));
        }




    }
}
