using System.ComponentModel;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;

namespace API_CONTAS_A_RECEBER_BAIXAS.Services
{
    public class ErroService
    {

        public List<String> listaComErros { get; set; }
        public List<String> listaComErrosTratados { get; set; }
        public List<Dictionary<String, List<String>>> listaComErrosNotasJson = new List<Dictionary<String, List<String>>>();

        public Dictionary<String,String> listaComErrosSap { get; set; }
        public ErroService()
        {
            listaComErros = new List<String>();
            listaComErrosSap = new Dictionary<String, String>();
            listaComErrosTratados = new List<String>();
            //erro login filial diferente no documento
            listaComErrosSap["ERRO_FILIAL_DIFERENTE"] = "Ensure selected branch is the same as the branch of documents to be paid";
            listaComErrosSap["ERRO_INVOICE_FECHADO"] = "Invoice is already closed or blocked";
            listaComErrosSap["ERRO_PARCEIRO_CONSOLIDADO"] = "Base document card and target document card do not match.";
            listaComErrosSap["ERRO_TRANS_ID"] = "OJDT";
            listaComErrosSap["ERRO_TIMEOUT"] = "<html><body><h1>504 Gateway Time-out</h1> The server didn't respond in time. </body></html>";
        }


        public void CriarDevolutivaDeErrosBaseadoEmDePara()
        {
            List<String> errosTratados = new List<String>();
            foreach (String erroEncontrado in listaComErros)
            {
                if (erroEncontrado.Contains(listaComErrosSap["ERRO_FILIAL_DIFERENTE"]))
                {
                    listaComErrosTratados.Add("Erro de filial. A filial indicada na composiçao está incorreta. Verifique as notas.");

                }
                else if (erroEncontrado.Contains(listaComErrosSap["ERRO_INVOICE_FECHADO"]))
                {
                    listaComErrosTratados.Add("Alguma nota fiscal no SAP já está baixada. Verifique.");

                }else if (erroEncontrado.Contains(listaComErrosSap["ERRO_PARCEIRO_CONSOLIDADO"]))
                {
                    listaComErrosTratados.Add("Erro de parceiro consolidado! Verifique se alguma nota fiscal está sem parceiro consolidado");

                }else if (erroEncontrado.Contains(listaComErrosSap["ERRO_TIMEOUT"]))
                {
                    listaComErrosTratados.Add("O sap está sofrendo lentidão devido a alta quantidade de notas. Por favor, espere!");

                }
                else
                {
                    listaComErrosTratados.Add(erroEncontrado);

                }
            }
            
        }
        public string GetListaDeErrosString()
        {
            return string.Join(',',listaComErros);
        }
        public string GetListaDeErrosTratadosString()
        {
            return string.Join(',', listaComErrosTratados);
        }


        public void ValidarErroDeEstrutura(Dictionary<String, String> erroHeaders, bool apenasUmaSheet, bool inicioDeDados)
        {
            var headersComProblema = erroHeaders.Where(x => x.Value != "OK").Select(x => x.Key);

            if (headersComProblema.Any())
            {
                var headersLista = string.Join(", ", headersComProblema);
                listaComErros.Add($"O seguintes headers na composicao estão divergentes! Verifique a composição: {headersLista}");

            }
            if (!apenasUmaSheet)
            {
                listaComErros.Add("Há mais de uma sheet na composição");

            }
            if (!inicioDeDados)
            {
                listaComErros.Add("O inicio de notas fiscais não começa na linha 10. Verifique a composição");
            }
        }
    }
}
