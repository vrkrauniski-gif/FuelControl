

using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Maui.Storage;

namespace FuelControl;

public static class FirebaseSyncService
{
    // Mantenha a mesma chave API do seu projeto.
    private const string ApiKey = "AIzaSyDWbk4L6QwgJ46TiDolWUCkm2GJ6YkVSoc";

    private static readonly HttpClient Http = new HttpClient
    {
        Timeout = TimeSpan.FromSeconds(15)
    };

    private static readonly SemaphoreSlim Bloqueio = new(1, 1);

    private const string RefreshUrl =
        "https://securetoken.googleapis.com/v1/token?key=";

    private const string SignUpUrl =
        "https://identitytoolkit.googleapis.com/v1/accounts:signUp?key=";

    public static string UsuarioFirebase =>
        Preferences.Default.Get("firebase_uid", "");

    public static string UltimoErro =>
        Preferences.Default.Get("firebase_erro", "");

    public static async Task<bool> ConectarAnonimamenteAsync()
    {
        await Bloqueio.WaitAsync();

        try
        {
            // Se não há internet, mantém os dados e tokens salvos.
            if (Connectivity.Current.NetworkAccess
                != NetworkAccess.Internet)
            {
                RegistrarErro("Sem conexão com a internet.");
                return false;
            }

            string refreshToken =
                Preferences.Default.Get("firebase_refresh_token", "");

            // Tenta renovar a autenticação existente.
            if (!string.IsNullOrWhiteSpace(refreshToken))
            {
                using var dados = new FormUrlEncodedContent(
                    new Dictionary<string, string>
                    {
                        ["grant_type"] = "refresh_token",
                        ["refresh_token"] = refreshToken
                    });

                using var resposta = await Http.PostAsync(
                    RefreshUrl + ApiKey, dados);

                string conteudo =
                    await resposta.Content.ReadAsStringAsync();

                if (resposta.IsSuccessStatusCode)
                {
                    using var json = JsonDocument.Parse(conteudo);
                    var raiz = json.RootElement;

                    Preferences.Default.Set(
                        "firebase_token",
                        raiz.GetProperty("id_token").GetString() ?? "");

                    Preferences.Default.Set(
                        "firebase_refresh_token",
                        raiz.GetProperty("refresh_token").GetString() ?? "");

                    Preferences.Default.Set(
                        "firebase_uid",
                        raiz.GetProperty("user_id").GetString() ?? "");

                    LimparErro();
                    return true;
                }

                // Somente credencial definitivamente inválida
                // autoriza descartar a autenticação anterior.
                if (resposta.StatusCode == HttpStatusCode.BadRequest &&
                    conteudo.Contains("INVALID_REFRESH_TOKEN",
                        StringComparison.OrdinalIgnoreCase))
                {
                    LimparAutenticacao();
                }
                else
                {
                    RegistrarErro(
                        $"Falha ao renovar token: HTTP {(int)resposta.StatusCode}. {conteudo}");

                    return false;
                }
            }

            // Cria usuário anônimo quando não existe autenticação válida.
            using var corpo = new StringContent(
                "{\"returnSecureToken\":true}",
                Encoding.UTF8,
                "application/json");

            using var novaResposta = await Http.PostAsync(
                SignUpUrl + ApiKey, corpo);

            string novoConteudo =
                await novaResposta.Content.ReadAsStringAsync();

            if (!novaResposta.IsSuccessStatusCode)
            {
                RegistrarErro(
                    $"Falha na autenticação: HTTP {(int)novaResposta.StatusCode}. {novoConteudo}");

                return false;
            }

            using var novoJson = JsonDocument.Parse(novoConteudo);
            var novoUsuario = novoJson.RootElement;

            Preferences.Default.Set(
                "firebase_token",
                novoUsuario.GetProperty("idToken").GetString() ?? "");

            Preferences.Default.Set(
                "firebase_refresh_token",
                novoUsuario.GetProperty("refreshToken").GetString() ?? "");

            Preferences.Default.Set(
                "firebase_uid",
                novoUsuario.GetProperty("localId").GetString() ?? "");

            LimparErro();

            return !string.IsNullOrWhiteSpace(UsuarioFirebase);
        }
        catch (TaskCanceledException)
        {
            RegistrarErro("Tempo limite excedido na conexão com Firebase.");
            return false;
        }
        catch (HttpRequestException ex)
        {
            RegistrarErro(
                $"Falha de rede ({ex.GetType().Name}): {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            RegistrarErro(
                $"Erro Firebase ({ex.GetType().Name}): {ex.Message}");
            return false;
        }
        finally
        {
            Bloqueio.Release();
        }
    }
    public static async Task<bool> SalvarAbastecimentoAsync(
    Abastecimento registro)
{
    try
    {
        if (!await ConectarAnonimamenteAsync())
            return false;

        string token = Preferences.Default.Get(
            "firebase_token", "");

        if (string.IsNullOrWhiteSpace(token))
            return false;

        string url =
            "https://firestore.googleapis.com/v1/projects/fuelcontrol-vrk/databases/(default)/documents/abastecimentos/"
            + Uri.EscapeDataString(registro.Id);

        var documento = new
        {
            fields = new
            {
                payload = new
                {
                    stringValue = JsonSerializer.Serialize(registro)
                },
                turno = new
                {
                    stringValue = registro.Turno
                },
                dispositivo = new
                {
                    stringValue = registro.EditadoPorDispositivo
                }
            }
        };

        using var requisicao = new HttpRequestMessage(
            HttpMethod.Patch, url);

        requisicao.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer", token);

        requisicao.Content = new StringContent(
            JsonSerializer.Serialize(documento),
            Encoding.UTF8,
            "application/json");

        using var resposta = await Http.SendAsync(requisicao);

        if (!resposta.IsSuccessStatusCode)
        {
            string erro = await resposta.Content.ReadAsStringAsync();
            RegistrarErro("Erro ao salvar abastecimento: " + erro);
            return false;
        }

        LimparErro();
        return true;
    }
    catch (Exception ex)
    {
        RegistrarErro("Erro ao sincronizar: " + ex.Message);
        return false;
    }
    }

    private static void LimparAutenticacao()
    {
        Preferences.Default.Remove("firebase_token");
        Preferences.Default.Remove("firebase_refresh_token");
        Preferences.Default.Remove("firebase_uid");
    }

    private static void RegistrarErro(string mensagem)
    {
        Preferences.Default.Set("firebase_erro", mensagem);
    }

    private static void LimparErro()
    {
        Preferences.Default.Remove("firebase_erro");
    }
}
