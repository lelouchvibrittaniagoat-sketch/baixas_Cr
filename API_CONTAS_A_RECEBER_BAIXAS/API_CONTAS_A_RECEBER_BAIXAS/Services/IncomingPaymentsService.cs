using Microsoft.Data.Analysis;
using Microsoft.EntityFrameworkCore;
using API_CONTAS_A_RECEBER_BAIXAS.DTOS;
using API_CONTAS_A_RECEBER_BAIXAS.Interfaces;
using API_CONTAS_A_RECEBER_BAIXAS.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using API_CONTAS_A_RECEBER_BAIXAS.Models_context;

namespace API_CONTAS_A_RECEBER_BAIXAS.Services
{
    public class IncomingPaymentsService
    {
        public DataFrame DataFrameComposicao { get; set; }
        public ContasAReceberDbContext ContasAReceberDbContextInstance { get; set; }
        public IncomingPaymentsService(DataFrame dataframeComposicao, ContasAReceberDbContext dbContext)
        {
            DataFrameComposicao = dataframeComposicao;
            ContasAReceberDbContextInstance = dbContext;
        }
        public Filiais GetIdEmpresaPorNome(String NomeFilial)
        {
            Filiais filial = ContasAReceberDbContextInstance.Filiais.FirstOrDefault(x => x.NomeFilial == NomeFilial.Trim().TrimEnd());
            return filial;
        }
        public void OrdernarComposicaoPorCodigoDeClsEValorLiquido()
        {

        }
        public List<PaymentInvoices> MontarComposicaoSap(Composicao composicao, String CodigoCl)
        {


            List<PaymentInvoices> paymentInvoices = new List<PaymentInvoices>();

            composicao.NotasASeremBaixadas.ForEach(x =>
            {
                if (x.Cl == CodigoCl)
                {
                    // Notas Fiscais de Saída
                    x.NotasFiscaisSaida.ForEach(notaFiscalSaida =>
                        {

                            decimal appliedSysValue = 0;
                            decimal.TryParse(notaFiscalSaida.NotaFiscalComposicao["VALOR LIQUIDO"]?.ToString(), out appliedSysValue);
                            double porcentagemDesconto = 0;
                            double.TryParse(notaFiscalSaida.NotaFiscalComposicao["% DESCONTO"]?.ToString(), out porcentagemDesconto);
                            double valorBruto = 0;
                            double.TryParse(notaFiscalSaida.NotaFiscalComposicao["VALOR BRUTO"]?.ToString(), out valorBruto);


                            if (porcentagemDesconto > 1)
                            {
                                porcentagemDesconto = porcentagemDesconto;
                            }
                            else
                            {
                                porcentagemDesconto = porcentagemDesconto * 100;
                            }
                            double valorBrutoCalculado = (double) valorBruto;
                            double ValorDesconto = Math.Round(valorBruto - (double)appliedSysValue, 2);
                            decimal liquido2Casas = (decimal)Math.Round((valorBruto - ValorDesconto),2);
                            var diferenca = liquido2Casas - Math.Round(appliedSysValue, 2, MidpointRounding.AwayFromZero);

                            NotaFiscaisDeSaida notaSaidaConvertido = notaFiscalSaida.NotaFiscalAnalisadaBanco as NotaFiscaisDeSaida;
                            //Essa opção é parar o sistema ser capaz de aplicar juros e não entender eles como se fosse um desconto
                            if(ValorDesconto < 0)
                            {
                                ValorDesconto = 0;
                            }
                            PaymentInvoices paymentInvoice = new PaymentInvoices
                            {
                                DocEntry = int.Parse(notaSaidaConvertido.DocEntry),
                                InvoiceType = "it_Invoice",
                                SumApplied = Math.Round(appliedSysValue, 2, MidpointRounding.AwayFromZero) + diferenca,
                                TotalDiscount =ValorDesconto
                            };
                            paymentInvoices.Add(paymentInvoice);
                        });

                    // Notas Fiscais de Devolução
                    x.NotasFiscaisDevolucao.ForEach(notaFiscalDevolucao =>
                    {
                        decimal appliedSysValue = 0;
                        decimal.TryParse(notaFiscalDevolucao.NotaFiscalComposicao["VALOR LIQUIDO"]?.ToString(), out appliedSysValue);
                        double porcentagemDesconto = 0;
                        double.TryParse(notaFiscalDevolucao.NotaFiscalComposicao["% DESCONTO"]?.ToString(), out porcentagemDesconto);
                        double valorBruto = 0;
                        double.TryParse(notaFiscalDevolucao.NotaFiscalComposicao["VALOR BRUTO"]?.ToString(), out valorBruto);


                        if (porcentagemDesconto > 1)
                        {
                            porcentagemDesconto = porcentagemDesconto;
                        }
                        else
                        {
                            porcentagemDesconto = porcentagemDesconto * 100;
                        }
                        double valorBrutoCalculado = (double)valorBruto;
                        double ValorDesconto = Math.Round(valorBruto - (double)appliedSysValue, 2);
                        decimal liquido2Casas = (decimal)Math.Round((valorBruto - ValorDesconto), 2);
                        var diferenca = liquido2Casas - Math.Round(appliedSysValue, 2, MidpointRounding.AwayFromZero);

                        NotasFiscaisDevolucao notaDevolucaoConvertido = notaFiscalDevolucao.NotaFiscalAnalisadaBanco as NotasFiscaisDevolucao;
                        PaymentInvoices paymentInvoice = new PaymentInvoices
                        {
                            DocEntry = int.Parse(notaDevolucaoConvertido.DocEntry),
                            InvoiceType = "it_CredItnote",
                            SumApplied = Math.Round(appliedSysValue, 2, MidpointRounding.AwayFromZero) + diferenca,
                            TotalDiscount = ValorDesconto
                        };

                        paymentInvoices.Add(paymentInvoice);
                    });
                }
            });

            return paymentInvoices;



        }
        public void GetNotasPorCl()
        {

        }
        public Dictionary<string, List<DataFrameRow>> CriarDictComClsESuasNotas()
        {

            var colunaComClsUnicos = this.DataFrameComposicao.Columns["CL"].Cast<string>()
                                                                            .Distinct()
                                                                            .ToList();
            var dictComClsESuasNotas = new Dictionary<string, List<DataFrameRow>>();
            foreach (var cl in colunaComClsUnicos)
            {
                var filtrado = this.DataFrameComposicao.Rows.Where(row => row["CL"].ToString() == $"{cl}")
                                                            .Select(row => row)
                                                            .ToList();
                dictComClsESuasNotas[cl] = filtrado;
            }
            return dictComClsESuasNotas;
        }

        public List<NotasASeremBaixadas> VerificarNotasFiscais(Dictionary<string, List<DataFrameRow>> NotasASeremValidadas)
        {

            Dictionary<string, List<NotaFiscal>> notasFiscaisComposicao = new Dictionary<string, List<NotaFiscal>>();
            List<NotasASeremBaixadas> notasASeremBaixadas = new List<NotasASeremBaixadas>();
            foreach (var parceirosComNotas in NotasASeremValidadas)
            {

                List<NotasFiscaisDeSaidaDTOHandler> listasDeNotasSaida = new List<NotasFiscaisDeSaidaDTOHandler>();
                List<NotaFiscalDeDevolucaoDTOHandler> listasDeNotasDevolucao = new List<NotaFiscalDeDevolucaoDTOHandler>();

                //Notas de devolução
                var notasDeDevolucao = parceirosComNotas.Value.Where(row => row["TIPO DO DOCUMENTO"].ToString() == "DS")
                                                            .Select(row => row)
                                                            .ToList();
                notasDeDevolucao.ForEach(nota =>
                {
                    Console.WriteLine($"Validando nota de devolução: {nota["NUMERO INTERNO"]}");
                    NotasFiscaisDevolucao ObjetoNoBanco = ContasAReceberDbContextInstance.NotasFiscaisDevolucaos.FirstOrDefault(n => n.DocNum == nota["NUMERO INTERNO"] && n.Canceled == "N");
                    bool SaldoEstaValido = ValidaSaldoNFDevolucao(ObjetoNoBanco, nota);
                    NotaFiscalDeDevolucaoDTOHandler notaFiscalDeDevolucaoDTOHandler = new NotaFiscalDeDevolucaoDTOHandler();
                    notaFiscalDeDevolucaoDTOHandler.SaldoEstaValido = SaldoEstaValido;
                    notaFiscalDeDevolucaoDTOHandler.NotaFiscalAnalisadaBanco =ObjetoNoBanco as NotasFiscaisDevolucao;
                    notaFiscalDeDevolucaoDTOHandler.NotaFiscalComposicao = nota;
                    listasDeNotasDevolucao.Add(notaFiscalDeDevolucaoDTOHandler);

                });
                //Notas de saída
                var notasDeSaida = parceirosComNotas.Value.Where(row => row["TIPO DO DOCUMENTO"].ToString() == "NS")
                                                            .Select(row => row)
                                                            .ToList();
                notasDeSaida.ForEach(nota =>
                {
                    Console.WriteLine($"Validando nota de saida: {nota["NUMERO INTERNO"]}");
                    NotaFiscaisDeSaida ObjetoNoBanco = ContasAReceberDbContextInstance.NotaFiscaisDeSaida.FirstOrDefault(n => n.DocNum == nota["NUMERO INTERNO"] && n.Canceled == "N");
                    bool SaldoEstaValido = ValidaSaldoNFSaida(ObjetoNoBanco, nota);
                    NotasFiscaisDeSaidaDTOHandler notasFiscaisDeSaidaDTOHandler = new NotasFiscaisDeSaidaDTOHandler();
                    notasFiscaisDeSaidaDTOHandler.SaldoEstaValido = SaldoEstaValido;
                    notasFiscaisDeSaidaDTOHandler.NotaFiscalAnalisadaBanco = ObjetoNoBanco as NotaFiscaisDeSaida;
                    notasFiscaisDeSaidaDTOHandler.NotaFiscalComposicao = nota;
                    listasDeNotasSaida.Add(notasFiscaisDeSaidaDTOHandler);

                });
                NotasASeremBaixadas notasASeremBaixadasInstance = new NotasASeremBaixadas
                {
                    Cl = parceirosComNotas.Key,
                    NotasFiscaisDevolucao = listasDeNotasDevolucao,
                    NotasFiscaisSaida = listasDeNotasSaida
                };
                notasASeremBaixadas.Add(notasASeremBaixadasInstance);
            }

            return notasASeremBaixadas;
        }
        public bool ValidaSaldoNFSaida(NotaFiscaisDeSaida NotaFiscalBancoDeDados, DataFrameRow notaFiscalDeSaidaComposicao)
        {

            if (NotaFiscalBancoDeDados == null || !NotaFiscalBancoDeDados.SaldoEmAberto.HasValue)
                return false;
            //necessário para conseguir aplicar juros nas baixas, é necessário fazer o comparativo com o valor bruto na NF. 
            //Se for baseado no liquido, há problemas
            var valorCelula = notaFiscalDeSaidaComposicao["VALOR BRUTO"]?.ToString();
            var valorDescontado = notaFiscalDeSaidaComposicao["VALOR DE DESCONTO"]?.ToString();
            if (string.IsNullOrWhiteSpace(valorCelula))
                return false;

            if (decimal.TryParse(valorCelula, out var valorLiquido))
            {
                var saldo = NotaFiscalBancoDeDados.SaldoEmAberto.Value;

                // Arredonda com precisão de 2 casas
                var valorNota = Math.Round(valorLiquido, 2);
                var saldoEsperado = Math.Round(saldo, 2);

                return valorNota <= saldoEsperado;
            }

            return false; // Não conseguiu converter
        }
        public bool ValidaSaldoNFDevolucao(NotasFiscaisDevolucao notaFiscalBancoDeDados, DataFrameRow notaFiscalDevolucaoComposicao)
        {
            if (notaFiscalBancoDeDados == null || !notaFiscalBancoDeDados.SaldoEmAberto.HasValue)
                return false;

            var valorCelula = notaFiscalDevolucaoComposicao["VALOR LIQUIDO"]?.ToString();

            if (string.IsNullOrWhiteSpace(valorCelula))
                return false;

            if (decimal.TryParse(valorCelula, out var valorLiquido))
            {
                var saldo = notaFiscalBancoDeDados.SaldoEmAberto.Value;

                // Arredonda com precisão de 2 casas
                var valorNota = Math.Round(valorLiquido, 2);
                var saldoEsperado = Math.Round(-saldo, 2);

                return valorNota >= saldoEsperado;
            }

            return false; // Não conseguiu converter
        }

        public void ValidaParceiroConsolidado(List<Object> NotasFiscaisComposicao)
        {

        }
        public void ValidaCLSNegativos(List<Object> NotasFiscaisComposicao)
        {
        }
    }
}
