using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using Spectre.Console;

namespace MazeTUI
{
    class Program
    {
        private const string MazeExe = "maze.exe";
        private const string TempFile = "maze_temp.txt";
        private const string RaceActiveFile = "race_active.tmp";
        private const string RaceResultsFile = "race_results.txt";

        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            
            if (!File.Exists(MazeExe))
            {
                ShowError("maze.exe not found in current directory!");
                return;
            }
            
            ShowWelcomeBanner();

            while (true)
            {
                var choice = ShowMainMenu();
                
                if (choice == "Exit")
                {
                    ShowGoodbye();
                    break;
                }

                try
                {
                    switch (choice)
                    {
                        case "🎲 Generate New Maze":
                            GenerateMaze();
                            break;
                        case "📂 Load Maze from File":
                            LoadMaze();
                            break;
                        case "💾 Save Current Maze":
                            SaveMaze();
                            break;
                        case "🔍 Find Path (A*)":
                            FindPath();
                            break;
                        case "🖨️  Print Current Maze":
                            PrintMaze();
                            break;
                        case "📊 Show Maze Statistics":
                            ShowStatus();
                            break;
                        case "🏁 Race Mode":
                            RaceMode();
                            break;
                    }
                }
                catch (Exception ex)
                {
                    ShowError(ex.Message);
                }

                PressAnyKey();
            }
        }

        static void ShowWelcomeBanner()
        {
            AnsiConsole.Clear();
            
            var gradient = new FigletText("MAZE SOLVER")
                .Centered()
                .Color(Color.Blue);
            
            AnsiConsole.Write(gradient);
            
            var subtitle = new Markup("[dim]Interactive Pathfinding & Maze Generation Tool[/]");
            AnsiConsole.Write(new Panel(subtitle)
                .BorderColor(Color.Grey)
                .Padding(1, 0));
            
            AnsiConsole.WriteLine();
        }

        static void ShowGoodbye()
        {
            AnsiConsole.Clear();
            var goodbye = new FigletText("Goodbye!")
                .Centered()
                .Color(Color.Green);
            AnsiConsole.Write(goodbye);
            AnsiConsole.WriteLine();
        }

        static string ShowMainMenu()
        {
            AnsiConsole.Clear();
            
            // Красивый заголовок
            var rule = new Rule("[cyan bold]MAIN MENU[/]")
            {
                Justification = Justify.Center,
                Style = Style.Parse("cyan")
            };
            AnsiConsole.Write(rule);
            AnsiConsole.WriteLine();

            // Статус панель с иконками
            var hasMaze = File.Exists(TempFile);
            var hasRace = File.Exists(RaceActiveFile);

            var statusTable = new Table()
                .Border(TableBorder.Rounded)
                .BorderColor(Color.Grey)
                .AddColumn(new TableColumn("[bold]Status[/]").Centered())
                .AddColumn(new TableColumn("[bold]Value[/]").Centered());

            statusTable.AddRow(
                "🗺️  Maze Loaded", 
                hasMaze ? "[green]✓ Yes[/]" : "[red]✗ No[/]"
            );
            statusTable.AddRow(
                "🏃 Active Race", 
                hasRace ? "[green]✓ Yes[/]" : "[grey]✗ No[/]"
            );

            AnsiConsole.Write(statusTable);
            AnsiConsole.WriteLine();

            // Меню с группировкой
            var menu = new SelectionPrompt<string>()
                .Title("[yellow bold]What would you like to do?[/]")
                .PageSize(12)
                .HighlightStyle(new Style(Color.Cyan1, decoration: Decoration.Bold))
                .AddChoiceGroup("[blue bold]Maze Operations[/]", new[] {
                    "🎲 Generate New Maze",
                    "📂 Load Maze from File",
                    "💾 Save Current Maze"
                })
                .AddChoiceGroup("[green bold]Pathfinding[/]", new[] {
                    "🔍 Find Path (A*)",
                    "🖨️  Print Current Maze",
                    "📊 Show Maze Statistics"
                })
                .AddChoiceGroup("[yellow bold]Interactive[/]", new[] {
                    "🏁 Race Mode"
                })
                .AddChoiceGroup("[red bold]System[/]", new[] {
                    "Exit"
                });

            return AnsiConsole.Prompt(menu);
        }

        static void GenerateMaze()
        {
            AnsiConsole.Clear();
            ShowSectionHeader("Generate New Maze", "🎲");

            var rows = AnsiConsole.Prompt(
                new TextPrompt<int>("Enter number of [cyan]rows[/] (1-60):")
                    .DefaultValue(20)
                    .ValidationErrorMessage("[red]Please enter a number between 1 and 60[/]")
                    .Validate(r => r >= 1 && r <= 60)
            );

            var cols = AnsiConsole.Prompt(
                new TextPrompt<int>("Enter number of [cyan]columns[/] (1-60):")
                    .DefaultValue(20)
                    .ValidationErrorMessage("[red]Please enter a number between 1 and 60[/]")
                    .Validate(c => c >= 1 && c <= 60)
            );

            AnsiConsole.WriteLine();

            string result = "";
            AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots2)
                .SpinnerStyle(Style.Parse("cyan bold"))
                .Start($"[cyan]Generating {rows}×{cols} maze...[/]", ctx =>
                {
                    result = RunCommand($"gen {rows} {cols}");
                });

            AnsiConsole.WriteLine();
            ShowSuccessPanel(result);
        }

        static void LoadMaze()
        {
            AnsiConsole.Clear();
            ShowSectionHeader("Load Maze from File", "📂");

            var filename = AnsiConsole.Prompt(
                new TextPrompt<string>("Enter [cyan]filename[/]:")
                    .DefaultValue("maze.txt")
            );

            if (!File.Exists(filename))
            {
                ShowError($"File '{filename}' not found!");
                return;
            }

            var result = RunCommand($"load {filename}");
            ShowSuccessPanel(result);
        }

        static void SaveMaze()
        {
            AnsiConsole.Clear();
            ShowSectionHeader("Save Current Maze", "💾");

            var useCustomName = AnsiConsole.Confirm(
                "Save with [cyan]custom filename[/]?", 
                false
            );
            
            string command;
            if (useCustomName)
            {
                var filename = AnsiConsole.Prompt(
                    new TextPrompt<string>("Enter [cyan]filename[/]:")
                        .DefaultValue("my_maze.txt")
                );
                command = $"save {filename}";
            }
            else
            {
                command = "save";
            }

            var result = RunCommand(command);
            ShowSuccessPanel(result);
        }

        static void FindPath()
        {
            AnsiConsole.Clear();
            ShowSectionHeader("Find Path using A* Algorithm", "🔍");

            if (!File.Exists(TempFile))
            {
                ShowError("No maze loaded! Generate or load a maze first.");
                return;
            }

            string result = "";
            AnsiConsole.Status()
                .Spinner(Spinner.Known.BouncingBar)
                .SpinnerStyle(Style.Parse("green bold"))
                .Start("[green]Computing optimal path...[/]", ctx =>
                {
                    result = RunCommand("find");
                });

            AnsiConsole.WriteLine();
            ShowSuccessPanel(result);
        }

        static void PrintMaze()
        {
            AnsiConsole.Clear();
            ShowSectionHeader("Current Maze", "🖨️");

            if (!File.Exists(TempFile))
            {
                ShowError("No maze loaded! Generate or load a maze first.");
                return;
            }

            var result = RunCommand("print");
            
            var panel = new Panel(result)
                .Border(BoxBorder.Double)
                .BorderColor(Color.Cyan1)
                .Header("[cyan bold] MAZE VISUALIZATION [/]");
            
            AnsiConsole.Write(panel);
        }

        static void ShowStatus()
        {
            AnsiConsole.Clear();
            ShowSectionHeader("Maze Statistics", "📊");

            var result = RunCommand("current");
            ShowSuccessPanel(result);
        }

        static void RaceMode()
        {
            if (!File.Exists(TempFile))
            {
                ShowError("No maze loaded! Generate or load a maze first.");
                System.Threading.Thread.Sleep(2000);
                return;
            }

            while (true)
            {
                AnsiConsole.Clear();
                
                var title = new FigletText("RACE MODE")
                    .Centered()
                    .Color(Color.Yellow);
                AnsiConsole.Write(title);
                
                AnsiConsole.WriteLine();

                var hasRace = File.Exists(RaceActiveFile);

                // Show current maze state if race is active
                if (hasRace)
                {
                    var stateResult = RunCommand("race_state");
                    AnsiConsole.Write(stateResult);
                    AnsiConsole.WriteLine();
                }

                var statusBadge = hasRace 
                    ? "[green bold on black] ACTIVE [/]" 
                    : "[grey bold on black] INACTIVE [/]";

                var choices = new[] {
                    "🚀 Start New Race",
                    "⬆️  Move Up",
                    "⬇️  Move Down",
                    "⬅️  Move Left",
                    "➡️  Move Right",
                    "🔄 Reset Race",
                    "🏆 View Results",
                    "⬅️  Back to Main Menu"
                };

                var choice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title($"Race Status: {statusBadge}\n\n[yellow]Choose your move:[/]")
                        .PageSize(10)
                        .HighlightStyle(new Style(Color.Yellow, decoration: Decoration.Bold))
                        .AddChoices(choices)
                );

                if (choice == "⬅️  Back to Main Menu")
                    break;

                try
                {
                    string result = "";
                    bool needsPause = true;

                    switch (choice)
                    {
                        case "🚀 Start New Race":
                            result = RunCommand("race_start");
                            AnsiConsole.WriteLine();
                            ShowInfoPanel(result);
                            break;
                        case "⬆️  Move Up":
                            result = RunCommand("race_up");
                            needsPause = false;
                            break;
                        case "⬇️  Move Down":
                            result = RunCommand("race_down");
                            needsPause = false;
                            break;
                        case "⬅️  Move Left":
                            result = RunCommand("race_left");
                            needsPause = false;
                            break;
                        case "➡️  Move Right":
                            result = RunCommand("race_right");
                            needsPause = false;
                            break;
                        case "🔄 Reset Race":
                            result = RunCommand("race_reset");
                            AnsiConsole.WriteLine();
                            ShowInfoPanel(result);
                            break;
                        case "🏆 View Results":
                            ViewRaceResults();
                            continue;
                    }

                    // Check if race was finished by the move
                    if (!File.Exists(RaceActiveFile) && hasRace && 
                        (choice.Contains("Move") || choice == "🚀 Start New Race"))
                    {
                        // Race finished! Show results
                        AnsiConsole.WriteLine();
                        PressAnyKey();
                        ViewRaceResults();
                        needsPause = false;
                    }

                    if (needsPause)
                    {
                        PressAnyKey();
                    }
                }
                catch (Exception ex)
                {
                    ShowError(ex.Message);
                    PressAnyKey();
                }
            }
        }

        static void ViewRaceResults()
        {
            AnsiConsole.Clear();
            ShowSectionHeader("Race Results", "🏆");

            if (!File.Exists(RaceResultsFile))
            {
                var noResults = new Panel("[yellow]No race results found yet.\nComplete a race to see your statistics here![/]")
                    .Border(BoxBorder.Rounded)
                    .BorderColor(Color.Yellow)
                    .Header("[yellow] INFO [/]");
                
                AnsiConsole.Write(noResults);
            }
            else
            {
                var results = File.ReadAllText(RaceResultsFile);
                var panel = new Panel(results)
                    .Border(BoxBorder.Double)
                    .BorderColor(Color.Gold1)
                    .Header("[gold1 bold] 🏆 LATEST RACE RESULTS 🏆 [/]");
                
                AnsiConsole.Write(panel);
            }

            PressAnyKey();
        }

        // Вспомогательные методы для красивого вывода

        static void ShowSectionHeader(string title, string icon)
        {
            var rule = new Rule($"[cyan bold]{icon} {title.ToUpper()}[/]")
            {
                Justification = Justify.Left,
                Style = Style.Parse("cyan")
            };
            AnsiConsole.Write(rule);
            AnsiConsole.WriteLine();
        }

        static void ShowSuccessPanel(string content)
        {
            var panel = new Panel(content)
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Green)
                .Header("[green] SUCCESS [/]");
            
            AnsiConsole.Write(panel);
        }

        static void ShowInfoPanel(string content)
        {
            var panel = new Panel(content)
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Cyan1)
                .Header("[cyan1] INFO [/]");
            
            AnsiConsole.Write(panel);
        }

        static void ShowError(string message)
        {
            var panel = new Panel($"[red]{message}[/]")
                .Border(BoxBorder.Heavy)
                .BorderColor(Color.Red)
                .Header("[red bold] ⚠ ERROR [/]");
            
            AnsiConsole.Write(panel);
        }

        static void PressAnyKey()
        {
            AnsiConsole.WriteLine();
            AnsiConsole.Markup("[grey]Press [cyan]any key[/] to continue...[/]");
            Console.ReadKey(true);
        }

        static string RunCommand(string arguments)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = MazeExe,
                        Arguments = arguments,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (!string.IsNullOrEmpty(error))
                {
                    return $"[red]{error}[/]";
                }

                return output;
            }
            catch (Exception ex)
            {
                return $"[red]Error executing command: {ex.Message}[/]";
            }
        }
    }
}