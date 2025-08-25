using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CR.DTOS
{
    public class BaixasCr
    {

            public int id { get; set; }
            public string status { get; set; }
            public object data_criacao { get; set; }
            public DateTime data_Atualizacao { get; set; }
            public string nome_arquivo { get; set; }
            public string data_baixa { get; set; }
            public string conta_contabil { get; set; }
            public string filial { get; set; }
            public string rede { get; set; }
            public string extensao { get; set; }
        
    }
}
