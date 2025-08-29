using ClosedXML;
using Microsoft.Data.Analysis;
using API_CONTAS_A_RECEBER_BAIXAS.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace API_CONTAS_A_RECEBER_BAIXAS.DTOS
{
    public class Composicao
    {
        public DataFrame ComposicaoCr { get; set; }
        public String Filial { get; set; }
        public String DataBaixa { get; set; }
        public String ContaContabil { get; set; }
        public String Obs { get; set; }
        public String Rede { get; set; }
        public List<NotaDeDevolucaoGetDto> notaDeDevolucaoGetDtos = new List<NotaDeDevolucaoGetDto>();
        public List<NotaDeSaidaGetDto> notaDeSaidaGetDtos = new List<NotaDeSaidaGetDto>();
        public bool ErroParceiroConsolidado { get; set; }
        public List<NotasASeremBaixadas> NotasASeremBaixadas { get; set; }
        public List<int> documentoCriados { get; set; }
        public double GetValorLiquido(string cl)
        {

            var valorColuna =  ComposicaoCr.Columns["VALOR LIQUIDO"] as  PrimitiveDataFrameColumn<double>;
            double valorLiquido = Math.Round( (double)valorColuna.Sum(),2);
            return valorLiquido;
        }

    } 

}
