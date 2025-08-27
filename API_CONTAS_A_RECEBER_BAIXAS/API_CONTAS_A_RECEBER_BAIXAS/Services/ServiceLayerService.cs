using System.Text.Json;
using System.Text;
using static System.Net.WebRequestMethods;
using API_CONTAS_A_RECEBER_BAIXAS.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using DocumentFormat.OpenXml.Office2013.Drawing.ChartStyle;
using API_CONTAS_A_RECEBER_BAIXAS.DTOS;

namespace API_CONTAS_A_RECEBER_BAIXAS.Services
{
    public class ServiceLayerService
    {
        public string BANCO_DE_DADOS = "SBO_GRUPOCAMPANHA_PRD";
        public string USUARIO = "ServiceLayerJFC";
        public string SENHA = "GPCA@12;";
        public ServiceLayerService() { 
        
        }
        public HttpClient httpClient;

        public async Task<bool> CancelarDocumento(int nroDoc)
        {
            httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://picamphdb.b1cloud.com.br:50000/b1s/v2/"),
                Timeout = TimeSpan.FromMinutes(5)
            };
            var httpComLogin = await httpClient.PostAsync(httpClient.BaseAddress + "IncomingPayments/Cancel",null);
            //http.DefaultRequestHeaders.Add("Prefer", "odata.maxpagesize=13000");
            if (httpComLogin.IsSuccessStatusCode)
            {
                return true;

            }
            return false;
        }
        public void RealizarLogin()
        {
            var loginData = new
            {
                CompanyDB = BANCO_DE_DADOS,
                UserName = USUARIO,
                Password = SENHA
            };
            string json = JsonSerializer.Serialize(loginData);

            // Cria o conteúdo da requisição
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://picamphdb.b1cloud.com.br:50000/b1s/v2/"),
                Timeout = TimeSpan.FromMinutes(5)
            };
            var httpComLogin = httpClient.PostAsync(httpClient.BaseAddress + "Login", content).Result;
            //http.DefaultRequestHeaders.Add("Prefer", "odata.maxpagesize=13000");
            if (httpComLogin.IsSuccessStatusCode)
            {
                Console.WriteLine("Login realizado com sucesso!");

            }
            
        }
        public HttpResponseMessage RealizarBaixasContasAReceber(string Json)
        {
            var content = new StringContent(Json, Encoding.UTF8, "application/json");
            var result = httpClient.PostAsync("IncomingPayments", content).GetAwaiter().GetResult();
            Console.WriteLine(result.Content.ReadAsStringAsync().GetAwaiter().GetResult());
            return result;
        }
        public async Task<List<NotaDeSaidaGetDto>> BaixarRelatorioNotasSaidaAsync(int idFilial, int documentoMinimo)
        {
            string baseUrl = $"https://picamphdb.b1cloud.com.br:50000/b1s/v2/sml.svc/0028_CV_NOTAS_DE_SAIDA_CR_AUTParameters(nro_doc_minimo_nf_saida={documentoMinimo},p_filial={idFilial})/0028_CV_NOTAS_DE_SAIDA_CR_AUT";
            string nextLink = baseUrl;

            var resultadoFinal = new List<NotaDeSaidaGetDto>();

            
            {
                httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                httpClient.DefaultRequestHeaders.Add("Prefer", "odata.maxpagesize=25000");

                while (!string.IsNullOrEmpty(nextLink))
                {
                    HttpResponseMessage response = null;
                    int tentativas = 0;
                    const int maxTentativas = 3;

                    while (tentativas < maxTentativas)
                    {
                        try
                        {
                            response = await httpClient.GetAsync(nextLink);
                            response.EnsureSuccessStatusCode();
                            break;
                        }
                        catch (Exception ex)
                        {
                            tentativas++;
                            Console.WriteLine($"Tentativa {tentativas} falhou: {ex.Message}");

                            if (tentativas >= maxTentativas)
                            {
                                Console.WriteLine("Número máximo de tentativas alcançado. Encerrando processo.");
                                return resultadoFinal; // Retorna o que conseguiu até agora
                            }

                            await Task.Delay(1000);
                        }
                    }

                    string content = await response.Content.ReadAsStringAsync();

                    using JsonDocument json = JsonDocument.Parse(content);
                    JsonElement root = json.RootElement;

                    if (root.TryGetProperty("value", out JsonElement items))
                    {
                        var pageData = JsonSerializer.Deserialize<List<NotaDeSaidaGetDto>>(items.GetRawText());
                        if (pageData != null)
                            resultadoFinal.AddRange(pageData);
                    }

                    if (root.TryGetProperty("@odata.nextLink", out JsonElement nextLinkElement))
                    {
                        nextLink = nextLinkElement.GetString();
                    }
                    else
                    {
                        nextLink = null;
                    }
                }
            }

            return resultadoFinal;
        }

        public async Task<List<NotaDeSaidaGetDto>> BaixarRelatorioNotasDevolucaoAsync(int idFilial, int documentoMinimo)
        {
            string baseUrl = $"https://suaapi.com/0029_CV_NOTAS_DE_DEVOLUCAO_CR_AUTParameters(nro_doc_minimo_nf_saida={documentoMinimo},p_filial={idFilial})/0029_CV_NOTAS_DE_DEVOLUCAO_CR_AUT";
            string nextLink = baseUrl;

            var resultadoFinal = new List<NotaDeSaidaGetDto>();


            {
                httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                httpClient.DefaultRequestHeaders.Add("Prefer", "odata.maxpagesize=25000");

                while (!string.IsNullOrEmpty(nextLink))
                {
                    HttpResponseMessage response = null;
                    int tentativas = 0;
                    const int maxTentativas = 3;

                    while (tentativas < maxTentativas)
                    {
                        try
                        {
                            response = await httpClient.GetAsync(nextLink);
                            response.EnsureSuccessStatusCode();
                            break;
                        }
                        catch (Exception ex)
                        {
                            tentativas++;
                            Console.WriteLine($"Tentativa {tentativas} falhou: {ex.Message}");

                            if (tentativas >= maxTentativas)
                            {
                                Console.WriteLine("Número máximo de tentativas alcançado. Encerrando processo.");
                                return resultadoFinal; // Retorna o que conseguiu até agora
                            }

                            await Task.Delay(1000);
                        }
                    }

                    string content = await response.Content.ReadAsStringAsync();

                    using JsonDocument json = JsonDocument.Parse(content);
                    JsonElement root = json.RootElement;

                    if (root.TryGetProperty("value", out JsonElement items))
                    {
                        var pageData = JsonSerializer.Deserialize<List<NotaDeSaidaGetDto>>(items.GetRawText());
                        if (pageData != null)
                            resultadoFinal.AddRange(pageData);
                    }

                    if (root.TryGetProperty("@odata.nextLink", out JsonElement nextLinkElement))
                    {
                        nextLink = nextLinkElement.GetString();
                    }
                    else
                    {
                        nextLink = null;
                    }
                }
            }

            return resultadoFinal;
        }
    }
}
