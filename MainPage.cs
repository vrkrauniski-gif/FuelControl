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
    readonly CultureInfo pt = CultureInfo.GetCultureInfo("pt-BR");

    List<Abastecimento> historico = new();
    Dictionary<string, DadosVeiculo> veiculos = new(StringComparer.OrdinalIgnoreCase);

    Entry veiculo = new(), motorista = new(), kmAnterior = new(), kmAtual = new();
    Entry litros = new(), precoLitro = new(), litrosArla = new();
    Entry metaKmL = new(), capacidadeTanque = new(), combustivelAtual = new(), limiteAlerta = new();
    Entry pesoBruto = new(), anoCarreta = new(), metaEnergetica = new();
    Entry filtroVeiculo = new(), filtroMotorista = new();

    DatePicker dataInicial = new() { Date = DateTime.Today.AddMonths(-1) };
    DatePicker dataFinal = new() { Date = DateTime.Today };

    Picker tipoVeiculo = new(), tipoCombustivel = new();
    CheckBox usarArla = new();

    VerticalStackLayout painelCarro = new(), painelArla = new(), lista = new();

    Label lblAbastecimentos = new(), lblGasto = new(), lblKm = new(), lblMedia = new();
    Label lblResultado = new(), lblCusto = new(), lblTotalCombustivel = new(), lblResultadoArla = new();
    Label lblTanque = new(), lblCarga = new(), lblEnergetico = new(), lblMeta = new();

    public MainPage()
    {
        Title = "FuelControl";
        BackgroundColor = Color.FromArgb("#F1F5F9");

        CarregarDados();

        Content = CriarTela();

        AtualizarDashboard();
        AtualizarHistorico();
        AtualizarPainelVeiculo();
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
            HeightRequest = 52,
            Margin = new Thickness(0, 3)
        };
    }

    Picker CriarPicker()
    {
        return new Picker
        {
            TextColor = Colors.Black,
            TitleColor = Colors.Gray,
            BackgroundColor = Colors.White,
            HeightRequest = 52,
            Margin = new Thickness(0, 3)
        };
    }

    Label Titulo(string t)
    {
        return new Label
        {
            Text = t,
            FontSize = 17,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#123B65"),
            Margin = new Thickness(0, 12, 0, 5)
        };
    }

    Label Rotulo(string t)
    {
        return new Label
        {
            Text = t,
            TextColor = Color.FromArgb("#334155"),
            FontSize = 13
        };
    }

    Label Indicador(string t)
    {
        return new Label
        {
            Text = t,
            FontSize = 18,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#123B65")
        };
    }

    Button Botao(string t, string cor = "#123B65")
    {
        return new Button
        {
            Text = t,
            BackgroundColor = Color.FromArgb(cor),
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold,
            HeightRequest = 52,
            CornerRadius = 10
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
                Spacing = 4,
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

    bool EhCaminhao =>
        tipoVeiculo.SelectedItem?.ToString() == "Caminhão";

    string IdVeiculo => (veiculo.Text ?? "").Trim();

    bool Ler(string? s, out double n)
    {
        if (double.TryParse(
            s?.Trim(),
            NumberStyles.Number,
            pt,
            out n))
            return true;

        return double.TryParse(
            s?.Trim(),
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out n);
    }

    string ChaveId(string id)
    {
        return id.Trim().ToUpperInvariant();
    }

    void CarregarDados()
    {
        try
        {
            var j = Preferences.Default.Get(Chave, "");

            if (!string.IsNullOrWhiteSpace(j))
            {
                historico =
                    JsonSerializer.Deserialize<List<Abastecimento>>(j)
                    ?? new();
            }
        }
        catch
        {
            historico = new();
        }

        try
        {
            var j = Preferences.Default.Get(ChaveVeiculos, "");

            if (!string.IsNullOrWhiteSpace(j))
            {
                veiculos =
                    JsonSerializer.Deserialize<Dictionary<string, DadosVeiculo>>(j)
                    ?? new(StringComparer.OrdinalIgnoreCase);
            }
        }
        catch
        {
            veiculos = new(StringComparer.OrdinalIgnoreCase);
        }
    }

    void SalvarDados()
    {
        Preferences.Default.Set(
            Chave,
            JsonSerializer.Serialize(historico));

        Preferences.Default.Set(
            ChaveVeiculos,
            JsonSerializer.Serialize(veiculos));
    }

    DadosVeiculo ObterVeiculo(string id)
    {
        string k = ChaveId(id);

        if (!veiculos.TryGetValue(k, out var d))
        {
            d = new DadosVeiculo();
            veiculos[k] = d;
        }

        return d;
    }

    Grid Dashboard()
    {
        var g = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            },
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto)
            },
            ColumnSpacing = 9,
            RowSpacing = 9
        };

        g.Add(Cartao("Abastecimentos", lblAbastecimentos), 0, 0);
        g.Add(Cartao("Gasto registrado", lblGasto), 1, 0);
        g.Add(Cartao("KM registrados", lblKm), 0, 1);
        g.Add(Cartao("Consumo médio", lblMedia), 1, 1);

        return g;
    }

    void TipoAlterado(object? s, EventArgs e)
    {
        bool cam = EhCaminhao;

        tipoCombustivel.ItemsSource = cam
            ? new List<string> { "Diesel S10" }
            : new List<string> { "Etanol", "Gasolina" };

        tipoCombustivel.SelectedIndex = 0;

        painelCarro.IsVisible = !cam;
        usarArla.IsVisible = cam;
        painelArla.IsVisible = cam && usarArla.IsChecked;
        metaKmL.IsVisible = cam;

        if (!cam)
            usarArla.IsChecked = false;
    }

    void AtualizarArla(object? s, CheckedChangedEventArgs e)
    {
        painelArla.IsVisible = EhCaminhao && e.Value;
    }

    View CriarTela()
    {
        tipoVeiculo = CriarPicker();
        tipoVeiculo.Title = "Tipo de veículo";
        tipoVeiculo.ItemsSource = new List<string>
        {
            "Carro",
            "Caminhão"
        };
        tipoVeiculo.SelectedIndex = 0;
        tipoVeiculo.SelectedIndexChanged += TipoAlterado;

        tipoCombustivel = CriarPicker();
        tipoCombustivel.Title = "Combustível";
        tipoCombustivel.ItemsSource = new List<string>
        {
            "Etanol",
            "Gasolina"
        };
        tipoCombustivel.SelectedIndex = 0;

        veiculo = Campo("Placa ou identificação do veículo");
        motorista = Campo("Nome do motorista");

        kmAnterior = Campo("Quilometragem anterior", Keyboard.Numeric);
        kmAtual = Campo("Quilometragem atual", Keyboard.Numeric);

        litros = Campo("Litros abastecidos", Keyboard.Numeric);
        precoLitro = Campo("Preço por litro (R$)", Keyboard.Numeric);
        litrosArla = Campo("Litros de Arla 32", Keyboard.Numeric);

        metaKmL = Campo("Meta do caminhão em km/l", Keyboard.Numeric);

        capacidadeTanque = Campo(
            "Capacidade total do tanque (litros)",
            Keyboard.Numeric);

        combustivelAtual = Campo(
            "Combustível atual no tanque (litros)",
            Keyboard.Numeric);

        limiteAlerta = Campo(
            "Alerta do tanque (%) - padrão 20",
            Keyboard.Numeric);

        pesoBruto = Campo(
            "Peso bruto da viagem (kg)",
            Keyboard.Numeric);

        anoCarreta = Campo(
            "Ano da carreta (ex.: 2008)",
            Keyboard.Numeric);

        metaEnergetica = Campo(
            "Meta do valor energético (padrão 82)",
            Keyboard.Numeric);

        usarArla = new CheckBox
        {
            Color = Color.FromArgb("#123B65"),
            IsVisible = false
        };

        usarArla.CheckedChanged += AtualizarArla;

        lblAbastecimentos = Indicador("0");
        lblGasto = Indicador("R$ 0,00");
        lblKm = Indicador("0 km");
        lblMedia = Indicador("-- km/l");

        lblResultado = Indicador("Consumo: -- km/l");
        lblCusto = Indicador("Custo por km: --");

        lblTotalCombustivel = Indicador("Total: R$ 0,00");
        lblResultadoArla = Indicador("Arla: --");

        lblTanque = Indicador("Informe o veículo para ver o tanque.");
        lblCarga = Indicador("Peso líquido: --");
        lblEnergetico = Indicador("Valor energético: --");
        lblMeta = Indicador("Meta: --");

        litros.TextChanged += (s, e) => AtualizarTotal();
        precoLitro.TextChanged += (s, e) => AtualizarTotal();
        veiculo.TextChanged += (s, e) => AtualizarPainelVeiculo();

        painelCarro = new VerticalStackLayout
        {
            Spacing = 5,
            Children =
            {
                Rotulo("Preço por litro"),
                precoLitro,
                lblTotalCombustivel
            }
        };

        painelArla = new VerticalStackLayout
        {
            IsVisible = false,
            Spacing = 5,
            Children =
            {
                Rotulo("Quantidade de Arla 32"),
                litrosArla,
                lblResultadoArla
            }
        };

        var linhaArla = new HorizontalStackLayout
        {
            Spacing = 8,
            Children =
            {
                usarArla,
                new Label
                {
                    Text = "Abasteceu Arla 32?",
                    TextColor = Colors.Black,
                    VerticalTextAlignment = TextAlignment.Center
                }
            }
        };

        var salvar = Botao("CALCULAR E SALVAR ABASTECIMENTO");
        salvar.Clicked += CalcularSalvar;

        var configurar = Botao("SALVAR CONFIGURAÇÃO DO VEÍCULO");
        configurar.Clicked += SalvarConfiguracao;

        var ajustar = Botao(
            "AJUSTAR COMBUSTÍVEL ATUAL DO TANQUE",
            "#0F766E");

        ajustar.Clicked += AjustarTanque;

        var gerarPdf = Botao("GERAR E COMPARTILHAR RELATÓRIO PDF");
        gerarPdf.Clicked += CompartilharPDF;

        var aplicarFiltro = Botao("APLICAR FILTROS DO RELATÓRIO");
        aplicarFiltro.Clicked += (s, e) => AtualizarHistorico(true);

        var limpar = Botao("LIMPAR HISTÓRICO", "#B91C1C");

        limpar.Clicked += async (s, e) =>
        {
            if (await DisplayAlert(
                "Confirmar",
                "Apagar todos os abastecimentos?",
                "Sim",
                "Não"))
            {
                historico.Clear();
                SalvarDados();
                AtualizarDashboard();
                AtualizarHistorico();
            }
        };

        lista = new VerticalStackLayout
        {
            Spacing = 8
        };

        return new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 18,
                Spacing = 9,
                Children =
                {
                    new Label
                    {
                        Text = "FUELCONTROL",
                        FontSize = 28,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = Color.FromArgb("#123B65"),
                        HorizontalOptions = LayoutOptions.Center
                    },

                    new Label
                    {
                        Text = "GESTÃO DE COMBUSTÍVEL, CARGA E DESEMPENHO",
                        FontSize = 11,
                        TextColor = Colors.Gray,
                        HorizontalOptions = LayoutOptions.Center
                    },

                    Titulo("DASHBOARD"),
                    Dashboard(),

                    Titulo("NOVO ABASTECIMENTO"),

                    Rotulo("Tipo de veículo"),
                    tipoVeiculo,

                    Rotulo("Combustível"),
                    tipoCombustivel,

                    linhaArla,

                    Rotulo("Veículo / placa"),
                    veiculo,

                    Rotulo("Motorista"),
                    motorista,

                    Rotulo("KM anterior"),
                    kmAnterior,

                    Rotulo("KM atual"),
                    kmAtual,

                    Rotulo("Litros abastecidos"),
                    litros,

                    painelCarro,
                    painelArla,

                    Rotulo("Meta de consumo do caminhão (km/l)"),
                    metaKmL,

                    salvar,
                    lblResultado,
                    lblCusto,

                    new BoxView
                    {
                        HeightRequest = 1,
                        Color = Colors.LightGray
                    },

                    Titulo("TANQUE DO VEÍCULO"),

                    Rotulo("Capacidade total (litros)"),
                    capacidadeTanque,

                    Rotulo("Combustível atual (litros)"),
                    combustivelAtual,

                    Rotulo("Alerta em percentual (padrão 20%)"),
                    limiteAlerta,

                    configurar,
                    ajustar,
                    lblTanque,

                    new BoxView
                    {
                        HeightRequest = 1,
                        Color = Colors.LightGray
                    },

                    Titulo("DADOS DA CARGA / VIAGEM"),

                    Rotulo("Peso bruto do conjunto (kg)"),
                    pesoBruto,

                    Rotulo("Ano da carreta"),
                    anoCarreta,

                    Rotulo("Meta do valor energético (padrão 82)"),
                    metaEnergetica,

                    lblCarga,
                    lblEnergetico,
                    lblMeta,

                    new BoxView
                    {
                        HeightRequest = 1,
                        Color = Colors.LightGray
                    },

                    Titulo("FILTROS DO RELATÓRIO"),

                    Rotulo("Veículo (vazio = todos)"),
                    filtroVeiculo,

                    Rotulo("Motorista (vazio = todos)"),
                    filtroMotorista,

                    Rotulo("Data inicial"),
                    dataInicial,

                    Rotulo("Data final"),
                    dataFinal,

                    aplicarFiltro,

                    Titulo("HISTÓRICO"),
                    lista,

                    gerarPdf,
                    limpar,

                    new Label
                    {
                        Text = "FuelControl - Dados salvos neste aparelho",
                        FontSize = 11,
                        TextColor = Colors.Gray,
                        HorizontalOptions = LayoutOptions.Center,
                        Margin = new Thickness(0, 12)
                    }
                }
            }
        };
    }

    void AtualizarTotal()
    {
        if (Ler(litros.Text, out var l) &&
            Ler(precoLitro.Text, out var p) &&
            l > 0 && p > 0)
        {
            lblTotalCombustivel.Text =
                "Total: " + (l * p).ToString("C", pt);
        }
        else
        {
            lblTotalCombustivel.Text = "Total: R$ 0,00";
        }
    }

    double MediaKmL(string id)
    {
        var itens = historico
            .Where(x =>
                x.Veiculo.Equals(
                    id,
                    StringComparison.OrdinalIgnoreCase)
                && x.Litros > 0
                && x.Consumo > 0)
            .ToList();

        return itens.Count == 0
            ? 0
            : itens.Average(x => x.Consumo);
    }

    void SalvarConfiguracao(object? s, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(IdVeiculo))
        {
            DisplayAlert(
                "Atenção",
                "Informe a identificação do veículo.",
                "OK");
            return;
        }

        var d = ObterVeiculo(IdVeiculo);

        if (Ler(capacidadeTanque.Text, out var cap) && cap > 0)
            d.Capacidade = cap;

        if (Ler(combustivelAtual.Text, out var atual) && atual >= 0)
        {
            d.LitrosAtuais = Math.Min(
                atual,
                d.Capacidade > 0 ? d.Capacidade : atual);
        }

        if (Ler(limiteAlerta.Text, out var alerta)
            && alerta > 0 && alerta < 100)
        {
            d.AlertaPercentual = alerta;
        }
        else if (d.AlertaPercentual <= 0)
        {
            d.AlertaPercentual = 20;
        }

        if (EhCaminhao &&
            Ler(metaKmL.Text, out var meta) &&
            meta > 0)
        {
            d.MetaKmL = meta;
        }

        if (Ler(metaEnergetica.Text, out var me) && me > 0)
            d.MetaEnergetica = me;

        if (d.UltimoKm <= 0 && Ler(kmAtual.Text, out var km))
            d.UltimoKm = km;

        SalvarDados();
        AtualizarPainelVeiculo();

        DisplayAlert(
            "Salvo",
            "Configuração do veículo salva.",
            "OK");
    }

    void AjustarTanque(object? s, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(IdVeiculo) ||
            !Ler(combustivelAtual.Text, out var l) ||
            l < 0)
        {
            DisplayAlert(
                "Atenção",
                "Informe o veículo e os litros atuais do tanque.",
                "OK");
            return;
        }

        var d = ObterVeiculo(IdVeiculo);

        if (d.Capacidade > 0 && l > d.Capacidade)
        {
            DisplayAlert(
                "Atenção",
                "Os litros atuais não podem superar a capacidade do tanque.",
                "OK");
            return;
        }

        d.LitrosAtuais = l;

        if (Ler(kmAtual.Text, out var km) && km > 0)
            d.UltimoKm = km;

        SalvarDados();
        AtualizarPainelVeiculo();

        DisplayAlert(
            "Tanque ajustado",
            "Nível atual corrigido manualmente.",
            "OK");
    }

    double TaraPorAno(int ano)
    {
        if (ano >= 1999 && ano <= 2005)
            return 34000;

        if (ano >= 2006 && ano <= 2007)
            return 36000;

        if ((ano >= 2008 && ano <= 2009) ||
            (ano >= 2012 && ano <= 2014))
            return 38000;

        if (ano >= 2016 && ano <= 2026)
            return 37000;

        return 0;
    }

    async void CalcularSalvar(object? s, EventArgs e)
    {
        bool cam = EhCaminhao;

        if (string.IsNullOrWhiteSpace(IdVeiculo) ||
            !Ler(kmAnterior.Text, out var ant) ||
            !Ler(kmAtual.Text, out var km) ||
            !Ler(litros.Text, out var qtd) ||
            ant < 0 || km <= ant || qtd <= 0)
        {
            await DisplayAlert(
                "Dados inválidos",
                "Preencha veículo, quilometragens e litros corretamente.",
                "OK");
            return;
        }

        double preco = 0;

        if (!cam &&
            (!Ler(precoLitro.Text, out preco) || preco <= 0))
        {
            await DisplayAlert(
                "Atenção",
                "Informe o preço por litro.",
                "OK");
            return;
        }

        bool temArla = cam && usarArla.IsChecked;
        double qArla = 0;

        if (temArla &&
            (!Ler(litrosArla.Text, out qArla) || qArla <= 0))
        {
            await DisplayAlert(
                "Atenção",
                "Informe os litros de Arla 32.",
                "OK");
            return;
        }

        string combustivel = cam
            ? "Diesel S10"
            : tipoCombustivel.SelectedItem?.ToString() ?? "";

        double distancia = km - ant;
        double consumo = distancia / qtd;
        double total = cam ? 0 : qtd * preco;
        double custoKm = cam ? 0 : total / distancia;

        var d = ObterVeiculo(IdVeiculo);

        // Estimativa do tanque.
        if (d.Capacidade > 0)
        {
            double kmBase = d.UltimoKm > 0
                ? d.UltimoKm
                : ant;

            double kmRodados = Math.Max(0, km - kmBase);

            double media = MediaKmL(IdVeiculo);

            if (media <= 0)
                media = consumo;

            if (d.UltimoKm > 0)
            {
                d.LitrosAtuais = Math.Max(
                    0,
                    d.LitrosAtuais - kmRodados / media);
            }

            d.LitrosAtuais = Math.Min(
                d.Capacidade,
                d.LitrosAtuais + qtd);
        }

        d.UltimoKm = km;

        if (cam && Ler(metaKmL.Text, out var meta) && meta > 0)
            d.MetaKmL = meta;

        if (Ler(metaEnergetica.Text, out var metaVe) && metaVe > 0)
            d.MetaEnergetica = metaVe;

        double tara = 0;
        double liquido = 0;
        double ve = 0;
        int ano = 0;
        double bruto = 0;

        if (cam &&
            Ler(pesoBruto.Text, out bruto) &&
            bruto > 0 &&
            int.TryParse(anoCarreta.Text, out ano))
        {
            tara = TaraPorAno(ano);

            if (tara > 0 && bruto > tara)
            {
                liquido = bruto - tara;

                // Valor energético: km/l x peso líquido em kg / 1.000.
                ve = consumo * liquido / 1000.0;
            }
        }

        var reg = new Abastecimento
        {
            Data = DateTime.Now,
            Veiculo = IdVeiculo,
            Motorista = motorista.Text?.Trim() ?? "",
            TipoVeiculo = cam ? "Caminhão" : "Carro",
            Combustivel = combustivel,

            KmAnterior = ant,
            KmAtual = km,

            Litros = qtd,
            PrecoLitro = preco,
            Valor = total,

            Consumo = consumo,
            CustoKm = custoKm,

            TemArla = temArla,
            LitrosArla = qArla,

            ConsumoArlaKmL = temArla
                ? distancia / qArla
                : 0,

            ConsumoArla100 = temArla
                ? qArla * 100 / distancia
                : 0,

            MetaKmL = cam ? d.MetaKmL : 0,

            PesoBruto = cam ? bruto : 0,
            AnoCarreta = ano,
            Tara = tara,
            PesoLiquido = liquido,

            ValorEnergetico = ve,
            MetaEnergetica = d.MetaEnergetica
        };

        historico.Insert(0, reg);

        SalvarDados();
        AtualizarDashboard();
        AtualizarHistorico();
        AtualizarPainelVeiculo();

        lblResultado.Text =
            $"Consumo: {consumo:N2} km/l";

        lblCusto.Text = cam
            ? $"Meta caminhão: {d.MetaKmL:N2} km/l | " +
              (d.MetaKmL > 0
                  ? (consumo >= d.MetaKmL
                      ? "META ATINGIDA"
                      : "ABAIXO DA META")
                  : "META NÃO DEFINIDA")
            : $"Custo: {custoKm:C2}/km | Meta R$ 0,50: " +
              (custoKm <= 0.50
                  ? "DENTRO DA META"
                  : "ACIMA DA META");

        lblResultadoArla.Text = temArla
            ? $"Arla: {reg.ConsumoArlaKmL:N2} km/l | " +
              $"{reg.ConsumoArla100:N2} L/100 km"
            : "Arla: não informado";

        if (cam && liquido > 0)
        {
            lblCarga.Text =
                $"Bruto: {reg.PesoBruto:N0} kg | " +
                $"Tara: {tara:N0} kg | " +
                $"Líquido: {liquido:N0} kg";

            lblEnergetico.Text =
                $"Valor energético: {ve:N2}";

            lblMeta.Text =
                $"Meta: {d.MetaEnergetica:N2} | " +
                (d.MetaEnergetica > 0
                    ? (ve >= d.MetaEnergetica
                        ? "META ATINGIDA"
                        : "ABAIXO DA META")
                    : "META NÃO DEFINIDA");
        }
        else if (cam && !string.IsNullOrWhiteSpace(pesoBruto.Text))
        {
            await DisplayAlert(
                "Tara não cadastrada",
                "Não existe tara automática para esse ano de carreta. Verifique o ano ou informe a tara correta.",
                "OK");
        }

        await DisplayAlert(
            "Salvo",
            "Abastecimento e dados da viagem registrados.",
            "OK");

        kmAnterior.Text = kmAtual.Text;
        kmAtual.Text = "";
        litros.Text = "";
        precoLitro.Text = "";
        litrosArla.Text = "";

        AtualizarTotal();
    }

    void AtualizarPainelVeiculo()
    {
        if (string.IsNullOrWhiteSpace(IdVeiculo))
        {
            lblTanque.Text = "Informe o veículo para ver o tanque.";
            return;
        }

        var d = ObterVeiculo(IdVeiculo);

        capacidadeTanque.Text = d.Capacidade > 0
            ? d.Capacidade.ToString("N1", pt)
            : capacidadeTanque.Text;

        limiteAlerta.Text = d.AlertaPercentual > 0
            ? d.AlertaPercentual.ToString("N0", pt)
            : "20";

        if (d.MetaKmL > 0)
            metaKmL.Text = d.MetaKmL.ToString("N2", pt);

        if (d.MetaEnergetica > 0)
            metaEnergetica.Text = d.MetaEnergetica.ToString("N2", pt);

        if (d.Capacidade <= 0)
        {
            lblTanque.Text = "Cadastre a capacidade e o nível atual do tanque.";
            return;
        }

        double pct = d.LitrosAtuais / d.Capacidade * 100;
        double media = MediaKmL(IdVeiculo);
        double autonomia = media > 0
            ? d.LitrosAtuais * media
            : 0;

        double alerta = d.AlertaPercentual > 0
            ? d.AlertaPercentual
            : 20;

        lblTanque.Text =
            $"Tanque: {d.LitrosAtuais:N1}/{d.Capacidade:N1} L " +
            $"({pct:N1}%) | Autonomia: " +
            (media > 0
                ? autonomia.ToString("N0", pt) + " km"
                : "--") +
            " | " +
            (pct <= alerta
                ? "ATENÇÃO: ABASTECER"
                : "Nível acima do alerta");
    }

    void AtualizarDashboard()
    {
        lblAbastecimentos.Text = historico.Count.ToString();

        lblGasto.Text =
            historico.Sum(x => x.Valor).ToString("C", pt);

        lblKm.Text =
            historico.Sum(x => Math.Max(
                0,
                x.KmAtual - x.KmAnterior))
            .ToString("N0", pt) + " km";

        var carros = historico
            .Where(x => x.TipoVeiculo == "Carro" && x.Litros > 0)
            .ToList();

        double km = carros.Sum(x => x.KmAtual - x.KmAnterior);
        double l = carros.Sum(x => x.Litros);

        lblMedia.Text = l > 0
            ? (km / l).ToString("N2", pt) + " km/l"
            : "-- km/l";
    }

    List<Abastecimento> Filtrar()
    {
        IEnumerable<Abastecimento> q = historico;

        if (!string.IsNullOrWhiteSpace(filtroVeiculo.Text))
        {
            q = q.Where(x =>
                x.Veiculo.Contains(
                    filtroVeiculo.Text.Trim(),
                    StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(filtroMotorista.Text))
        {
            q = q.Where(x =>
                x.Motorista.Contains(
                    filtroMotorista.Text.Trim(),
                    StringComparison.OrdinalIgnoreCase));
        }

        DateTime ini = dataInicial.Date;
        DateTime fim = dataFinal.Date.AddDays(1).AddTicks(-1);

        q = q.Where(x => x.Data >= ini && x.Data <= fim);

        return q.OrderByDescending(x => x.Data).ToList();
    }

    void AtualizarHistorico(bool aplicarFiltro = false)
    {
        if (lista == null)
            return;

        lista.Children.Clear();

        var itens = aplicarFiltro
            ? Filtrar()
            : historico;

        if (itens.Count == 0)
        {
            lista.Children.Add(new Label
            {
                Text = "Nenhum registro encontrado.",
                TextColor = Colors.Gray
            });

            return;
        }

        foreach (var x in itens)
        {
            var v = new VerticalStackLayout
            {
                Spacing = 4
            };

            v.Children.Add(new Label
            {
                Text = $"{x.Veiculo} - {x.Data:dd/MM/yyyy HH:mm}",
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#123B65")
            });

            v.Children.Add(new Label
            {
                Text = $"{x.TipoVeiculo} | {x.Combustivel} | Motorista: {x.Motorista}",
                TextColor = Colors.Black
            });

            v.Children.Add(new Label
            {
                Text = $"KM: {x.KmAnterior:N0} → {x.KmAtual:N0} | {x.Litros:N2} L | {x.Consumo:N2} km/l",
                TextColor = Colors.Black
            });

            if (x.TipoVeiculo == "Carro")
            {
                v.Children.Add(new Label
                {
                    Text =
                        $"Custo: {x.CustoKm:C2}/km | " +
                        (x.CustoKm <= 0.50
                            ? "Dentro da meta de R$ 0,50"
                            : "Acima da meta"),
                    TextColor = x.CustoKm <= 0.50
                        ? Colors.Green
                        : Colors.Red,
                    FontAttributes = FontAttributes.Bold
                });
            }
            else
            {
                v.Children.Add(new Label
                {
                    Text =
                        $"Meta km/l: {x.MetaKmL:N2} | " +
                        (x.MetaKmL > 0 && x.Consumo >= x.MetaKmL
                            ? "Meta atingida"
                            : "Abaixo/sem meta"),
                    TextColor = Colors.Black
                });

                if (x.PesoLiquido > 0)
                {
                    v.Children.Add(new Label
                    {
                        Text =
                            $"Carga: bruto {x.PesoBruto:N0} - " +
                            $"tara {x.Tara:N0} = " +
                            $"líquido {x.PesoLiquido:N0} kg | " +
                            $"VE: {x.ValorEnergetico:N2} / " +
                            $"meta {x.MetaEnergetica:N2}",
                        TextColor = Colors.Black
                    });
                }
            }

            if (x.TemArla)
            {
                v.Children.Add(new Label
                {
                    Text =
                        $"Arla: {x.LitrosArla:N2} L | " +
                        $"{x.ConsumoArlaKmL:N2} km/l | " +
                        $"{x.ConsumoArla100:N2} L/100 km",
                    TextColor = Colors.Black
                });
            }

            lista.Children.Add(new Border
            {
                BackgroundColor = Colors.White,
                Stroke = Color.FromArgb("#DCE4ED"),
                Padding = 11,
                Content = v
            });
        }
    }

    async void CompartilharPDF(object? s, EventArgs e)
    {
        var itens = Filtrar();

        if (itens.Count == 0)
        {
            await DisplayAlert(
                "Atenção",
                "Não há registros no filtro selecionado.",
                "OK");
            return;
        }

#if ANDROID
        try
        {
            string caminho = Path.Combine(
                FileSystem.CacheDirectory,
                "Relatorio_FuelControl.pdf");

            using var doc = new AndroidPdfDocument();

            int paginaNumero = 1;

            AndroidPdfDocument.Page? pagina = null;
            Android.Graphics.Canvas? canvas = null;

            int y = 0;

            AndroidPaint titulo = new()
            {
                Color = AndroidColor.Rgb(18, 59, 101),
                TextSize = 16,
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

                var info =
                    new AndroidPdfDocument.PageInfo.Builder(
                        595,
                        842,
                        paginaNumero++)
                    .Create();

                pagina = doc.StartPage(info);
                canvas = pagina.Canvas;
                y = 40;

                canvas.DrawText(
                    "FUELCONTROL - RELATORIO",
                    30,
                    y,
                    titulo);

                y += 22;

                canvas.DrawText(
                    $"Gerado em {DateTime.Now:dd/MM/yyyy HH:mm}",
                    30,
                    y,
                    texto);

                y += 22;
            }

            NovaPagina();

            foreach (var x in itens)
            {
                if (y > 770)
                    NovaPagina();

                canvas!.DrawText(
                    $"{x.Data:dd/MM/yyyy} | {x.Veiculo} | {x.Motorista}",
                    30,
                    y,
                    titulo);

                y += 15;

                canvas.DrawText(
                    $"{x.TipoVeiculo} - {x.Combustivel} | " +
                    $"KM {x.KmAnterior:N0} a {x.KmAtual:N0} | " +
                    $"{x.Litros:N2} L | {x.Consumo:N2} km/l",
                    30,
                    y,
                    texto);

                y += 14;

                if (x.TipoVeiculo == "Carro")
                {
                    canvas.DrawText(
                        $"Custo: {x.CustoKm:C2}/km (meta R$ 0,50)",
                        30,
                        y,
                        texto);

                    y += 14;
                }
                else
                {
                    canvas.DrawText(
                        $"Meta km/l: {x.MetaKmL:N2} | " +
                        $"Peso bruto {x.PesoBruto:N0} | " +
                        $"Tara {x.Tara:N0} | " +
                        $"Liquido {x.PesoLiquido:N0} kg",
                        30,
                        y,
                        texto);

                    y += 14;

                    canvas.DrawText(
                        $"Valor energetico: {x.ValorEnergetico:N2} | " +
                        $"Meta: {x.MetaEnergetica:N2}",
                        30,
                        y,
                        texto);

                    y += 14;
                }

                if (x.TemArla)
                {
                    canvas.DrawText(
                        $"Arla: {x.LitrosArla:N2} L | {x.ConsumoArlaKmL:N2} km/l",
                        30,
                        y,
                        texto);

                    y += 14;
                }

                y += 10;
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
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Erro no PDF",
                ex.Message,
                "OK");
        }
#else
        await DisplayAlert(
            "Indisponível",
            "PDF configurado para Android.",
            "OK");
#endif
    }
}

public class DadosVeiculo
{
    public double Capacidade { get; set; }
    public double LitrosAtuais { get; set; }
    public double UltimoKm { get; set; }

    public double AlertaPercentual { get; set; } = 20;

    public double MetaKmL { get; set; }

    public double MetaEnergetica { get; set; } = 82;
}

public class Abastecimento
{
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

    public double PesoBruto { get; set; }
    public int AnoCarreta { get; set; }
    public double Tara { get; set; }
    public double PesoLiquido { get; set; }

    public double ValorEnergetico { get; set; }
    public double MetaEnergetica { get; set; }
}
