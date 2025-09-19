using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text;
using API_CONTAS_A_RECEBER_BAIXAS.DTOS;
using API_CONTAS_A_RECEBER_BAIXAS.Models;
using API_CONTAS_A_RECEBER_BAIXAS.Models_context;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Data.Analysis;
using static System.Net.WebRequestMethods;
using API_CONTAS_A_RECEBER_BAIXAS.Interfaces;
using System.Security.Cryptography.Xml;
using System;
using Microsoft.EntityFrameworkCore;
using DocumentFormat.OpenXml.ExtendedProperties;


namespace API_CONTAS_A_RECEBER_BAIXAS.Services
{
    public class ComposicaoService
    {

        public ContasAReceberDbContext Context { get; set; }
        public List<Dictionary<String, List<String>>> listaComErrosNotasJson = new List<Dictionary<String, List<String>>>();
        public ServiceLayerService serviceLayerService { get; set; }
        public IncomingPaymentsService incomingPaymentsService { get; set; }
        public List<NotaFiscal> TodasnotasFiscaisComproblemas { get; set; }
        public ComposicaoService(ContasAReceberDbContext dbContext)
        {
            Context = dbContext;
            incomingPaymentsService = new IncomingPaymentsService(null, dbContext);
            serviceLayerService = new ServiceLayerService();
        }
        public async Task<byte[]> CriarComposicaoComErros(int idComposicao)
        {
            var composicoesComErro = Context.BaixasCR.Where(x => (x.status != "SEM ERROS" || x.json_erros.Count > 0) && x.id == idComposicao).ToList();
            if (composicoesComErro.Count == 0)
            {
                return null;
            }
            byte[] arquivoComposicao = composicoesComErro.FirstOrDefault().arquivo_excel;
            return this.AtualizarPlanilhaComErros(arquivoComposicao, composicoesComErro.FirstOrDefault().json_erros).arquivoAtualizado;
        }
        public async Task<byte[]> CriarComposicaoComErrosV2(int idBaixa)
        {
            List<NotasFiscaisStatus> notasFiscaisStatuses =  Context.NotasFiscaisStatus.Where(x => x.erros.Count>0 &&   x.idBaixa == idBaixa).ToList();
            if(notasFiscaisStatuses.Count == 0 )
            {
                return null;
            }

            var composicoesComErro = Context.BaixasCR.Where(x =>x.id == idBaixa).ToList();
            var erros = Context.NotasFiscaisStatus
                    .Select(n => new Dictionary<string, List<string>>
                    {
                        { n.docNum.ToString(), n.erros }
                    })
                    .ToList();

            if (composicoesComErro.Count == 0)
            {
                return null;
            }

            byte[] arquivoComposicao = composicoesComErro.FirstOrDefault().arquivo_excel;
            return this.AtualizarPlanilhaComErros(arquivoComposicao, erros).arquivoAtualizado;
        }
        public bool SalvarNotasNoBanco(NotasASeremBaixadas notasASeremBaixadas, int idBaixa)
        {

            // Notas de saída
            bool possuiProblemas = false;
            foreach (var x in notasASeremBaixadas.NotasFiscaisSaida)
            {
                var nf = (NotaDeSaidaGetDto)x.NotaFiscalAnalisadaBanco;
                int docEntry = nf.DocEntry;

                var existente = Context.NotasFiscaisStatus
                    .FirstOrDefault(s => s.idBaixa == idBaixa && s.docEntry == docEntry);

                if (existente == null)
                {
                    existente = new NotasFiscaisStatus
                    {
                        docNum = nf.DocNum,
                        docEntry = docEntry,
                        idBaixa = idBaixa,
                        tipoDoc = 13,
                        erros = new List<string>(),
                        cL = nf.CardCode
                    };
                    Context.NotasFiscaisStatus.Add(existente);
                }

                if (!x.NotasEstaApta())
                {
                    possuiProblemas = true;
                    if (existente.docEntryContasAReceber > 0)
                    {
                        List<string> list = new List<string>();
                        list.Add($"Essa nota fiscal já foi baixada pela automação anteriormente. Verique a baixa N°:{existente.docNumContasAReceber}");
                        existente.erros = list;
                        existente.jaBaixado = true;
                    }
                    else
                    {
                        existente.jaBaixado = false;
                        existente.erros = x.Problemas;
                    }
                }
                Context.SaveChanges();

            }

            // Notas de devolução
            foreach (var x in notasASeremBaixadas.NotasFiscaisDevolucao)
            {
                var nf = (NotaDeDevolucaoGetDto)x.NotaFiscalAnalisadaBanco;
                int docEntry = nf.DocEntry;

                var existente = Context.NotasFiscaisStatus
                    .FirstOrDefault(s => s.idBaixa == idBaixa && s.docEntry == docEntry);

                if (existente == null)
                {
                    existente = new NotasFiscaisStatus
                    {
                        //nroNota = int.Parse(nf.Serial),
                        docNum = nf.DocNum,
                        docEntry = docEntry,
                        idBaixa = idBaixa,
                        tipoDoc = 14,
                        erros = new List<string>(),
                        cL = nf.CardCode
                    };
                    Context.NotasFiscaisStatus.Add(existente);
                }


                if (!x.NotasEstaApta())
                {
                    possuiProblemas = true;
                    if (existente.docEntryContasAReceber > 0)
                    {
                        List<string> list = new List<string>();
                        list.Add($"Essa nota fiscal já foi baixada pela automação anteriormente. Verique a baixa N°:{existente.docNumContasAReceber}");
                        existente.erros = list;
                        existente.jaBaixado = true;
                    }
                    else
                    {
                        existente.jaBaixado = false;
                        existente.erros = x.Problemas;
                    }
                }
                Context.SaveChanges();
            }


            return possuiProblemas;
        }
        public async Task AlterarNotasQueForamBaixadasComSucesso(int idBaixa, string cl, int docNumContasAReceber, int docEntryContasAReceber)
        {
            List<NotasFiscaisStatus> notasFiscaisStatuses = this.Context.NotasFiscaisStatus.Where(x => x.idBaixa == idBaixa && x.cL == cl).ToList();
            notasFiscaisStatuses.ForEach(x =>
            {
                x.docEntryContasAReceber = docEntryContasAReceber;
                x.docNumContasAReceber = docNumContasAReceber;
                x.jaBaixado = true;
            });
            await this.Context.SaveChangesAsync();
        }
        public async Task AlterarClQuePossuiErros(int idBaixa, string cl, List<string> erros)
        {
            List<NotasFiscaisStatus> notasFiscaisStatuses = this.Context.NotasFiscaisStatus.Where(x => x.idBaixa == idBaixa && x.cL == cl).ToList();
            notasFiscaisStatuses.ForEach(x =>
            {
                if (x.possuiErros == false && x.docNumContasAReceber > 0)
                {
                    List<string> list = new List<string>();
                    list.Add($"Essa nota fiscal já foi baixada pela automação anteriormente. Verique a baixa N°:{x.docNumContasAReceber}");
                    x.erros = list;
                }
                else
                {
                    x.erros = erros;
                }

            });
            await this.Context.SaveChangesAsync();
        }

        public async Task<Composicao> MainExecution(Composicao composicao, List<String> errosEncontrados, string ContaContabil, BaixasCR baixasCR)
        {
            await serviceLayerService.RealizarLogin();
            incomingPaymentsService.DataFrameComposicao = composicao.ComposicaoCr;
            var notasFiscaisComposicao = incomingPaymentsService.CriarDictComClsESuasNotas();
            Filiais filialEmQuestao = incomingPaymentsService.GetIdEmpresaPorNome(composicao.Filial);
            if (filialEmQuestao == null)
            {
                errosEncontrados.Add("Filial não encontrada nas opções disponiveis no sistema. Verifique a composição!");
                return null;
            }
            List<NotasASeremBaixadas> notasFiscais = incomingPaymentsService.VerificarNotasFiscais(notasFiscaisComposicao, composicao);
            composicao.NotasASeremBaixadas = notasFiscais;
            List<NotaFiscal> TodasAsNotasComProblema = new List<NotaFiscal>();
            foreach (NotasASeremBaixadas nota in composicao.NotasASeremBaixadas)
            {
                var notasdeSaidaComProblemas = nota.NotasFiscaisSaida.Where(x => x.NotasEstaApta() == false).ToList();
                var notasDeDevolucaoComProblemas = nota.NotasFiscaisDevolucao.Where(x => x.NotasEstaApta() == false).ToList();
                bool possuiProblemas = SalvarNotasNoBanco(nota, baixasCR.id);
                if (possuiProblemas)
                {
                    //Como o cl possui erros, vamos parao próximo cl, para não comprometer os lançamentos;
                    continue;
                }

                var formatos = new[] { "dd/MM/yyyy", "dd/MM/yyyy HH:mm:ss" };
                DateOnly dataBaixa = DateOnly.ParseExact(composicao.DataBaixa, formatos, CultureInfo.InvariantCulture);

                IncomingPayments incomingPayments = new IncomingPayments();
                incomingPayments.Series = 15;
                incomingPayments.Remarks = composicao.Obs;

                // ou usando TryParse:

                incomingPayments.BPLID = filialEmQuestao.IdSap;



                incomingPayments.TransferSum = Math.Round(nota.GetValorLiquidoNotasDevolucoes() + nota.GetValorLiquidoNotasSaidas(), 2);
                incomingPayments.TransferDate = dataBaixa;
                if (ContaContabil == "2.01.01.02.01")
                {
                    incomingPayments.TransferAccount = ContaContabil;
                    incomingPayments.DocDate = PrimeiroDomingoDoMes(dataBaixa);
                    incomingPayments.TaxDate = PrimeiroDomingoDoMes(dataBaixa);
                }
                else if (ContaContabil == "4.02.01.01.21")
                {
                    incomingPayments.TransferAccount = ContaContabil;
                    incomingPayments.DocDate = dataBaixa;
                    incomingPayments.TaxDate = dataBaixa;
                }
                else
                {
                    incomingPayments.TransferAccount = composicao.ContaContabil;
                    incomingPayments.DocDate = dataBaixa;
                    incomingPayments.TaxDate = dataBaixa;
                }


                incomingPayments.CardCode = nota.Cl;
                var paymentInvoices = incomingPaymentsService.MontarComposicaoSap(composicao, nota.Cl);
                incomingPayments.PaymentInvoices = paymentInvoices;
                incomingPayments.U_ContaContabilDeOrigem = composicao.ContaContabil;
                double totalCashFlow = Math.Round(nota.GetValorLiquidoNotasDevolucoes() + nota.GetValorLiquidoNotasSaidas(), 2);

                if (ContaContabil != "2.01.01.02.01" && ContaContabil != "4.02.01.01.21")
                {
                    CashFlowAssignments cashFlowAssignments = new CashFlowAssignments
                    {
                        PaymentMeans = "pmtBankTransfer"//,
                                                        //AmountLC = totalCashFlow
                    };
                    incomingPayments.CashFlowAssignments = new List<CashFlowAssignments> { cashFlowAssignments };
                }
                //incomingPayments.UnderOverpaymentdifference = totalCashFlow - totalComposicaoLiquido;
                var Json = JsonSerializer.Serialize(incomingPayments, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                Console.WriteLine(Json);
                var result = serviceLayerService.RealizarBaixasContasAReceber(Json);
                var resultadoComoString = result.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                if (!result.IsSuccessStatusCode)
                {
                    List<string> erroEncontradoT = new List<string>();
                    erroEncontradoT.Add($"O CL {incomingPayments.CardCode} não foi baixado devido ao seguinte problema:{resultadoComoString}");
                    await AlterarClQuePossuiErros(baixasCR.id, nota.Cl, erroEncontradoT);
                }
                else
                {
                    try
                    {
                        using (JsonDocument doc = JsonDocument.Parse(resultadoComoString))
                        {
                            JsonElement root = doc.RootElement;

                            if (root.TryGetProperty("DocEntry", out JsonElement docEntryElement) && root.TryGetProperty("DocNum", out JsonElement docNumElement))
                            {
                                int docEntry = docEntryElement.GetInt32();
                                int docNum = docNumElement.GetInt32();
                                await AlterarNotasQueForamBaixadasComSucesso(baixasCR.id, nota.Cl, docNum, docEntry);
                                Console.WriteLine($"DocEntry: {docEntry}");

                            }
                            else
                            {
                                Console.WriteLine("Campo docEntry não encontrado na resposta.");
                            }
                        }
                    }
                    catch (JsonException ex)
                    {
                        Console.WriteLine("Erro ao ler JSON: " + ex.Message);
                    }
                }
                Console.WriteLine(resultadoComoString);

            }
            return composicao;

        }
        public void GetNotasComErros()
        {

        }
        public Composicao GetComposicao(XLWorkbook composicao)
        {
            var worksheet = composicao.Worksheet(1);
            var rede = worksheet.Cell(1, 2).GetValue<string>();
            var filial = worksheet.Cell(2, 2).GetValue<string>();
            var dataPagamento = worksheet.Cell(4, 2).GetValue<string>();
            var contaContabil = worksheet.Cell(5, 2).GetValue<string>();
            var obs = worksheet.Cell(6, 2).GetValue<string>();
            var lastRow = worksheet.LastRowUsed().RowNumber();
            var lastCol = worksheet.LastColumnUsed().ColumnNumber();
            if (lastRow > 100000)
            {
                throw new Exception("Composição possui mais de 100000 notas a serem baixadas! Verifique o seu arquivo excel");

            }
            DataFrameColumn[] composicaoNotas = GetNotasFiscais(worksheet, lastRow);
            Composicao composicaoDTo = new Composicao
            {
                Filial = filial,
                DataBaixa = dataPagamento,
                ContaContabil = contaContabil,
                Obs = obs,
                ComposicaoCr = new DataFrame(composicaoNotas),
                Rede = rede

            };
            return composicaoDTo;
        }
        public record OcorrenciaNegativa(string CL, int LinhaExcel, double? ValorLiquido);
        public List<OcorrenciaNegativa> VerificarCLsNegativos(DataFrameColumn[] columns)
        {
            var clColumn = columns[1] as StringDataFrameColumn;
            var valorLiquidoColumn = columns[8] as DoubleDataFrameColumn;

            var ocorrencias = new List<OcorrenciaNegativa>();

            // 1️⃣ Agrupa por CL e calcula o total
            var totalPorCL = new Dictionary<string, double>();
            for (int i = 0; i < clColumn.Length; i++)
            {
                var cl = clColumn[i];
                var valor = valorLiquidoColumn[i];

                if (totalPorCL.ContainsKey(cl))
                    totalPorCL[cl] += (double)valor;
                else
                    totalPorCL[cl] = (double)valor;
            }

            // 2️⃣ Identifica os CLs cujo total é negativo
            var clsNegativos = totalPorCL.Where(kv => kv.Value < 0)
                                         .Select(kv => kv.Key)
                                         .ToHashSet();

            // 3️⃣ Adiciona ocorrência para todas as linhas desses CLs
            for (int i = 0; i < clColumn.Length; i++)
            {
                if (clsNegativos.Contains(clColumn[i]))
                {
                    ocorrencias.Add(new OcorrenciaNegativa(
                        CL: clColumn[i],
                        LinhaExcel: i + 10,               // linha do Excel
                        ValorLiquido: valorLiquidoColumn[i]
                    ));
                }
            }

            return ocorrencias;
        }


        public DateOnly PrimeiroDomingoDoMes(DateOnly data)
        {
            DateTime dataReferencia = data.ToDateTime(new TimeOnly(0, 0)); // usa a data passada como referência
            DateTime hoje = DateTime.Today;

            DateTime mesParaCalculo;

            // Se a data referência for nos últimos 180 dias
            if ((hoje - dataReferencia).TotalDays <= 180)
            {
                // Usar o mês atual
                mesParaCalculo = new DateTime(hoje.Year, hoje.Month, 1);
            }
            else
            {
                // Usar o mês seguinte à data de referência
                mesParaCalculo = new DateTime(dataReferencia.Year, dataReferencia.Month, 1).AddMonths(1);
            }

            // Calcular o primeiro domingo
            int diasParaDomingo = ((int)DayOfWeek.Sunday - (int)mesParaCalculo.DayOfWeek + 7) % 7;
            DateTime primeiroDomingo = mesParaCalculo.AddDays(diasParaDomingo);

            // Retornar como DateOnly
            return DateOnly.FromDateTime(primeiroDomingo);
        }
        public Dictionary<string, int> GetDocsMinimos(DataFrame dataFrame)
        {
            var numeroInternoCol = dataFrame.Columns["NUMERO INTERNO"] as StringDataFrameColumn;
            var tipoDocCol = dataFrame.Columns["TIPO DO DOCUMENTO"] as StringDataFrameColumn;

            var rowCount = dataFrame.Rows.Count;

            // Dicionário com chave = Tipo do Documento, valor = menor Numero Interno (convertido pra int)
            var minNumeroPorTipoDoc = new Dictionary<string, int>();

            for (int i = 0; i < rowCount; i++)
            {
                var tipoDoc = tipoDocCol[i];
                if (int.TryParse(numeroInternoCol[i], out int numeroInterno))
                {
                    if (minNumeroPorTipoDoc.TryGetValue(tipoDoc, out int minAtual))
                    {
                        if (numeroInterno < minAtual)
                            minNumeroPorTipoDoc[tipoDoc] = numeroInterno;
                    }
                    else
                    {
                        minNumeroPorTipoDoc.Add(tipoDoc, numeroInterno);
                    }
                }
            }

            // Exibe os mínimos encontrados
            return minNumeroPorTipoDoc;
        }
        public DataFrameColumn[] GetNotasFiscais(IXLWorksheet worksheet, int lastRow)
        {
            // ✅ Cabeçalhos fixos
            var totalRows = lastRow - 10 + 1; // número de linhas que vamos le
            var columns = new DataFrameColumn[9];
            columns[0] = new StringDataFrameColumn("DATA DE EMISSÃO");
            columns[1] = new StringDataFrameColumn("CL");
            columns[2] = new StringDataFrameColumn("NUMERO INTERNO");
            columns[3] = new StringDataFrameColumn("NUMERO DA NOTA");
            columns[4] = new StringDataFrameColumn("TIPO DO DOCUMENTO");
            columns[5] = new DoubleDataFrameColumn("VALOR BRUTO");
            columns[6] = new DoubleDataFrameColumn("% DESCONTO");
            columns[7] = new DoubleDataFrameColumn("VALOR DE DESCONTO");
            columns[8] = new DoubleDataFrameColumn("VALOR LIQUIDO");
            //columns[9] = new Int32DataFrameColumn("LinhaExcel", totalRows); // coluna extra linha
            double GetDouble(IXLCell cell) =>
                double.TryParse(cell.GetValue<string>(), out var result) ? result : 0;

            for (int row = 10; row <= lastRow; row++)
            {
                (columns[0] as StringDataFrameColumn)!.Append(worksheet.Cell(row, 1).GetValue<string>());
                (columns[1] as StringDataFrameColumn)!.Append(worksheet.Cell(row, 2).GetValue<string>());
                (columns[2] as StringDataFrameColumn)!.Append(worksheet.Cell(row, 3).GetValue<string>());
                (columns[3] as StringDataFrameColumn)!.Append(worksheet.Cell(row, 4).GetValue<string>());
                (columns[4] as StringDataFrameColumn)!.Append(worksheet.Cell(row, 5).GetValue<string>());
                (columns[5] as DoubleDataFrameColumn)!.Append(GetDouble(worksheet.Cell(row, 6)));
                (columns[6] as DoubleDataFrameColumn)!.Append(GetDouble(worksheet.Cell(row, 7)));
                (columns[7] as DoubleDataFrameColumn)!.Append(GetDouble(worksheet.Cell(row, 8)));
                (columns[8] as DoubleDataFrameColumn)!.Append(GetDouble(worksheet.Cell(row, 9)));
                //(columns[9] as Int32DataFrameColumn)!.Append(row);
            }
            return columns;

        }
        public byte[] AtualizarPlanilhaComErrosPorLinha(
                byte[] arquivoComposicao,
                List<OcorrenciaNegativa> ocorrenciaNegativas)
        {
            using var memoryStream = new MemoryStream(arquivoComposicao);
            using var workbook = new XLWorkbook(memoryStream);
            var worksheet = workbook.Worksheet(1);

            int lastRow = worksheet.LastRowUsed().RowNumber();

            // Marca os erros na coluna J (10)
            foreach (var occ in ocorrenciaNegativas)
            {
                if (occ.LinhaExcel <= lastRow)
                {
                    var celulaErro = worksheet.Cell(occ.LinhaExcel, 10); // coluna J
                    var textoExistente = celulaErro.GetValue<string>();

                    var novaMensagem = $"CL {occ.CL} valor líquido negativo ({occ.ValorLiquido})";
                    if (!string.IsNullOrEmpty(textoExistente))
                        celulaErro.Value = $"{textoExistente}; {novaMensagem}";
                    else
                        celulaErro.Value = novaMensagem;
                }
            }

            using var outputStream = new MemoryStream();
            workbook.SaveAs(outputStream);
            return outputStream.ToArray();
        }
        public byte[] AtualizarPlanilhaComErrosPorLinhaProblemasNaNotaFiscal(
                byte[] arquivoComposicao,
                List<OcorrenciaNegativa> ocorrenciaNegativas)
        {
            using var memoryStream = new MemoryStream(arquivoComposicao);
            using var workbook = new XLWorkbook(memoryStream);
            var worksheet = workbook.Worksheet(1);

            int lastRow = worksheet.LastRowUsed().RowNumber();

            // Marca os erros na coluna J (10)
            foreach (var occ in ocorrenciaNegativas)
            {
                if (occ.LinhaExcel <= lastRow)
                {
                    var celulaErro = worksheet.Cell(occ.LinhaExcel, 10); // coluna J
                    var textoExistente = celulaErro.GetValue<string>();

                    var novaMensagem = $"CL {occ.CL} valor líquido negativo ({occ.ValorLiquido})";
                    if (!string.IsNullOrEmpty(textoExistente))
                        celulaErro.Value = $"{textoExistente}; {novaMensagem}";
                    else
                        celulaErro.Value = novaMensagem;
                }
            }

            using var outputStream = new MemoryStream();
            workbook.SaveAs(outputStream);
            return outputStream.ToArray();
        }
        public (IXLWorksheet worksheetAtualizada, byte[] arquivoAtualizado) AtualizarPlanilhaComErros(
            byte[] arquivoComposicao,
            List<Dictionary<string, List<string>>> erros)
        {
            using (var memoryStream = new MemoryStream(arquivoComposicao))
            using (var workbook = new XLWorkbook(memoryStream))
            {
                var worksheet = workbook.Worksheet(1);
                int lastRow = worksheet.LastRowUsed().RowNumber();

                // Aplica os erros na worksheet
                for (int row = 10; row <= lastRow; row++)
                {
                    var numeroNota = worksheet.Cell(row, 3).GetValue<string>(); // Coluna 4 = NUMERO DA NOTA
                    var mensagensErro = new List<string>();

                    foreach (var erroDict in erros)
                    {
                        if (erroDict.TryGetValue(numeroNota, out var errosEncontrados))
                        {
                            mensagensErro.AddRange(errosEncontrados);
                        }
                    }

                    if (mensagensErro.Any())
                    {
                        var erroUnificado = string.Join("; ", mensagensErro);
                        worksheet.Cell(row, 10).Value = erroUnificado; // Coluna J (10)
                    }
                }

                // Salva o workbook em novo MemoryStream
                using (var outputStream = new MemoryStream())
                {
                    workbook.SaveAs(outputStream);
                    var arquivoAtualizado = outputStream.ToArray();

                    // Retorna a worksheet atualizada + o arquivo
                    return (worksheet, arquivoAtualizado);
                }
            }
        }

        public void GetHeadersComposicao()
        {

        }

        public BaixasCR SalvarInstanciaBaixa(byte[]? arquivoExcel, string listaComErros, string nomeArquivo, Composicao composicao, string extensao, List<Dictionary<String, List<String>>> notasJson, string adiantamentoCliente)
        {
            BaixasCR baixasCR = new BaixasCR();

            baixasCR.status = listaComErros;
            baixasCR.data_Atualizacao = DateTime.UtcNow;
            baixasCR.nome_arquivo = nomeArquivo;
            if (arquivoExcel != null)
            {
                baixasCR.arquivo_excel = arquivoExcel;
            }
            if (composicao == null)
            {
                throw new ArgumentNullException("Não informado nenhum objeto de composição.");

            }
            string[] formatosAceitos = { "dd/MM/yyyy", "dd/MM/yyyy HH:mm:ss" };

            DateTime dataFiltrada;
            bool conversaoOk = DateTime.TryParseExact(
                composicao.DataBaixa,
                formatosAceitos,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out dataFiltrada
            );


            baixasCR.data_baixa = dataFiltrada.ToString("dd/MM/yyyy");
            baixasCR.conta_contabil = composicao.ContaContabil;
            baixasCR.filial = composicao.Filial;
            baixasCR.rede = composicao.Rede;
            baixasCR.extensao = extensao;
            baixasCR.json_erros = listaComErrosNotasJson;

            baixasCR.nro_baixas = new List<int>();
            if (adiantamentoCliente == "null")
            {
                adiantamentoCliente = composicao.ContaContabil;

            }
            baixasCR.conta_contabil_efetiva = adiantamentoCliente;
            BaixasCR baixaEncontrada = Context.BaixasCR.FirstOrDefault(x => x.data_baixa == dataFiltrada.ToString("dd/MM/yyyy") && x.filial == composicao.Filial && x.conta_contabil == composicao.ContaContabil && x.rede == composicao.Rede);
            if (baixaEncontrada == null)
            {
                Context.Add(baixasCR);
            }
            else
            {
                if (listaComErros == "")
                {
                    listaComErros = "SEM ERROS";
                }
                baixaEncontrada.status = listaComErros;
                if (arquivoExcel != null)
                {
                    baixasCR.extensao = extensao;
                    baixasCR.arquivo_excel = arquivoExcel;
                }
                baixaEncontrada.data_Atualizacao = baixasCR.data_Atualizacao;
                baixaEncontrada.json_erros = baixasCR.json_erros;
                baixaEncontrada.conta_contabil_efetiva = adiantamentoCliente;
                baixasCR = baixaEncontrada;
            }
            Context.SaveChanges();
            return baixasCR;



        }
    }
}
