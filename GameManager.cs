using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

public struct QuestionData
{
    public string Question;
    public string[] Options; // Array com 4 opções
    public int CorrectIndex; // Índice da resposta certa dentro de 'Options'
    public int Nivel; // Um valor de 1 a 5 para demostrar a dificuldade da questão
}

public class JsonQuestion
{
    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("questao")]
    public string Questao { get; set; }

    [JsonPropertyName("resp-correta")]
    public string RespCorreta { get; set; }

    [JsonPropertyName("alternativas")]
    public string[] Alternativas { get; set; }

    [JsonPropertyName("nivel")]
    public int Nivel { get; set; }
}

public class JsonMathConfig
{
    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("min1")]
    public int Min1 { get; set; }

    [JsonPropertyName("max1")]
    public int Max1 { get; set; }

    [JsonPropertyName("min2")]
    public int Min2 { get; set; }

    [JsonPropertyName("max2")]
    public int Max2 { get; set; }
}


public partial class GameManager : Node2D
{
    [Export] public Godot.Collections.Array<AnswerZone> Zones { get; set; }
    [Export] public Player LocalPlayer { get; set; }
    [Export] public Label QuestionLabel { get; set; }
    [Export] public Label TimerLabel { get; set; }
    [Export] public Timer RoundTimer { get; set; }
    [Export] public CollisionShape2D BarreiraSeccao1 { get; set; }
    [Export] public CollisionShape2D BarreiraSeccao2 { get; set; }
    [Export] public Control GameOverPanel { get; set; }
    [Export] public Label GameOverLabel { get; set; }
    [Export] public Button BtnReturnMenu { get; set; }
    [Export] public Button BtnTopMenu { get; set; }

    // Array com as cores sequenciais: Vermelho, Azul, Verde e Roxo
    private readonly Color[] _zoneColors = new Color[]
    {
        new Color("e74c3c"), // Vermelho
        new Color("3498db"), // Azul
        new Color("2ecc71"), // Verde
        new Color("9b59b6")  // Roxo
    };

    private readonly List<QuestionData> _questions = new();
    private QuestionData _currentQuestion;
    private int _correctZoneIndex;
    private readonly Random _random = new();

    public override void _Ready()
    {
        // Atribui as cores respectivamente para cada zona no array 'Zones'
        for (int i = 0; i < Zones.Count && i < _zoneColors.Length; i++)
        {
            Console.WriteLine("1");
            Zones[i].SetZoneColor(_zoneColors[i]);
        }
        
        LoadQuestions();
        
        RoundTimer.Timeout += OnRoundTimeout;
        StartNewRound();
        
        if (BtnReturnMenu != null) BtnReturnMenu.Pressed += OnReturnToMenuPressed;
        if (BtnTopMenu != null) BtnTopMenu.Pressed += ToggleReturnMenu;
    
        if (GameOverPanel != null) GameOverPanel.Visible = false;
    }
    
    public void OnPlayerDefeated()
    {
        // No Singleplayer, paramos o tempo. No Multiplayer, o timer continua rodando no servidor para os sobreviventes.
        if (!IsMultiplayerActive())
        {
            RoundTimer?.Stop();
        }

        // A UI de Game Over é local e só aparece na tela de quem perdeu
        if (GameOverPanel != null)
        {
            GD.Print(1);
            GameOverPanel.Visible = true;
        }

        if (BtnTopMenu != null)
        {
            GD.Print(2);
            BtnTopMenu.Visible = false;
        }

        if (GameOverLabel != null)
        {
            GD.Print(3);
            GameOverLabel.Text = "Você perdeu! Acompanhe a partida ou volte ao menu.";
        }
    }

    private void ToggleReturnMenu()
    {
        GameOverPanel.Visible = !GameOverPanel.Visible;
        GD.Print("Mudou");
    }

    private void OnReturnToMenuPressed()
    {
        // Troca de volta para o menu principal
        GetTree().ChangeSceneToFile("res://main_menu.tscn");
    }
    
    public override void _Process(double delta)
    {
        if (!RoundTimer.IsStopped())
        {
            TimerLabel.Text = $"Tempo: {Mathf.CeilToInt(RoundTimer.TimeLeft)}s";
        }
    }
    
    private void LoadQuestions()
{
    _questions.Clear();

    // ==========================================
    // ETAPA 1: CARREGAR QUESTÕES DE TEXTO (JSON)
    // ==========================================
    List<string> filesToLoad = new List<string>
    {
        "res://silaba-cont.json",
        "res://vogal-cont.json",
        "res://consoante-cont.json"
    };

    filesToLoad.AddRange(GlobalData.SelectedCustomFiles);

    foreach (string filePath in filesToLoad)
    {
        if (!FileAccess.FileExists(filePath)) continue;

        using var file = FileAccess.Open(filePath, FileAccess.ModeFlags.Read);
        string jsonText = file.GetAsText();

        List<JsonQuestion> rawQuestions;
        try
        {
            rawQuestions = JsonSerializer.Deserialize<List<JsonQuestion>>(jsonText);
        }
        catch
        {
            continue;
        }

        if (rawQuestions == null) continue;

        foreach (var q in rawQuestions)
        {
            // Verifica se a categoria foi selecionada ou se é arquivo customizado
            if (GlobalData.SelectedCategories.Contains(q.Type) || GlobalData.SelectedCustomFiles.Contains(filePath))
            {
                string[] wrongOptions = q.Alternativas;

                if (wrongOptions == null || wrongOptions.Length == 0)
                {
                    wrongOptions = GenerateAlternatives(q.RespCorreta);
                }

                List<string> allOptions = new List<string>(wrongOptions) { q.RespCorreta };
                ShuffleList(allOptions);

                int correctIdx = allOptions.IndexOf(q.RespCorreta);

                _questions.Add(new QuestionData
                {
                    Question = q.Questao,
                    Options = allOptions.ToArray(),
                    CorrectIndex = correctIdx,
                    Nivel = q.Nivel
                });
            }
        }
    }

    // ==========================================
    // ETAPA 2: GERAR QUESTÕES DE MATEMÁTICA
    // ==========================================
    LoadMathQuestions();

    // Embaralha todas as perguntas (Português + Matemática + Customizadas) juntas
    ShuffleList(_questions);
}
    private void LoadMathQuestions()
    {
        string mathConfigPath = "res://math_config.json";

        if (!FileAccess.FileExists(mathConfigPath)) return;

        using var file = FileAccess.Open(mathConfigPath, FileAccess.ModeFlags.Read);
        string jsonText = file.GetAsText();

        List<JsonMathConfig> mathConfigs;
        try
        {
            mathConfigs = JsonSerializer.Deserialize<List<JsonMathConfig>>(jsonText);
        }
        catch
        {
            return;
        }

        if (mathConfigs == null) return;

        foreach (var config in mathConfigs)
        {
            // Se o jogador selecionou essa operação no menu (ex: "math_addition")
            if (GlobalData.SelectedCategories.Contains(config.Type))
            {
                // Gera 5 perguntas aleatórias para esta operação
                for (int i = 0; i < 5; i++)
                {
                    QuestionData mathQuestion = GenerateMathQuestion(
                        config.Type, 
                        config.Min1, 
                        config.Max1, 
                        config.Min2, 
                        config.Max2
                    );

                    _questions.Add(mathQuestion);
                }
            }
        }
    }

private string[] GenerateAlternatives(string correctText)
{
    // Se a resposta for numérica, aplica a lógica de variações
    if (int.TryParse(correctText, out int correctVal))
    {
        int alt1, alt2, alt3;

        if (correctVal == 1)
        {
            // Caso seja 1: +1, +2, +3 (Resultado: 2, 3, 4)
            alt1 = correctVal + 1;
            alt2 = correctVal + 2;
            alt3 = correctVal + 3;
        }
        else if (correctVal == 2)
        {
            // Caso seja 2: -1, +1, +2 (Resultado: 1, 3, 4)
            alt1 = correctVal - 1;
            alt2 = correctVal + 1;
            alt3 = correctVal + 2;
        }
        else
        {
            // Caso seja 3 ou maior: -1, +1, -2 (Evita números negativos)
            alt1 = correctVal - 1;
            alt2 = correctVal + 1;
            alt3 = correctVal - 2;
        }

        return new string[] { alt1.ToString(), alt2.ToString(), alt3.ToString() };
    }

    // Fallback caso a resposta não seja um número inteiro válido
    return new string[] { "0", "1", "2" };
}

private QuestionData GenerateMathQuestion(string type, int min1, int max1, int min2, int max2)
{
    Random rand = new Random();
    int n1 = rand.Next(min1, max1 + 1);
    int n2 = rand.Next(min2, max2 + 1);
    int correctResult = 0;
    int nivel = 2;
    string symbol = "+";

    switch (type)
    {
        case "math_addition":
            symbol = "+";
            correctResult = n1 + n2;
            break;

        case "math_subtraction":
            symbol = "-";
            // Inverte para garantir que n1 seja maior que n2 (evita resultado negativo)
            if (n1 < n2) (n1, n2) = (n2, n1);
            correctResult = n1 - n2;
            break;

        case "math_multiplication":
            symbol = "x";
            correctResult = n1 * n2;
            nivel = 3;
            break;

        case "math_division":
            symbol = "÷";
            // Técnica para divisão exata sem vírgulas:
            // n2 é o divisor, correctResult é a resposta sorteada, e n1 vira o produto exato
            correctResult = rand.Next(min1, max1 + 1);
            n1 = n2 * correctResult;
            nivel = 3;
            break;
    }

    string questionText = $"{n1} {symbol} {n2} = ?";
    string correctStr = correctResult.ToString();

    // Reutiliza a função de criar alternativas que fizemos antes!
    string[] wrongOptions = GenerateAlternatives(correctStr);

    List<string> options = new List<string>(wrongOptions) { correctStr };
    ShuffleList(options);

    return new QuestionData
    {
        Question = questionText,
        Options = options.ToArray(),
        CorrectIndex = options.IndexOf(correctStr),
        Nivel = nivel // <--- Definindo nível padrão (ou você pode passar como parâmetro se desejar)
    };
}

    private void StartNewRound()
    {
        Console.WriteLine("4");
        if (_questions.Count == 0)
        {
            QuestionLabel.Text = "Parabéns! Você venceu todas as perguntas!";
            //Aqui que vem a parte de acabar
            // A UI de Game Over é local e só aparece na tela de quem perdeu
            if (GameOverPanel != null)
            {
                GD.Print(1);
                GameOverPanel.Visible = true;
            }

            if (BtnTopMenu != null)
            {
                GD.Print(2);
                BtnTopMenu.Visible = false;
            }

            if (GameOverLabel != null)
            {
                GD.Print(3);
                GameOverLabel.Text = "As perguntas acabaram! Você venceu, parabens!";
            }
            RoundTimer.Start(1000000000000000); 
            TimerLabel.Visible = false;
            return;
        }

        _currentQuestion = _questions[0];
        _questions.RemoveAt(0);

        QuestionLabel.Text = _currentQuestion.Question;

        // Cria uma lista com os índices [0, 1, 2, 3] e embaralha
        List<int> optionIndices = new List<int> { 0, 1, 2, 3 };
        ShuffleList(optionIndices);

        // Distribui os índices sorteados para as 4 zonas
        for (int i = 0; i < Zones.Count; i++)
        {   
            Zones[i].ResetZone();
            
            int chosenOptionIndex = optionIndices[i];
            Zones[i].SetAnswerText(_currentQuestion.Options[chosenOptionIndex]);

            // Se o índice sorteado for o correto, registra qual zona (i) está com a vitória
            if (chosenOptionIndex == _currentQuestion.CorrectIndex)
            {
                _correctZoneIndex = i;
            }
        }
        float baseTime = 10.0f; // Tempo base do jogo
        float bonusTime = GetBonusTime(_currentQuestion.Nivel);
    
        // Inicia o timer com 10s + Bônus
        RoundTimer.Start(baseTime + bonusTime);
    }

    // Algoritmo de embaralhamento (Fisher-Yates)
    private void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int randomIndex = _random.Next(i + 1);
            (list[i], list[randomIndex]) = (list[randomIndex], list[i]);
        }
    }

    private async void OnRoundTimeout()
    {
        GD.Print("Chegou na função");
        if (IsMultiplayerActive() && !Multiplayer.IsServer()) return;

        GD.Print("Passou da função");
    
        // 1. Tranca as barreiras
        BarreiraSeccao1.Disabled = false;
        BarreiraSeccao2.Disabled = false;

        // 2. Aguarda 1 segundo de suspense
        await ToSignal(GetTree().CreateTimer(1.0f), SceneTreeTimer.SignalName.Timeout);

        // 3. Oculta o chão das zonas incorretas e elimina jogadores
        for (int i = 0; i < Zones.Count; i++)
        {
            if (i != _correctZoneIndex)
            {
                Zones[i].SetFloorVisible(false);

                foreach (var player in Zones[i].GetPlayersInside())
                {
                    if (player != null)
                    {
                        player.Eliminate();
                    }
                }
            }
        }

        // 4. Aguarda 3 segundos antes da próxima rodada
        await ToSignal(GetTree().CreateTimer(3.0f), SceneTreeTimer.SignalName.Timeout);

        // Se o jogador local existir e estiver vivo, avança
        if (LocalPlayer != null && LocalPlayer.IsAlive)
        {
            GD.Print("Vivo, começando novo round");
            StartNewRound();
        }
        else
        {
            // Se morreu (ou é null), roda a derrota independente de ser singleplayer ou multiplayer
            GD.Print("Morto, encerrando");
            if (QuestionLabel != null) QuestionLabel.Text = "Game Over! Você errou a resposta.";
            AcabarJogo();
            OnPlayerDefeated();
        }

        BarreiraSeccao1.Disabled = true;
        BarreiraSeccao2.Disabled = true;
    }   
    
    private float GetBonusTime(int nivel)
    {
        // Se o nível for < 1, vira 1. Se for > 5, vira 5.
        int clampedNivel = Mathf.Clamp(nivel, 1, 5);

        return clampedNivel switch
        {
            3 => 3.0f,
            4 => 5.0f,
            5 => 8.0f,
            _ => 0.0f // Tratamento para níveis 1 e 2 (retorna 0)
        };
    }

    private void AcabarJogo()
    {
        RoundTimer.Start(1000000000000000);
        TimerLabel.Visible = false;
        OnPlayerDefeated();
    }
    
    // CHECAR MULTIPLAYER:
    private bool IsMultiplayerActive()
    {
        return Multiplayer.HasMultiplayerPeer() && 
               Multiplayer.MultiplayerPeer.GetConnectionStatus() == MultiplayerPeer.ConnectionStatus.Connected;
    }
}