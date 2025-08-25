using System.Runtime.InteropServices;

public class FileDownloadService
{
    public async Task<string> SalvarArquivoAsync(byte[] dados, string nomeArquivo)
    {
        string pastaDestino;

        if (OperatingSystem.IsWindows())
        {
            // 🪟 Pega a pasta "Downloads" do usuário
            pastaDestino = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        }
        else
        {
            // Outros sistemas: salva no cache
            pastaDestino = FileSystem.CacheDirectory;
        }

        // Garante que a pasta existe
        Directory.CreateDirectory(pastaDestino);

        var caminhoCompleto = Path.Combine(pastaDestino, nomeArquivo);
        await File.WriteAllBytesAsync(caminhoCompleto, dados);

        return caminhoCompleto;
    }
}
