using API_CONTAS_A_RECEBER_BAIXAS.DTOS;
using API_CONTAS_A_RECEBER_BAIXAS.Models;
using API_CONTAS_A_RECEBER_BAIXAS.Models_context;
using Microsoft.EntityFrameworkCore;

namespace API_CONTAS_A_RECEBER_BAIXAS.Services
{
    public class RelatoriosService
    {
        public ServiceLayerService ServiceLayerService { get; set; }
        public ContasAReceberDbContext Context { get; set; }    
        public List<NotaDeDevolucaoGetDto> notaDeDevolucaoGetDtos { get; set; }
        public List<NotaDeSaidaGetDto> notaDeSaidaGetDtos { get; set; }
        public RelatoriosService(ContasAReceberDbContext dbContext)
        {
            Context = dbContext;
            ServiceLayerService = new ServiceLayerService();
            notaDeDevolucaoGetDtos = new List<NotaDeDevolucaoGetDto>();
            notaDeSaidaGetDtos = new List<NotaDeSaidaGetDto>();
        }
        public bool VerificarSeBaixaPossuiErros(int idBaixa)
        {
            if (idBaixa == 0)
            {
                throw new Exception("Erro de id baixa.");
            }
            List<NotasFiscaisStatus> notasFiscaisStatuses =  this.Context.NotasFiscaisStatus.Where(x=>x.idBaixa ==idBaixa && x.cancelado ==false).ToList();
            bool notasComProblemas = notasFiscaisStatuses.Where(x => x.erros.Count>0).Any();
            return notasComProblemas;
        }
        public List<NotasFiscaisStatus> GetNotasComErroDaBaixa(int idBaixa)
        {
            if (idBaixa == 0)
            {
                throw new Exception("Erro de id baixa.");
            }
            List<NotasFiscaisStatus> notasFiscaisStatuses = this.Context.NotasFiscaisStatus.Where(x => x.idBaixa == idBaixa && x.cancelado ==false).ToList();
            List<NotasFiscaisStatus> notasComProblemas = notasFiscaisStatuses.Where(x => x.possuiErros == true).ToList();
            return notasComProblemas;
        }
        public async Task SalvarDados(List<NotaDeDevolucaoGetDto> notaDevolucao, List<NotaDeSaidaGetDto> notasSaida)
        {
            // Pega todos os DocEntry das duas listas (para as duas entidades)
            var docEntriesSaida = notasSaida.Select(x => x.DocEntry.ToString()).ToList();
            var docEntriesDevolucao = notaDevolucao.Select(x => x.DocEntry.ToString()).ToList();

            // Busca em lote todas as entidades existentes para notas saida
            var entidadesSaidaExistentes = await Context.NotaFiscaisDeSaida
                .Where(n => docEntriesSaida.Contains(n.DocEntry))
                .ToListAsync();

            // Busca em lote todas as entidades existentes para notas devolucao
            var entidadesDevolucaoExistentes = await Context.NotasFiscaisDevolucaos
                .Where(n => docEntriesDevolucao.Contains(n.DocEntry))
                .ToListAsync();

            // Atualiza ou adiciona as notas saida
            foreach (var x in notasSaida)
            {
                var entity = entidadesSaidaExistentes.FirstOrDefault(n => n.DocEntry == x.DocEntry.ToString());

                if (entity == null)
                {
                    entity = new NotaFiscaisDeSaida
                    {
                        DocEntry = x.DocEntry.ToString()
                    };
                    Context.NotaFiscaisDeSaida.Add(entity);
                    entidadesSaidaExistentes.Add(entity); // para evitar duplicação
                }

                entity.DocDate = x.DocDate;
                entity.Bplid = x.BPLId.ToString();
                entity.CardCode = x.CardCode;
                entity.FatherCard = x.FatherCard?.ToString() ?? string.Empty;
                entity.DocStatus = x.DocStatus;
                entity.DocNum = x.DocNum.ToString();
                entity.SaldoEmAberto = (decimal)x.Saldo_disponivel;
                entity.NomeRedeCr = x.Nome_rede_CR ?? string.Empty;
                entity.Canceled = x.CANCELED;
            }

            // Atualiza ou adiciona as notas devolucao
            foreach (var x in notaDevolucao)
            {
                var entity = entidadesDevolucaoExistentes.FirstOrDefault(n => n.DocEntry == x.DocEntry.ToString());

                if (entity == null)
                {
                    entity = new NotasFiscaisDevolucao
                    {
                        DocEntry = x.DocEntry.ToString()
                    };
                    Context.NotasFiscaisDevolucaos.Add(entity);
                    entidadesDevolucaoExistentes.Add(entity);
                }

                entity.DocDate = x.DocDate;
                entity.Bplid = x.BPLId.ToString();
                entity.CardCode = x.CardCode;
                entity.FatherCard = x.FatherCard?.ToString() ?? string.Empty;
                entity.DocStatus = x.DocStatus;
                entity.DocNum = x.DocNum.ToString();
                entity.SaldoEmAberto = (decimal)x.Saldo_disponivel;
                entity.NomeRedeCr = x.Nome_rede_CR ?? string.Empty;
                entity.Canceled = x.CANCELED;
            }

            await Context.SaveChangesAsync();
        }
        public async Task AtualizarRelatorioDeBaixas(int idFilial, int docMinimoSaida, int docMinimoDevolucao)
        {
            Console.WriteLine("Baixando dados atuais e salvando no postgres! ATENÇÃO...");
            await  ServiceLayerService.RealizarLogin();
            if ( docMinimoSaida > 0)
            {
                notaDeSaidaGetDtos = await ServiceLayerService.BaixarRelatorioNotasSaidaAsync(idFilial, docMinimoSaida);
            }
            if ( docMinimoDevolucao > 0)
            {
                notaDeDevolucaoGetDtos = await ServiceLayerService.BaixarRelatorioNotasDevolucaoAsync(idFilial, docMinimoDevolucao);
            }
            //await SalvarDados(notaDeDevolucao, notasSaida);
        }
    }
}
