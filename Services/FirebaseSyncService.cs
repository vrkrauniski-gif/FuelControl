
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Maui.Storage;

namespace FuelControl;

public static class FirebaseSyncService
{
 private const string ApiKey = "AIzaSyDWbk4L6QwgJ46TiDolWUCkm2GJ6YkVSoc";
        

    private static readonly HttpClient Http = new();

    private const string Url =
        "https://identitytoolkit.googleapis.com/v1/accounts:signUp?key=";

    public static string UsuarioFirebase =>
        Preferences.Default.Get("firebase_uid", "");

    public static async Task<bool> ConectarAnonimamenteAsync()
    {
        try
        {
            string tokenSalvo =
                Preferences.Default.Get("firebase_token", "");

            if (!string.IsNullOrWhiteSpace(tokenSalvo))
                return true;

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

            string token = json.RootElement
                .GetProperty("idToken").GetString() ?? "";

            string uid = json.RootElement
                .GetProperty("localId").GetString() ?? "";

            Preferences.Default.Set("firebase_token", token);
            Preferences.Default.Set("firebase_uid", uid);

            return !string.IsNullOrWhiteSpace(token);
        }
        catch (Exception ex)
{
    Preferences.Default.Set("firebase_erro", ex.Message);
    return false;
        }
}

}
