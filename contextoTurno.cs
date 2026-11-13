
using Microsoft.Maui.Storage;
using System.Security.Cryptography;
using System.Text;

namespace FuelControl;

public static class ContextoTurno
{
    private const string ChaveTurno = "fuelcontrol_turno";
    private const string ChaveDispositivo = "fuelcontrol_dispositivo";

    private const string ChaveAdmin = "fuelcontrol_admin_hash";
    private const string ChavePinA = "fuelcontrol_pin_a";
    private const string ChavePinB = "fuelcontrol_pin_b";
    private const string ChavePinC = "fuelcontrol_pin_c";

    public static bool EhAdministrador { get; set; }

    public static bool AcessoConfigurado =>
        !string.IsNullOrWhiteSpace(
            Preferences.Default.Get(ChaveAdmin, ""));

    public static string Turno
    {
        get => Preferences.Default.Get(ChaveTurno, "");
        set
        {
            if (value != "A" && value != "B" && value != "C")
                throw new ArgumentException("Turno inválido.");

            Preferences.Default.Set(ChaveTurno, value);
        }
    }

    public static string Dispositivo
    {
        get
        {
            string id = Preferences.Default.Get(ChaveDispositivo, "");

            if (string.IsNullOrWhiteSpace(id))
            {
                id = Guid.NewGuid().ToString("N");
                Preferences.Default.Set(ChaveDispositivo, id);
            }

            return id;
        }
    }

    private static string GerarHash(string senha)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(senha);
        byte[] hash = SHA256.HashData(bytes);

        return Convert.ToBase64String(hash);
    }

    public static void ConfigurarAcessos(
        string senhaAdmin,
        string pinA,
        string pinB,
        string pinC)
    {
        Preferences.Default.Set(
            ChaveAdmin, GerarHash(senhaAdmin));

        Preferences.Default.Set(
            ChavePinA, GerarHash(pinA));

        Preferences.Default.Set(
            ChavePinB, GerarHash(pinB));

        Preferences.Default.Set(
            ChavePinC, GerarHash(pinC));
    }

    public static bool ValidarAdministrador(string senha)
    {
        string salvo = Preferences.Default.Get(ChaveAdmin, "");

        return !string.IsNullOrEmpty(salvo) &&
               GerarHash(senha) == salvo;
    }

    public static bool ValidarTurno(string turno, string pin)
    {
        string chave = turno switch
        {
            "A" => ChavePinA,
            "B" => ChavePinB,
            "C" => ChavePinC,
            _ => ""
        };

        if (string.IsNullOrEmpty(chave))
            return false;

        string salvo = Preferences.Default.Get(chave, "");

        return !string.IsNullOrEmpty(salvo) &&
               GerarHash(pin) == salvo;
    }

    public static void AlterarPin(string turno, string novoPin)
    {
        string chave = turno switch
        {
            "A" => ChavePinA,
            "B" => ChavePinB,
            "C" => ChavePinC,
            _ => ""
        };

        if (string.IsNullOrEmpty(chave))
            throw new ArgumentException("Turno inválido.");

        Preferences.Default.Set(chave, GerarHash(novoPin));
    }
}
