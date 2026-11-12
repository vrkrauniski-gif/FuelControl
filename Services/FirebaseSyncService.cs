
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Networking;

namespace FuelControl;

public static class FirebaseSyncService
{
    private const string ApiKey =
        "AIzaSyDWbk4L6QwgJ46TiDolWUCkm2GJ6YkVSoc";

    private const string BaseUrl =
        "https://firestore.googleapis.com/v1/projects/fuelcontrol-vrk/databases/(default)/documents/abastecimentos";
    private const string BaseUrlViagens =
    "https://firestore.googleapis.com/v1/projects/fuelcontrol-vrk/databases/(default)/documents/viagens";
    private const string RefreshUrl =
        "https://securetoken.googleapis.com/v1/token?key=";

    private const string SignUpUrl =
        "https://identitytoolkit.googleapis.com/v1/accounts:signUp?key=";

    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    private static readonly SemaphoreSlim Bloqueio = new(1, 1);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static string UsuarioFirebase =>
        Preferences.Default.Get("firebase_uid", "");

    public static string UltimoErro =>
        Preferences.Default.Get("firebase_erro", "");

    private static void Erro(string mensagem)
    {
        Preferences.Default.Set("firebase_erro", mensagem);
    }

    private static void LimparErro()
    {
        Preferences.Default.Remove("firebase_erro");
    }

    private static void LimparAutenticacao()
    {
        Preferences.Default.Remove("firebase_token");
        Preferences.Default.Remove("firebase_refresh_token");
        Preferences.Default.Remove("firebase_uid");
    }

    public static async Task<bool> ConectarAnonimamenteAsync()
    {
        await Bloqueio.WaitAsync();

        try
        {
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            {
                Erro("Sem conexão com a internet.");
                return false;
            }

            string refreshToken =
                Preferences.Default.Get("firebase_refresh_token", "");

            if (!string.IsNullOrWhiteSpace(refreshToken))
            {
                using var corpo = new FormUrlEncodedContent(
                    new Dictionary<string, string>
                    {
                        ["grant_type"] = "refresh_token",
                        ["refresh_token"] = refreshToken
                    });

                using var resposta = await Http.PostAsync(
                    RefreshUrl + ApiKey, corpo);

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

                if (conteudo.Contains("INVALID_REFRESH_TOKEN",
                    StringComparison.OrdinalIgnoreCase) ||
                    conteudo.Contains("TOKEN_EXPIRED",
                    StringComparison.OrdinalIgnoreCase) ||
                    conteudo.Contains("USER_DISABLED",
                    StringComparison.OrdinalIgnoreCase))
                {
                    LimparAutenticacao();
                }
                else
                {
                    Erro("Falha ao renovar autenticação: " + conteudo);
                    return false;
                }
            }

            using var novoCorpo = new StringContent(
                "{\"returnSecureToken\":true}",
                Encoding.UTF8,
                "application/json");

            using var novaResposta = await Http.PostAsync(
                SignUpUrl + ApiKey, novoCorpo);

            string novoConteudo =
                await novaResposta.Content.ReadAsStringAsync();

            if (!novaResposta.IsSuccessStatusCode)
            {
                Erro("Falha na autenticação: " + novoConteudo);
                return false;
            }

            using var novoJson = JsonDocument.Parse(novoConteudo);
            var usuario = novoJson.RootElement;

            Preferences.Default.Set(
                "firebase_token",
                usuario.GetProperty("idToken").GetString() ?? "");

            Preferences.Default.Set(
                "firebase_refresh_token",
                usuario.GetProperty("refreshToken").GetString() ?? "");

            Preferences.Default.Set(
                "firebase_uid",
                usuario.GetProperty("localId").GetString() ?? "");

            LimparErro();

            return !string.IsNullOrWhiteSpace(UsuarioFirebase);
        }
        catch (Exception ex)
        {
            Erro("Erro de autenticação: " + ex.Message);
            return false;
        }
        finally
        {
            Bloqueio.Release();
        }
    }

    private static HttpRequestMessage CriarRequisicao(
        HttpMethod metodo, string url, string token)
    {
        var requisicao = new HttpRequestMessage(metodo, url);

        requisicao.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        return requisicao;
    }

    public static async Task<bool> SalvarAbastecimentoAsync(
        Abastecimento registro)
    {
        try
        {
            if (registro == null ||
                string.IsNullOrWhiteSpace(registro.Id))
            {
                Erro("Abastecimento sem identificador.");
                return false;
            }

            if (registro.Turno is not ("A" or "B" or "C"))
            {
                Erro("Turno inválido no abastecimento.");
                return false;
            }

            if (!await ConectarAnonimamenteAsync())
                return false;

            string token = Preferences.Default.Get(
                "firebase_token", "");

            var documento = new
            {
                fields = new
                {
                    payload = new
                    {
                        stringValue =
                            JsonSerializer.Serialize(registro, JsonOptions)
                    },
                    turno = new
                    {
                        stringValue = registro.Turno
                    },
                    dispositivo = new
                    {
                        stringValue = registro.EditadoPorDispositivo
                    },
                    editadoEmUtc = new
                    {
                        timestampValue =
                            registro.EditadoEmUtc.ToUniversalTime()
                                .ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'")
                    }
                }
            };

            string url = BaseUrl + "/" +
                Uri.EscapeDataString(registro.Id);

            using var requisicao =
                CriarRequisicao(HttpMethod.Patch, url, token);

            requisicao.Content = new StringContent(
                JsonSerializer.Serialize(documento),
                Encoding.UTF8,
                "application/json");

            using var resposta = await Http.SendAsync(requisicao);

            if (!resposta.IsSuccessStatusCode)
            {
                Erro("Erro ao salvar: " +
                    await resposta.Content.ReadAsStringAsync());
                return false;
            }

            LimparErro();
            return true;
        }
        catch (Exception ex)
        {
            Erro("Erro ao salvar abastecimento: " + ex.Message);
            return false;
        }
    }

    
public static async Task<bool> SalvarViagemAsync(Viagem viagem)
{
    try
    {
        if (viagem == null ||
            string.IsNullOrWhiteSpace(viagem.Id))
        {
            Erro("Viagem sem identificador.");
            return false;
        }

        if (viagem.Turno is not ("A" or "B" or "C"))
        {
            Erro("Turno inválido na viagem.");
            return false;
        }

        if (!await ConectarAnonimamenteAsync())
            return false;

        string token = Preferences.Default.Get(
            "firebase_token", "");

        var documento = new
        {
            fields = new
            {
                payload = new
                {
                    stringValue =
                        JsonSerializer.Serialize(viagem, JsonOptions)
                },
                turno = new
                {
                    stringValue = viagem.Turno
                },
                dispositivo = new
                {
                    stringValue = viagem.EditadoPorDispositivo
                },
                editadoEmUtc = new
                {
                    timestampValue =
                        viagem.EditadoEmUtc.ToUniversalTime()
                            .ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'")
                }
            }
        };

        string url = BaseUrlViagens + "/" +
            Uri.EscapeDataString(viagem.Id);

        using var requisicao =
            CriarRequisicao(HttpMethod.Patch, url, token);

        requisicao.Content = new StringContent(
            JsonSerializer.Serialize(documento),
            Encoding.UTF8,
            "application/json");

        using var resposta = await Http.SendAsync(requisicao);

        if (!resposta.IsSuccessStatusCode)
        {
            Erro("Erro ao salvar viagem: " +
                await resposta.Content.ReadAsStringAsync());
            return false;
        }

        LimparErro();
        return true;
    }
    catch (Exception ex)
    {
        Erro("Erro ao salvar viagem: " + ex.Message);
        return false;
    }
}

    public static async Task<List<Abastecimento>?>
        ListarAbastecimentosAsync()
    {
        try
        {
            if (!await ConectarAnonimamenteAsync())
                return null;

            string token = Preferences.Default.Get(
                "firebase_token", "");

            var resultado = new List<Abastecimento>();
            string? proximaPagina = null;

            do
            {
                string url = BaseUrl + "?pageSize=1000";

                if (!string.IsNullOrWhiteSpace(proximaPagina))
                {
                    url += "&pageToken=" +
                        Uri.EscapeDataString(proximaPagina);
                }

                using var requisicao =
                    CriarRequisicao(HttpMethod.Get, url, token);

                using var resposta = await Http.SendAsync(requisicao);

                if (!resposta.IsSuccessStatusCode)
                {
                    Erro("Erro ao listar: " +
                        await resposta.Content.ReadAsStringAsync());
                    return null;
                }

                string conteudo =
                    await resposta.Content.ReadAsStringAsync();

                using var json = JsonDocument.Parse(conteudo);
                var raiz = json.RootElement;

                if (raiz.TryGetProperty("documents", out var documentos))
                {
                    foreach (var documento in documentos.EnumerateArray())
                    {
                        if (!documento.TryGetProperty(
                            "fields", out var campos))
                            continue;

                        if (!campos.TryGetProperty(
                            "payload", out var payload))
                            continue;

                        if (!payload.TryGetProperty(
                            "stringValue", out var valor))
                            continue;

                        string? dados = valor.GetString();

                        if (string.IsNullOrWhiteSpace(dados))
                            continue;

                        var registro =
                            JsonSerializer.Deserialize<Abastecimento>(
                                dados, JsonOptions);

                        if (registro == null)
                            continue;

                        if (string.IsNullOrWhiteSpace(registro.Id))
                            continue;

                        // Compatibilidade com registros antigos.
                        if (string.IsNullOrWhiteSpace(registro.Turno) &&
                            campos.TryGetProperty("turno", out var turno))
                        {
                            if (turno.TryGetProperty(
                                "stringValue", out var turnoValor))
                            {
                                registro.Turno =
                                    turnoValor.GetString() ?? "";
                            }
                        }

                        resultado.Add(registro);
                    }
                }

                proximaPagina =
                    raiz.TryGetProperty(
                        "nextPageToken", out var proximo)
                    ? proximo.GetString()
                    : null;

            } while (!string.IsNullOrWhiteSpace(proximaPagina));

            // Impede duplicidade por ID na lista retornada.
            resultado = resultado
                .GroupBy(x => x.Id)
                .Select(g => g.OrderByDescending(
                    x => x.EditadoEmUtc).First())
                .OrderByDescending(x => x.Data)
                .ToList();

            LimparErro();
            return resultado;
        }
        catch (Exception ex)
        {
            Erro("Erro ao listar abastecimentos: " + ex.Message);
            return null;
        }
    }
}
