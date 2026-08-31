using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using FileAccess = Godot.FileAccess;

public partial class MainMenu : Control
{
    // Checkboxes - Português
    [Export] public CheckBox CheckSyllables { get; set; }
    [Export] public CheckBox CheckConsonants { get; set; }
    [Export] public CheckBox CheckVowels { get; set; }

    // Checkboxes - Matemática
    [Export] public CheckBox CheckAddition { get; set; }
    [Export] public CheckBox CheckSubtraction { get; set; }
    [Export] public CheckBox CheckMultiplication { get; set; }
    [Export] public CheckBox CheckDivision { get; set; }

    // Botões de Ação
    [Export] public Button BtnCreateQuestion { get; set; }
    [Export] public Button BtnImportQuestion { get; set; }
    [Export] public Button BtnPlaySolo { get; set; }
    [Export] public Button BtnCreateRoom { get; set; }
    [Export] public Button BtnJoinRoom { get; set; }
    
    // Partes da custom
    [Export] public VBoxContainer CustomFilesContainer { get; set; }
    [Export] public Label labelImport { get; set; }
    
    private readonly List<string> _selectedCustomFilePaths = new();
    
    // Multiplayer
    [Export] public LineEdit IpInput { get; set; }
    [Export] public Button BtnHost { get; set; }
    [Export] public Button BtnJoin { get; set; }

    private const int PORT = 7000;
    private const int MAX_PLAYERS = 4;
    
    // Feedback erro e outros
    [Export] public Label labelErro { get; set; }
    [Export] public FileDialog ImportFileDialog { get; set; }

    public override void _Ready()
    {
        // Conectar os botões principais
        BtnPlaySolo.Pressed += OnPlaySoloPressed;
        BtnCreateRoom.Pressed += OnCreateRoomPressed;
        BtnJoinRoom.Pressed += OnJoinRoomPressed;
        BtnCreateQuestion.Pressed += OnCreateQuestionPressed;
        BtnImportQuestion.Pressed += OnImportQuestionPressed;
        
        DirAccess.MakeDirAbsolute("user://custom_quizzes/");

        if (ImportFileDialog != null)
        {
            ImportFileDialog.FileSelected += OnFileSelectedForImport;
        }
        
        if (BtnHost != null) BtnHost.Pressed += OnHostPressed;
        if (BtnJoin != null) BtnJoin.Pressed += OnJoinPressed;

        // Carrega a lista de arquivos salvos assim que a tela abre
        RefreshCustomFilesList();
    }

    // Retorna a lista de categorias que o jogador marcou nas CheckBoxes
    public List<string> GetSelectedQuizTypes()
    {
        List<string> selectedTypes = new();

        if (CheckSyllables != null && CheckSyllables.ButtonPressed) selectedTypes.Add("spec-silabaCount");
        if (CheckConsonants != null && CheckConsonants.ButtonPressed) selectedTypes.Add("spec-consoanteCount");
        if (CheckVowels != null && CheckVowels.ButtonPressed) selectedTypes.Add("spec-vogalCount");

        if (CheckAddition != null && CheckAddition.ButtonPressed) selectedTypes.Add("math_addition");
        if (CheckSubtraction != null && CheckSubtraction.ButtonPressed) selectedTypes.Add("math_subtraction");
        if (CheckMultiplication != null && CheckMultiplication.ButtonPressed) selectedTypes.Add("math_multiplication");
        if (CheckDivision != null && CheckDivision.ButtonPressed) selectedTypes.Add("math_division");
    
        List<string> selectCustom = GetSelectedCustomFiles();
    
        // CORREÇÃO: Verifica se realmente existe algum arquivo selecionado na lista
        if (selectCustom != null && selectCustom.Count > 0)
        {
            GD.Print("Achou arquivos customizados selecionados!");
            selectedTypes.Add("custom");
        }

        return selectedTypes;
    }

    private void OnPlaySoloPressed()
    {
        
        List<string> selectedCategories = GetSelectedQuizTypes();
        
        if (selectedCategories.Count == 0)
        {
            labelErro.Visible = true;
            labelErro.Text = "Selecione pelo menos um tipo de quiz antes de jogar!";
            labelErro.AddThemeColorOverride("font_color", Colors.Red);
            GD.Print("Selecione pelo menos um tipo de quiz antes de jogar!");
            return;
        }
        
        labelErro.Visible = true;
        labelErro.Text = $"Iniciando jogo Solo com os modos: {string.Join(", ", selectedCategories)}";
        labelErro.AddThemeColorOverride("font_color", Colors.White);
        GD.Print($"Iniciando jogo Solo com os modos: {string.Join(", ", selectedCategories)}");
        
        GlobalData.SelectedCategories = selectedCategories;
        GlobalData.SelectedCustomFiles = GetSelectedCustomFiles();

        // Troca para a cena do jogo
        GetTree().ChangeSceneToFile("res://GameManager.tscn");
        
        // Aqui você fará a transição para a MainScene passando os tópicos escolhidos
        // GetTree().ChangeSceneToFile("res://MainScene.tscn");
    }

    // --- GANCHOS PARA O MULTIPLAYER FUTURO ---

    private void OnCreateRoomPressed()
    {
        List<string> selectedCategories = GetSelectedQuizTypes();
        labelErro.Visible = true;
        labelErro.Text = "Criando sala multiplayer com as categorias selecionadas...";
        labelErro.AddThemeColorOverride("font_color", Colors.Blue);
        
        GD.Print("Criando sala multiplayer com as categorias selecionadas...");
        // TODO: Inicializar Host (ENetMultiplayerPeer) e carregar arena
    }

    private void OnJoinRoomPressed()
    {
        labelErro.Visible = true;
        labelErro.Text = "Abrindo janela/popup para digitar IP ou código da sala...";
        labelErro.AddThemeColorOverride("font_color", Colors.Blue);
        
        GD.Print("Abrindo janela/popup para digitar IP ou código da sala...");
        // TODO: Conectar como Cliente (ENetMultiplayerPeer)
    }

    private void OnCreateQuestionPressed()
    {
        labelErro.Visible = true;
        labelErro.Text = "Abrindo criador de questões...";
        labelErro.AddThemeColorOverride("font_color", Colors.Blue);
        
        GD.Print("Abrindo criador de questões...");
        // TODO: Interface visual para adicionar novas perguntas ao JSON
    }

    private void OnImportQuestionPressed()
    {
        labelErro.Visible = true;
        labelErro.Text = "Abrindo seletor de arquivo JSON local...";
        labelErro.AddThemeColorOverride("font_color", Colors.Blue);
        
        GD.Print("Abrindo seletor de arquivo JSON local...");
        
        ImportFileDialog?.PopupCentered(new Vector2I(700, 500));
    }
    private void OnFileSelectedForImport(string path)
    {
        try
        {
            using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
            if (file == null) return;

            string jsonContent = file.GetAsText();
            List<JsonQuestion> importedQuestions = JsonSerializer.Deserialize<List<JsonQuestion>>(jsonContent);

            if (!ValidateImportedQuestions(importedQuestions))
            {
                GD.PrintErr("Formato de arquivo JSON inválido!");
                return;
            }

            // Pega o nome do arquivo selecionado e salva dentro da pasta user://custom_quizzes/
            string fileName = Path.GetFileName(path);
            string destinationPath = $"user://custom_quizzes/{fileName}";

            using var saveFile = FileAccess.Open(destinationPath, FileAccess.ModeFlags.Write);
            saveFile.StoreString(jsonContent);

            GD.Print($"Arquivo {fileName} salvo com sucesso!");

            // Atualiza a lista de CheckBoxes na tela
            RefreshCustomFilesList();
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Erro ao importar JSON: {ex.Message}");
        }
    }
    
    public void RefreshCustomFilesList()
    {
        if (CustomFilesContainer == null) return;

        foreach (Node child in CustomFilesContainer.GetChildren())
        {
            child.QueueFree();
        }

        string folderPath = "user://custom_quizzes/";
        using var dir = DirAccess.Open(folderPath);
        if (dir == null) return;

        dir.ListDirBegin();
        string fileName = dir.GetNext();

        while (fileName != "")
        {
            if (!dir.CurrentIsDir() && fileName.EndsWith(".json"))
            {
                string fullPath = $"{folderPath}{fileName}";

                HBoxContainer row = new HBoxContainer();

                CheckBox checkBox = new CheckBox
                {
                    Text = fileName.Replace(".json", "")
                };

                // CORREÇÃO: Guarda o caminho real nos Metadados do nó
                checkBox.SetMeta("full_path", fullPath);
                
                FontFile minhaFonte = GD.Load<FontFile>("res://Fonts/pixel_operator/PixelOperatorSC-Bold.ttf");
                
                checkBox.AddThemeFontOverride("font", minhaFonte);
                checkBox.AddThemeFontSizeOverride("font_size", 24);

                Button deleteButton = new Button
                {
                    Text = "X"
                };

                string fileToDelete = fullPath;
                deleteButton.Pressed += () => DeleteCustomFile(fileToDelete);

                row.AddChild(checkBox);
                row.AddChild(deleteButton);
                CustomFilesContainer.AddChild(row);
            }
            fileName = dir.GetNext();
        }
    }

    private void DeleteCustomFile(string filePath)
    {
        if (FileAccess.FileExists(filePath))
        {
            DirAccess.RemoveAbsolute(filePath);
            GD.Print($"Arquivo removido: {filePath}");
            RefreshCustomFilesList();
        }
    }
    
    public List<string> GetSelectedCustomFiles()
    {
        List<string> activeFiles = new();

        if (CustomFilesContainer != null)
        {
            foreach (Node child in CustomFilesContainer.GetChildren())
            {
                if (child is HBoxContainer row)
                {
                    var checkBox = row.GetChildOrNull<CheckBox>(0);
                    if (checkBox != null && checkBox.ButtonPressed)
                    {
                        // CORREÇÃO: Pega o caminho original salvo nos metadados
                        if (checkBox.HasMeta("full_path"))
                        {
                            string fullPath = checkBox.GetMeta("full_path").AsString();
                            activeFiles.Add(fullPath);
                        }
                    }
                }
            }
        }

        return activeFiles;
    }

    private bool ValidateImportedQuestions(List<JsonQuestion> questions)
    {
        if (questions == null || questions.Count == 0) return false;

        foreach (var q in questions)
        {
            // Valida se os campos obrigatórios não estão vazios/nulos
            if (string.IsNullOrWhiteSpace(q.Type) ||
                string.IsNullOrWhiteSpace(q.Questao) ||
                string.IsNullOrWhiteSpace(q.RespCorreta))
            {
                labelImport.Visible = true;
                labelImport.AddThemeColorOverride("font_color", Colors.Red);
                labelImport.Text = "Arquivo invalido, ou incorreto. Por favor verifique ele.";
                return false;
            }
        }
        labelImport.Visible = true;
        labelImport.AddThemeColorOverride("font_color", Colors.Blue);
        labelImport.Text = "Arquivo Encontrado e validado com sucesso!";
        return true;
    }
    
    // MULTIPLAYER
    // adicionar os negocio
    private void OnHostPressed()
    {
        var peer = new ENetMultiplayerPeer();
        Error error = peer.CreateServer(PORT, MAX_PLAYERS);

        if (error != Error.Ok)
        {
            GD.PrintErr("Erro ao criar servidor: " + error);
            return;
        }

        Multiplayer.MultiplayerPeer = peer;
        GD.Print("Servidor iniciado! Mudando para a cena principal...");

        // O Host muda direto para o jogo
        GetTree().ChangeSceneToFile("res://GameManager.tscn");
    }

    private void OnJoinPressed()
    {
        var peer = new ENetMultiplayerPeer();
        string ip = string.IsNullOrWhiteSpace(IpInput?.Text) ? "127.0.0.1" : IpInput.Text;
        
        Error error = peer.CreateClient(ip, PORT);

        if (error != Error.Ok)
        {
            GD.PrintErr("Erro ao conectar ao servidor: " + error);
            return;
        }

        Multiplayer.MultiplayerPeer = peer;

        // O cliente escuta o evento de conexão bem-sucedida antes de mudar de cena
        Multiplayer.ConnectedToServer += OnConnectedToServer;
    }

    private void OnConnectedToServer()
    {
        Multiplayer.ConnectedToServer -= OnConnectedToServer;
        GD.Print("Conectado ao Host com sucesso!");
        GetTree().ChangeSceneToFile("res://GameManager.tscn");
    }
}