using API_CONTAS_A_RECEBER_BAIXAS.DTOS;
using API_CONTAS_A_RECEBER_BAIXAS.Interfaces;
using ClosedXML;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Data.Analysis;

namespace API_CONTAS_A_RECEBER_BAIXAS.Services
{
    public class ExcelService
    {
        public String[] headersComposicao;
        public XLWorkbook workbook;
        public int linhaComHeaders = 9;
        public ExcelService(XLWorkbook workbookComposicao)
        {
            headersComposicao = new String[] { "DATA DE EMISSÃO","CL","NUMERO INTERNO","NUMERO DA NOTA", "TIPO DO DOCUMENTO","VALOR BRUTO","% DESCONTO", "VALOR DE DESCONTO", "VALOR LIQUIDO"};
            workbook = workbookComposicao;
        }   
        
        public bool ValidaQuantidadeDeSheets()
        {
            var quantidadeDeSheets = workbook.Worksheets.Count;
            if(quantidadeDeSheets > 1 || quantidadeDeSheets ==0)
            {
                return false;
            }
            else
            {
                return true;
            }

        }
        public List<String> GetHeaders()
        {
            var linha = workbook.Worksheet(1).Row(linhaComHeaders).CellsUsed();

            var valoresDaLinha = linha.Select(c => c.GetValue<string>()).ToList();
            return valoresDaLinha;
        }
        public Dictionary<String, String> ValidarHeaders()
        {

            // Lista com os valores da linha
            var headersArquivo= GetHeaders();
            
            Dictionary<String, String> headersDict = new Dictionary<String, String>();
            foreach (String header in headersComposicao)
            {
                
                if (headersArquivo.Contains(header))
                {
                    headersDict[$"{header}"] = "OK";
                    continue;
                }
                headersDict[header] = "ERROR";

            }
            return headersDict;
        }
        public bool ValidarInicioDeNotas()
        {
            var worksheet = workbook.Worksheet(1);;
            var CodigoCl = worksheet.Cell(9, 2).GetValue<string>();
            if (CodigoCl != "CL")
            {
                return false;
            }
            else
            {
                return true;
            }

        }
        public void GravarErroNaSheet( List<NotaFiscal> TodasnotasFiscaisComproblemas, IXLWorksheet composicao )
        {

            TodasnotasFiscaisComproblemas.ForEach(
                nota =>
                {
                    var LinhaComErro = nota.NotaFiscalComposicao["LinhaIndex"];
                    composicao.Cell($"J{LinhaComErro}").Value = nota.ProblemasJson.ToString();
                }
            );

        }
    }
}
