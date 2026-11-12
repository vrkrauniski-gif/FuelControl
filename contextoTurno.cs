
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
}

