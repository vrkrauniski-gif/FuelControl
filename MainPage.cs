
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

    readonly CultureInfo pt = CultureInfo.GetCultureInfo("pt-BR");

    List<Abastecimento> abastecimentos = new();
    List<Viagem> viagens = new();
    Dictionary<string, DadosVeiculo> veiculos = new(StringComparer.OrdinalIgnoreCase);

    Entry veiculo = new(), motorista = new();
    Entry kmAnterior = new(), kmAtual = new();
    Entry litros = new(), precoLitro = new(), litrosArla = new();
    Entry capacidadeTanque = new(), combustivelAtual = new();
    Entry limiteAlerta = new(), metaKmL = new();

    Entry cargaVeiculo = new(), cargaMotorista = new();
    Entry anoCarreta = new(), pesoBruto = new();
    Entry litrosViagem = new(), consumoViagem = new();
    Entry metaEnergetica = new();

    Entry filtroVeiculo = new(), filtroMotorista = new();

    Picker tipoVeiculo = new(), tipoCombustivel = new();
    Picker periodoRelatorio = new(), tipoRelatorio = new();

    DatePicker dataViagem = new() { Date = DateTime.Today };
    DatePicker dataFiltro = new() { Date = DateTime.Today };

    CheckBox usarArla = new();

    VerticalStackLayout painelCarro = new();
    VerticalStackLayout painelArla = new();
    VerticalStackLayout lista = new();

    Label lblResultado = new();
    Label lblCusto = new();
    Label lblTotal = new();
    Label lblTanque = new();
    Label lblPeso = new();
    Label lblEnergetico = new();
    Label lblComparacao = new();

    bool salvandoViagem;

    public MainPage()
    {
        Title = "FuelControl";
        BackgroundColor = Color.FromArgb("#F1F5F9");
        CarregarDados();
        MostrarMenu("Abastecimentos");
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
    }

    void SalvarDados()
    {
        Preferences.Default.Set(Chave, JsonSerializer.Serialize(abastecimentos));
        Preferences.Default.Set(ChaveVeiculos, JsonSerializer.Serialize(veiculos));
        Preferences.Default.Set(ChaveCargas, JsonSerializer.Serialize(viagens));
    }

    void MostrarMenu(string menu)
    {
        try
        {
            var botoes = new HorizontalStackLayout { Spacing = 4 };

            string[] nomes =
            {
                "Abastecimentos", "Cargas", "Indicadores", "Relatórios"
            };

            foreach (string nome in nomes)
            {
                var botao = Botao(nome);
                botao.FontSize = 10;
                botao.Padding = new Thickness(4, 0);
                botao.Clicked += (s, e) => MostrarMenu(nome);
                botoes.Children.Add(botao);
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
        tipoVeiculo = NovoPicker("Tipo de veículo", "Carro", "Caminhão");
        tipoCombustivel = NovoPicker("Combustível", "Etanol", "Gasolina");

        veiculo = Campo("Placa ou identificação do veículo");
        motorista = Campo("Nome do motorista");

        kmAnterior = Campo("Quilometragem anterior", Keyboard.Numeric);
        kmAtual = Campo("Quilometragem atual", Keyboard.Numeric);
        litros = Campo("Litros abastecidos", Keyboard.Numeric);
        precoLitro = Campo("Preço por litro (R$)", Keyboard.Numeric);
        litrosArla = Campo("Litros de Arla 32", Keyboard.Numeric);
        metaKmL = Campo("Meta de consumo do caminhão (km/l)");

        capacidadeTanque = Campo("Capacidade total do tanque (litros)", Keyboard.Numeric);
        combustivelAtual = Campo("Litros atuais no tanque", Keyboard.Numeric);
        limiteAlerta = Campo("Alerta do tanque em % (padrão 20)", Keyboard.Numeric);

        lblTotal = Indicador("Total: R$ 0,00");
        lblResultado = Indicador("Consumo: -- km/l");
        lblCusto = Indicador("Custo por km: --");
        lblTanque = Indicador("Digite a placa para consultar o tanque.");

        // Atualiza o painel automaticamente ao digitar a identificação.
        veiculo.TextChanged += (s, e) =>
            AtualizarTanqueTexto(e.NewTextValue ?? "");

        painelCarro = new VerticalStackLayout
        {
            Children = { Rotulo("Preço por litro"), precoLitro, lblTotal }
        };

        painelArla = new VerticalStackLayout
        {
            IsVisible = false,
            Children = { Rotulo("Litros de Arla 32"), litrosArla }
        };

        usarArla = new CheckBox { IsVisible = false };

        usarArla.CheckedChanged += (s, e) =>
        {
            painelArla.IsVisible =
                tipoVeiculo.SelectedItem?.ToString() == "Caminhão" && e.Value;
        };

        tipoVeiculo.SelectedIndexChanged += (s, e) =>
        {
            bool caminhao =
                tipoVeiculo.SelectedItem?.ToString() == "Caminhão";

            tipoCombustivel.ItemsSource = caminhao
                ? new List<string> { "Diesel S10" }
                : new List<string> { "Etanol", "Gasolina" };

            tipoCombustivel.SelectedIndex = 0;
            painelCarro.IsVisible = !caminhao;
            usarArla.IsVisible = caminhao;
            painelArla.IsVisible = caminhao && usarArla.IsChecked;
            metaKmL.IsVisible = caminhao;

            if (!caminhao)
                usarArla.IsChecked = false;
        };

        litros.TextChanged += (s, e) => AtualizarTotal();
        precoLitro.TextChanged += (s, e) => AtualizarTotal();

        var salvar = Botao("CALCULAR E SALVAR ABASTECIMENTO");
        salvar.Clicked += SalvarAbastecimento;

        var configurar = Botao("SALVAR CONFIGURAÇÃO DO TANQUE");
        configurar.Clicked += SalvarConfiguracaoTanque;

        var ajustar = Botao("AJUSTAR NÍVEL ATUAL DO TANQUE", "#0F766E");
        ajustar.Clicked += AjustarTanque;

        return new VerticalStackLayout
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
                Titulo("CONSULTA DO TANQUE"),
                lblTanque,
                Rotulo("Motorista"), motorista,
                Rotulo("KM anterior"), kmAnterior,
                Rotulo("KM atual"), kmAtual,
                Rotulo("Litros abastecidos"), litros,
                painelCarro, painelArla,
                Rotulo("Meta de consumo do caminhão"), metaKmL,
                salvar, lblResultado, lblCusto,
                Titulo("CONFIGURAÇÃO DO TANQUE"),
                Rotulo("Capacidade total"), capacidadeTanque,
                Rotulo("Combustível atual"), combustivelAtual,
                Rotulo("Alerta em percentual"), limiteAlerta,
                configurar, ajustar
            }
        };
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

            if (dados.Capacidade > 0 &&
                dados.LitrosAtuais + quantidade > dados.Capacidade)
            {
                await DisplayAlert("Atenção",
                    "O abastecimento ultrapassaria a capacidade do tanque.", "OK");
                return;
            }

            double nivelAntigo = dados.LitrosAtuais;
            double metaAntiga = dados.MetaKmL;

            if (dados.Capacidade > 0)
                dados.LitrosAtuais += quantidade;

            if (caminhao && Ler(metaKmL.Text, out var meta) && meta > 0)
                dados.MetaKmL = meta;

            var registro = new Abastecimento
            {
                Data = DateTime.Now,
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
                MetaKmL = caminhao ? dados.MetaKmL : 0
            };

            abastecimentos.Insert(0, registro);

            try
            {
                SalvarDados();
            }
            catch
            {
                abastecimentos.Remove(registro);
                dados.LitrosAtuais = nivelAntigo;
                dados.MetaKmL = metaAntiga;
                throw;
            }

            lblResultado.Text = $"Consumo automático: {consumo:N2} km/l";

            lblCusto.Text = caminhao
                ? $"Meta: {dados.MetaKmL:N2} km/l | " +
                  (dados.MetaKmL > 0 && consumo >= dados.MetaKmL
                    ? "META ATINGIDA" : "ABAIXO OU SEM META")
                : $"Custo: {custo:C2}/km | " +
                  (custo <= 0.50 ? "DENTRO DA META" : "ACIMA DA META");

            AtualizarTanqueTexto(veiculo.Text);

            await DisplayAlert("Salvo",
                "Abastecimento registrado com sucesso.", "OK");

            kmAnterior.Text = atual.ToString("N0", pt);
            kmAtual.Text = "";
            litros.Text = "";
            precoLitro.Text = "";
            litrosArla.Text = "";
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erro ao salvar abastecimento", ex.Message, "OK");
        }
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

            if (Ler(limiteAlerta.Text, out var alerta) &&
                alerta > 0 && alerta < 100)
                d.AlertaPercentual = alerta;
            else
                d.AlertaPercentual = 20;

            SalvarDados();
            AtualizarTanqueTexto(veiculo.Text);
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

        double percentual = d.LitrosAtuais / d.Capacidade * 100;
        double alerta = d.AlertaPercentual > 0 ? d.AlertaPercentual : 20;
        double media = MediaAbastecimentos(nome);
        double autonomia = media > 0 ? d.LitrosAtuais * media : 0;

        var registros = abastecimentos
            .Where(x => Id(x.Veiculo) == chave && x.Litros > 0)
            .ToList();

        double mediaKm = registros.Count > 0
            ? registros.Average(x => Math.Max(0, x.KmAtual - x.KmAnterior))
            : 0;

        double mediaLitros = registros.Count > 0
            ? registros.Average(x => x.Litros)
            : 0;

        lblTanque.Text =
            $"NÍVEL DO TANQUE: {d.LitrosAtuais:N1} de {d.Capacidade:N1} L " +
            $"({percentual:N1}%)\n" +
            $"Autonomia estimada: {(media > 0 ? autonomia.ToString("N0", pt) + " km" : "--")}\n" +
            $"Média de KM por abastecimento: " +
            $"{(registros.Count > 0 ? mediaKm.ToString("N0", pt) + " km" : "--")}\n" +
            $"Média de litros por abastecimento: " +
            $"{(registros.Count > 0 ? mediaLitros.ToString("N1", pt) + " L" : "--")}\n" +
            (percentual <= alerta ? "ATENÇÃO: ABASTECER" : "Nível normal");
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
        dataViagem = new DatePicker { Date = DateTime.Today };

        cargaVeiculo = Campo("Caminhão / identificação");
        cargaMotorista = Campo("Motorista");
        anoCarreta = Campo("Ano da carreta", Keyboard.Numeric);
        pesoBruto = Campo("Peso bruto da carga (kg)", Keyboard.Numeric);
        litrosViagem = Campo("Litros consumidos nesta viagem", Keyboard.Numeric);
        consumoViagem = Campo("Consumo médio manual da viagem (km/l)", Keyboard.Numeric);
        metaEnergetica = Campo("Meta energética (padrão 82)", Keyboard.Numeric);

        lblPeso = Indicador("Tara: -- | Peso líquido: --");
        lblEnergetico = Indicador("Valor energético calculado: --");

        anoCarreta.TextChanged += (s, e) => AtualizarPeso();
        pesoBruto.TextChanged += (s, e) => AtualizarPeso();
        consumoViagem.TextChanged += (s, e) => AtualizarPeso();
        metaEnergetica.TextChanged += (s, e) => AtualizarPeso();

        var salvar = Botao("SALVAR VIAGEM E DESCONTAR DO TANQUE");
        salvar.Clicked += SalvarViagem;

        var layout = new VerticalStackLayout
        {
            Spacing = 5,
            Children =
            {
                Titulo("REGISTRO DE CARGAS POR VIAGEM"),
                Rotulo("Data da viagem"), dataViagem,
                Rotulo("Caminhão"), cargaVeiculo,
                Rotulo("Motorista"), cargaMotorista,
                Rotulo("Ano da carreta"), anoCarreta,
                Rotulo("Peso bruto (kg)"), pesoBruto,
                lblPeso,
                Rotulo("Litros de combustível consumidos na viagem"),
                litrosViagem,
                Rotulo("Consumo médio da viagem (km/l) - manual"),
                consumoViagem,
                Rotulo("VALOR ENERGÉTICO CALCULADO AUTOMATICAMENTE"),
                lblEnergetico,
                Rotulo("Meta do valor energético"),
                metaEnergetica,
                salvar,
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
                $"Tara automática: {tara:N0} kg | Informe o peso bruto.";
            lblEnergetico.Text = "Valor energético calculado: --";
            return;
        }

        double liquido = bruto - tara;
        lblPeso.Text = $"Tara: {tara:N0} kg | Peso líquido: {liquido:N0} kg";

        if (Ler(consumoViagem.Text, out var consumo) && consumo > 0)
        {
            double ve = consumo * liquido / 1000.0;
            double meta = 82;

            if (Ler(metaEnergetica.Text, out var m) && m > 0)
                meta = m;

            lblEnergetico.Text =
                $"Valor energético calculado: {ve:N2} | " +
                $"Meta: {meta:N2} | " +
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

    async void SalvarViagem(object? sender, EventArgs e)
    {
        if (salvandoViagem)
            return;

        salvandoViagem = true;

        try
        {
            string nomeVeiculo = cargaVeiculo.Text?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(nomeVeiculo) ||
                !int.TryParse(anoCarreta.Text, out var ano) ||
                !Ler(pesoBruto.Text, out var bruto) ||
                !Ler(litrosViagem.Text, out var litrosConsumidos) ||
                !Ler(consumoViagem.Text, out var consumo) ||
                bruto <= 0 || litrosConsumidos <= 0 || consumo <= 0)
            {
                await DisplayAlert("Atenção",
                    "Preencha caminhão, ano, peso bruto, litros consumidos e consumo km/l.",
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

            if (dados.Capacidade > 0 &&
                litrosConsumidos > dados.LitrosAtuais)
            {
                await DisplayAlert("Combustível insuficiente",
                    $"Tanque: {dados.LitrosAtuais:N1} L.\n" +
                    $"Consumo da viagem: {litrosConsumidos:N1} L.",
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
                Id = Guid.NewGuid().ToString(),
                Data = dataViagem.Date,
                Veiculo = nomeVeiculo,
                Motorista = cargaMotorista.Text?.Trim() ?? "",
                AnoCarreta = ano,
                PesoBruto = bruto,
                Tara = tara,
                PesoLiquido = liquido,
                LitrosConsumidos = litrosConsumidos,
                ConsumoKmL = consumo,
                ValorEnergetico = valorEnergetico,
                MetaEnergetica = meta,
                TanqueDescontado = dados.Capacidade > 0
            };

            double litrosAntigos = dados.LitrosAtuais;

            if (viagem.TanqueDescontado)
                dados.LitrosAtuais -= litrosConsumidos;

            viagens.Insert(0, viagem);

            try
            {
                SalvarDados();
            }
            catch
            {
                viagens.Remove(viagem);
                dados.LitrosAtuais = litrosAntigos;
                throw;
            }

            await DisplayAlert("Viagem salva",
                $"Registro salvo com sucesso!\n\n" +
                $"Peso líquido: {liquido:N0} kg\n" +
                $"Consumo: {consumo:N2} km/l\n" +
                $"Valor energético: {valorEnergetico:N2}\n" +
                $"Meta: {meta:N2}\n" +
                $"Combustível descontado: {litrosConsumidos:N1} L",
                "OK");

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

    View CriarListaViagens()
    {
        var painel = new VerticalStackLayout { Spacing = 8 };

        if (viagens.Count == 0)
        {
            painel.Children.Add(new Label
            {
                Text = "Nenhuma viagem registrada.",
                TextColor = Colors.Gray
            });
            return painel;
        }

        foreach (var x in viagens.OrderByDescending(v => v.Data))
        {
            var detalhes = new VerticalStackLayout { Spacing = 3 };

            detalhes.Children.Add(new Label
            {
                Text = $"{x.Data:dd/MM/yyyy} - {x.Veiculo}",
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#123B65")
            });

            detalhes.Children.Add(new Label
            {
                Text = $"Motorista: {x.Motorista} | Carreta: {x.AnoCarreta}"
            });

            detalhes.Children.Add(new Label
            {
                Text = $"Bruto: {x.PesoBruto:N0} kg | Tara: {x.Tara:N0} kg | Líquido: {x.PesoLiquido:N0} kg"
            });

            detalhes.Children.Add(new Label
            {
                Text = $"Combustível: {x.LitrosConsumidos:N1} L | Consumo: {x.ConsumoKmL:N2} km/l"
            });

            detalhes.Children.Add(new Label
            {
                Text = $"Valor energético: {x.ValorEnergetico:N2} | Meta: {x.MetaEnergetica:N2}"
            });

            var excluir = Botao("EXCLUIR VIAGEM", "#B91C1C");

            excluir.Clicked += async (s, e) =>
            {
                bool ok = await DisplayAlert(
                    "Excluir viagem",
                    "Deseja excluir esta viagem? Os litros descontados serão devolvidos ao tanque cadastrado.",
                    "Sim", "Não");

                if (!ok)
                    return;

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

                viagens.Remove(x);

                try
                {
                    SalvarDados();
                    MostrarMenu("Cargas");
                }
                catch (Exception ex)
                {
                    viagens.Add(x);

                    if (d != null)
                        d.LitrosAtuais = litrosAntigos;

                    await DisplayAlert("Erro", ex.Message, "OK");
                }
            };

            detalhes.Children.Add(excluir);

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

    View TelaIndicadores()
    {
        DateTime hoje = DateTime.Today;

        var abastecimentosValidos = abastecimentos
            .Where(x => x.Consumo > 0)
            .ToList();

        List<Abastecimento> AbNoPeriodo(Func<DateTime, bool> filtro)
        {
            return abastecimentosValidos.Where(x => filtro(x.Data)).ToList();
        }

        List<Viagem> ViNoPeriodo(Func<DateTime, bool> filtro)
        {
            return viagens.Where(x => filtro(x.Data)).ToList();
        }

        View CartaoPeriodo(string titulo, Func<DateTime, bool> filtro)
        {
            var ab = AbNoPeriodo(filtro);
            var cg = ViNoPeriodo(filtro);

            // KM: diferença entre hodômetro atual e anterior.
            double kmTotal = ab.Sum(x =>
                Math.Max(0, x.KmAtual - x.KmAnterior));

            // Litros consumidos: lançamentos das viagens.
            double litrosTotal = cg.Sum(x => x.LitrosConsumidos);

            // Toneladas líquidas: peso líquido das viagens dividido por 1.000.
            double toneladas = cg.Sum(x => x.PesoLiquido) / 1000.0;

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
                        Indicador($"Total de KM: {kmTotal:N0} km"),
                        Indicador($"Combustível consumido: {litrosTotal:N1} L"),
                        Indicador($"Toneladas líquidas: {toneladas:N2} t")
                    }
                }
            };
        }

        double mediaViagem = viagens.Count > 0
            ? viagens.Average(x => x.ConsumoKmL)
            : 0;

        double kmGeral = abastecimentosValidos.Sum(x =>
            Math.Max(0, x.KmAtual - x.KmAnterior));

        double litrosAbastecidos = abastecimentosValidos.Sum(x => x.Litros);
        double mediaAbast = litrosAbastecidos > 0
            ? kmGeral / litrosAbastecidos
            : 0;

        double totalViagens = viagens.Sum(x => x.LitrosConsumidos);

        double mediaEnergetica = viagens.Count > 0
            ? viagens.Average(x => x.ValorEnergetico)
            : 0;

        lblComparacao = Indicador(
            mediaAbast > 0 && mediaViagem > 0
                ? $"{mediaAbast - mediaViagem:N2} km/l"
                : "Sem dados suficientes");

        var painel = new VerticalStackLayout { Spacing = 8 };

        painel.Children.Add(Titulo("INDICADORES GERAIS"));

        painel.Children.Add(CartaoPeriodo("HOJE",
            d => d.Date == hoje));

        painel.Children.Add(CartaoPeriodo("ESTE MÊS",
            d => d.Month == hoje.Month && d.Year == hoje.Year));

        painel.Children.Add(CartaoPeriodo("ESTE ANO",
            d => d.Year == hoje.Year));

        painel.Children.Add(Titulo("RESUMO DA FROTA"));

        painel.Children.Add(Cartao("Quantidade de abastecimentos",
            Indicador(abastecimentos.Count.ToString())));

        painel.Children.Add(Cartao("Total gasto",
            Indicador(abastecimentos.Sum(x => x.Valor).ToString("C", pt))));

        painel.Children.Add(Cartao("Média km/l nos abastecimentos",
            Indicador(mediaAbast > 0 ? $"{mediaAbast:N2} km/l" : "--")));

        painel.Children.Add(Cartao("Quantidade de viagens",
            Indicador(viagens.Count.ToString())));

        painel.Children.Add(Cartao("Média km/l nas viagens",
            Indicador(mediaViagem > 0 ? $"{mediaViagem:N2} km/l" : "--")));

        painel.Children.Add(Cartao("Litros consumidos nas viagens",
            Indicador($"{totalViagens:N1} L")));

        painel.Children.Add(Cartao("Média do valor energético",
            Indicador(mediaEnergetica > 0
                ? mediaEnergetica.ToString("N2", pt) : "--")));

        painel.Children.Add(Cartao("Diferença entre médias",
            lblComparacao));

        painel.Children.Add(Titulo("INDICADORES POR VEÍCULO"));

        var nomesVeiculos = abastecimentos.Select(x => x.Veiculo)
            .Concat(viagens.Select(x => x.Veiculo))
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

            var viVeiculo = viagens
                .Where(x => Id(x.Veiculo) == Id(nome)).ToList();

            double kmVeiculo = abVeiculo.Sum(x =>
                Math.Max(0, x.KmAtual - x.KmAnterior));

            double litrosVeiculo = viVeiculo.Sum(x => x.LitrosConsumidos);
            double toneladasVeiculo =
                viVeiculo.Sum(x => x.PesoLiquido) / 1000.0;

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
                        new Label { Text = $"KM registrados: {kmVeiculo:N0} km" },
                        new Label { Text = $"Litros consumidos: {litrosVeiculo:N1} L" },
                        new Label { Text = $"Toneladas líquidas: {toneladasVeiculo:N2} t" }
                    }
                }
            });
        }

        painel.Children.Add(new Label
        {
            Text = "KM calculados pela diferença entre KM atual e anterior nos abastecimentos. " +
                   "Litros consumidos e toneladas líquidas somados pelos registros de viagens.",
            TextColor = Colors.Gray,
            FontSize = 12
        });

        return painel;
    }

    View TelaRelatorios()
    {
        dataFiltro = new DatePicker { Date = DateTime.Today };

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
        DateTime referencia = dataFiltro.Date;

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
            .Where(x => DataNoPeriodo(x.Data))
            .Where(x => PassaFiltro(x.Veiculo, x.Motorista))
            .OrderByDescending(x => x.Data)
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

        lista.Children.Add(new Label
        {
            Text = $"Abastecimentos: {ab.Count} | Viagens: {cg.Count}",
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#123B65")
        });

        lista.Children.Add(new Label
        {
            Text = $"Gasto: {ab.Sum(x => x.Valor):C2} | " +
                   $"Litros abastecidos: {ab.Sum(x => x.Litros):N1} L"
        });

        lista.Children.Add(new Label
        {
            Text = $"Litros consumidos nas viagens: " +
                   $"{cg.Sum(x => x.LitrosConsumidos):N1} L"
        });

        foreach (var x in ab)
        {
            lista.Children.Add(new Label
            {
                Text = $"{x.Data:dd/MM/yyyy} | {x.Veiculo} | " +
                       $"{x.Litros:N1} L | {x.Consumo:N2} km/l",
                TextColor = Colors.Black
            });
        }

        foreach (var x in cg)
        {
            lista.Children.Add(new Label
            {
                Text = $"{x.Data:dd/MM/yyyy} | {x.Veiculo} | " +
                       $"Viagem: {x.LitrosConsumidos:N1} L | " +
                       $"{x.ConsumoKmL:N2} km/l | VE: {x.ValorEnergetico:N2}",
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
            Linha("RELATORIO DE ABASTECIMENTOS");

            foreach (var x in ab)
            {
                Linha($"{x.Data:dd/MM/yyyy} | {x.Veiculo} | {x.Motorista}");
                Linha($"KM: {x.KmAnterior:N0} a {x.KmAtual:N0} | " +
                      $"{x.Litros:N1} L | {x.Consumo:N2} km/l");
                Linha($"Valor: {x.Valor:C2} | Custo/km: {x.CustoKm:C2}");

                if (x.TemArla)
                    Linha($"Arla: {x.LitrosArla:N1} L");

                y += 8;
            }

            Linha("RELATORIO DE CARGAS / VIAGENS");

            foreach (var x in cg)
            {
                Linha($"{x.Data:dd/MM/yyyy} | {x.Veiculo} | {x.Motorista}");
                Linha($"Carreta: {x.AnoCarreta} | Bruto: {x.PesoBruto:N0} kg | Tara: {x.Tara:N0} kg");
                Linha($"Liquido: {x.PesoLiquido:N0} kg | Combustivel: {x.LitrosConsumidos:N1} L");
                Linha($"Consumo: {x.ConsumoKmL:N2} km/l | VE: {x.ValorEnergetico:N2} | Meta: {x.MetaEnergetica:N2}");
                y += 8;
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

public class Viagem
{
    public string Id { get; set; } = "";
    public DateTime Data { get; set; }
    public string Veiculo { get; set; } = "";
    public string Motorista { get; set; } = "";
    public int AnoCarreta { get; set; }
    public double PesoBruto { get; set; }
    public double Tara { get; set; }
    public double PesoLiquido { get; set; }
    public double LitrosConsumidos { get; set; }
    public double ConsumoKmL { get; set; }
    public double ValorEnergetico { get; set; }
    public double MetaEnergetica { get; set; }
    public bool TanqueDescontado { get; set; }
}
