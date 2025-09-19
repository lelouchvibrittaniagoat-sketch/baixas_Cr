namespace API_CONTAS_A_RECEBER_BAIXAS.Models
{
    public class NotasFiscaisStatus
    {
        public int id {  get; set; }
        public int docNum { get; set; }
        public int docEntry { get; set; }
        public List<string> erros { get; set; }
        public int idBaixa {  get; set; }
        public bool jaBaixado { get; set; }
        public bool possuiErros { get { return erros.Count > 0 ? true:false; } }
        public int tipoDoc { get; set; }
        public int docEntryContasAReceber { get; set; }
        public int docNumContasAReceber { get; set; }
        public string cL {  get; set; }
    }
}
