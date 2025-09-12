using API_CONTAS_A_RECEBER_BAIXAS.Services;
using Microsoft.AspNetCore.Mvc;
using API_CONTAS_A_RECEBER_BAIXAS.Models;
using API_CONTAS_A_RECEBER_BAIXAS.Models_context;
using API_CONTAS_A_RECEBER_BAIXAS.DTOS;
using Microsoft.AspNetCore.Diagnostics;
using Apache.Arrow;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Update.Internal;

namespace API_CONTAS_A_RECEBER_BAIXAS.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ComposicaoController : Controller
    {
        public ExcelService excelService;
        public ErroService erroService;
        public ComposicaoService composicaoService;
        public ServiceLayerService serviceLayerService;
        public RelatoriosService relatoriosService;

        public ComposicaoController(ContasAReceberDbContext dbContext)
        {
             composicaoService= new ComposicaoService(dbContext);
            serviceLayerService = new ServiceLayerService();
            relatoriosService = new RelatoriosService(dbContext);
        }
        [HttpGet("BaixasRegistradasPelaAutomacao")]
        public async Task<IActionResult> Index( )
        {
            var listaDeNotas = await composicaoService.Context.BaixasCR
                .OrderByDescending(x => x.data_Atualizacao)
                .Take(10)
                .ToListAsync(); // <-- note o ToListAsync()
            List<DtoBaixasCR> dtoBaixasCRs = new List<DtoBaixasCR>();
            listaDeNotas.ForEach(
                x => {

                        var baixa = new DtoBaixasCR()
                        {
                            id = x.id,
                            status = x.status,
                            data_criacao = x.data_criacao,
                            data_Atualizacao = x.data_Atualizacao,
                            nome_arquivo = x.nome_arquivo,
                            data_baixa = x.data_baixa,
                            conta_contabil = x.conta_contabil,
                            filial = x.filial,
                            rede = x.rede,
                            extensao = x.extensao
                        };
                        dtoBaixasCRs.Add(baixa);
                    
                }
             );
            Console.WriteLine(dtoBaixasCRs.Count);
            return Ok(dtoBaixasCRs); // melhor prática do que Json()
        }
        [HttpGet("GetContaBancaria")]
        public async Task<IActionResult> GetContaBancaria()
        {
            List<String> contas = this.composicaoService.Context.BaixasCR.Select(x=>x.conta_contabil).Distinct().ToList();
            return Ok(contas);
        }
        [HttpGet("AtualizarRelatorioDeBaixas")]
        public async Task<IActionResult> GetInformacoes(int idFilial, int DocMinimoSaida, int DocMinimoDevolucao)
        {
            Console.WriteLine("Baixando dados atuais e salvando no postgres! ATENÇÃO, ALTAS CHANCES DE DAR PROBLEMA, CASO HAJA MULTIPLAS NOTAS. CASO VOCÊ TENHA ALGUM PROBLEMA DE REQUISIÇÕES, REVEJA ESSE CONTROLLER TOTALMENTE!");
            serviceLayerService.RealizarLogin();
            List<NotaDeSaidaGetDto> notasSaida =  await serviceLayerService.BaixarRelatorioNotasSaidaAsync(3, 900000);
            List<NotaDeDevolucaoGetDto> notaDeDevolucao =  await serviceLayerService.BaixarRelatorioNotasDevolucaoAsync(3, 250000);
            await relatoriosService.SalvarDados(notaDeDevolucao,notasSaida);
            return Ok();
        }
        [HttpGet("GetRedeCr")]
        public async Task<IActionResult> GetRedeCr()
        {
            List<String> redes = this.composicaoService.Context.BaixasCR.Select(x => x.rede).Distinct().ToList();
            return Ok(redes);
        }
        [HttpDelete("CancelarComposicao")]
        public async Task<IActionResult> CancelarComposicao(int idBaixasCr)
        {
            BaixasCR baixasCR = composicaoService.Context.BaixasCR.Where(x=>x.id == idBaixasCr).FirstOrDefault();  
            if(baixasCR == null)
            {
                return BadRequest("Erro! Essa baixa não existe em nosso banco de dados");

            }
            Dictionary<String, bool> dict = new Dictionary<String, bool>();
            foreach (int nro_baixas in baixasCR.nro_baixas)
            {
                var cancelado = await this.composicaoService.serviceLayerService.CancelarDocumento(nro_baixas);
                dict[nro_baixas.ToString()]  = cancelado;
                    
            }
            return Ok(dict);
            
        }
        [HttpGet("GetComposicaoComErros")]
        public async Task<IActionResult> GetComposicaoComErros(int idBaixasCr)
        {
            var baixasCR = composicaoService.Context.BaixasCR
                .FirstOrDefault(x => x.id == idBaixasCr);

            if (baixasCR == null)
            {
                return BadRequest("Erro! Essa baixa não existe em nosso banco de dados");
            }

            byte[] result = await composicaoService.CriarComposicaoComErros(idBaixasCr);

            if (result == null || result.Length == 0)
            {
                return NotFound("Nenhum arquivo de composição com erros foi encontrado.");
            }

            // Monta nome do arquivo com extensão original
            var nomeArquivoCriado = $"ComposicaoComErros_{idBaixasCr}.{baixasCR.extensao}";

            // Salva em wwwroot/arquivos
            var pasta = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "arquivos");
            if (!Directory.Exists(pasta))
                Directory.CreateDirectory(pasta);

            var caminhoCompleto = Path.Combine(pasta, nomeArquivoCriado);
            await System.IO.File.WriteAllBytesAsync(caminhoCompleto, result);

            // Monta URL pública para download
            var urlDownload = $"{Request.Scheme}://{Request.Host}/arquivos/{nomeArquivoCriado}";

            return Ok(new { Url = urlDownload });
        }

        [HttpGet("BaixasRegistradasPelaAutomacaoComFiltros")]
        public async Task<IActionResult> Index2(string? redeCr, string?  contaContabil, string? dataDaBaixa)
        {
            var query = composicaoService.Context.BaixasCR.AsQueryable();

            // Filtros condicionais
            if (!string.IsNullOrEmpty(redeCr))
                query = query.Where(x => x.rede == redeCr);

            if (!string.IsNullOrEmpty(contaContabil))
                query = query.Where(x => x.conta_contabil == contaContabil);

            if (!string.IsNullOrEmpty(dataDaBaixa))
                query = query.Where(x => x.data_baixa == dataDaBaixa);


            var listaDeNotas = await query
                .OrderByDescending(x => x.data_Atualizacao)
                .Take(10)
                .ToListAsync();

            var dtoBaixasCRs = listaDeNotas.Select(x => new DtoBaixasCR
            {
                id = x.id,
                status = x.status,
                data_criacao = x.data_criacao,
                data_Atualizacao = x.data_Atualizacao,
                nome_arquivo = x.nome_arquivo,
                data_baixa = x.data_baixa,
                conta_contabil = x.conta_contabil,
                filial = x.filial,
                rede = x.rede,
                extensao = x.extensao
            }).ToList();

            Console.WriteLine(dtoBaixasCRs.Count);
            return Ok(dtoBaixasCRs);
        }


        [HttpPost("UploadDeArquivoBaixasCR")]
        public async Task<IActionResult> UploudArquivoDeBaixaExcel(IFormFile file, [FromForm] string adiantamentoCliente)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Arquivo inválido.");

            // Verifica a extensão do arquivo
            var extensaoValida = new[] { ".xlsx",".xlsm"};
            var extensao = Path.GetExtension(file.FileName).ToLowerInvariant();
            var nomeArquivo = Path.GetFileName(file.FileName);
            Console.WriteLine(adiantamentoCliente);
            var contaContabilEfetiva = adiantamentoCliente;
            if (!extensaoValida.Contains(extensao))
                return BadRequest("Extensão de arquivo não suportada. Apenas arquivos .xlsx são permitidos.");
            

            using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream);
                // Carrega a planilha usando ClosedXML
                using (var workbook = new ClosedXML.Excel.XLWorkbook(memoryStream))
                {
                    excelService = new ExcelService(workbook);
                    erroService = new ErroService();
                    var apenasHaUmaSheet = excelService.ValidaQuantidadeDeSheets();
                    var headersOk = excelService.ValidarHeaders();
                    var inicioDeDados = excelService.ValidarInicioDeNotas();
                    Composicao composicao = composicaoService.GetComposicao(workbook);
                    Dictionary<string,int> keyValuePairs =  composicaoService.GetDocsMinimos(composicao.ComposicaoCr);
                    
                    var clsNegativos =  composicaoService.VerificarCLsNegativos(composicao.ComposicaoCr.Columns.ToArray());
                    if (clsNegativos.Any())
                    {
                        var arquivoAtualizado = composicaoService.AtualizarPlanilhaComErrosPorLinha(
                                memoryStream.ToArray(),
                                clsNegativos // lista de ocorrências negativas já calculadas
                            );

                        // Salva temporariamente na pasta wwwroot/arquivos
                        var nomeArquivoCriado = $"arquivo_{DateTime.Now:yyyyMMdd_HHmmss}{extensao}";
                        var pasta = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "arquivos");
                        if (!Directory.Exists(pasta)) Directory.CreateDirectory(pasta);
                        var caminhoCompleto = Path.Combine(pasta, nomeArquivoCriado);
                        await System.IO.File.WriteAllBytesAsync(caminhoCompleto, arquivoAtualizado);

                        // Retorna a URL pública para o front
                        var urlDownload = $"{Request.Scheme}://{Request.Host}/arquivos/{nomeArquivoCriado}";
                        return BadRequest(new { Url = urlDownload });
                    }
                    composicao.documentoCriados = new List<int>();
                    erroService.ValidarErroDeEstrutura(headersOk, apenasHaUmaSheet, inicioDeDados);

                    if (!apenasHaUmaSheet || !inicioDeDados) {

                        return BadRequest("Verifique se arquivo possui apenas 1 sheet e se os dados de notas começam na linha 10! ");

                    }
                    Filiais filial = composicaoService.incomingPaymentsService.GetIdEmpresaPorNome(composicao.Filial);
                    if(filial == null)
                    {
                        return BadRequest("Filial incorreta");
                    }
                    int ns = keyValuePairs.TryGetValue("NS", out int tempNs) ? tempNs : 0;
                    int ds = keyValuePairs.TryGetValue("DS", out int tempDs) ? tempDs : 0;

                    

                    await relatoriosService.AtualizarRelatorioDeBaixas(filial.IdSap, ns, ds);
                        
                    


                    composicao.notaDeSaidaGetDtos = this.relatoriosService.notaDeSaidaGetDtos;
                    composicao.notaDeDevolucaoGetDtos = this.relatoriosService.notaDeDevolucaoGetDtos;

                    BaixasCR baixasCR =  composicaoService.SalvarInstanciaBaixa(memoryStream.ToArray(), erroService.GetListaDeErrosString(), nomeArquivo, composicao, extensao, null, contaContabilEfetiva);

                    var headersComProblema = headersOk.Where(x => x.Value != "OK").Select(x => x.Key);

                    if (headersComProblema.Any())
                    {
                        var headersLista = string.Join(", ", headersComProblema);
                        return BadRequest($"Os headers:{headersLista} não condizem com os cabeçalhos válidos. Reveja os nomes das colunas na composição");
                    }
                    
                    Composicao composicao1ComDocs = await  composicaoService.MainExecution(composicao, erroService.listaComErros, contaContabilEfetiva, baixasCR);
                    if (composicaoService.listaComErrosNotasJson.Count > 0)
                    {
                        var errosString = JsonSerializer.Serialize (composicaoService.listaComErrosNotasJson);
                        composicaoService.SalvarInstanciaBaixa(null, errosString, nomeArquivo, composicao, extensao, composicaoService.listaComErrosNotasJson, contaContabilEfetiva);
                        return BadRequest(new { Erros = $"{erroService.GetListaDeErrosTratadosString()}" });

                    }
                    if (erroService.listaComErros.Count> 0)
                    {
                        var errosString = string.Join(", ", erroService.listaComErros);
                        erroService.CriarDevolutivaDeErrosBaseadoEmDePara();
                        composicaoService.SalvarInstanciaBaixa(null, erroService.GetListaDeErrosTratadosString(), nomeArquivo,composicao, extensao,composicaoService.listaComErrosNotasJson, contaContabilEfetiva);
                        return BadRequest(new { Erros = $"{erroService.GetListaDeErrosTratadosString()}" });

                    }
                    else
                    {
                        baixasCR.status = "SEM ERROS";
                        baixasCR.nro_baixas.AddRange(composicao1ComDocs.documentoCriados);
                        composicaoService.Context.BaixasCR.Update(baixasCR);
                        composicaoService.Context.SaveChanges();
                    }

                    workbook.Save();



                }

                // Aqui você pode processar o Excel se quiser usando EPPlus, NPOI etc.
                var fileBytes = memoryStream.ToArray();
                var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                var fileName = $"{nomeArquivo}";

                return File(fileBytes, contentType, fileName);
            }
        }

    }
}
