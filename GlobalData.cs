using Godot;
using System.Collections.Generic;

public partial class GlobalData : Node
{
    public static List<string> SelectedCategories { get; set; } = new();
    
    // Guarda os caminhos completos dos arquivos JSON customizados ativos
    public static List<string> SelectedCustomFiles { get; set; } = new();
}