namespace API_CONTAS_A_RECEBER_BAIXAS.Models
{
    public class ProblemasNotas
    {
        public int id {  get; set; }    
        public int id_baixa_cr {  get; set; }
        public string nro_doc { get; set; }
        public string nro_nota { get; set; }
        public string cl {  get; set; }
        public string problema { get; set; }
    }
}
