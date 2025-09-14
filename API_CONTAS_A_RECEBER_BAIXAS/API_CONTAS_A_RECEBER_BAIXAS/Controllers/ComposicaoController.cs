using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using API_CONTAS_A_RECEBER_BAIXAS.Models_context;
using API_CONTAS_A_RECEBER_BAIXAS.Services;
using API_CONTAS_A_RECEBER_BAIXAS.DTOS;

[ApiController]
[Route("[controller]")]
public class ComposicaoController : ControllerBase
{
    private readonly ContasAReceberDbContext _db;
    private readonly ComposicaoService _composicaoService;
    private readonly ServiceLayerService _serviceLayerService;
    private readonly RelatoriosService _relatoriosService;
    private readonly ILogger<ComposicaoController> _logger;
    private readonly IWebHostEnvironment _env;

    public ComposicaoController(
        ContasAReceberDbContext dbContext,
        ComposicaoService composicaoService,
        ServiceLayerService serviceLayerService,
        RelatoriosService relatoriosService,
        ILogger<ComposicaoController> logger,
        IWebHostEnvironment env)
    {
        _db = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _composicaoService = composicaoService ?? throw new ArgumentNullException(nameof(composicaoService));
        _serviceLayerService = serviceLayerService ?? throw new ArgumentNullException(nameof(serviceLayerService));
        _relatoriosService = relatoriosService ?? throw new ArgumentNullException(nameof(relatoriosService));
        _logger = logger;
        _env = env;
    }

    [HttpGet("BaixasRegistradasPelaAutomacao")]
    public async Task<IActionResult> Index()
    {
        var listaDeNotas = await _composicaoService.Context.BaixasCR
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

        return Ok(dtoBaixasCRs);
    }

    [HttpGet("GetContaBancaria")]
    public async Task<IActionResult> GetContaBancaria()
    {
        var contas = await _composicaoService.Context.BaixasCR
            .Select(x => x.conta_contabil)
            .Where(c => c != null)
            .Distinct()
            .ToListAsync();

        return Ok(contas);
    }

    [HttpGet("GetRedeCr")]
    public async Task<IActionResult> GetRedeCr()
    {
        var redes = await _composicaoService.Context.BaixasCR
            .Select(x => x.rede)
            .Where(r => r != null)
            .Distinct()
            .ToListAsync();
        return Ok(redes);
    }

    [HttpDelete("CancelarComposicao")]
    public async Task<IActionResult> CancelarComposicao(int idBaixasCr)
    {
        var baixasCR = await _composicaoService.Context.BaixasCR
            .FirstOrDefaultAsync(x => x.id == idBaixasCr);

        if (baixasCR == null)
            return BadRequest(new { Erros = "Essa baixa não existe em nosso banco de dados" });

        if (baixasCR.nro_baixas == null || !baixasCR.nro_baixas.Any())
            return BadRequest(new { Erros = "Não há documentos para cancelar nesta composição." });

        var dict = new Dictionary<string, bool>();
        foreach (var nro in baixasCR.nro_baixas)
        {
            try
            {
                var cancelado = await _composicaoService.serviceLayerService.CancelarDocumento(nro);
                dict[nro.ToString()] = cancelado;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao cancelar documento {Nro}", nro);
                dict[nro.ToString()] = false;
            }
        }

        return Ok(dict);
    }

    [HttpGet("GetComposicaoComErros")]
    public async Task<IActionResult> GetComposicaoComErros(int idBaixasCr)
    {
        var baixasCR = await _composicaoService.Context.BaixasCR
            .FirstOrDefaultAsync(x => x.id == idBaixasCr);

        if (baixasCR == null) return BadRequest(new { Erros = "Essa baixa não existe em nosso banco de dados" });

        byte[] result;
        try
        {
            result = await _composicaoService.CriarComposicaoComErros(idBaixasCr);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar composicao com erros para id {Id}", idBaixasCr);
            return StatusCode(500, new { Erros = "Erro interno ao gerar o arquivo." });
        }

        if (result == null || result.Length == 0)
            return NotFound(new { Erros = "Nenhum arquivo de composição com erros foi encontrado." });

        var nomeArquivoCriado = $"ComposicaoComErros_{idBaixasCr}.{baixasCR.extensao}";
        var pasta = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "arquivos");
        if (!Directory.Exists(pasta)) Directory.CreateDirectory(pasta);

        var caminhoCompleto = Path.Combine(pasta, nomeArquivoCriado);
        await System.IO.File.WriteAllBytesAsync(caminhoCompleto, result);

        var urlDownload = $"{Request.Scheme}://{Request.Host}/arquivos/{nomeArquivoCriado}";
        return Ok(new { Url = urlDownload });
    }

    // NOTE: mantive seu Upload em grande parte, mas com validações e async corretos.
    [HttpPost("UploadDeArquivoBaixasCR")]
    public async Task<IActionResult> UploudArquivoDeBaixaExcel(IFormFile file, [FromForm] string adiantamentoCliente)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { Erros = "Arquivo inválido." });

        var extensaoValida = new[] { ".xlsx", ".xlsm" };
        var extensao = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!extensaoValida.Contains(extensao))
            return BadRequest(new { Erros = "Extensão de arquivo não suportada. Apenas .xlsx/.xlsm" });

        if (file.Length > (10 * 1024 * 1024)) // exemplo: 10MB limite
            return BadRequest(new { Erros = "Arquivo muito grande." });

        try
        {
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);

            // load ClosedXML
            using var workbook = new ClosedXML.Excel.XLWorkbook(memoryStream);

            var excelService = new ExcelService(workbook);
            var erroService = new ErroService();

            var apenasHaUmaSheet = excelService.ValidaQuantidadeDeSheets();
            var headersOk = excelService.ValidarHeaders();
            var inicioDeDados = excelService.ValidarInicioDeNotas();

            var composicao = _composicaoService.GetComposicao(workbook);
            var keyValuePairs = _composicaoService.GetDocsMinimos(composicao.ComposicaoCr);
            var clsNegativos = _composicaoService.VerificarCLsNegativos(composicao.ComposicaoCr.Columns.ToArray());

            if (clsNegativos.Any())
            {
                var arquivoAtualizado = _composicaoService.AtualizarPlanilhaComErrosPorLinha(memoryStream.ToArray(), clsNegativos);
                var nomeArquivoCriado = $"arquivo_{DateTime.Now:yyyyMMdd_HHmmss}{extensao}";
                var pasta = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "arquivos");
                if (!Directory.Exists(pasta)) Directory.CreateDirectory(pasta);
                var caminhoCompleto = Path.Combine(pasta, nomeArquivoCriado);
                await System.IO.File.WriteAllBytesAsync(caminhoCompleto, arquivoAtualizado);
                var urlDownload = $"{Request.Scheme}://{Request.Host}/arquivos/{nomeArquivoCriado}";
                return BadRequest(new { Url = urlDownload });
            }

            erroService.ValidarErroDeEstrutura(headersOk, apenasHaUmaSheet, inicioDeDados);
            if (!apenasHaUmaSheet || !inicioDeDados)
                return BadRequest(new { Erros = "Verifique se arquivo possui apenas 1 sheet e se os dados de notas começam na linha 10!" });

            var filial = _composicaoService.incomingPaymentsService.GetIdEmpresaPorNome(composicao.Filial);
            if (filial == null) return BadRequest(new { Erros = "Filial incorreta" });

            int ns = keyValuePairs.TryGetValue("NS", out var tempNs) ? tempNs : 0;
            int ds = keyValuePairs.TryGetValue("DS", out var tempDs) ? tempDs : 0;

            await _relatoriosService.AtualizarRelatorioDeBaixas(filial.IdSap, ns, ds);

            composicao.notaDeSaidaGetDtos = _relatoriosService.notaDeSaidaGetDtos;
            composicao.notaDeDevolucaoGetDtos = _relatoriosService.notaDeDevolucaoGetDtos;

            var baixasCR = _composicaoService.SalvarInstanciaBaixa(memoryStream.ToArray(), erroService.GetListaDeErrosString(), file.FileName, composicao, extensao, null, adiantamentoCliente);

            var headersComProblema = headersOk.Where(x => x.Value != "OK").Select(x => x.Key);
            if (headersComProblema.Any())
                return BadRequest(new { Erros = $"Os headers: {string.Join(", ", headersComProblema)} não condizem com os cabeçalhos válidos." });

            var composicaoResult = await _composicaoService.MainExecution(composicao, erroService.listaComErros, adiantamentoCliente, baixasCR);

            if (_composicaoService.listaComErrosNotasJson.Count > 0)
            {
                var errosString = JsonSerializer.Serialize(_composicaoService.listaComErrosNotasJson);
                _composicaoService.SalvarInstanciaBaixa(null, errosString, file.FileName, composicao, extensao, _composicaoService.listaComErrosNotasJson, adiantamentoCliente);
                return BadRequest(new { Erros = erroService.GetListaDeErrosTratadosString() });
            }

            if (erroService.listaComErros.Count > 0)
            {
                var errosString = string.Join(", ", erroService.listaComErros);
                erroService.CriarDevolutivaDeErrosBaseadoEmDePara();
                _composicaoService.SalvarInstanciaBaixa(null, erroService.GetListaDeErrosTratadosString(), file.FileName, composicao, extensao, _composicaoService.listaComErrosNotasJson, adiantamentoCliente);
                return BadRequest(new { Erros = erroService.GetListaDeErrosTratadosString() });
            }

            // tudo ok
            baixasCR.status = "SEM ERROS";
            baixasCR.nro_baixas.AddRange(composicaoResult.documentoCriados);
            _composicaoService.Context.BaixasCR.Update(baixasCR);
            await _composicaoService.Context.SaveChangesAsync();

            return Ok(new { Mensagem = "Processado com sucesso", Id = baixasCR.id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro no upload/processamento do arquivo");
            return StatusCode(500, new { Erros = "Erro interno ao processar o arquivo." });
        }
    }

    // ... outros endpoints refatorados conforme o padrão acima
}
