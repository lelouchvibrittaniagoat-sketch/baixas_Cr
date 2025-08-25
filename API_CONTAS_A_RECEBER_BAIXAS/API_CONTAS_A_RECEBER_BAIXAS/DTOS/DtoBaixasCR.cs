namespace API_CONTAS_A_RECEBER_BAIXAS.DTOS
{
    public class DtoBaixasCR
    {
        public int id { get; set; }
        public string status { get; set; }
        public DateTime? data_criacao { get; set; }
        public DateTime? data_Atualizacao { get; set; }
        public string nome_arquivo { get; set; }

        public string data_baixa { get; set; }
        public string conta_contabil { get; set; }
        public string filial { get; set; }
        public string rede { get; set; }
        public string extensao { get; set; }
    }
}
