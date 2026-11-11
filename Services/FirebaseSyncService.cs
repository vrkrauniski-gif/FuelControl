

using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Maui.Storage;

namespace FuelControl;

public static class FirebaseSyncService
{
    // Mantenha aqui a chave que já existe no seu arquivo.
    private const string ApiKey = "AIzaSyDWbk4L6QwgJ46TiDolWUCkm2GJ6YkVSoc";
        

    private static readonly HttpClient Http = new();

    private const string Url =
        "https://identitytoolkit.googleapis.com/v1/accounts:signUp?key=";

    private const string RefreshUrl =
        "https://securetoken.googleapis.com/v1/token?key=";

    public static string UsuarioFirebase =>
        Preferences.Default.Get("firebase_uid", "");

    public static async Task<bool> ConectarAnonimamenteAsync()
    {
        try
        {
            string refreshToken =
                Preferences.Default.Get("firebase_refresh_token", "");

            // Renova a autenticação quando já existe refresh token.
            if (!string.IsNullOrWhiteSpace(refreshToken))
            {
                var dados = new Dictionary<string, string>
                {
                    ["grant_type"] = "refresh_token",
                    ["refresh_token"] = refreshToken
                };

                var respostaRefresh = await Http.PostAsync(
                    RefreshUrl + ApiKey,
                    new FormUrlEncodedContent(dados));

                string conteudoRefresh =
                    await respostaRefresh.Content.ReadAsStringAsync();

                if (respostaRefresh.IsSuccessStatusCode)
                {
                    using var jsonRefresh =
                        JsonDocument.Parse(conteudoRefresh);

                    var raiz = jsonRefresh.RootElement;

                    Preferences.Default.Set(
                        "firebase_token",
                        raiz.GetProperty("id_token").GetString() ?? "");

                    Preferences.Default.Set(
                        "firebase_refresh_token",
                        raiz.GetProperty("refresh_token").GetString() ?? "");

                    Preferences.Default.Set(
                        "firebase_uid",
                        raiz.GetProperty("user_id").GetString() ?? "");

                    return true;
                }

                Preferences.Default.Remove("firebase_token");
                Preferences.Default.Remove("firebase_refresh_token");
                Preferences.Default.Remove("firebase_uid");
            }

            // Verifica o token antigo, se existir.
            string tokenSalvo =
                Preferences.Default.Get("firebase_token", "");

            if (!string.IsNullOrWhiteSpace(tokenSalvo))
            {
                var verificacao = await Http.PostAsync(
                    "https://identitytoolkit.googleapis.com/v1/accounts:lookup?key=" + ApiKey,
                    new StringContent(
                        JsonSerializer.Serialize(new { idToken = tokenSalvo }),
                        Encoding.UTF8,
                        "application/json"));

                if (verificacao.IsSuccessStatusCode)
                    return true;

                Preferences.Default.Remove("firebase_token");
                Preferences.Default.Remove("firebase_uid");
            }

            // Cria autenticação anônima.
            var resposta = await Http.PostAsync(
                Url + ApiKey,
                new StringContent(
                    "{\"returnSecureToken\":true}",
                    Encoding.UTF8,
                    "application/json"));

            string conteudo = await resposta.Content.ReadAsStringAsync();

            if (!resposta.IsSuccessStatusCode)
            {
                Preferences.Default.Set(
                    "firebase_erro",
                    "Erro HTTP " + (int)resposta.StatusCode + ": " + conteudo);

                return false;
            }

            using var json = JsonDocument.Parse(conteudo);

            var raizNova = json.RootElement;

            Preferences.Default.Set(
                "firebase_token",
                raizNova.GetProperty("idToken").GetString() ?? "");

            Preferences.Default.Set(
                "firebase_refresh_token",
                raizNova.GetProperty("refreshToken").GetString() ?? "");

            Preferences.Default.Set(
                "firebase_uid",
                raizNova.GetProperty("localId").GetString() ?? "");

            Preferences.Default.Remove("firebase_erro");

            return !string.IsNullOrWhiteSpace(UsuarioFirebase);
        }
        catch (Exception ex)
        {
        Preferences.Default.Set(
    "firebase_erro",
    "Falha na conexão (" + ex.GetType().Name + "): " + ex.Message);

return false;
        }
    }
}
