namespace API_CONTAS_A_RECEBER_BAIXAS.DTOS
{

    public class NotaDeDevolucaoGetDto
    {
        public string DocDate { get; set; }
        public int BPLId { get; set; }
        public string CardCode { get; set; }
        public string FatherCard { get; set; }
        public string DocStatus { get; set; }
        public int DocNum { get; set; }
        public float Saldo_disponivel { get; set; }
        public string Nome_rede_CR { get; set; }
        public string CANCELED { get; set; }
        public int DocEntry { get; set; }
        public string TIPO_NF { get; set; }
        public int id__ { get; set; }
    }

}
