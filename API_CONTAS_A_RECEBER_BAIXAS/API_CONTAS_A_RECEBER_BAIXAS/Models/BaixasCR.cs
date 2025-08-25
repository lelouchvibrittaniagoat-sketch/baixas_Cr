namespace API_CONTAS_A_RECEBER_BAIXAS.Models
{
    public class BaixasCR
    {
        public int id { get; set; }
        public byte[] arquivo_excel { get; set; }
        public string status { get; set; }
        public DateTime? data_criacao { get; set; }
        public DateTime? data_Atualizacao { get; set; }
        public string nome_arquivo { get; set; }

        public string data_baixa { get; set; }
        public string conta_contabil { get;set; }
        public string filial { get;set; }
        public string rede { get; set; }
        public string extensao { get; set; }
        public List<Dictionary<String, List<String>>>? json_erros { get; set; }
        public string? conta_contabil_efetiva { get; set; }
        public List<int>? nro_baixas { get; set; }
        public List<Dictionary<String, List<String>>>? baixas_e_docs_vinculados { get; set; }
    }
}
