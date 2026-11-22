using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using System.Globalization;
using System.Text.Json;

#if ANDROID
using AndroidPaint = Android.Graphics.Paint;
using AndroidColor = Android.Graphics.Color;
using AndroidPdfDocument = Android.Graphics.Pdf.PdfDocument;
#endif

namespace FuelControl;

public class MainPage : ContentPage
{
    const string Chave = "fuelcontrol_historico";
    const string ChaveVeiculos = "fuelcontrol_veiculos";
    const string ChaveCargas = "fuelcontrol_viagens";
    const string ChaveTanquesArla = "fuelcontrol_tanques_arla";

    readonly CultureInfo pt = CultureInfo.GetCultureInfo("pt-BR");

    List<Abastecimento> abastecimentos = new();
    List<Viagem> viagens = new();
    Dictionary<string, DadosVeiculo> veiculos = new(StringComparer.OrdinalIgnoreCase);
    Dictionary<string, DadosArla> tanquesArla = new(StringComparer.OrdinalIgnoreCase);

    Entry veiculo = new(), motorista = new();
    Entry kmAnterior = new(), kmAtual = new();
    Entry litros = new(), precoLitro = new(), litrosArla = new();
    Entry capacidadeTanque = new(), combustivelAtual = new();
    Entry limiteAlerta = new(), metaKmL = new();
    
    Entry capacidadeArla = new();
Entry litrosArlaAtual = new();

    Entry cargaVeiculo = new(), cargaMotorista = new();
    Entry anoCarreta = new(), pesoBruto = new();
    Entry litrosViagem = new(), consumoViagem = new();
    Entry metaEnergetica = new();
    Button botaoAcaoViagem = new();
    Entry kmInicialViagem = new(), kmFinalViagem = new();

    Entry filtroVeiculo = new(), filtroMotorista = new();

    Picker tipoVeiculo = new(), tipoCombustivel = new();
    Picker periodoRelatorio = new(), tipoRelatorio = new();

    DatePicker dataViagem = new() { Date = DateTime.Today, TextColor = Colors.Black, BackgroundColor = Colors.White };
    TimePicker horaInicialViagem = new() { Time = DateTime.Now.TimeOfDay, TextColor = Colors.Black, BackgroundColor = Colors.White };
    TimePicker horaFinalViagem = new() { Time = DateTime.Now.AddHours(1).TimeOfDay, TextColor = Colors.Black, BackgroundColor = Colors.White };
    VerticalStackLayout painelFrentes = new();
    readonly Dictionary<int, CheckBox> checksFrentes = new();
    string frenteSelecionada = "";
    Picker filtroFrenteIndicadores = new();
    string frenteFiltroIndicadores = "Todas";
    DatePicker dataFiltro = new() { Date = DateTime.Today, TextColor = Colors.Black, BackgroundColor = Colors.White };

    CheckBox usarArla = new();

    VerticalStackLayout painelCarro = new();
    VerticalStackLayout painelArla = new();
    VerticalStackLayout lista = new();

    Label lblResultado = new();
    Label lblCusto = new();
    Label lblTotal = new();
    Label lblTanque = new();
    Label lblTanqueArla = new();
    Label lblPeso = new();
    Label lblEnergetico = new();
    Label lblComparacao = new();

    VerticalStackLayout graficoTanque = new();
    VerticalStackLayout graficoArla = new();

    bool salvandoViagem;
    Abastecimento? abastecimentoEmEdicao;
    Viagem? viagemEmEdicao;

    public MainPage()
{
    Title = "FuelControl";
    BackgroundColor = Color.FromArgb("#F1F5F9");
    CarregarDados();
    MostrarMenu("Abastecimentos");

    _ = SincronizarAbastecimentosAsync();
    _ = SincronizarViagensAsync();

 }
private string ChaveAbastecimento(Abastecimento x)
{
    return string.Join("|",
        x.Veiculo.Trim().ToUpperInvariant(),
        x.Motorista.Trim().ToUpperInvariant(),
        x.Data.ToString("yyyyMMddHHmmss"),
        x.KmAnterior.ToString("0.###", CultureInfo.InvariantCulture),
        x.KmAtual.ToString("0.###", CultureInfo.InvariantCulture),
        x.Litros.ToString("0.###", CultureInfo.InvariantCulture),
        x.Combustivel.Trim().ToUpperInvariant());
}


private async Task SincronizarAbastecimentosAsync()
{
    try
    {
        bool conectado =
            await FirebaseSyncService.ConectarAnonimamenteAsync();

        if (!conectado)
            return;

        var dadosNuvem =
            await FirebaseSyncService.ListarAbastecimentosAsync();

        if (dadosNuvem == null)
            return;

        // Identifica somente os registros do Toyota Corolla.
        bool EhToyotaCorolla(Abastecimento x)
        {
            return Id(x.Veiculo).Contains("TOYOTA COROLLA");
        }

        // Exclui primeiro os registros do Corolla que estão na nuvem.
        var registrosCarro = dadosNuvem
            .Where(EhToyotaCorolla)
            .ToList();

        foreach (var registro in registrosCarro)
        {
            if (!string.IsNullOrWhiteSpace(registro.Id))
            {
                bool excluido =
                    await FirebaseSyncService.ExcluirAbastecimentoAsync(
                        registro.Id);

                if (!excluido)
                {
                    System.Diagnostics.Debug.WriteLine(
                        "Não foi possível excluir registro do Corolla.");
                    return;
                }
            }
        }

        // Retira os registros do Corolla da cópia local.
        abastecimentos = abastecimentos
            .Where(x => !EhToyotaCorolla(x))
            .ToList();

        // Junta somente os abastecimentos que devem permanecer.
        var todos = abastecimentos
            .Concat(dadosNuvem.Where(x => !EhToyotaCorolla(x)))
            .Where(x => x != null)
            .GroupBy(x =>
                string.IsNullOrWhiteSpace(x.Id)
                    ? Guid.NewGuid().ToString("N")
                    : x.Id)
            .Select(grupo => grupo
                .OrderByDescending(x => x.EditadoEmUtc)
                .First())
            .OrderByDescending(x => x.Data)
            .ToList();

        abastecimentos = todos;

        foreach (var registro in abastecimentos)
        {
            if (string.IsNullOrWhiteSpace(registro.Id))
                registro.Id = Guid.NewGuid().ToString("N");

            if (registro.Turno is not ("A" or "B" or "C"))
                registro.Turno = ContextoTurno.Turno;

            if (registro.CriadoEmUtc == default)
                registro.CriadoEmUtc = DateTime.UtcNow;

            if (string.IsNullOrWhiteSpace(registro.CriadoPorDispositivo))
                registro.CriadoPorDispositivo = ContextoTurno.Dispositivo;

            if (registro.EditadoEmUtc == default)
                registro.EditadoEmUtc = DateTime.UtcNow;

            if (string.IsNullOrWhiteSpace(registro.EditadoPorDispositivo))
                registro.EditadoPorDispositivo = ContextoTurno.Dispositivo;

            await FirebaseSyncService.SalvarAbastecimentoAsync(registro);
        }

        SalvarDados();

        MainThread.BeginInvokeOnMainThread(() =>
        {
            MostrarMenu("Abastecimentos");
        });
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine(
            "Erro na sincronização Firebase: " + ex.Message);
    }
}



private async Task SincronizarViagensAsync()
{
    try
    {
        bool conectado =
            await FirebaseSyncService.ConectarAnonimamenteAsync();

        if (!conectado)
        {
            System.Diagnostics.Debug.WriteLine(
                "Firebase viagens: " + FirebaseSyncService.UltimoErro);
            return;
        }

        // Busca as viagens que já estão na nuvem.
        var dadosNuvem =
            await FirebaseSyncService.ListarViagensAsync();

        if (dadosNuvem == null)
            return;

        // Completa os dados de identificação das viagens locais antigas.
        foreach (var viagem in viagens)
        {
            if (string.IsNullOrWhiteSpace(viagem.Id))
                viagem.Id = Guid.NewGuid().ToString();

            if (string.IsNullOrWhiteSpace(viagem.Turno))
                viagem.Turno = ContextoTurno.Turno;

            if (viagem.CriadoEmUtc == default)
                viagem.CriadoEmUtc = DateTime.UtcNow;

            if (string.IsNullOrWhiteSpace(viagem.CriadoPorDispositivo))
                viagem.CriadoPorDispositivo = ContextoTurno.Dispositivo;

            if (viagem.EditadoEmUtc == default)
                viagem.EditadoEmUtc = DateTime.UtcNow;

            if (string.IsNullOrWhiteSpace(viagem.EditadoPorDispositivo))
                viagem.EditadoPorDispositivo = ContextoTurno.Dispositivo;
        }

        // Junta os registros locais e da nuvem pelo ID.
        var todasViagens = viagens
            .Concat(dadosNuvem)
            .Where(x => x != null)
            .GroupBy(x => x.Id)
            .Select(grupo => grupo
                .OrderByDescending(x => x.EditadoEmUtc)
                .First())
            .OrderByDescending(x => x.Data)
            .ToList();

        viagens = todasViagens;

        // Corrige a data de chegada da viagem antiga do turno C.
var viagensParaCorrigir = viagens.Where(v =>
    v.Turno == "C" &&
    v.Data.Date == new DateTime(2026, 11, 20) &&
    v.HoraInicial == new TimeSpan(21, 50, 0) &&
    v.DataHoraChegada == null
).ToList();

if (viagensParaCorrigir.Count == 1)
{
    var viagemAntiga = viagensParaCorrigir[0];

    viagemAntiga.DataHoraChegada =
        new DateTime(2026, 11, 21, 3, 23, 0);

    viagemAntiga.EditadoEmUtc = DateTime.UtcNow;
    viagemAntiga.EditadoPorDispositivo = ContextoTurno.Dispositivo;
}

        // Envia os registros consolidados ao Firebase.
        foreach (var viagem in viagens)
        {
            await FirebaseSyncService.SalvarViagemAsync(viagem);
        }

        // Mantém a cópia local atualizada.
        SalvarDados();

        System.Diagnostics.Debug.WriteLine(
            $"Sincronização de viagens concluída: {viagens.Count} registros.");
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine(
            "Erro na sincronização das viagens: " + ex.Message);
    }
}


    Entry Campo(string nome, Keyboard? teclado = null)
    {
        return new Entry
        {
            Placeholder = nome,
            TextColor = Colors.Black,
            PlaceholderColor = Color.FromArgb("#64748B"),
            BackgroundColor = Colors.White,
            Keyboard = teclado ?? Keyboard.Default,
            HeightRequest = 50,
            Margin = new Thickness(0, 3)
        };
    }

    Picker NovoPicker(string titulo, params string[] itens)
    {
        var p = new Picker
        {
            Title = titulo,
            TextColor = Colors.Black,
            BackgroundColor = Colors.White,
            HeightRequest = 50,
            ItemsSource = itens.ToList()
        };

        if (itens.Length > 0)
            p.SelectedIndex = 0;

        return p;
    }

    Label Titulo(string texto)
    {
        return new Label
        {
            Text = texto,
            FontSize = 18,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#123B65"),
            Margin = new Thickness(0, 12, 0, 5)
        };
    }

    Label Rotulo(string texto)
    {
        return new Label
        {
            Text = texto,
            FontSize = 13,
            TextColor = Color.FromArgb("#334155")
        };
    }

    Label Indicador(string texto)
    {
        return new Label
        {
            Text = texto,
            FontSize = 17,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#123B65")
        };
    }

    Button Botao(string texto, string cor = "#123B65")
    {
        return new Button
        {
            Text = texto,
            BackgroundColor = Color.FromArgb(cor),
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold,
            HeightRequest = 48,
            CornerRadius = 9
        };
    }

    View Cartao(string titulo, Label valor)
    {
        return new Border
        {
            BackgroundColor = Colors.White,
            Stroke = Color.FromArgb("#DCE4ED"),
            Padding = 12,
            Content = new VerticalStackLayout
            {
                Children =
                {
                    new Label
                    {
                        Text = titulo,
                        FontSize = 12,
                        TextColor = Colors.Gray
                    },
                    valor
                }
            }
        };
    }

    bool Ler(string? texto, out double valor)
    {
        if (double.TryParse(texto, NumberStyles.Number, pt, out valor))
            return true;

        return double.TryParse(
            texto,
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out valor);
    }

    
    string Id(string? texto)
    {
        return (texto ?? "").Trim().ToUpperInvariant();
    }

    double? ObterUltimoKmRegistrado(string nomeVeiculo)
    {
        string chave = Id(nomeVeiculo);

        if (string.IsNullOrWhiteSpace(chave))
            return null;

        var eventos = new List<(DateTime Data, double Km)>();

        eventos.AddRange(abastecimentos
            .Where(x => Id(x.Veiculo) == chave && x.KmAtual > 0)
            .Select(x => (x.Data, x.KmAtual)));

        eventos.AddRange(viagens
            .Where(x => !x.Excluida &&
                        Id(x.Veiculo) == chave &&
                        x.KmFinal > 0)
            .Select(x => (x.Data, x.KmFinal)));

        return eventos
            .OrderByDescending(x => x.Data)
            .Select(x => (double?)x.Km)
            .FirstOrDefault();
    }

    DadosVeiculo ObterVeiculo(string nome)
    {
        string chave = Id(nome);

        if (!veiculos.TryGetValue(chave, out var dados))
        {
            dados = new DadosVeiculo();
            veiculos[chave] = dados;
        }

        return dados;
    }


    void CarregarDados()
    {
        try
        {
            var json = Preferences.Default.Get(Chave, "");
            if (!string.IsNullOrWhiteSpace(json))
                abastecimentos =
                    JsonSerializer.Deserialize<List<Abastecimento>>(json) ?? new();
        }
        catch { abastecimentos = new(); }

        try
        {
            var json = Preferences.Default.Get(ChaveVeiculos, "");
            if (!string.IsNullOrWhiteSpace(json))
                veiculos =
                    JsonSerializer.Deserialize<Dictionary<string, DadosVeiculo>>(json)
                    ?? new(StringComparer.OrdinalIgnoreCase);
        }
        catch { veiculos = new(StringComparer.OrdinalIgnoreCase); }

        try
        {
            var json = Preferences.Default.Get(ChaveCargas, "");
            if (!string.IsNullOrWhiteSpace(json))
                viagens =
                    JsonSerializer.Deserialize<List<Viagem>>(json) ?? new();
        }
        catch { viagens = new(); }
        try
{
    var json = Preferences.Default.Get(ChaveTanquesArla, "");
    if (!string.IsNullOrWhiteSpace(json))
        tanquesArla =
            JsonSerializer.Deserialize<Dictionary<string, DadosArla>>(json)
            ?? new(StringComparer.OrdinalIgnoreCase);
}
catch { tanquesArla = new(StringComparer.OrdinalIgnoreCase); }
    }

    void SalvarDados()
    {
        Preferences.Default.Set(Chave, JsonSerializer.Serialize(abastecimentos));
        Preferences.Default.Set(ChaveVeiculos, JsonSerializer.Serialize(veiculos));
        Preferences.Default.Set(ChaveCargas, JsonSerializer.Serialize(viagens));
        Preferences.Default.Set(ChaveTanquesArla, JsonSerializer.Serialize(tanquesArla));
    }



    void MostrarMenu(string menu)
    {
        try
        {
        var botoes = new VerticalStackLayout
{
    Spacing = 8,
    HorizontalOptions = LayoutOptions.Fill
};
            

            string[] nomes =
            {
                "Abastecimentos", "Cargas", "Indicadores", "Relatórios"
            };

            


// Botões dos menus
foreach (string nome in nomes)
{
    var botao = Botao(nome);
    botao.HorizontalOptions = LayoutOptions.Fill;
    botao.FontSize = 11;
    botao.Padding = new Thickness(8, 0);
    botao.Margin = new Thickness(2);

    botao.Clicked += (s, e) => MostrarMenu(nome);

    botoes.Children.Add(botao);
}

// Botão para trocar o turno
var botaoTurno = Botao(
    "TROCAR TURNO (" + ContextoTurno.Turno + ")",
    "#0F766E");
botaoTurno.HorizontalOptions = LayoutOptions.Fill;
botaoTurno.FontSize = 11;
botaoTurno.Margin = new Thickness(2);


botaoTurno.Clicked += async (s, e) =>
{
    string escolha = await DisplayActionSheet(
        "Selecione o novo turno",
        "Cancelar",
        null,
        "Turno A",
        "Turno B",
        "Turno C");

    string novoTurno = escolha switch
    {
        "Turno A" => "A",
        "Turno B" => "B",
        "Turno C" => "C",
        _ => ""
    };

    if (string.IsNullOrEmpty(novoTurno))
        return;

    if (!ContextoTurno.EhAdministrador)
    {
        string pin = await DisplayPromptAsync(
            "Confirmar acesso",
            "Digite o PIN do turno " + novoTurno + ":",
            "Confirmar",
            "Cancelar",
            maxLength: 8,
            keyboard: Keyboard.Numeric);

        if (string.IsNullOrEmpty(pin) ||
            !ContextoTurno.ValidarTurno(novoTurno, pin))
        {
            await DisplayAlert(
                "Acesso negado",
                "PIN incorreto.",
                "OK");
            return;
        }
    }

    ContextoTurno.Turno = novoTurno;

    MostrarMenu(menu);
};


botoes.Children.Add(botaoTurno);



if (ContextoTurno.EhAdministrador)
{
    var botaoAdmin = Botao("ADMINISTRAÇÃO - PINs", "#7C3AED");
    botaoAdmin.HorizontalOptions = LayoutOptions.Fill;

    botaoAdmin.Clicked += async (s, e) =>
    {
        string escolha = await DisplayActionSheet(
            "Qual PIN deseja redefinir?",
            "Cancelar",
            null,
            "Turno A",
            "Turno B",
            "Turno C");

        string turno = escolha switch
        {
            "Turno A" => "A",
            "Turno B" => "B",
            "Turno C" => "C",
            _ => ""
        };

        if (string.IsNullOrEmpty(turno))
            return;

        string pin = await DisplayPromptAsync(
            "Novo PIN",
            "Digite o novo PIN numérico (4 a 8 dígitos):",
            "Continuar",
            "Cancelar",
            maxLength: 8,
            keyboard: Keyboard.Numeric);

        if (string.IsNullOrWhiteSpace(pin) ||
            pin.Length < 4 ||
            !pin.All(char.IsDigit))
        {
            await DisplayAlert(
                "PIN inválido",
                "O PIN deve ter de 4 a 8 números.",
                "OK");
            return;
        }

        string confirmar = await DisplayPromptAsync(
            "Confirmar PIN",
            "Digite novamente o novo PIN:",
            "Salvar",
            "Cancelar",
            maxLength: 8,
            keyboard: Keyboard.Numeric);

        if (confirmar != pin)
        {
            await DisplayAlert(
                "Erro",
                "Os PINs não são iguais.",
                "OK");
            return;
        }

        ContextoTurno.AlterarPin(turno, pin);

        await DisplayAlert(
            "Sucesso",
            "PIN do turno " + turno + " alterado!",
            "OK");
    };

    botoes.Children.Add(botaoAdmin);
}


            View conteudo = menu switch
            {
                "Abastecimentos" => TelaAbastecimentos(),
                "Cargas" => TelaCargas(),
                "Indicadores" => TelaIndicadores(),
                "Relatórios" => TelaRelatorios(),
                _ => TelaAbastecimentos()
            };

            Content = new ScrollView
            {
                Content = new VerticalStackLayout
                {
                    Padding = 14,
                    Spacing = 9,
                    Children =
                    {
                        new Label
                        {
                            Text = "FUELCONTROL",
                            FontSize = 27,
                            FontAttributes = FontAttributes.Bold,
                            TextColor = Color.FromArgb("#123B65"),
                            HorizontalOptions = LayoutOptions.Center
                        },
                        new Label
                        {
                            Text = "CONTROLE DE ABASTECIMENTO E CARGAS",
                            FontSize = 10,
                            TextColor = Colors.Gray,
                            HorizontalOptions = LayoutOptions.Center
                        },
                        botoes,
                        new BoxView
                        {
                            HeightRequest = 1,
                            Color = Colors.LightGray
                        },
                        conteudo
                    }
                }
            };
        }
        catch (Exception ex)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
                await DisplayAlert("Erro ao abrir tela", ex.Message, "OK"));
        }
    }

    View TelaAbastecimentos()
    {
    tipoVeiculo = NovoPicker("Tipo de veículo", "Caminhão");
    tipoCombustivel = NovoPicker("Combustível", "Diesel S10");

        veiculo = Campo("Placa ou identificação do veículo");
        veiculo.IsEnabled = true;
        motorista = Campo("Nome do motorista");

        kmAnterior = Campo("Quilometragem anterior", Keyboard.Numeric);
        kmAtual = Campo("Quilometragem atual", Keyboard.Numeric);
        litros = Campo("Litros abastecidos", Keyboard.Numeric);
        precoLitro = Campo("Preço por litro (R$)", Keyboard.Numeric);
        litrosArla = Campo("Litros de Arla 32", Keyboard.Numeric);
        metaKmL = Campo("Meta de consumo do caminhão (km/l)");
        capacidadeArla = Campo("Capacidade do tanque ARLA 32 (litros)", Keyboard.Numeric);
        litrosArlaAtual = Campo("Litros atuais no tanque ARLA 32", Keyboard.Numeric);

        capacidadeTanque = Campo("Capacidade total do tanque (litros)", Keyboard.Numeric);
        combustivelAtual = Campo("Litros atuais no tanque", Keyboard.Numeric);
        limiteAlerta = Campo("Alerta do tanque em % (padrão 20)", Keyboard.Numeric);

        lblTotal = Indicador("Total: R$ 0,00");
        lblResultado = Indicador("Consumo: -- km/l");
        lblCusto = Indicador("Custo por km: --");
        lblTanque = Indicador("Digite a placa para consultar o tanque.");
        lblTanqueArla = Indicador("Digite a placa para consultar o ARLA 32.");

        graficoTanque = new VerticalStackLayout
{
    Spacing = 3
};
graficoArla = new VerticalStackLayout
{
    Spacing = 3
};
        veiculo.TextChanged += (s, e) =>
{
    AtualizarTanqueTexto(e.NewTextValue ?? "");
    AtualizarTanqueArlaTexto(e.NewTextValue ?? "");
        string chaveArla = Id(e.NewTextValue ?? "");

    if (tanquesArla.TryGetValue(chaveArla, out var tanqueArla))
{
    capacidadeArla.Text = tanqueArla.Capacidade.ToString("0.##", pt);
    litrosArlaAtual.Text = tanqueArla.LitrosEstimados.ToString("0.##", pt);
}
else
{
    capacidadeArla.Text = "";
    litrosArlaAtual.Text = "";
}

    if (abastecimentoEmEdicao == null)
    {
        double? ultimoKm =
            ObterUltimoKmRegistrado(e.NewTextValue ?? "");

        kmAnterior.Text = ultimoKm.HasValue
    ? ultimoKm.Value.ToString("0.0", pt)
    : "";
    }
};

        painelCarro = new VerticalStackLayout
        {
            Children = { Rotulo("Preço por litro"), precoLitro, lblTotal }
        };

        painelArla = new VerticalStackLayout
        {
            IsVisible = false,
            Children = { Rotulo("Litros de Arla 32"), litrosArla }
        };

        usarArla = new CheckBox { IsVisible = true };

        usarArla.CheckedChanged += (s, e) =>
        {
            painelArla.IsVisible =
                tipoVeiculo.SelectedItem?.ToString() == "Caminhão" && e.Value;
        };

    

        litros.TextChanged += (s, e) => AtualizarTotal();
        precoLitro.TextChanged += (s, e) => AtualizarTotal();

        var salvar = Botao("CALCULAR E SALVAR ABASTECIMENTO");
        salvar.Clicked += SalvarAbastecimento;

        var configurar = Botao("SALVAR CONFIGURAÇÃO DO TANQUE");
        configurar.Clicked += SalvarConfiguracaoTanque;

        var ajustar = Botao("AJUSTAR NÍVEL ATUAL DO TANQUE", "#0F766E");
        ajustar.Clicked += AjustarTanque;

        var layout = new VerticalStackLayout
        {
            Spacing = 5,
            Children =
            {
                Titulo("NOVO ABASTECIMENTO"),
                Rotulo("Tipo de veículo"), tipoVeiculo,
                Rotulo("Combustível"), tipoCombustivel,
                new HorizontalStackLayout
                {
                    Children = { usarArla, Rotulo("Abasteceu Arla 32?") }
                },
                Rotulo("Veículo / placa"), veiculo,
                Titulo("CONSULTA DOS TANQUES"),
Rotulo("TANQUE DIESEL"),
lblTanque,
graficoTanque,
Rotulo("TANQUE ARLA 32"),
lblTanqueArla,
graficoArla,
                Rotulo("Motorista"), motorista,
                Rotulo("KM anterior"), kmAnterior,
                Rotulo("KM atual"), kmAtual,
                Rotulo("Litros abastecidos"), litros,
                painelArla,
                Rotulo("Meta de consumo do caminhão"), metaKmL,
                salvar, lblResultado, lblCusto,
                Titulo("CONFIGURAÇÃO DOS TANQUES"),
Rotulo("Capacidade total Diesel (litros)"), capacidadeTanque,
Rotulo("Combustível atual"), combustivelAtual,
Rotulo("Capacidade do ARLA 32 (litros)"), capacidadeArla,
Rotulo("Litros atuais de ARLA 32"), litrosArlaAtual,
Rotulo("Alerta em percentual"), limiteAlerta,
configurar, ajustar,
                Titulo("HISTÓRICO DE ABASTECIMENTOS")
            }
        };
        layout.Children.Add(CriarListaAbastecimentos());
        return layout;
    }

    void AtualizarTotal()
    {
        if (Ler(litros.Text, out var l) &&
            Ler(precoLitro.Text, out var p) && l > 0 && p > 0)
            lblTotal.Text = "Total: " + (l * p).ToString("C", pt);
        else
            lblTotal.Text = "Total: R$ 0,00";
    }

    double MediaAbastecimentos(string nome)
    {
        var dados = abastecimentos
            .Where(x => Id(x.Veiculo) == Id(nome) && x.Consumo > 0)
            .ToList();

        double km = dados.Sum(x => x.KmAtual - x.KmAnterior);
        double l = dados.Sum(x => x.Litros);

        return l > 0 ? km / l : 0;
    }

    async void SalvarAbastecimento(object? sender, EventArgs e)
    {
        try
        {
            bool caminhao =
                tipoVeiculo.SelectedItem?.ToString() == "Caminhão";

            if (string.IsNullOrWhiteSpace(veiculo.Text) ||
                !Ler(kmAnterior.Text, out var anterior) ||
                !Ler(kmAtual.Text, out var atual) ||
                !Ler(litros.Text, out var quantidade) ||
                anterior < 0 || atual <= anterior || quantidade <= 0)
            {
                await DisplayAlert("Atenção",
                    "Informe veículo, quilometragens e litros corretamente.", "OK");
                return;
            }

            double preco = 0;

            if (!caminhao &&
                (!Ler(precoLitro.Text, out preco) || preco <= 0))
            {
                await DisplayAlert("Atenção", "Informe o preço por litro.", "OK");
                return;
            }

            bool temArla = caminhao && usarArla.IsChecked;
            double arla = 0;

            if (temArla &&
                (!Ler(litrosArla.Text, out arla) || arla <= 0))
            {
                await DisplayAlert("Atenção", "Informe os litros de Arla.", "OK");
                return;
            }

            double distancia = atual - anterior;
            double consumo = distancia / quantidade;
            double valor = caminhao ? 0 : quantidade * preco;
            double custo = caminhao ? 0 : valor / distancia;

            var dados = ObterVeiculo(veiculo.Text);

            double nivelAntigo = dados.LitrosAtuais;
            double metaAntiga = dados.MetaKmL;
            Abastecimento? registroAnterior = abastecimentoEmEdicao;
            DadosArla? arlaAnterior = null;

if (caminhao &&
    tanquesArla.TryGetValue(Id(veiculo.Text), out var tanqueArlaAtual))
{
    arlaAnterior = new DadosArla
    {
        Capacidade = tanqueArlaAtual.Capacidade,
        LitrosEstimados = tanqueArlaAtual.LitrosEstimados,
        KmUltimoAbastecimento = tanqueArlaAtual.KmUltimoAbastecimento,
        MediaKmL = tanqueArlaAtual.MediaKmL,
        AbastecerNoProximo = tanqueArlaAtual.AbastecerNoProximo
    };
}

            if (dados.Capacidade > 0)
            {
                double nivelNovo = dados.LitrosAtuais;
                if (registroAnterior != null)
                    nivelNovo -= registroAnterior.Litros;
                nivelNovo += quantidade;

                if (nivelNovo < 0 || nivelNovo > dados.Capacidade)
                {
                    await DisplayAlert("Atenção",
                        "A edição ultrapassaria a capacidade do tanque ou deixaria o nível inválido.", "OK");
                    return;
                }
                dados.LitrosAtuais = nivelNovo;
            }

            if (caminhao && Ler(metaKmL.Text, out var meta) && meta > 0)
                dados.MetaKmL = meta;

            var registro = new Abastecimento
            {
            Id = registroAnterior?.Id ?? Guid.NewGuid().ToString("N"),

Turno = registroAnterior != null &&
        !string.IsNullOrWhiteSpace(registroAnterior.Turno)
        ? registroAnterior.Turno
        : ContextoTurno.Turno,

CriadoEmUtc = registroAnterior == null ||
              registroAnterior.CriadoEmUtc == default
              ? DateTime.UtcNow
              : registroAnterior.CriadoEmUtc,

CriadoPorDispositivo = registroAnterior == null ||
                       string.IsNullOrWhiteSpace(registroAnterior.CriadoPorDispositivo)
                       ? ContextoTurno.Dispositivo
                       : registroAnterior.CriadoPorDispositivo,

EditadoEmUtc = DateTime.UtcNow,
EditadoPorDispositivo = ContextoTurno.Dispositivo,
                Data = registroAnterior?.Data ?? DateTime.Now,
                Veiculo = veiculo.Text.Trim(),
                Motorista = motorista.Text?.Trim() ?? "",
                TipoVeiculo = caminhao ? "Caminhão" : "Carro",
                Combustivel = caminhao ? "Diesel S10" :
                    tipoCombustivel.SelectedItem?.ToString() ?? "",
                KmAnterior = anterior,
                KmAtual = atual,
                Litros = quantidade,
                PrecoLitro = preco,
                Valor = valor,
                Consumo = consumo,
                CustoKm = custo,
                TemArla = temArla,
                LitrosArla = arla,
                ConsumoArlaKmL = temArla ? distancia / arla : 0,
                ConsumoArla100 = temArla ? arla * 100 / distancia : 0,
                MetaKmL = caminhao ? dados.MetaKmL : 0,
                TanqueAdicionado = dados.Capacidade > 0
            };
            if (caminhao && registroAnterior == null &&
    tanquesArla.TryGetValue(Id(veiculo.Text), out var tanqueArlaSalvar))
{
    if (temArla)
    {
        if (tanqueArlaSalvar.KmUltimoAbastecimento > 0 &&
            atual > tanqueArlaSalvar.KmUltimoAbastecimento)
        {
            tanqueArlaSalvar.MediaKmL =
                (atual - tanqueArlaSalvar.KmUltimoAbastecimento) / arla;
        }

        tanqueArlaSalvar.LitrosEstimados = tanqueArlaSalvar.Capacidade;
        tanqueArlaSalvar.KmUltimoAbastecimento = atual;
        tanqueArlaSalvar.AbastecerNoProximo = false;
    }
    else
    {
        tanqueArlaSalvar.AbastecerNoProximo = true;

        if (tanqueArlaSalvar.MediaKmL > 0 &&
            tanqueArlaSalvar.KmUltimoAbastecimento > 0)
        {
            tanqueArlaSalvar.LitrosEstimados = Math.Clamp(
                tanqueArlaSalvar.Capacidade -
                (atual - tanqueArlaSalvar.KmUltimoAbastecimento) /
                tanqueArlaSalvar.MediaKmL,
                0,
                tanqueArlaSalvar.Capacidade);
        }
    }
}

            if (registroAnterior != null)
            {
                int indice = abastecimentos.IndexOf(registroAnterior);
                if (indice >= 0) abastecimentos[indice] = registro;
                else abastecimentos.Insert(0, registro);
            }
            else
                abastecimentos.Insert(0, registro);

            try
            {
                SalvarDados();
            }
            catch
            {
                if (registroAnterior != null)
                {
                    int indice = abastecimentos.IndexOf(registro);
                    if (indice >= 0) abastecimentos[indice] = registroAnterior;
                }
                else
                    abastecimentos.Remove(registro);

                dados.LitrosAtuais = nivelAntigo;
                dados.MetaKmL = metaAntiga;
                if (arlaAnterior != null)
{
    tanquesArla[Id(veiculo.Text)] = arlaAnterior;
}
                throw;
            }
            
bool sincronizado =
    await FirebaseSyncService.SalvarAbastecimentoAsync(registro);

if (sincronizado)
{
    await DisplayAlert("Firebase",
        "Abastecimento salvo e sincronizado na nuvem.",
        "OK");
}
else
{
    await DisplayAlert("Firebase",
        "Abastecimento salvo no celular, mas não sincronizado. " +
        FirebaseSyncService.UltimoErro,
        "OK");
}
            abastecimentoEmEdicao = null;
            veiculo.IsEnabled = true;

            lblResultado.Text = "Consumo automático: " + consumo.ToString("N2", pt) + " km/l";

            lblCusto.Text = caminhao
                ? "Meta: " + dados.MetaKmL.ToString("N2", pt) + " km/l | " +
                  (dados.MetaKmL > 0 && consumo >= dados.MetaKmL
                    ? "META ATINGIDA" : "ABAIXO OU SEM META")
                : $"Custo: {custo:C2}/km | " +
                  (custo <= 0.50 ? "DENTRO DA META" : "ACIMA DA META");

            AtualizarTanqueTexto(veiculo.Text);
            AtualizarTanqueArlaTexto(veiculo.Text);

            await DisplayAlert("Salvo",
                registroAnterior != null
                    ? "Abastecimento atualizado com sucesso."
                    : "Abastecimento registrado com sucesso.", "OK");

            kmAnterior.Text = atual.ToString("N0", pt);
            kmAtual.Text = "";
            litros.Text = "";
            precoLitro.Text = "";
            litrosArla.Text = "";
            MostrarMenu("Abastecimentos");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erro ao salvar abastecimento", ex.Message, "OK");
        }
    }


    View CriarListaAbastecimentos()
    {
        var painel = new VerticalStackLayout { Spacing = 8 };
        if (abastecimentos.Count == 0)
        {
            painel.Children.Add(new Label
            {
                Text = "Nenhum abastecimento registrado.",
                TextColor = Colors.Gray
            });
            return painel;
        }

        foreach (var x in abastecimentos.OrderByDescending(a => a.Data))
        {
            var card = new VerticalStackLayout
            {
                Spacing = 4,
                Padding = 10,
                BackgroundColor = Colors.White
            };
            card.Children.Add(new Label
            {
                Text = $"{x.Data:dd/MM/yyyy HH:mm} | {x.Veiculo} | {x.TipoVeiculo}",
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#123B65")
            });
            card.Children.Add(new Label
{
    Text = $"Motorista: {x.Motorista} | {x.Combustivel} | {x.Litros:N1} L",
    TextColor = Color.FromArgb("#123B65")
});

card.Children.Add(new Label
{
    Text = $"KM: {x.KmAnterior.ToString("N1", pt)} → {x.KmAtual.ToString("N1", pt)} | Média: {x.Consumo.ToString("N2", pt)} km/l",
    TextColor = Color.FromArgb("#123B65")
});

if (x.TemArla)
    card.Children.Add(new Label
    {
        Text = "Arla 32: " + x.LitrosArla.ToString("N1", pt) + " L",
        TextColor = Color.FromArgb("#123B65")
    });

            if (ContextoTurno.EhAdministrador ||
    string.Equals(
        x.Turno,
        ContextoTurno.Turno,
        StringComparison.OrdinalIgnoreCase))
{
    var editar = Botao("EDITAR ABASTECIMENTO", "#0F766E");
    editar.Clicked += (sender, args) =>
        CarregarAbastecimentoParaEdicao(x);

    card.Children.Add(editar);
}
else
{
    card.Children.Add(new Label
    {
        Text = $"Somente consulta | Cadastrado no turno {x.Turno}",
        TextColor = Colors.Gray,
        FontAttributes = FontAttributes.Italic
    });
}
painel.Children.Add(card);
        }
        return painel;
    }

    void CarregarAbastecimentoParaEdicao(Abastecimento x)
    {

        if (!ContextoTurno.EhAdministrador &&
    !string.Equals(
        x.Turno,
        ContextoTurno.Turno,
        StringComparison.OrdinalIgnoreCase))
    {
        _ = DisplayAlert(
            "Edição não permitida",
            "Você só pode editar abastecimentos cadastrados pelo seu turno.",
            "OK");

        return;
    }
        abastecimentoEmEdicao = x;
        veiculo.IsEnabled = false;
        tipoVeiculo.SelectedItem = x.TipoVeiculo;
        tipoCombustivel.SelectedItem = x.Combustivel;
        veiculo.Text = x.Veiculo;
        motorista.Text = x.Motorista;
        kmAnterior.Text = x.KmAnterior.ToString("0.##", CultureInfo.InvariantCulture);
        kmAtual.Text = x.KmAtual.ToString("0.##", CultureInfo.InvariantCulture);
        litros.Text = x.Litros.ToString("0.##", CultureInfo.InvariantCulture);
        precoLitro.Text = x.PrecoLitro.ToString("0.##", CultureInfo.InvariantCulture);
        usarArla.IsChecked = x.TemArla;
        litrosArla.Text = x.LitrosArla.ToString("0.##", CultureInfo.InvariantCulture);
        metaKmL.Text = x.MetaKmL.ToString("0.##", CultureInfo.InvariantCulture);
        AtualizarTotal();
        AtualizarTanqueTexto(x.Veiculo);
        DisplayAlert("Editar abastecimento",
            "Os dados foram carregados. Altere os campos e toque em CALCULAR E SALVAR ABASTECIMENTO.",
            "OK");
    }

    void SalvarConfiguracaoTanque(object? sender, EventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(veiculo.Text))
            {
                DisplayAlert("Atenção", "Informe o veículo.", "OK");
                return;
            }

            if (!Ler(capacidadeTanque.Text, out var capacidade) ||
                capacidade <= 0 ||
                !Ler(combustivelAtual.Text, out var atual) ||
                atual < 0 || atual > capacidade)
            {
                DisplayAlert("Atenção",
                    "Informe capacidade e nível atual válidos.", "OK");
                return;
            }

            var d = ObterVeiculo(veiculo.Text);
            d.Capacidade = capacidade;
            d.LitrosAtuais = atual;
            if (Ler(capacidadeArla.Text, out var capacidadeA) && capacidadeA > 0)
{
    if (!Ler(litrosArlaAtual.Text, out var litrosAtuaisArla) ||
        litrosAtuaisArla < 0 ||
        litrosAtuaisArla > capacidadeA)
    {
        DisplayAlert("Atenção",
            "Informe os litros atuais do ARLA 32 entre zero e a capacidade do tanque.",
            "OK");
        return;
    }

    if (!tanquesArla.TryGetValue(Id(veiculo.Text), out var arla))
    {
        arla = new DadosArla();
        tanquesArla[Id(veiculo.Text)] = arla;
    }

    arla.Capacidade = capacidadeA;
    arla.LitrosEstimados = litrosAtuaisArla;
    arla.KmUltimoAbastecimento = ObterUltimoKmRegistrado(veiculo.Text) ?? 0;
}

            if (Ler(limiteAlerta.Text, out var alerta) &&
                alerta > 0 && alerta < 100)
                d.AlertaPercentual = alerta;
            else
                d.AlertaPercentual = 20;

            SalvarDados();
            AtualizarTanqueTexto(veiculo.Text);
            AtualizarTanqueArlaTexto(veiculo.Text);
            DisplayAlert("Salvo", "Configuração do tanque salva.", "OK");
        }
        catch (Exception ex)
        {
            DisplayAlert("Erro", ex.Message, "OK");
        }
    }

    void AjustarTanque(object? sender, EventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(veiculo.Text) ||
                !Ler(combustivelAtual.Text, out var atual) || atual < 0)
            {
                DisplayAlert("Atenção",
                    "Informe veículo e litros atuais.", "OK");
                return;
            }

            var d = ObterVeiculo(veiculo.Text);

            if (d.Capacidade > 0 && atual > d.Capacidade)
            {
                DisplayAlert("Atenção",
                    "O nível informado supera a capacidade do tanque.", "OK");
                return;
            }

            d.LitrosAtuais = atual;
            SalvarDados();
            AtualizarTanqueTexto(veiculo.Text);
            DisplayAlert("Salvo", "Nível do tanque ajustado.", "OK");
        }
        catch (Exception ex)
        {
            DisplayAlert("Erro", ex.Message, "OK");
        }
    }
void AtualizarTanqueTexto(string nome)
{
    if (lblTanque == null)
        return;

    graficoTanque.Children.Clear();

    if (string.IsNullOrWhiteSpace(nome))
    {
        lblTanque.Text = "Digite a placa para consultar o tanque.";
        return;
    }

    string chave = Id(nome);

    if (!veiculos.TryGetValue(chave, out var d) || d.Capacidade <= 0)
    {
        lblTanque.Text =
            "Tanque não configurado. Informe capacidade e nível inicial.";
        return;
    }

    double percentual = Math.Clamp(
        d.LitrosAtuais / d.Capacidade * 100, 0, 100);

    double litrosReserva = d.Capacidade * 2.0 / 20.0;

    int partesPreenchidas = (int)Math.Ceiling(percentual / 5.0);
    partesPreenchidas = Math.Clamp(partesPreenchidas, 0, 20);

    var segmentos = new Grid
    {
        ColumnSpacing = 2,
        HeightRequest = 28
    };

    for (int i = 0; i < 20; i++)
        segmentos.ColumnDefinitions.Add(
            new ColumnDefinition(GridLength.Star));

    for (int i = 0; i < 20; i++)
    {
        var bloco = new BoxView
        {
            HeightRequest = 26,
            Color = i < partesPreenchidas
                ? (i < 2
                    ? Color.FromArgb("#DC2626")
                    : Color.FromArgb("#16A34A"))
                : Color.FromArgb("#334155"),
            Margin = new Thickness(1)
        };

        Grid.SetColumn(bloco, i);
        segmentos.Children.Add(bloco);
    }

    var marcacoes = new Grid
    {
        ColumnSpacing = 2
    };

    for (int i = 0; i < 5; i++)
        marcacoes.ColumnDefinitions.Add(
            new ColumnDefinition(GridLength.Star));

    string[] nomes = { "E", "¼", "½", "¾", "F" };

    for (int i = 0; i < 5; i++)
    {
        double litrosMarca = d.Capacidade * i / 4.0;

        var texto = new VerticalStackLayout
        {
            Spacing = 0,
            HorizontalOptions = LayoutOptions.Center,
            Children =
            {
                new Label
                {
                    Text = nomes[i],
                    FontSize = 12,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#123B65"),
                    HorizontalTextAlignment =
                        TextAlignment.Center
                },
                new Label
                {
                    Text = litrosMarca.ToString("N0", pt) + " L",
                    FontSize = 10,
                    TextColor = Color.FromArgb("#123B65"),
                    HorizontalTextAlignment =
                        TextAlignment.Center
                }
            }
        };

        Grid.SetColumn(texto, i);
        marcacoes.Children.Add(texto);
    }

    graficoTanque.Children.Add(segmentos);
    graficoTanque.Children.Add(marcacoes);

    double media = MediaAbastecimentos(nome);
    double autonomia = media > 0
        ? d.LitrosAtuais * media : 0;

    var registros = abastecimentos
        .Where(x => Id(x.Veiculo) == chave && x.Litros > 0)
        .ToList();

    double mediaKm = registros.Count > 0
        ? registros.Average(x =>
            Math.Max(0, x.KmAtual - x.KmAnterior))
        : 0;

    double mediaLitros = registros.Count > 0
        ? registros.Average(x => x.Litros)
        : 0;

    lblTanque.Text =
        "NÍVEL ATUAL: " +
        d.LitrosAtuais.ToString("N1", pt) + "/" +
        d.Capacidade.ToString("N1", pt) + " L (" +
        percentual.ToString("N1", pt) + "%)\n" +
        "Reserva: " + litrosReserva.ToString("N1", pt) + " L\n" +
        "Autonomia estimada: " +
        (media > 0 ? autonomia.ToString("N1", pt) + " km" : "--") +
        "\nMédia de KM por abastecimento: " +
        (registros.Count > 0 ? mediaKm.ToString("N1", pt) + " km" : "--") +
        "\nMédia de litros por abastecimento: " +
        (registros.Count > 0 ? mediaLitros.ToString("N1", pt) + " L" : "--") +
        "\n" +
        (d.LitrosAtuais <= litrosReserva
            ? "ATENÇÃO: RESERVA ATINGIDA!"
            : "Nível normal");
}
void AtualizarTanqueArlaTexto(string nome)
{
    graficoArla.Children.Clear();

    if (string.IsNullOrWhiteSpace(nome))
    {
        lblTanqueArla.Text = "Digite a placa para consultar o ARLA 32.";
        return;
    }

    string chave = Id(nome);

    if (!tanquesArla.TryGetValue(chave, out var arla) ||
        arla.Capacidade <= 0)
    {
        lblTanqueArla.Text = "Tanque ARLA 32 não configurado.";
        return;
    }

    double litros = arla.LitrosEstimados;

double? kmAtualArla = ObterUltimoKmRegistrado(nome);

if (arla.MediaKmL > 0 &&
    arla.KmUltimoAbastecimento > 0 &&
    kmAtualArla.HasValue &&
    kmAtualArla.Value >= arla.KmUltimoAbastecimento)
{
    litros = Math.Clamp(
    arla.LitrosEstimados -
    (kmAtualArla.Value - arla.KmUltimoAbastecimento) / arla.MediaKmL,
    0,
    arla.Capacidade);
}
else
{
    litros = Math.Clamp(litros, 0, arla.Capacidade);
}

    double percentual = litros / arla.Capacidade * 100;
    double reserva = arla.Capacidade * 2.0 / 20.0;

    int partesPreenchidas = Math.Clamp(
        (int)Math.Ceiling(percentual / 5.0), 0, 20);

    var segmentos = new Grid
    {
        ColumnSpacing = 2,
        HeightRequest = 28
    };

    for (int i = 0; i < 20; i++)
        segmentos.ColumnDefinitions.Add(
            new ColumnDefinition(GridLength.Star));

    for (int i = 0; i < 20; i++)
    {
        var bloco = new BoxView
        {
            HeightRequest = 26,
            Color = i < partesPreenchidas
                ? (i < 2
                    ? Color.FromArgb("#DC2626")
                    : Color.FromArgb("#16A34A"))
                : Color.FromArgb("#334155"),
            Margin = new Thickness(1)
        };

        Grid.SetColumn(bloco, i);
        segmentos.Children.Add(bloco);
    }

    graficoArla.Children.Add(segmentos);
    var marcacoesArla = new Grid
{
    ColumnSpacing = 2
};

for (int i = 0; i < 5; i++)
    marcacoesArla.ColumnDefinitions.Add(
        new ColumnDefinition(GridLength.Star));

string[] nomesArla = { "E", "¼", "½", "¾", "F" };

for (int i = 0; i < 5; i++)
{
    double litrosMarca = arla.Capacidade * i / 4.0;

    var texto = new VerticalStackLayout
    {
        Spacing = 0,
        HorizontalOptions = LayoutOptions.Center,
        Children =
        {
            new Label
            {
                Text = nomesArla[i],
                FontSize = 12,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#123B65")
            },
            new Label
            {
                Text = litrosMarca.ToString("N0", pt) + " L",
                FontSize = 10,
                TextColor = Color.FromArgb("#123B65")
            }
        }
    };

    Grid.SetColumn(texto, i);
    marcacoesArla.Children.Add(texto);
}

graficoArla.Children.Add(marcacoesArla);

    double autonomiaArla = arla.MediaKmL > 0
    ? litros * arla.MediaKmL
    : 0;

lblTanqueArla.Text =
    "NÍVEL ARLA 32: " +
    litros.ToString("N1", pt) + "/" +
    arla.Capacidade.ToString("N1", pt) + " L (" +
    percentual.ToString("N1", pt) + "%)\n" +
    "Reserva: " + reserva.ToString("N1", pt) + " L\n" +
    "Autonomia estimada: " +
    (autonomiaArla > 0
        ? autonomiaArla.ToString("N1", pt) + " km"
        : "Aguardando histórico de consumo") + "\n" +
    (litros <= reserva
        ? "ATENÇÃO: RESERVA DE ARLA ATINGIDA!\n"
        : "Nível normal\n") +
    (arla.AbastecerNoProximo
        ? "ATENÇÃO: ABASTECER ARLA NO PRÓXIMO ABASTECIMENTO DIESEL!"
        : "Alternância de abastecimento normal.");
}


    double TaraPorAno(int ano)
    {
        if (ano >= 1999 && ano <= 2005) return 34000;
        if (ano >= 2006 && ano <= 2007) return 36000;

        if ((ano >= 2008 && ano <= 2009) ||
            (ano >= 2012 && ano <= 2014))
            return 38000;

        if (ano >= 2016 && ano <= 2026) return 37000;

        return 0;
    }

    View TelaCargas()
    {
        dataViagem = new DatePicker
        {
            Date = DateTime.Today,
            TextColor = Colors.Black,
            BackgroundColor = Colors.White
        };
        horaInicialViagem = new TimePicker
        {
            Time = DateTime.Now.TimeOfDay,
            TextColor = Colors.Black,
            BackgroundColor = Colors.White
        };
        horaFinalViagem = new TimePicker
        {
            Time = DateTime.Now.AddHours(1).TimeOfDay,
            TextColor = Colors.Black,
            BackgroundColor = Colors.White
        };
        painelFrentes = new VerticalStackLayout { Spacing = 4 };
        checksFrentes.Clear();
        frenteSelecionada = "";
        painelFrentes.Children.Add(Rotulo("Selecione uma frente (1 a 7)"));
        for (int i = 1; i <= 7; i++)
        {
            int frente = i;
            var check = new CheckBox { Color = Color.FromArgb("#123B65") };
            check.CheckedChanged += (sender, args) =>
            {
                if (args.Value)
                {
                    frenteSelecionada = frente.ToString();
                    foreach (var item in checksFrentes)
                        if (item.Key != frente) item.Value.IsChecked = false;
                }
                else if (frenteSelecionada == frente.ToString())
                    frenteSelecionada = "";
            };
            checksFrentes[i] = check;
            painelFrentes.Children.Add(new HorizontalStackLayout
            {
                Spacing = 8,
                Children = { check, Rotulo($"Frente {i}") }
            });
        }

     cargaVeiculo = Campo("Caminhão / identificação");
cargaVeiculo.IsEnabled = true;
cargaMotorista = Campo("Motorista");

cargaVeiculo.TextChanged += (s, e) =>
{
    if (viagemEmEdicao == null)
    {
        double? ultimoKm =
            ObterUltimoKmRegistrado(e.NewTextValue ?? "");

        kmInicialViagem.Text = ultimoKm.HasValue
            ? ultimoKm.Value.ToString("0.0", pt)
            : "";
    }
};
        anoCarreta = Campo("Ano da carreta", Keyboard.Numeric);
        pesoBruto = Campo("Peso bruto da carga (kg)", Keyboard.Numeric);
        litrosViagem = Campo("Litros consumidos nesta viagem", Keyboard.Numeric);
        consumoViagem = Campo("Consumo médio manual da viagem (km/l)", Keyboard.Numeric);
        metaEnergetica = Campo("Meta energética (padrão 82)", Keyboard.Numeric);
       kmInicialViagem = Campo("KM inicial da viagem", Keyboard.Numeric);
       kmFinalViagem = Campo("KM final da viagem", Keyboard.Numeric);

        lblPeso = Indicador("Tara: -- | Peso líquido: --");
        lblEnergetico = Indicador("Valor energético calculado: --");
        
        var lblPreviaViagem = new Label
        {
            Text = "Preencha os dados para visualizar a viagem.",
            FontSize = 14,
            TextColor = Color.FromArgb("#123B65"),
            BackgroundColor = Colors.White,
            Padding = 12
        };

        void AtualizarPreviaViagem()
        {
            lblPreviaViagem.Text =
                "VEÍCULO: " + (cargaVeiculo.Text ?? "--") + "\n" +
                "MOTORISTA: " + (cargaMotorista.Text ?? "--") + "\n" +
                "FRENTE: " + (frenteSelecionada == "" ? "--" : frenteSelecionada) + "\n" +
                                "DATA: " + dataViagem.Date.ToString() + "\n" +
                "INÍCIO: " + horaInicialViagem.Time.ToString() + "\n" +
                "KM INICIAL: " + (kmInicialViagem.Text ?? "--") + "\n" +
                "KM FINAL: " + (kmFinalViagem.Text ?? "--") + "\n" +
                "PESO BRUTO: " + (pesoBruto.Text ?? "--") + " kg\n" +
                "LITROS: " + (litrosViagem.Text ?? "--") + " L\n" +
                "STATUS: VIAGEM EM PREENCHIMENTO";
        }

        cargaVeiculo.TextChanged += (s, e) => AtualizarPreviaViagem();
        cargaMotorista.TextChanged += (s, e) => AtualizarPreviaViagem();
        kmInicialViagem.TextChanged += (s, e) => AtualizarPreviaViagem();
        kmFinalViagem.TextChanged += (s, e) => AtualizarPreviaViagem();
        pesoBruto.TextChanged += (s, e) => AtualizarPreviaViagem();
        litrosViagem.TextChanged += (s, e) => AtualizarPreviaViagem();

        dataViagem.DateSelected += (s, e) => AtualizarPreviaViagem();
        horaInicialViagem.PropertyChanged += (s, e) => AtualizarPreviaViagem();

        AtualizarPreviaViagem();
        
                foreach (var item in checksFrentes.Values)
        {
            item.CheckedChanged += (s, e) => AtualizarPreviaViagem();
        }
        
        anoCarreta.TextChanged += (s, e) => AtualizarPeso();
        pesoBruto.TextChanged += (s, e) => AtualizarPeso();
        consumoViagem.TextChanged += (s, e) => AtualizarPeso();
        metaEnergetica.TextChanged += (s, e) => AtualizarPeso();

        botaoAcaoViagem = Botao("INICIAR VIAGEM");

botaoAcaoViagem.Clicked += (s, e) =>
{
    if (viagemEmEdicao == null)
        IniciarViagem(s, e);
    else
        SalvarViagem(s, e);
};

        var layout = new VerticalStackLayout
        {
            Spacing = 5,
            Children =
            {
                Titulo("REGISTRO DE CARGAS POR VIAGEM"),
                Rotulo("Data da viagem"), dataViagem,
                Rotulo("Hora inicial da viagem"), horaInicialViagem,
                Rotulo("Hora final da viagem"), horaFinalViagem,
                Titulo("FRENTE DE CARREGAMENTO"), painelFrentes,
                Rotulo("Caminhão"), cargaVeiculo,
                Rotulo("Motorista"), cargaMotorista,
                Rotulo("Ano da carreta"), anoCarreta,
                Rotulo("Peso bruto (kg)"), pesoBruto,
                lblPeso,
                Titulo("QUILOMETRAGEM DA VIAGEM"),
                Rotulo("KM inicial"), kmInicialViagem,
                Rotulo("KM final"), kmFinalViagem,
                Rotulo("Litros de combustível consumidos na viagem"),
                litrosViagem,
                Rotulo("Consumo médio da viagem (km/l) - manual"),
                consumoViagem,
                Rotulo("VALOR ENERGÉTICO CALCULADO AUTOMATICAMENTE"),
                lblEnergetico,
                Rotulo("Meta do valor energético"),
                metaEnergetica,
                                botaoAcaoViagem,
                Titulo("VIAGEM EM PREENCHIMENTO"),
                lblPreviaViagem,
                Titulo("HISTÓRICO DE VIAGENS")
            }
        };

        layout.Children.Add(CriarListaViagens());
        return layout;
    }

    void AtualizarPeso()
    {
        double tara = TaraAtual();

        if (tara <= 0)
        {
            lblPeso.Text = "Tara não cadastrada para este ano.";
            lblEnergetico.Text = "Valor energético calculado: --";
            return;
        }

        if (!Ler(pesoBruto.Text, out var bruto) || bruto <= tara)
        {
            lblPeso.Text =
            "Tara automática: " + tara.ToString("N1", pt) + " kg | Informe o peso bruto.";
            lblEnergetico.Text = "Valor energético calculado: --";
            return;
        }

        double liquido = bruto - tara;
        lblPeso.Text = "Tara: " + tara.ToString("N1", pt) +
               " kg | Peso líquido: " + liquido.ToString("N1", pt) + " kg";

        if (Ler(consumoViagem.Text, out var consumo) && consumo > 0)
        {
            double ve = consumo * liquido / 1000.0;
            double meta = 82;

            if (Ler(metaEnergetica.Text, out var m) && m > 0)
                meta = m;

            lblEnergetico.Text =
                $"Valor energético calculado: {ve.ToString("N2", pt)} | " +
                $"Meta: {meta.ToString("N2", pt)} | " +
                (ve >= meta ? "META ATINGIDA" : "ABAIXO DA META");
        }
        else
        {
            lblEnergetico.Text =
                "Valor energético: informe o consumo em km/l.";
        }
    }

    double TaraAtual()
    {
        return int.TryParse(anoCarreta.Text, out var ano)
            ? TaraPorAno(ano)
            : 0;
    }
    
async void IniciarViagem(object? sender, EventArgs e)
{
    if (salvandoViagem)
        return;

    salvandoViagem = true;

    try
    {
        string nomeVeiculo = cargaVeiculo.Text?.Trim() ?? "";
        string nomeMotorista = cargaMotorista.Text?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(nomeVeiculo) ||
            string.IsNullOrWhiteSpace(nomeMotorista) ||
            string.IsNullOrWhiteSpace(frenteSelecionada))
        {
            await DisplayAlert(
                "Atenção",
                "Informe o caminhão, o motorista e a frente de carregamento.",
                "OK");
            return;
        }

        bool jaExiste = viagens.Any(x =>
            !x.Excluida &&
            x.EmAndamento &&
            Id(x.Veiculo) == Id(nomeVeiculo));

        if (jaExiste)
        {
            await DisplayAlert(
                "Viagem em andamento",
                "Este caminhão já possui uma viagem em andamento. Finalize ou retome essa viagem antes de iniciar outra.",
                "OK");
            return;
        }

        double? ultimoKm = ObterUltimoKmRegistrado(nomeVeiculo);

        double kmInicial;

        if (ultimoKm.HasValue && ultimoKm.Value > 0)
        {
            kmInicial = ultimoKm.Value;
        }
        else if (!Ler(kmInicialViagem.Text, out kmInicial) ||
                 kmInicial < 0)
        {
            await DisplayAlert(
                "KM inicial",
                "Não existe KM anterior registrado para este caminhão. Informe o KM inicial manualmente.",
                "OK");
            return;
        }

        kmInicialViagem.Text =
            kmInicial.ToString("0.##", CultureInfo.InvariantCulture);

        DateTime dataHoraInicio =
            (dataViagem.Date ?? DateTime.Today).Date +
            (horaInicialViagem.Time ?? DateTime.Now.TimeOfDay);

        var viagem = new Viagem
        {
            Id = Guid.NewGuid().ToString(),
            Turno = ContextoTurno.Turno,
            TurnoResponsavel = ContextoTurno.Turno,
            CriadoEmUtc = DateTime.UtcNow,
            CriadoPorDispositivo = ContextoTurno.Dispositivo,
            EditadoEmUtc = DateTime.UtcNow,
            EditadoPorDispositivo = ContextoTurno.Dispositivo,

            Data = dataHoraInicio,
            HoraInicial = dataHoraInicio.TimeOfDay,
            HoraFinal = TimeSpan.Zero,

            Frente = frenteSelecionada,
            Veiculo = nomeVeiculo,
            Motorista = nomeMotorista,
            MotoristaAtual = nomeMotorista,

            KmInicial = kmInicial,
            EmAndamento = true,
            TanqueDescontado = false,

         Transferencias = new List<TransferenciaViagem>(),
TransferenciasTurno = new List<TransferenciaTurno>()
};

        viagens.Insert(0, viagem);
        SalvarDados();

        bool sincronizada =
            await FirebaseSyncService.SalvarViagemAsync(viagem);

        if (!sincronizada)
        {
            await DisplayAlert(
                "Viagem salva no celular",
                "A viagem foi registrada localmente, mas não foi possível sincronizar com o Firebase.\n\n" +
                FirebaseSyncService.UltimoErro,
                "OK");
        }
        else
        {
            await DisplayAlert(
                "Viagem iniciada",
                "Saída registrada com sucesso!\n\n" +
                "Caminhão: " + nomeVeiculo + "\n" +
                "Motorista: " + nomeMotorista + "\n" +
                "Frente: " + frenteSelecionada + "\n" +
                "KM inicial: " + kmInicial.ToString("N1", pt),
                "OK");
        }

        MostrarMenu("Cargas");
    }
    catch (Exception ex)
    {
        await DisplayAlert(
            "Erro ao iniciar viagem",
            ex.Message,
            "OK");
    }
    finally
    {
        salvandoViagem = false;
    }
}


async void FinalizarViagem(Viagem viagem)
{
        string turnoResponsavel =
            string.IsNullOrWhiteSpace(viagem.TurnoResponsavel)
                ? viagem.Turno
                : viagem.TurnoResponsavel;

        if (!ContextoTurno.EhAdministrador &&
            turnoResponsavel != ContextoTurno.Turno)
        {
            await DisplayAlert(
                "Acesso negado",
                "Somente o turno responsável pela viagem ou o administrador pode finalizar.",
                "OK");
            return;
        }
    if (salvandoViagem || !viagem.EmAndamento)
        return;

    bool confirmar = await DisplayAlert(
        "Finalizar viagem",
        "Confirma o retorno do caminhão " + viagem.Veiculo + "?",
        "Continuar",
        "Cancelar");

    if (!confirmar)
        return;

    salvandoViagem = true;

    try
    {
        string textoKm = await DisplayPromptAsync(
            "KM final",
            "Informe o KM atual do caminhão:",
            "Continuar", "Cancelar",
            "KM final", 20, Keyboard.Numeric);

        if (!Ler(textoKm, out double kmFinal))
        {
            await DisplayAlert("Atenção", "KM final inválido.", "OK");
            return;
        }

        double ultimoKm = viagem.Transferencias?.LastOrDefault()?.Km
            ?? viagem.KmInicial;

        if (kmFinal <= ultimoKm)
        {
            await DisplayAlert(
                "KM inválido",
                "O KM final deve ser maior que o último KM registrado (" +
                ultimoKm.ToString("N1", pt) + ").",
                "OK");
            return;
        }

        string textoAno = await DisplayPromptAsync(
            "Ano da carreta",
            "Informe o ano da carreta:",
            "Continuar", "Cancelar",
            "Ano", 4, Keyboard.Numeric);

        if (!int.TryParse(textoAno, out int ano))
        {
            await DisplayAlert("Atenção", "Ano inválido.", "OK");
            return;
        }

        string textoBruto = await DisplayPromptAsync(
            "Peso bruto",
            "Informe o peso bruto da carga em kg:",
            "Continuar", "Cancelar",
            "Peso bruto", 15, Keyboard.Numeric);

        if (!Ler(textoBruto, out double bruto) || bruto <= 0)
        {
            await DisplayAlert("Atenção", "Peso bruto inválido.", "OK");
            return;
        }

        string textoLitros = await DisplayPromptAsync(
            "Combustível consumido",
            "Informe os litros consumidos na viagem:",
            "Continuar", "Cancelar",
            "Litros", 15, Keyboard.Numeric);

        if (!Ler(textoLitros, out double litros) || litros <= 0)
        {
            await DisplayAlert("Atenção", "Litros inválidos.", "OK");
            return;
        }

        string textoConsumo = await DisplayPromptAsync(
            "Consumo médio",
            "Informe o consumo médio em km/l:",
            "Continuar", "Cancelar",
            "Consumo km/l", 15, Keyboard.Numeric);

        if (!Ler(textoConsumo, out double consumo) || consumo <= 0)
        {
            await DisplayAlert("Atenção", "Consumo inválido.", "OK");
            return;
        }

        double tara = TaraPorAno(ano);

        if (tara <= 0 || bruto <= tara)
        {
            await DisplayAlert(
                "Peso inválido",
                "Verifique o ano da carreta e o peso bruto. O peso bruto deve ser maior que a tara.",
                "OK");
            return;
        }

        var dados = ObterVeiculo(viagem.Veiculo);

        if (dados.Capacidade > 0 &&
            !viagem.TanqueDescontado &&
            litros > dados.LitrosAtuais)
        {
            await DisplayAlert(
                "Combustível insuficiente",
                "O consumo informado é maior que o combustível disponível no tanque.",
                "OK");
            return;
        }

        double liquido = bruto - tara;
        double valorEnergetico = consumo * liquido / 1000.0;
        double meta = viagem.MetaEnergetica > 0
            ? viagem.MetaEnergetica
            : 82;

        double litrosAntigos = dados.LitrosAtuais;
        bool tanqueDescontadoAnterior = viagem.TanqueDescontado;

        DateTime retorno = DateTime.Now;
        viagem.DataHoraChegada = retorno;

        viagem.HoraFinal = retorno.TimeOfDay;
        viagem.AnoCarreta = ano;
        viagem.PesoBruto = bruto;
        viagem.Tara = tara;
        viagem.PesoLiquido = liquido;
        viagem.KmFinal = kmFinal;
        viagem.LitrosConsumidos = litros;
        viagem.ConsumoKmL = consumo;
        viagem.ValorEnergetico = valorEnergetico;
        viagem.MetaEnergetica = meta;
        viagem.EmAndamento = false;

        if (dados.Capacidade > 0 && !viagem.TanqueDescontado)
        {
            dados.LitrosAtuais -= litros;
            viagem.TanqueDescontado = true;
        }

        viagem.EditadoEmUtc = DateTime.UtcNow;
        viagem.EditadoPorDispositivo = ContextoTurno.Dispositivo;

        try
        {
            SalvarDados();
        }
        catch
        {
            dados.LitrosAtuais = litrosAntigos;
            viagem.TanqueDescontado = tanqueDescontadoAnterior;
            viagem.EmAndamento = true;
            throw;
        }

        bool sincronizada =
            await FirebaseSyncService.SalvarViagemAsync(viagem);

        await DisplayAlert(
            "Viagem finalizada",
            "Caminhão: " + viagem.Veiculo + "\n" +
            "KM final: " + kmFinal.ToString("N1", pt) + "\n" +
            "KM rodados: " + (kmFinal - viagem.KmInicial).ToString("N1", pt) + "\n" +
            "Peso líquido: " + liquido.ToString("N1", pt) + " kg\n" +
            "Consumo: " + consumo.ToString("N2", pt) + " km/l\n" +
            "Combustível descontado: " +
            (viagem.TanqueDescontado ? litros.ToString("N1", pt) : "não aplicado") +
            " L\n" +
            (sincronizada
                ? "Sincronizado com Firebase."
                : "Salvo no celular. Sincronização pendente."),
            "OK");

        AtualizarTanqueArlaTexto(viagem.Veiculo);
        MostrarMenu("Cargas");
    }
    catch (Exception ex)
    {
        await DisplayAlert(
            "Erro ao finalizar viagem",
            ex.Message,
            "OK");
    }
    finally
    {
        salvandoViagem = false;
    }
}

    async void SalvarViagem(object? sender, EventArgs e)
    {
        if (salvandoViagem)
            return;

        salvandoViagem = true;

        try
        {
            string nomeVeiculo = cargaVeiculo.Text?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(nomeVeiculo) ||
                string.IsNullOrWhiteSpace(frenteSelecionada) ||
                horaFinalViagem.Time == horaInicialViagem.Time ||
                !int.TryParse(anoCarreta.Text, out var ano) ||
                !Ler(pesoBruto.Text, out var bruto) ||
                !Ler(litrosViagem.Text, out var litrosConsumidos) ||
                !Ler(consumoViagem.Text, out var consumo) ||
                !Ler(kmInicialViagem.Text, out var kmInicial) ||
                !Ler(kmFinalViagem.Text, out var kmFinal) ||
                bruto <= 0 || litrosConsumidos <= 0 || consumo <= 0 ||
                kmInicial < 0 || kmFinal <= kmInicial)
            {
                await DisplayAlert("Atenção",
                    "Preencha caminhão, frente, horários diferentes, ano, peso bruto, litros, consumo e KM inicial/final válidos.",
                    "OK");
                return;
            }

            double tara = TaraPorAno(ano);

            if (tara <= 0)
            {
                await DisplayAlert("Ano sem tara",
                    "Não existe tara cadastrada para esse ano.", "OK");
                return;
            }

            if (bruto <= tara)
            {
                await DisplayAlert("Peso inválido",
                    "O peso bruto precisa ser maior que a tara.", "OK");
                return;
            }

            var dados = ObterVeiculo(nomeVeiculo);

            double litrosDisponiveisParaEdicao = dados.LitrosAtuais +
                (viagemEmEdicao?.TanqueDescontado == true ? viagemEmEdicao.LitrosConsumidos : 0);

            if (dados.Capacidade > 0 &&
                litrosConsumidos > litrosDisponiveisParaEdicao)
            {
                await DisplayAlert("Combustível insuficiente",
                    "Tanque: " + dados.LitrosAtuais.ToString("N1", pt) + " L.\n" +
                    "Consumo da viagem: " + litrosConsumidos.ToString("N1", pt) + " L.",
                    "OK");
                return;
            }

            double liquido = bruto - tara;
            double valorEnergetico = consumo * liquido / 1000.0;
            double meta = 82;

            if (Ler(metaEnergetica.Text, out var metaDigitada) &&
                metaDigitada > 0)
                meta = metaDigitada;

            var viagem = new Viagem
            {
                Id = viagemEmEdicao?.Id ?? Guid.NewGuid().ToString(),
                Turno = viagemEmEdicao != null &&
        (viagemEmEdicao.Turno == "A" ||
         viagemEmEdicao.Turno == "B" ||
         viagemEmEdicao.Turno == "C")
        ? viagemEmEdicao.Turno
        : ContextoTurno.Turno,

CriadoEmUtc = viagemEmEdicao?.CriadoEmUtc ?? DateTime.UtcNow,

CriadoPorDispositivo = viagemEmEdicao?.CriadoPorDispositivo
    ?? ContextoTurno.Dispositivo,

EditadoEmUtc = DateTime.UtcNow,

EditadoPorDispositivo = ContextoTurno.Dispositivo,
                Data = (dataViagem.Date ?? DateTime.Today)
    .Date.Add(horaInicialViagem.Time ?? TimeSpan.Zero),

HoraInicial = horaInicialViagem.Time ?? TimeSpan.Zero,

HoraFinal = horaFinalViagem.Time ?? TimeSpan.Zero,
                Frente = frenteSelecionada,
                Veiculo = nomeVeiculo,
                Motorista = viagemEmEdicao?.Motorista
    ?? cargaMotorista.Text?.Trim() ?? "",

MotoristaAtual = viagemEmEdicao?.MotoristaAtual
    ?? cargaMotorista.Text?.Trim() ?? "",

Transferencias = viagemEmEdicao?.Transferencias
    ?? new List<TransferenciaViagem>(),

EmAndamento = viagemEmEdicao?.EmAndamento ?? false,
DataHoraChegada = viagemEmEdicao?.DataHoraChegada,
TurnoResponsavel = viagemEmEdicao?.TurnoResponsavel,
TransferenciasTurno = viagemEmEdicao?.TransferenciasTurno ?? new List<TransferenciaTurno>(),
                AnoCarreta = ano,
                PesoBruto = bruto,
                Tara = tara,
                PesoLiquido = liquido,
                KmInicial = kmInicial,
                KmFinal = kmFinal,
                LitrosConsumidos = litrosConsumidos,
                ConsumoKmL = consumo,
                ValorEnergetico = valorEnergetico,
                MetaEnergetica = meta,
                TanqueDescontado = viagemEmEdicao?.TanqueDescontado ?? (dados.Capacidade > 0)
            };

            double litrosAntigos = dados.LitrosAtuais;
            Viagem? viagemAnterior = viagemEmEdicao;

            if (viagemAnterior?.TanqueDescontado == true && dados.Capacidade > 0)
                dados.LitrosAtuais += viagemAnterior.LitrosConsumidos;

            if (viagem.TanqueDescontado)
                dados.LitrosAtuais -= litrosConsumidos;

            if (dados.Capacidade > 0 &&
                (dados.LitrosAtuais < 0 || dados.LitrosAtuais > dados.Capacidade))
            {
                dados.LitrosAtuais = litrosAntigos;
                await DisplayAlert("Atenção", "A edição deixaria o nível do tanque inválido.", "OK");
                return;
            }

            if (viagemAnterior != null)
            {
                int indice = viagens.IndexOf(viagemAnterior);
                if (indice >= 0) viagens[indice] = viagem;
                else viagens.Insert(0, viagem);
            }
            else
                viagens.Insert(0, viagem);

            try
            {
                SalvarDados();
            }
            catch
            {
                if (viagemAnterior != null)
                {
                    int indice = viagens.IndexOf(viagem);
                    if (indice >= 0) viagens[indice] = viagemAnterior;
                }
                else
                    viagens.Remove(viagem);
                dados.LitrosAtuais = litrosAntigos;
                throw;
            }

                        bool sincronizada =
                await FirebaseSyncService.SalvarViagemAsync(viagem);

            
            if (!sincronizada)
{
    await DisplayAlert(
        "Erro ao sincronizar viagem",
        FirebaseSyncService.UltimoErro,
        "OK");
}
else
{
    await DisplayAlert(
        "Firebase",
        "Viagem enviada para a nuvem com sucesso!",
        "OK");
}
            
            viagemEmEdicao = null;
            cargaVeiculo.IsEnabled = true;

            await DisplayAlert("Viagem salva",
                (viagemAnterior != null ? "Viagem atualizada com sucesso!\n\n" : "Registro salvo com sucesso!\n\n") +
                $"Frente: {frenteSelecionada} | Horário: {horaInicialViagem.Time:hh\\:mm} até {horaFinalViagem.Time:hh\\:mm}\n" +
                "Peso líquido: " + liquido.ToString("N1", pt) + " kg\n" +
           $"KM inicial: {kmInicial.ToString("N1", pt)} | KM final: {kmFinal.ToString("N1", pt)}\n" +
           $"KM rodados: {(kmFinal-kmInicial).ToString("N1", pt)} km\n" +
           $"Raio individual: {((kmFinal-kmInicial)/2.0).ToString("N2", pt)} km\n" +
                $"Consumo: {consumo.ToString("N2", pt)} km/l\n" +
                $"Valor energético: {valorEnergetico.ToString("N2", pt)}\n" +
                $"Meta: {meta.ToString("N2", pt)}\n" +
                $"Combustível descontado: {litrosConsumidos.ToString("N1", pt)} L",
                "OK");
                AtualizarTanqueArlaTexto(viagem.Veiculo);

            MostrarMenu("Cargas");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erro ao salvar viagem", ex.Message, "OK");
        }
        finally
        {
            salvandoViagem = false;
        }
    }


    async void CarregarViagemParaEdicao(Viagem x)
    {
    if (!ContextoTurno.EhAdministrador &&
    x.Turno != ContextoTurno.Turno)
{
    await DisplayAlert(
        "Acesso restrito",
        "Somente o turno que cadastrou esta viagem pode editá-la.",
        "OK");
    return;
}
        viagemEmEdicao = x;
        botaoAcaoViagem.Text = "SALVAR ALTERAÇÕES";
        cargaVeiculo.IsEnabled = false;
        dataViagem.Date = x.Data.Date;
        horaInicialViagem.Time = x.HoraInicial;
        horaFinalViagem.Time = x.HoraFinal;
        frenteSelecionada = x.Frente;

        foreach (var item in checksFrentes)
            item.Value.IsChecked = item.Key.ToString() == x.Frente;

        cargaVeiculo.Text = x.Veiculo;
        cargaMotorista.Text = x.Motorista;
        anoCarreta.Text = x.AnoCarreta.ToString();
        pesoBruto.Text = x.PesoBruto.ToString("0.##", CultureInfo.InvariantCulture);
        kmInicialViagem.Text = x.KmInicial.ToString("0.##", CultureInfo.InvariantCulture);
        kmFinalViagem.Text = x.KmFinal.ToString("0.0", pt);
        litrosViagem.Text = x.LitrosConsumidos.ToString("0.##", CultureInfo.InvariantCulture);
        consumoViagem.Text = x.ConsumoKmL.ToString("0.00", pt);
        metaEnergetica.Text = x.MetaEnergetica.ToString("0.##", CultureInfo.InvariantCulture);
        AtualizarPeso();

        DisplayAlert("Editar viagem",
            "Os dados foram carregados. Altere o que precisar e toque em SALVAR ALTERAÇÕES.",
            "OK");
    }

    
View CriarListaViagens()
{
    var painel = new VerticalStackLayout { Spacing = 8 };

    if (!viagens.Any(v => !v.Excluida))
    {
        painel.Children.Add(new Label
        {
            Text = "Nenhuma viagem registrada.",
            TextColor = Colors.Gray
        });
        return painel;
    }

    foreach (var x in viagens.Where(v => !v.Excluida).OrderByDescending(v => DataContabilizacaoViagem(v)))
    {
        var detalhes = new VerticalStackLayout { Spacing = 3 };

        detalhes.Children.Add(new Label
        {
            Text = x.EmAndamento
    ? $"{x.Data:dd/MM/yyyy} | {x.HoraInicial:hh\\:mm}–EM ANDAMENTO - {x.Veiculo} | Frente {x.Frente}"
    : $"Saída: {x.Data:dd/MM/yyyy} {x.HoraInicial:hh\\:mm} | Chegada: {(x.DataHoraChegada ?? x.Data.Date.Add(x.HoraFinal)):dd/MM/yyyy HH:mm} - {x.Veiculo} | Frente {x.Frente}",
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#123B65")
        });
           detalhes.Children.Add(new Label
{
    Text = x.EmAndamento
        ? "STATUS: VIAGEM EM ANDAMENTO"
        : "STATUS: VIAGEM CONCLUÍDA",
    FontAttributes = FontAttributes.Bold,
    TextColor = x.EmAndamento
        ? Colors.DarkOrange
        : Colors.Green
});

detalhes.Children.Add(new Label
{
    Text = "TURNO DE ORIGEM: " + x.Turno +
        " | RESPONSÁVEL ATUAL: " +
        (string.IsNullOrWhiteSpace(x.TurnoResponsavel)
            ? x.Turno
            : x.TurnoResponsavel),
    FontAttributes = FontAttributes.Bold,
    TextColor = Color.FromArgb("#7C3AED")
});
                

        detalhes.Children.Add(new Label
        {
            Text = $"Motorista: {x.Motorista} | Carreta: {x.AnoCarreta}",
            TextColor = Color.FromArgb("#123B65")
        });
        detalhes.Children.Add(new Label
{
    Text = "MOTORISTA ATUAL: " +
        (string.IsNullOrWhiteSpace(x.MotoristaAtual)
            ? x.Motorista
            : x.MotoristaAtual),
    FontAttributes = FontAttributes.Bold,
    TextColor = Color.FromArgb("#0F766E")
});

if (x.Transferencias != null && x.Transferencias.Any())
{
    detalhes.Children.Add(new Label
    {
        Text = "HISTÓRICO DE TRANSFERÊNCIAS:\n" +
            string.Join("\n", x.Transferencias.Select(t =>
                $"{t.MotoristaAnterior} → {t.MotoristaNovo} | " +
                $"{t.DataHora:dd/MM/yyyy HH:mm} | KM: {t.Km.ToString("N1", pt)}")),
        TextColor = Color.FromArgb("#123B65")
    });
}
if (x.TransferenciasTurno != null &&
    x.TransferenciasTurno.Any())
{
    detalhes.Children.Add(new Label
    {
        Text = "HISTÓRICO DE TURNOS:\n" +
            string.Join("\n", x.TransferenciasTurno.Select(t =>
                $"Turno {t.TurnoAnterior} → Turno {t.TurnoNovo} | " +
                $"{t.DataHora:dd/MM/yyyy HH:mm} | " +
                $"Responsável: {t.Responsavel}")),
        TextColor = Color.FromArgb("#7C3AED")
    });
}
        detalhes.Children.Add(new Label
        {
            Text = $"Bruto: {x.PesoBruto.ToString("N1", pt)} kg | Tara: {x.Tara.ToString("N1", pt)} kg | Líquido: {x.PesoLiquido.ToString("N1", pt)} kg",
            TextColor = Color.FromArgb("#123B65")
        });

    

double ultimoKmTransferencia =
    x.Transferencias?
        .OrderBy(t => t.DataHora)
        .LastOrDefault()?.Km ?? x.KmInicial;

string textoKm = x.EmAndamento
    ? $"KM inicial: {x.KmInicial.ToString("0.0", pt)} | Último KM registrado: {ultimoKmTransferencia.ToString("0.0", pt)}\n"
    : (ViagemComKmValido(x)
        ? $"KM inicial: {x.KmInicial.ToString("0.0", pt)} | KM final: {x.KmFinal.ToString("0.0", pt)} | Rodados: {ObterKmRodados(x).ToString("0.0", pt)} km | Raio: {(ObterKmRodados(x) / 2.0).ToString("N2", pt)} km\n"
        : "Quilometragem não informada\n");

detalhes.Children.Add(new Label
{
    Text = textoKm +
        (VelocidadeMediaViagem(x) > 0
            ? "Velocidade média da viagem: " + VelocidadeMediaViagem(x).ToString("N2", pt) + " km/h\n"
            : "Velocidade média: não calculada (verifique KM e horários)\n") +
        $"Combustível: {x.LitrosConsumidos.ToString("N1", pt)} L | Consumo: {x.ConsumoKmL.ToString("N2", pt)} km/l",
    TextColor = Color.FromArgb("#123B65")
});

        detalhes.Children.Add(new Label
        {
            Text = $"Valor energético: {x.ValorEnergetico.ToString("N2", pt)} | Meta: {x.MetaEnergetica.ToString("N2", pt)}",
            TextColor = Color.FromArgb("#123B65")
        });

        var botoesViagem = new VerticalStackLayout { Spacing = 8 };
        if (x.EmAndamento)
{
    string turnoResponsavel =
        string.IsNullOrWhiteSpace(x.TurnoResponsavel)
            ? x.Turno
            : x.TurnoResponsavel;

    bool podeOperar =
        ContextoTurno.EhAdministrador ||
        turnoResponsavel == ContextoTurno.Turno;

    if (podeOperar)
    {
        var finalizar = Botao("FINALIZAR VIAGEM", "#15803D");
        finalizar.Clicked += (s, e) => FinalizarViagem(x);
        botoesViagem.Children.Add(finalizar);

        var trocarMotorista = Botao("TROCAR MOTORISTA", "#2563EB");
        trocarMotorista.Clicked += (s, e) => TransferirCaminhao(x);
        botoesViagem.Children.Add(trocarMotorista);

        var transferirTurno = Botao("TRANSFERIR TURNO", "#7C3AED");
        transferirTurno.Clicked += (s, e) =>
            TransferirResponsabilidadeTurno(x);
        botoesViagem.Children.Add(transferirTurno);
    }
    else
    {
        detalhes.Children.Add(new Label
        {
            Text = "RESPONSÁVEL: TURNO " + turnoResponsavel,
            TextColor = Colors.DarkOrange
        });
    }
}

        
if (!x.EmAndamento &&
    (ContextoTurno.EhAdministrador ||
     x.Turno == ContextoTurno.Turno))

        
        {
            var editarViagem = Botao("EDITAR VIAGEM", "#0F766E");
            editarViagem.Clicked += (sender, args) =>
                CarregarViagemParaEdicao(x);

            botoesViagem.Children.Add(editarViagem);

            var excluir = Botao("EXCLUIR VIAGEM", "#B91C1C");

            excluir.Clicked += async (s, e) =>
            {
                bool ok = await DisplayAlert(
                    "Excluir viagem",
                    "Deseja excluir esta viagem? Os litros descontados serão devolvidos ao tanque cadastrado.",
                    "Sim", "Não");

                if (!ok)
                    return;

                if (!ContextoTurno.EhAdministrador &&
                    x.Turno != ContextoTurno.Turno)
                {
                    await DisplayAlert(
                        "Acesso restrito",
                        "Somente o turno que cadastrou esta viagem pode excluí-la.",
                        "OK");
                    return;
                }

                x.Excluida = true;
                x.EditadoEmUtc = DateTime.UtcNow;
                x.EditadoPorDispositivo = ContextoTurno.Dispositivo;

                bool excluidaNuvem =
                    await FirebaseSyncService.SalvarViagemAsync(x);

                if (!excluidaNuvem)
                {
                    x.Excluida = false;

                    await DisplayAlert(
                        "Erro ao excluir",
                        FirebaseSyncService.UltimoErro,
                        "OK");

                    return;
                }

                double litrosAntigos = 0;
                DadosVeiculo? d = null;

                if (x.TanqueDescontado)
                {
                    d = ObterVeiculo(x.Veiculo);
                    litrosAntigos = d.LitrosAtuais;

                    d.LitrosAtuais = Math.Min(
                        d.Capacidade,
                        d.LitrosAtuais + x.LitrosConsumidos);
                }

                try
                {
                    SalvarDados();
                    MostrarMenu("Cargas");
                }
                catch (Exception ex)
                {
                    if (d != null)
                        d.LitrosAtuais = litrosAntigos;

                    await DisplayAlert("Erro", ex.Message, "OK");
                }
            };

            botoesViagem.Children.Add(excluir);
        }

        detalhes.Children.Add(botoesViagem);

        painel.Children.Add(new Border
        {
            BackgroundColor = Colors.White,
            Stroke = Color.FromArgb("#DCE4ED"),
            Padding = 10,
            Content = detalhes
        });
    }

    return painel;
}
async void TransferirResponsabilidadeTurno(Viagem viagem)
{
    string turnoAtual =
        string.IsNullOrWhiteSpace(viagem.TurnoResponsavel)
            ? viagem.Turno
            : viagem.TurnoResponsavel;

    if (!viagem.EmAndamento)
        return;

    if (!ContextoTurno.EhAdministrador &&
        turnoAtual != ContextoTurno.Turno)
    {
        await DisplayAlert(
            "Acesso negado",
            "Somente o turno responsável ou o administrador pode transferir a viagem.",
            "OK");
        return;
    }

    string novoTurno = await DisplayActionSheet(
        "Transferir responsabilidade para:",
        "Cancelar",
        null,
        "Turno A",
        "Turno B",
        "Turno C");

    if (string.IsNullOrWhiteSpace(novoTurno) ||
        novoTurno == "Cancelar")
        return;

    novoTurno = novoTurno.Replace("Turno ", "");

    if (novoTurno == turnoAtual)
    {
        await DisplayAlert(
            "Atenção",
            "A viagem já está sob responsabilidade desse turno.",
            "OK");
        return;
    }

    bool confirmar = await DisplayAlert(
        "Confirmar transferência",
        "Transferir a responsabilidade do turno " +
        turnoAtual + " para o turno " + novoTurno + "?",
        "Confirmar",
        "Cancelar");

    if (!confirmar)
        return;

    viagem.TransferenciasTurno ??=
        new List<TransferenciaTurno>();

    viagem.TransferenciasTurno.Add(
        new TransferenciaTurno
        {
            TurnoAnterior = turnoAtual,
            TurnoNovo = novoTurno,
            DataHora = DateTime.Now,
            Responsavel = ContextoTurno.EhAdministrador
                ? "ADMINISTRADOR"
                : "TURNO " + ContextoTurno.Turno
        });

    viagem.TurnoResponsavel = novoTurno;
    viagem.EditadoEmUtc = DateTime.UtcNow;
    viagem.EditadoPorDispositivo = ContextoTurno.Dispositivo;

    SalvarDados();

    bool sincronizada =
        await FirebaseSyncService.SalvarViagemAsync(viagem);

    await DisplayAlert(
        "Transferência registrada",
        "Responsabilidade transferida para o turno " +
        novoTurno +
        (sincronizada
            ? "\nSincronizado com Firebase."
            : "\nSalvo no celular. Sincronização pendente."),
        "OK");

    MostrarMenu("Cargas");
}


async void TransferirCaminhao(Viagem viagem)
{
    string turnoResponsavel =
        string.IsNullOrWhiteSpace(viagem.TurnoResponsavel)
            ? viagem.Turno
            : viagem.TurnoResponsavel;

    if (!ContextoTurno.EhAdministrador &&
        turnoResponsavel != ContextoTurno.Turno)
    {
        await DisplayAlert(
            "Acesso negado",
            "Somente o turno responsável pela viagem ou o administrador pode trocar o motorista.",
            "OK");
        return;
    }
    string motoristaAtual =
        string.IsNullOrWhiteSpace(viagem.MotoristaAtual)
            ? viagem.Motorista
            : viagem.MotoristaAtual;

    string novoMotorista = await DisplayPromptAsync(
        "Transferir caminhão",
        "Motorista que está recebendo o caminhão:",
        "Continuar",
        "Cancelar",
        "Nome do parceiro",
        80);

    if (string.IsNullOrWhiteSpace(novoMotorista))
        return;

    novoMotorista = novoMotorista.Trim();

    if (Id(novoMotorista) == Id(motoristaAtual))
    {
        await DisplayAlert(
            "Atenção",
            "Informe um parceiro diferente do motorista atual.",
            "OK");
        return;
    }

    string textoKm = await DisplayPromptAsync(
        "KM da transferência",
        "Informe a quilometragem atual do caminhão:",
        "Registrar",
        "Cancelar",
        "KM atual",
        20,
        Keyboard.Numeric);

    if (!Ler(textoKm, out double km))
    {
        await DisplayAlert(
            "KM inválido",
            "Informe uma quilometragem válida.",
            "OK");
        return;
    }

    double ultimoKm = viagem.Transferencias?.LastOrDefault()?.Km
        ?? viagem.KmInicial;

    if (km < ultimoKm)
    {
        await DisplayAlert(
            "KM inválido",
            "O KM da transferência não pode ser menor que o último KM registrado.",
            "OK");
        return;
    }

    string motoristaAnterior = motoristaAtual;

    viagem.Transferencias ??= new List<TransferenciaViagem>();

    viagem.Transferencias.Add(new TransferenciaViagem
    {
        MotoristaAnterior = motoristaAnterior,
        MotoristaNovo = novoMotorista,
        DataHora = DateTime.Now,
        Km = km
    });

    viagem.MotoristaAtual = novoMotorista;
    viagem.EditadoEmUtc = DateTime.UtcNow;
    viagem.EditadoPorDispositivo = ContextoTurno.Dispositivo;

    SalvarDados();

    bool sincronizada =
        await FirebaseSyncService.SalvarViagemAsync(viagem);

    await DisplayAlert(
        "Transferência registrada",
        "Caminhão: " + viagem.Veiculo + "\n" +
        "Entregue por: " + motoristaAnterior + "\n" +
        "Recebido por: " + novoMotorista + "\n" +
        "KM: " + km.ToString("N1", pt) + "\n" +
        "Horário: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm") +
        (sincronizada
            ? "\nSincronizado com Firebase."
            : "\nSalvo no celular. Sincronização pendente."),
        "OK");

    MostrarMenu("Cargas");
}

    bool ViagemComKmValido(Viagem v) => v.KmFinal > v.KmInicial && v.KmInicial >= 0;
    double ObterKmRodados(Viagem v) => ViagemComKmValido(v) ? v.KmFinal - v.KmInicial : 0;
    DateTime DataContabilizacaoViagem(Viagem v) =>
    (v.DataHoraChegada ?? v.Data).Date;
    double SomarKmViagens(IEnumerable<Viagem> registros) =>
        registros.Where(ViagemComKmValido).Sum(ObterKmRodados);
    double CalcularRaioMedio(IEnumerable<Viagem> registros)
    {
        var validas = registros.Where(ViagemComKmValido).ToList();
        return validas.Count == 0 ? 0 : SomarKmViagens(validas) / validas.Count / 2.0;
    }

    double DuracaoHoras(Viagem v)
    {
        TimeSpan duracao = v.HoraFinal - v.HoraInicial;
        if (duracao.TotalMinutes < 0) duracao += TimeSpan.FromHours(24);
        return duracao.TotalHours;
    }

    double CalcularVelocidadeMedia(IEnumerable<Viagem> registros)
    {
        var validas = registros.Where(v => ViagemComKmValido(v) && DuracaoHoras(v) > 0).ToList();
        double horas = validas.Sum(DuracaoHoras);
        return horas > 0 ? validas.Sum(ObterKmRodados) / horas : 0;
    }

    double VelocidadeMediaViagem(Viagem v)
    {
        double horas = DuracaoHoras(v);
        return ViagemComKmValido(v) && horas > 0
            ? ObterKmRodados(v) / horas
            : 0;
    }

    List<Viagem> FiltrarViagensPorFrente(IEnumerable<Viagem> registros)
    {
        return frenteFiltroIndicadores == "Todas"
            ? registros.ToList()
            : registros.Where(v => v.Frente == frenteFiltroIndicadores).ToList();
    }

    // Média de consumo de Arla calculada somente com abastecimentos que
    // possuem Arla informado e intervalo de KM válido.
    List<Abastecimento> AbastecimentosComArlaValidos(IEnumerable<Abastecimento> registros) =>
        registros.Where(x => x.TemArla && x.LitrosArla > 0 && x.KmAtual > x.KmAnterior).ToList();

    double CalcularMediaArlaKmL(IEnumerable<Abastecimento> registros)
    {
        var validos = AbastecimentosComArlaValidos(registros);
        double litros = validos.Sum(x => x.LitrosArla);
        double km = validos.Sum(x => x.KmAtual - x.KmAnterior);
        return litros > 0 ? km / litros : 0;
    }

    double CalcularMediaArla100Km(IEnumerable<Abastecimento> registros)
    {
        var validos = AbastecimentosComArlaValidos(registros);
        double litros = validos.Sum(x => x.LitrosArla);
        double km = validos.Sum(x => x.KmAtual - x.KmAnterior);
        return km > 0 ? litros * 100.0 / km : 0;
    }

    // Meta de toneladas em 24 horas conforme a tabela de raio médio enviada.
    // A chave é o raio arredondado para o inteiro mais próximo.
    readonly Dictionary<int, double> metasPremioDiario = new()
    {
        {5,1230},{6,1171},{7,1113},{8,1054},{9,995},{10,936},{11,874},{12,843},
        {13,812},{14,784},{15,755},{16,731},{17,706},{18,682},{19,657},{20,633},
        {21,612},{22,592},{23,572},{24,552},{25,545},{26,531},{27,510},{28,495},
        {29,485},{30,478},{31,469},{32,453},{33,443},{34,434},{35,426},{36,419},
        {37,410},{38,401},{39,394},{40,384},{41,375},{42,374},{43,366},{44,362},
        {45,350},{46,346},{47,342},{48,338},{49,330},{50,321},{51,316},{52,311},
        {53,306},{54,302},{55,297},{56,293},{57,289},{58,284},{59,280},{60,276},
        {61,272},{62,269},{63,265},{64,262},{65,258},{66,255},{67,252},{68,248},
        {69,245},{70,242},{71,239},{72,236},{73,234},{74,231},{75,228},{76,225},
        {77,223},{78,220},{79,218},{80,215},{81,213},{82,211},{83,208},{84,206},
        {85,204},{86,202},{87,200},{88,198},{89,196},{90,194},{91,192},{92,190},
        {93,188},{94,186},{95,185},{96,183},{97,181},{98,180},{99,178},{100,177}
    };

    View CartaoPremioDiario(IEnumerable<Viagem> viagensDoDia)
    {
        var registros = viagensDoDia.ToList();
        double raio = CalcularRaioMedio(registros);
        double toneladas = registros.Sum(v => v.PesoLiquido) / 1000.0;
        int raioTabela = raio > 0
            ? (int)Math.Round(raio, MidpointRounding.AwayFromZero)
            : 0;

        var painel = new VerticalStackLayout { Spacing = 6 };
        painel.Children.Add(Titulo("PRÊMIO DE PRODUTIVIDADE — 24 HORAS"));
        painel.Children.Add(Indicador($"Raio médio do dia: {(raio > 0 ? raio.ToString("N2", pt) + " km" : "--")}"));
        painel.Children.Add(Indicador("Toneladas transportadas: " + toneladas.ToString("N2", pt) + " t"));

        if (raioTabela < 5 || raioTabela > 100 || !metasPremioDiario.TryGetValue(raioTabela, out double meta))
        {
            painel.Children.Add(new Label
            {
                Text = "Não foi possível verificar o prêmio: o raio médio precisa estar entre 5 e 100 km e haver viagens com KM válido.",
                TextColor = Colors.DarkOrange,
                FontAttributes = FontAttributes.Bold
            });
        }
        else
        {
            double falta = Math.Max(0, meta - toneladas);
            painel.Children.Add(Indicador($"Raio considerado na tabela: {raioTabela} km"));
            painel.Children.Add(Indicador("Meta para esse raio: " + meta.ToString("N1", pt) + " toneladas"));

            bool atingiu = toneladas >= meta;
            painel.Children.Add(new Label
            {
                Text = atingiu
                    ? "✓ VOCÊ ATINGIU O PRÊMIO DIÁRIO!"
                    : "✗ VOCÊ NÃO ATINGIU O PRÊMIO DIÁRIO",
                TextColor = atingiu ? Colors.Green : Colors.Red,
                FontAttributes = FontAttributes.Bold,
                FontSize = 18
            });
            painel.Children.Add(new Label
            {
                Text = atingiu
                    ? "Prêmio atingido! Excedente: " + (toneladas - meta).ToString("N2", pt) + " toneladas."
                    : "Faltam " + falta.ToString("N2", pt) + " toneladas para atingir o prêmio diário.",
                TextColor = atingiu ? Colors.DarkGreen : Colors.DarkRed,
                FontAttributes = FontAttributes.Bold,
                FontSize = 16
            });
        }

        return new Border
        {
            BackgroundColor = Colors.White,
            Stroke = Color.FromArgb("#DCE4ED"),
            Padding = 12,
            Content = painel
        };
    }

    View TelaIndicadores()
    {
        DateTime hoje = DateTime.Today;
        filtroFrenteIndicadores = NovoPicker("Filtrar por frente", "Todas", "Frente 1", "Frente 2", "Frente 3", "Frente 4", "Frente 5", "Frente 6", "Frente 7");
        filtroFrenteIndicadores.SelectedIndex = frenteFiltroIndicadores == "Todas" ? 0 : int.Parse(frenteFiltroIndicadores) ;
        filtroFrenteIndicadores.SelectedIndexChanged += (sender, args) =>
        {
            string selecionado = filtroFrenteIndicadores.SelectedItem?.ToString() ?? "Todas";
            frenteFiltroIndicadores = selecionado == "Todas" ? "Todas" : selecionado.Replace("Frente ", "");
            MostrarMenu("Indicadores");
        };

        var viagensIndicadores = FiltrarViagensPorFrente(
    viagens.Where(x => !x.Excluida).ToList());
        var abastecimentosValidos = abastecimentos
            .Where(x => x.Consumo > 0)
            .ToList();

        List<Abastecimento> AbNoPeriodo(Func<DateTime, bool> filtro)
        {
            return abastecimentosValidos.Where(x => filtro(x.Data)).ToList();
        }

        List<Viagem> ViNoPeriodo(Func<DateTime, bool> filtro)
        {
            return viagens.Where(x =>
    !x.Excluida &&
    filtro(DataContabilizacaoViagem(x))
).ToList();

};

        View CartaoPeriodo(string titulo, Func<DateTime, bool> filtro)
        {
            var ab = AbNoPeriodo(filtro);
            var cg = FiltrarViagensPorFrente(ViNoPeriodo(filtro));

            double kmTotal = SomarKmViagens(cg);

            double litrosTotal = cg.Sum(x => x.LitrosConsumidos);
            double toneladas = cg.Sum(x => x.PesoLiquido) / 1000.0;
            double raioMedio = CalcularRaioMedio(cg);
            double velocidadeMedia = CalcularVelocidadeMedia(cg);

            return new Border
            {
                BackgroundColor = Colors.White,
                Stroke = Color.FromArgb("#DCE4ED"),
                Padding = 12,
                Content = new VerticalStackLayout
                {
                    Spacing = 5,
                    Children =
                    {
                        Titulo(titulo),
                        Indicador($"Total de KM: {kmTotal.ToString("N1", pt)} km"),
                        Indicador($"Viagens realizadas: {cg.Count}"),
                        Indicador(velocidadeMedia > 0
    ? "Velocidade média: " + velocidadeMedia.ToString("N2", pt) + " km/h"
    : "Velocidade média: --"),
                    
                           Indicador(cg.Count > 0
    ? "Raio médio por viagem: " + raioMedio.ToString("N2", pt) + " km"
    : "Raio médio por viagem: --"),
Indicador($"Combustível consumido: {litrosTotal.ToString("N1", pt)} L"),
                        
                        Indicador(AbastecimentosComArlaValidos(ab).Sum(x => x.LitrosArla) > 0
    ? "Arla 32: " +
      AbastecimentosComArlaValidos(ab).Sum(x => x.LitrosArla).ToString("N1", pt) +
      " L | Média: " +
      CalcularMediaArlaKmL(ab).ToString("N2", pt) +
      " km/l (" +
      CalcularMediaArla100Km(ab).ToString("N2", pt) +
      " L/100 km)"
    : "Arla 32: --"),
                  Indicador($"Toneladas líquidas: {toneladas.ToString("N2", pt)} t")
                    }
                }
            };
        }

        double mediaViagem = viagensIndicadores.Count > 0
            ? viagensIndicadores.Average(x => x.ConsumoKmL)
            : 0;
        double velocidadeMediaGeral = CalcularVelocidadeMedia(viagensIndicadores);

        double kmAbastecimentos = abastecimentosValidos.Sum(x =>
            Math.Max(0, x.KmAtual - x.KmAnterior));

        double litrosAbastecidos = abastecimentosValidos.Sum(x => x.Litros);
        double mediaAbast = litrosAbastecidos > 0
            ? kmAbastecimentos / litrosAbastecidos
            : 0;

        double kmGeral = SomarKmViagens(viagensIndicadores);

        double totalViagens = viagensIndicadores.Sum(x => x.LitrosConsumidos);
        double toneladasGeral = viagensIndicadores.Sum(x => x.PesoLiquido) / 1000.0;
        double raioMedioGeral = CalcularRaioMedio(viagensIndicadores);

        double mediaEnergetica = viagensIndicadores.Count > 0
            ? viagensIndicadores.Average(x => x.ValorEnergetico)
            : 0;

        lblComparacao = Indicador(
            mediaAbast > 0 && mediaViagem > 0
                ? (mediaAbast - mediaViagem).ToString("N2", pt) + " km/l"
                : "Sem dados suficientes");

        var painel = new VerticalStackLayout { Spacing = 8 };

        painel.Children.Add(Titulo("INDICADORES GERAIS"));
        painel.Children.Add(Rotulo("Filtro por frente"));
        painel.Children.Add(filtroFrenteIndicadores);
        painel.Children.Add(Cartao("Velocidade média das viagens",
    Indicador(velocidadeMediaGeral > 0
        ? velocidadeMediaGeral.ToString("N2", pt) + " km/h"
        : "--")));

        var viagensHojePremio = FiltrarViagensPorFrente(
    viagens.Where(v =>
        !v.Excluida &&
        DataContabilizacaoViagem(v) == hoje));
        painel.Children.Add(CartaoPremioDiario(viagensHojePremio));

        painel.Children.Add(CartaoPeriodo("HOJE",
            d => d.Date == hoje));

        painel.Children.Add(CartaoPeriodo("ESTE MÊS",
            d => d.Month == hoje.Month && d.Year == hoje.Year));

        painel.Children.Add(CartaoPeriodo("ESTE ANO",
            d => d.Year == hoje.Year));

        painel.Children.Add(Titulo("RESUMO DA FROTA"));

        painel.Children.Add(Cartao("Quantidade de abastecimentos",
            Indicador(abastecimentos.Count.ToString())));

        

        painel.Children.Add(Cartao("Média km/l nos abastecimentos",
            Indicador(mediaAbast > 0
    ? mediaAbast.ToString("N2", pt) + " km/l"
    : "--")));

        double mediaArlaKmL = CalcularMediaArlaKmL(abastecimentos);
        double mediaArla100 = CalcularMediaArla100Km(abastecimentos);
        double totalArla = AbastecimentosComArlaValidos(abastecimentos).Sum(x => x.LitrosArla);

        painel.Children.Add(Cartao("Média de consumo de Arla 32",
            Indicador(mediaArlaKmL > 0
                ? mediaArlaKmL.ToString("N2", pt) + " km/l | " +
  mediaArla100.ToString("N2", pt) + " L/100 km"
: "--")));
        painel.Children.Add(Cartao("Total de Arla 32 utilizado",
    Indicador(totalArla > 0
        ? totalArla.ToString("N1", pt) + " L"
        : "--")));

painel.Children.Add(Cartao("Quantidade de viagens",
    Indicador(viagensIndicadores.Count.ToString())));

painel.Children.Add(Cartao("Raio médio por viagem",
    Indicador(viagensIndicadores.Count > 0
        ? raioMedioGeral.ToString("N2", pt) + " km"
        : "--")));

        painel.Children.Add(Cartao("Tonelagem líquida total",
            Indicador(toneladasGeral.ToString("N2", pt) + " t")));

        painel.Children.Add(Cartao("Média km/l nas viagens",
            Indicador(mediaViagem > 0
    ? mediaViagem.ToString("N2", pt) + " km/l"
    : "--")));

        painel.Children.Add(Cartao("Litros consumidos nas viagens",
            Indicador(totalViagens.ToString("N1", pt) + " L")));

        painel.Children.Add(Cartao("Média do valor energético",
            Indicador(mediaEnergetica > 0
                ? mediaEnergetica.ToString("N2", pt) : "--")));

        painel.Children.Add(Cartao("Diferença entre médias",
            lblComparacao));

        painel.Children.Add(Titulo("INDICADORES POR VEÍCULO"));

        var nomesVeiculos = abastecimentos.Select(x => x.Veiculo)
            .Concat(viagensIndicadores.Select(x => x.Veiculo))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();

        if (nomesVeiculos.Count == 0)
        {
            painel.Children.Add(new Label
            {
                Text = "Ainda não existem dados por veículo.",
                TextColor = Colors.Gray
            });
        }

        foreach (string nome in nomesVeiculos)
        {
            var abVeiculo = abastecimentosValidos
                .Where(x => Id(x.Veiculo) == Id(nome)).ToList();
            var abArlaVeiculo = AbastecimentosComArlaValidos(abastecimentos)
                .Where(x => Id(x.Veiculo) == Id(nome)).ToList();
            double mediaArlaVeiculo = CalcularMediaArlaKmL(abArlaVeiculo);
            double mediaArla100Veiculo = CalcularMediaArla100Km(abArlaVeiculo);
            double litrosArlaVeiculo = abArlaVeiculo.Sum(x => x.LitrosArla);

            var viVeiculo = viagensIndicadores
                .Where(x => Id(x.Veiculo) == Id(nome)).ToList();

            double kmVeiculo = SomarKmViagens(viVeiculo);

            double litrosVeiculo = viVeiculo.Sum(x => x.LitrosConsumidos);
            double toneladasVeiculo =
                viVeiculo.Sum(x => x.PesoLiquido) / 1000.0;

            double raioMedioVeiculo =
                CalcularRaioMedio(viVeiculo);

            painel.Children.Add(new Border
            {
                BackgroundColor = Colors.White,
                Stroke = Color.FromArgb("#DCE4ED"),
                Padding = 12,
                Content = new VerticalStackLayout
                {
                    Spacing = 4,
                    Children =
                    {
                        new Label
                        {
                            Text = nome,
                            FontAttributes = FontAttributes.Bold,
                            FontSize = 16,
                            TextColor = Color.FromArgb("#123B65")
                        },
                        new Label { Text = $"KM registrados: {kmVeiculo.ToString("N1", pt)} km" },
                        new Label { Text = $"Viagens realizadas: {viVeiculo.Count}" },
                        new Label
{
    Text = CalcularVelocidadeMedia(viVeiculo) > 0
        ? "Velocidade média: " + CalcularVelocidadeMedia(viVeiculo).ToString("N2", pt) + " km/h"
        : "Velocidade média: --"
},
                        new Label
                        {
                            Text = viVeiculo.Count > 0
                                ?  "Raio médio por viagem: " + raioMedioVeiculo.ToString("N2", pt) + " km"
                                : "Raio médio por viagem: --"
                        },
                        new Label { Text = $"Litros consumidos: {litrosVeiculo.ToString("N1", pt)} L" },
                        new Label { Text = litrosArlaVeiculo > 0
                            ? "Arla 32: " + litrosArlaVeiculo.ToString("N1", pt) +
  " L | Média: " + mediaArlaVeiculo.ToString("N2", pt) +
  " km/l (" + mediaArla100Veiculo.ToString("N2", pt) + " L/100 km)"
                            : "Arla 32: sem registros" },
                        new Label { Text = $"Toneladas líquidas: {toneladasVeiculo.ToString("N2", pt)} t" }
                    }
                }
            });
        }

        painel.Children.Add(new Label
        {
            Text = "Raio médio = KM registrados no período ÷ número de viagens ÷ 2. " +
                   "Os KM vêm da diferença entre KM final e KM inicial informados em cada viagem. " +
                   "A velocidade média usa KM rodados divididos pelo tempo entre hora inicial e final.",
            TextColor = Colors.Gray,
            FontSize = 12
        });

        return painel;
    }

    View TelaRelatorios()
    {
        dataFiltro = new DatePicker
        {
            Date = DateTime.Today,
            TextColor = Colors.Black,
            BackgroundColor = Colors.White
        };

        tipoRelatorio = NovoPicker(
            "Tipo de relatório", "Todos", "Abastecimentos", "Cargas");

        periodoRelatorio = NovoPicker(
            "Período", "Diário", "Mensal", "Anual");

        filtroVeiculo = Campo("Veículo (vazio = todos)");
        filtroMotorista = Campo("Motorista (vazio = todos)");

        var aplicar = Botao("APLICAR FILTROS");
        aplicar.Clicked += (s, e) => MostrarResultadoRelatorio();

        var pdf = Botao("GERAR E COMPARTILHAR PDF");
        pdf.Clicked += CompartilharPDF;

        lista = new VerticalStackLayout { Spacing = 8 };

        return new VerticalStackLayout
        {
            Spacing = 5,
            Children =
            {
                Titulo("RELATÓRIOS E FILTROS"),
                Rotulo("Tipo de relatório"), tipoRelatorio,
                Rotulo("Período"), periodoRelatorio,
                Rotulo("Data de referência"), dataFiltro,
                Rotulo("Veículo"), filtroVeiculo,
                Rotulo("Motorista"), filtroMotorista,
                aplicar, pdf,
                Titulo("RESULTADO"), lista
            }
        };
    }

    bool DataNoPeriodo(DateTime data)
    {
        DateTime referencia = dataFiltro.Date ?? DateTime.Today;

        return periodoRelatorio.SelectedItem?.ToString() switch
        {
            "Mensal" =>
                data.Month == referencia.Month &&
                data.Year == referencia.Year,

            "Anual" => data.Year == referencia.Year,

            _ => data.Date == referencia.Date
        };
    }

    bool PassaFiltro(string nome, string nomeMotorista)
    {
        bool veiculoOk =
            string.IsNullOrWhiteSpace(filtroVeiculo.Text) ||
            nome.Contains(filtroVeiculo.Text.Trim(),
                StringComparison.OrdinalIgnoreCase);

        bool motoristaOk =
            string.IsNullOrWhiteSpace(filtroMotorista.Text) ||
            nomeMotorista.Contains(filtroMotorista.Text.Trim(),
                StringComparison.OrdinalIgnoreCase);

        return veiculoOk && motoristaOk;
    }

    List<Abastecimento> FiltrarAbastecimentos()
    {
        return abastecimentos
            .Where(x => DataNoPeriodo(x.Data))
            .Where(x => PassaFiltro(x.Veiculo, x.Motorista))
            .OrderByDescending(x => x.Data)
            .ToList();
    }

    List<Viagem> FiltrarViagens()
    {
        return viagens
    .Where(x => !x.Excluida)
    .Where(x => DataNoPeriodo(DataContabilizacaoViagem(x)))
       
            .Where(x => PassaFiltro(x.Veiculo, x.Motorista))
            .OrderByDescending(x => DataContabilizacaoViagem(x))
            .ToList();
    }

    void MostrarResultadoRelatorio()
    {
        lista.Children.Clear();

        string tipo = tipoRelatorio.SelectedItem?.ToString() ?? "Todos";

        var ab = tipo == "Cargas"
            ? new List<Abastecimento>()
            : FiltrarAbastecimentos();

        var cg = tipo == "Abastecimentos"
            ? new List<Viagem>()
            : FiltrarViagens();

        if (ab.Count == 0 && cg.Count == 0)
        {
            lista.Children.Add(new Label
            {
                Text = "Nenhum registro encontrado.",
                TextColor = Colors.Gray
            });
            return;
        }

        double kmTotal = SomarKmViagens(cg);

        double toneladasTotal = cg.Sum(x => x.PesoLiquido) / 1000.0;
        double raioMedio = CalcularRaioMedio(cg);

        lista.Children.Add(new Label
        {
            Text = $"Abastecimentos: {ab.Count} | Viagens: {cg.Count}",
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#123B65")
        });

        lista.Children.Add(new Label
        
            
{
    Text = "KM rodados: " + kmTotal.ToString("N1", pt) + " km",
    TextColor = Color.FromArgb("#123B65")
});

lista.Children.Add(new Label
{
    Text = cg.Count > 0
        ? "Raio médio por viagem: " + raioMedio.ToString("N2", pt) + " km"
        : "Raio médio por viagem: --",
    TextColor = Color.FromArgb("#123B65")
});

lista.Children.Add(new Label
{
    Text = "Tonelagem líquida total: " + toneladasTotal.ToString("N2", pt) + " t",
    TextColor = Color.FromArgb("#123B65")
});

lista.Children.Add(new Label
{
    Text = $"Gasto: {ab.Sum(x => x.Valor):C2} | " +
           "Litros abastecidos: " + ab.Sum(x => x.Litros).ToString("N1", pt) + " L",
    TextColor = Color.FromArgb("#123B65")
});

        double mediaArlaRelatorio = CalcularMediaArlaKmL(ab);
        double mediaArla100Relatorio = CalcularMediaArla100Km(ab);
        double litrosArlaRelatorio = AbastecimentosComArlaValidos(ab).Sum(x => x.LitrosArla);

        lista.Children.Add(new Label
{
    Text = litrosArlaRelatorio > 0
        ? "Arla 32: " + litrosArlaRelatorio.ToString("N1", pt) +
          " L | Média: " + mediaArlaRelatorio.ToString("N2", pt) +
          " km/l (" + mediaArla100Relatorio.ToString("N2", pt) + " L/100 km)"
        : "Arla 32: sem registros no período",
    TextColor = Color.FromArgb("#123B65")
});

lista.Children.Add(new Label
{
    Text = "Litros consumidos nas viagens: " +
           cg.Sum(x => x.LitrosConsumidos).ToString("N1", pt) + " L",
    TextColor = Color.FromArgb("#123B65")
});

// Premiação diária calculada separadamente por data
var gruposPremiacao = cg
    .GroupBy(v => DataContabilizacaoViagem(v))
    .OrderBy(g => g.Key);

foreach (var grupoPremio in gruposPremiacao)
{
    double raioDia = CalcularRaioMedio(grupoPremio);
    double toneladasDia = grupoPremio.Sum(v => v.PesoLiquido) / 1000.0;

    int raioTabelaDia = raioDia > 0
        ? (int)Math.Round(raioDia, MidpointRounding.AwayFromZero)
        : 0;

    lista.Children.Add(new Label
    {
        Text = $"PREMIAÇÃO DIÁRIA - {grupoPremio.Key:dd/MM/yyyy}",
        FontAttributes = FontAttributes.Bold,
        FontSize = 18,
        TextColor = Color.FromArgb("#123B65")
    });

    if (raioTabelaDia >= 5 && raioTabelaDia <= 100 &&
        metasPremioDiario.TryGetValue(raioTabelaDia, out double metaDia))
    {
        bool atingiuDia = toneladasDia >= metaDia;
        double faltaDia = Math.Max(0, metaDia - toneladasDia);

        lista.Children.Add(new Label
        {
            Text = $"Raio médio: {raioDia:N2} km | Toneladas: {toneladasDia:N2} t",
            TextColor = Color.FromArgb("#123B65")
        });

        lista.Children.Add(new Label
        {
            Text = $"Meta: {metaDia:N1} toneladas",
            TextColor = Color.FromArgb("#123B65")
        });

        lista.Children.Add(new Label
        {
            Text = atingiuDia
                ? "✓ PRÊMIO DIÁRIO ATINGIDO!"
                : "✗ PRÊMIO DIÁRIO NÃO ATINGIDO",
            FontAttributes = FontAttributes.Bold,
            TextColor = atingiuDia ? Colors.Green : Colors.Red
        });

        lista.Children.Add(new Label
        {
            Text = atingiuDia
                ? $"Excedente: {(toneladasDia - metaDia):N2} toneladas."
                : $"Faltam {faltaDia:N2} toneladas para atingir o prêmio.",
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#123B65")
        });
    }
    else
    {
        lista.Children.Add(new Label
        {
            Text = "Não foi possível calcular a premiação para esse dia.",
            TextColor = Colors.DarkOrange
        });
    }
}
        // Soma de tonelagem líquida agrupada por dia.
        var resumoDiario = cg
            .GroupBy(x => DataContabilizacaoViagem(x))
            .OrderBy(x => x.Key);

        foreach (var grupo in resumoDiario)
        {
            double toneladasDia =
                grupo.Sum(x => x.PesoLiquido) / 1000.0;
            double velocidadeDia = CalcularVelocidadeMedia(grupo);

            lista.Children.Add(new Label
            {
                Text = $"{grupo.Key:dd/MM/yyyy} - " +
                       $"Viagens: {grupo.Count()} | " +
                       "Velocidade média: " + velocidadeDia.ToString("N2", pt) + " km/h | " +
                       "Tonelagem líquida: " + toneladasDia.ToString("N2", pt) + " t",
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#0F766E")
            });
        }

        foreach (var x in ab)
        {
            lista.Children.Add(new Label
            {
                Text = $"{x.Data:dd/MM/yyyy HH:mm} | {x.Veiculo} | " +
                       x.Litros.ToString("N1", pt) + " L | " + x.Consumo.ToString("N2", pt) + " km/l",
                TextColor = Colors.Black
            });
        }

        foreach (var x in cg)
        {
            lista.Children.Add(new Label
            {
                Text = $"Saída: {x.Data:dd/MM/yyyy} {x.HoraInicial:hh\\:mm} | " +
       $"Chegada: {(x.DataHoraChegada ?? x.Data.Date.Add(x.HoraFinal)):dd/MM/yyyy HH:mm} | Frente {x.Frente} | {x.Veiculo} | " +
                       "Líquido: " + x.PesoLiquido.ToString("N1", pt) + " kg | " +
                    "KM: " + ObterKmRodados(x).ToString("N1", pt) +
" | Velocidade média: " +
(VelocidadeMediaViagem(x) > 0 ? VelocidadeMediaViagem(x).ToString("N2", pt) + " km/h" : "--") + " | " +
                       "Viagem: " + x.LitrosConsumidos.ToString("N1", pt) + " L | " +
                    x.ConsumoKmL.ToString("N2", pt) + " km/l | VE: " + x.ValorEnergetico.ToString("N2", pt),
                TextColor = Colors.Black
            });
        }
    }

    async void CompartilharPDF(object? sender, EventArgs e)
    {
        try
        {
            string tipo = tipoRelatorio.SelectedItem?.ToString() ?? "Todos";

            var ab = tipo == "Cargas"
                ? new List<Abastecimento>()
                : FiltrarAbastecimentos();

            var cg = tipo == "Abastecimentos"
                ? new List<Viagem>()
                : FiltrarViagens();

            if (ab.Count == 0 && cg.Count == 0)
            {
                await DisplayAlert("Atenção",
                    "Não existem registros para gerar o PDF.", "OK");
                return;
            }

#if ANDROID
            string caminho = Path.Combine(
                FileSystem.CacheDirectory,
                "Relatorio_FuelControl.pdf");

            using var doc = new AndroidPdfDocument();

            AndroidPdfDocument.Page? pagina = null;
            Android.Graphics.Canvas? canvas = null;

            int numeroPagina = 1;
            int y = 0;

            AndroidPaint titulo = new()
            {
                Color = AndroidColor.Rgb(18, 59, 101),
                TextSize = 15,
                FakeBoldText = true,
                AntiAlias = true
            };

            AndroidPaint subtitulo = new()
            {
                Color = AndroidColor.Rgb(15, 118, 110),
                TextSize = 11,
                FakeBoldText = true,
                AntiAlias = true
            };

            AndroidPaint texto = new()
            {
                Color = AndroidColor.Black,
                TextSize = 9,
                AntiAlias = true
            };

            void NovaPagina()
            {
                if (pagina != null)
                    doc.FinishPage(pagina);

                var info = new AndroidPdfDocument.PageInfo.Builder(
                    595, 842, numeroPagina++).Create();

                pagina = doc.StartPage(info);
                canvas = pagina.Canvas;
                y = 40;

                canvas.DrawText("FUELCONTROL - RELATORIO", 30, y, titulo);
                y += 20;

                canvas.DrawText(
                    $"Gerado em {DateTime.Now:dd/MM/yyyy HH:mm}",
                    30, y, texto);

                y += 25;
            }

            void Linha(string conteudo)
            {
                if (y > 790)
                    NovaPagina();

                string linha = conteudo.Length > 100
                    ? conteudo.Substring(0, 100)
                    : conteudo;

                canvas!.DrawText(linha, 30, y, texto);
                y += 15;
            }

            NovaPagina();

            Linha("RESUMO DO RELATORIO");

            Linha($"Abastecimentos: {ab.Count}");

if (tipo != "Abastecimentos")
{
    double kmTotal = SomarKmViagens(cg);
    double toneladasTotal = cg.Sum(x => x.PesoLiquido) / 1000.0;
    double raioMedio = CalcularRaioMedio(cg);

    Linha($"Viagens realizadas: {cg.Count}");
    Linha("KM rodados: " + kmTotal.ToString("N1", pt) + " km");
    Linha("Raio médio por viagem: " + raioMedio.ToString("N2", pt) + " km");
    Linha("Tonelagem líquida total: " + toneladasTotal.ToString("N2", pt) + " t");
    // Premiação calculada separadamente por dia no PDF
var gruposPremiacaoPdf = cg
    .GroupBy(v => DataContabilizacaoViagem(v))
    .OrderBy(g => g.Key);

foreach (var grupoPremioPdf in gruposPremiacaoPdf)
{
    double raioDiaPdf = CalcularRaioMedio(grupoPremioPdf);
    double toneladasDiaPdf =
        grupoPremioPdf.Sum(v => v.PesoLiquido) / 1000.0;

    int raioTabelaPdf = raioDiaPdf > 0
        ? (int)Math.Round(raioDiaPdf, MidpointRounding.AwayFromZero)
        : 0;

    Linha("PREMIACAO DIARIA - " + grupoPremioPdf.Key.ToString("dd/MM/yyyy"));

    if (raioTabelaPdf >= 5 && raioTabelaPdf <= 100 &&
        metasPremioDiario.TryGetValue(raioTabelaPdf, out double metaDiaPdf))
    {
        bool atingiuDiaPdf = toneladasDiaPdf >= metaDiaPdf;
        double faltaDiaPdf = Math.Max(0, metaDiaPdf - toneladasDiaPdf);

        Linha("Raio medio: " + raioDiaPdf.ToString("N2", pt) + " km");
        Linha("Toneladas: " + toneladasDiaPdf.ToString("N2", pt) + " t");
        Linha("Meta: " + metaDiaPdf.ToString("N1", pt) + " toneladas");

        Linha(atingiuDiaPdf
            ? "PREMIO DIARIO ATINGIDO!"
            : "PREMIO DIARIO NAO ATINGIDO");

        Linha(atingiuDiaPdf
            ? "Excedente: " + (toneladasDiaPdf - metaDiaPdf).ToString("N2", pt) + " toneladas."
            : "Faltam " + faltaDiaPdf.ToString("N2", pt) + " toneladas.");
    }
    else
    {
        Linha("Nao foi possivel calcular a premiacao deste dia.");
    }

}
}


y += 10;

            y += 10;

            Linha("RELATORIO DE ABASTECIMENTOS");

            foreach (var x in ab)
            {
                Linha($"{x.Data:dd/MM/yyyy HH:mm} | {x.Veiculo} | {x.Motorista}");
        Linha("KM: " + x.KmAnterior.ToString("N1", pt) +
      " a " + x.KmAtual.ToString("N1", pt) + " | " +
      x.Litros.ToString("N1", pt) + " L | " +
x.Consumo.ToString("N2", pt) + " km/l");
            

                if (x.TemArla)
                    Linha("Arla: " + x.LitrosArla.ToString("N1", pt) + " L");

                y += 8;
            }

            if (tipo != "Abastecimentos")
{
    Linha("RELATORIO DE CARGAS / VIAGENS");

    foreach (var x in cg)
    {
  Linha($"Saída: {x.Data:dd/MM/yyyy} {x.HoraInicial:hh\\:mm} | Chegada: {(x.DataHoraChegada ?? x.Data.Date.Add(x.HoraFinal)):dd/MM/yyyy HH:mm} | Frente {x.Frente} | {x.Veiculo} | {x.Motorista}");
        Linha("Carreta: " + x.AnoCarreta +
              " | Bruto: " + x.PesoBruto.ToString("N1", pt) +
              " kg | Tara: " + x.Tara.ToString("N1", pt) + " kg");
        Linha("Liquido: " + x.PesoLiquido.ToString("N1", pt) +
              " kg | Combustivel: " + x.LitrosConsumidos.ToString("N1", pt) + " L");

        if (ViagemComKmValido(x))
            Linha($"KM inicial: {x.KmInicial.ToString("0.0", pt)} | KM final: {x.KmFinal.ToString("0.0", pt)} | Rodados: {ObterKmRodados(x).ToString("0.0", pt)} | Raio: {(ObterKmRodados(x)/2.0).ToString("N2", pt)} km");

        Linha(VelocidadeMediaViagem(x) > 0
            ? "Velocidade média da viagem: " +
              VelocidadeMediaViagem(x).ToString("N2", pt) + " km/h"
            : "Velocidade média: não calculada (verifique KM e horários)");

        Linha($"Consumo: {x.ConsumoKmL.ToString("N2", pt)} km/l | VE: {x.ValorEnergetico.ToString("N2", pt)} | Meta: {x.MetaEnergetica.ToString("N2", pt)}");
        y += 8;
    }
}

            if (pagina != null)
                doc.FinishPage(pagina);

            using (var fs = File.Create(caminho))
                doc.WriteTo(fs);

            await Share.Default.RequestAsync(
                new ShareFileRequest
                {
                    Title = "Relatório FuelControl",
                    File = new ShareFile(caminho)
                });
#else
            await DisplayAlert("Indisponível",
                "A geração de PDF está configurada para Android.", "OK");
#endif
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erro no PDF", ex.Message, "OK");
        }
    }
}

public class DadosVeiculo
{
    public double Capacidade { get; set; }
    public double LitrosAtuais { get; set; }
    public double AlertaPercentual { get; set; } = 20;
    public double MetaKmL { get; set; }
}
public class DadosArla
{
    public double Capacidade { get; set; }
    public double LitrosEstimados { get; set; }
    public double KmUltimoAbastecimento { get; set; }
    public double MediaKmL { get; set; }
    public bool AbastecerNoProximo { get; set; }
}



public class Abastecimento
{
    // Identificação e controle de sincronização
    public string Id { get; set; } = System.Guid.NewGuid().ToString("N");
    public string Turno { get; set; } = "";
    public DateTime CriadoEmUtc { get; set; } = DateTime.UtcNow;
    public string CriadoPorDispositivo { get; set; } = "";
    public DateTime EditadoEmUtc { get; set; } = DateTime.UtcNow;
    public string EditadoPorDispositivo { get; set; } = "";

    // Dados do abastecimento
    public DateTime Data { get; set; }
    public string Veiculo { get; set; } = "";
    public string Motorista { get; set; } = "";
    public string TipoVeiculo { get; set; } = "Carro";
    public string Combustivel { get; set; } = "";
    public double KmAnterior { get; set; }
    public double KmAtual { get; set; }
    public double Litros { get; set; }
    public double PrecoLitro { get; set; }
    public double Valor { get; set; }
    public double Consumo { get; set; }
    public double CustoKm { get; set; }
    public bool TemArla { get; set; }
    public double LitrosArla { get; set; }
    public double ConsumoArlaKmL { get; set; }
    public double ConsumoArla100 { get; set; }
    public double MetaKmL { get; set; }
    public bool TanqueAdicionado { get; set; }
    public double PesoBruto { get; set; }
    public int AnoCarreta { get; set; }
    public double Tara { get; set; }
    public double PesoLiquido { get; set; }
    public double ValorEnergetico { get; set; }
    public double MetaEnergetica { get; set; }
}




public class Viagem
{
    public string Id { get; set; } = "";
    public string Turno { get; set; } = "";
    public string TurnoResponsavel { get; set; } = "";
    public DateTime CriadoEmUtc { get; set; } = DateTime.UtcNow;
    public string CriadoPorDispositivo { get; set; } = "";
    public DateTime EditadoEmUtc { get; set; } = DateTime.UtcNow;
    public string EditadoPorDispositivo { get; set; } = "";
        
    public DateTime Data { get; set; }
public TimeSpan HoraInicial { get; set; }
public TimeSpan HoraFinal { get; set; }
public DateTime? DataHoraChegada { get; set; }
    public string Frente { get; set; } = "";
    public string Veiculo { get; set; } = "";
    public string Motorista { get; set; } = "";
    public string MotoristaAtual { get; set; } = "";

public List<TransferenciaViagem> Transferencias { get; set; } = new();
    public List<TransferenciaTurno> TransferenciasTurno { get; set; } = new();
    public int AnoCarreta { get; set; }
    public double PesoBruto { get; set; }
    public double Tara { get; set; }
    public double PesoLiquido { get; set; }
    public double KmInicial { get; set; }
    public double KmFinal { get; set; }
    public double LitrosConsumidos { get; set; }
    public double ConsumoKmL { get; set; }
    public double ValorEnergetico { get; set; }
    public double MetaEnergetica { get; set; }
    public bool TanqueDescontado { get; set; }
    public bool EmAndamento { get; set; } = false;
    public bool Excluida { get; set; } = false;
}
public class TransferenciaViagem
{
    public string MotoristaAnterior { get; set; } = "";
    public string MotoristaNovo { get; set; } = "";
    public DateTime DataHora { get; set; }
    public double Km { get; set; }
}
public class TransferenciaTurno
{
    public string TurnoAnterior { get; set; } = "";
    public string TurnoNovo { get; set; } = "";
    public DateTime DataHora { get; set; }
    public string Responsavel { get; set; } = "";
}
