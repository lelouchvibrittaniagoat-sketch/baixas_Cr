using System.Text.Json;
using System.Text;
using static System.Net.WebRequestMethods;
using API_CONTAS_A_RECEBER_BAIXAS.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using DocumentFormat.OpenXml.Office2013.Drawing.ChartStyle;

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
    }
}
