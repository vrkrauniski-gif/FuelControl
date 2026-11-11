
using Microsoft.Maui.Storage;

namespace FuelControl;

public static class ContextoTurno
{
    private const string ChaveTurno = "fuelcontrol_turno";
    private const string ChaveDispositivo = "fuelcontrol_dispositivo";

    public static string Turno
    {
        get => Preferences.Default.Get(ChaveTurno, "");
        set
        {
            if (value != "A" && value != "B" && value != "C")
                throw new ArgumentException("O turno deve ser A, B ou C.");

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
}

public class RegistroAuditoria
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Turno { get; set; } = "";

    public DateTime CriadoEmUtc { get; set; }

    public string CriadoPorDispositivo { get; set; } = "";

    public DateTime EditadoEmUtc { get; set; }

    public string EditadoPorDispositivo { get; set; } = "";
}

