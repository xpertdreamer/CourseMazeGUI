using System.Diagnostics;
using Spectre.Console;

namespace MazeRace
{
    internal static class Program
    {
        private const string MazeExe = "maze.exe";
        private const string TempFile = "maze_temp.txt";
        private const string RaceActiveFile = "race_active.tmp";
        private const string RaceResultsFile = "race_results.txt";
        private const string RaceStateFile = "race_state.tmp";
        private const int SpinnerMinDuration = 1000; // milliseconds
        
        private static int _lastConsoleWidth;
        private static int _lastConsoleHeight;

        public static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.CursorVisible = false;
            
            _lastConsoleWidth = Console.WindowWidth;
            _lastConsoleHeight = Console.WindowHeight;
            
            if (!File.Exists(MazeExe))
            {
                ShowError("maze.exe not found in current directory!");
                Console.CursorVisible = true;
                return;
            }

            try
            {
                while (true)
                {
                    var choice = ShowMainMenu();
                    
                    if (choice == "🚪 Exit")
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
                            case "🖨️ Print Current Maze":
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
            finally
            {
                CleanupTempFiles();
                Console.CursorVisible = true;
            }
        }

        private static void CleanupTempFiles()
        {
            try
            {
                if (File.Exists(TempFile))
                {
                    File.Delete(TempFile);
                }
                if (File.Exists(RaceActiveFile))
                {
                    File.Delete(RaceActiveFile);
                }
                if (File.Exists(RaceStateFile))
                {
                    File.Delete(RaceStateFile);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        private static bool CheckConsoleResize()
        {
            try
            {
                int currentWidth = Console.WindowWidth;
                int currentHeight = Console.WindowHeight;
                
                if (currentWidth != _lastConsoleWidth || currentHeight != _lastConsoleHeight)
                {
                    _lastConsoleWidth = currentWidth;
                    _lastConsoleHeight = currentHeight;
                    return true;
                }
            }
            catch
            {
                // Ignore errors when checking console size
            }
            
            return false;
        }

        private static void ShowAppTitle()
        {
            var title = new FigletText("BUDNIKOW A.S. COURSE WORK")
                .Centered()
                .Color(Color.Cyan1);
            AnsiConsole.Write(title);
        }

        private static void ShowGoodbye()
        {
            SmoothClear();
            ShowAppTitle();
            AnsiConsole.WriteLine();
            
            var panel = new Panel(
                Align.Center(
                    new Markup("[cyan1 bold]Thank you for using![/]\n[dim]See you next time! 👋[/]"),
                    VerticalAlignment.Middle
                )
            )
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Cyan1)
                .Header("[cyan1 bold]GOODBYE[/]")
                .HeaderAlignment(Justify.Center)
                .Padding(2, 1);
            
            AnsiConsole.Write(Align.Center(panel));
            AnsiConsole.WriteLine();
            PressAnyKey();
        }

        private static string ShowMainMenu()
        {
            SmoothClear();
            ShowAppTitle();
            AnsiConsole.WriteLine();

            var hasMaze = File.Exists(TempFile) && new FileInfo(TempFile).Length > 0;
            var hasRace = File.Exists(RaceActiveFile);

            // Status panel
            var statusText = $"🗺️  Maze: {(hasMaze ? "[green bold]LOADED[/]" : "[red bold]NOT LOADED[/]")}    🏃 Race: {(hasRace ? "[green bold]ACTIVE[/]" : "[grey bold]INACTIVE[/]")}";
            var statusPanel = new Panel(Align.Center(new Markup(statusText)))
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Grey)
                .Padding(1, 0);
            
            AnsiConsole.Write(Align.Center(statusPanel));
            AnsiConsole.WriteLine();

            var menu = new SelectionPrompt<string>()
                .Title(Align.Center(new Markup("[yellow bold]What would you like to do?[/]")).ToString())
                .PageSize(20)
                .HighlightStyle(new Style(Color.Black, Color.Yellow, Decoration.Bold))
                .AddChoiceGroup("Maze Operations", new[]
                {
                    "🎲 Generate New Maze",
                    "📂 Load Maze from File",
                    "💾 Save Current Maze"
                })
                .AddChoiceGroup("Pathfinding", new[]
                {
                    "🔍 Find Path (A*)",
                    "🖨️ Print Current Maze",
                    "📊 Show Maze Statistics"
                })
                .AddChoiceGroup("Interactive", new[]
                {
                    "🏁 Race Mode"
                })
                .AddChoiceGroup("System", new[]
                {
                    "🚪 Exit"
                });
                
            return AnsiConsole.Prompt(menu);
        }

        private static void GenerateMaze()
        {
            SmoothClear();
            ShowAppTitle();
            AnsiConsole.WriteLine();

            var headerPanel = new Panel(Align.Center(new Markup("[cyan1 bold]🎲 GENERATE NEW MAZE[/]")))
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Cyan1)
                .Padding(1, 0);
            AnsiConsole.Write(Align.Center(headerPanel));
            AnsiConsole.WriteLine();

            var dimensions = AnsiConsole.Prompt(
                new TextPrompt<string>("[cyan1]Enter maze size[/] [dim](rows cols, e.g., 20 20)[/]:")
                    .DefaultValue("20 20")
                    .ValidationErrorMessage("[red]Min 5x5, max 60x60[/]")
                    .Validate(input =>
                    {
                        var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length != 2) return ValidationResult.Error("[red]Enter two numbers[/]");
                        
                        if (!int.TryParse(parts[0], out int rows) || !int.TryParse(parts[1], out int cols))
                            return ValidationResult.Error("[red]Invalid numbers[/]");
                        
                        if (rows < 5 || cols < 5)
                            return ValidationResult.Error("[red]Min 5x5[/]");
                        
                        if (rows > 60 || cols > 60)
                            return ValidationResult.Error("[red]Max 60x60[/]");
                        
                        return ValidationResult.Success();
                    })
            );

            var parts = dimensions.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            int rows = int.Parse(parts[0]);
            int cols = int.Parse(parts[1]);

            AnsiConsole.WriteLine();

            string result = "";
            AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots12)
                .SpinnerStyle(Style.Parse("cyan1 bold"))
                .Start($"[cyan1]⚡ Generating {rows}×{cols} maze...[/]", ctx =>
                {
                    var stopwatch = Stopwatch.StartNew();
                    result = RunCommand($"gen {rows} {cols}");
                    EnsureMinimumDuration(stopwatch);
                });

            // Verify maze was generated correctly
            AnsiConsole.WriteLine();
            AnsiConsole.Write(Align.Center(new Markup("[cyan1]🔍 Verifying maze has valid path...[/]")));
            
            var pathResult = RunCommand("find");
            bool hasValidPath = !pathResult.Contains("ERROR: No path found") && 
                               !pathResult.Contains("No path found!");
            
            AnsiConsole.WriteLine();
            
            if (hasValidPath)
            {
                var resultPanel = new Panel(Align.Center(new Markup($"[green]{result}[/]\n[dim]✓ Maze has valid path[/]")))
                    .Border(BoxBorder.Rounded)
                    .BorderColor(Color.Green)
                    .Header("[green bold]✓ SUCCESS[/]")
                    .HeaderAlignment(Justify.Center)
                    .Padding(1, 0);
                AnsiConsole.Write(Align.Center(resultPanel));
            }
            else
            {
                var resultPanel = new Panel(Align.Center(
                    new Markup($"[yellow]{result}[/]\n[red]⚠️ Generated maze has no valid path![/]\n[dim]Try generating again...[/]")))
                    .Border(BoxBorder.Rounded)
                    .BorderColor(Color.Yellow)
                    .Header("[yellow bold]⚠️ WARNING[/]")
                    .HeaderAlignment(Justify.Center)
                    .Padding(1, 0);
                AnsiConsole.Write(Align.Center(resultPanel));
            }
        }

        private static void LoadMaze()
        {
            SmoothClear();
            ShowAppTitle();
            AnsiConsole.WriteLine();

            var headerPanel = new Panel(Align.Center(new Markup("[cyan1 bold]📂 LOAD MAZE FROM FILE[/]")))
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Cyan1)
                .Padding(1, 0);
            AnsiConsole.Write(Align.Center(headerPanel));
            AnsiConsole.WriteLine();

            var filename = AnsiConsole.Prompt(
                new TextPrompt<string>("[cyan1]Enter filename[/]:")
                    .DefaultValue("maze.txt")
            );

            if (!File.Exists(filename))
            {
                ShowError($"File '{filename}' not found!");
                return;
            }

            string result = "";
            AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots12)
                .SpinnerStyle(Style.Parse("cyan1 bold"))
                .Start($"[cyan1]📥 Loading maze from '{filename}'...[/]", ctx =>
                {
                    var stopwatch = Stopwatch.StartNew();
                    result = RunCommand($"load {filename}");
                    EnsureMinimumDuration(stopwatch);
                });

            AnsiConsole.WriteLine();
            
            // Verify maze loaded correctly
            var statusResult = RunCommand("current");
            bool loadedSuccessfully = !statusResult.Contains("No maze loaded") && 
                                     !statusResult.Contains("Error");
            
            if (loadedSuccessfully)
            {
                var resultPanel = new Panel(Align.Center(new Markup($"[green]{result}[/]")))
                    .Border(BoxBorder.Rounded)
                    .BorderColor(Color.Green)
                    .Header("[green bold]✓ SUCCESS[/]")
                    .HeaderAlignment(Justify.Center)
                    .Padding(1, 0);
                AnsiConsole.Write(Align.Center(resultPanel));
            }
            else
            {
                ShowError($"Failed to load maze from '{filename}'!\nFile may be corrupted or invalid format.");
            }
        }

        private static void SaveMaze()
        {
            SmoothClear();
            ShowAppTitle();
            AnsiConsole.WriteLine();

            var headerPanel = new Panel(Align.Center(new Markup("[cyan1 bold]💾 SAVE CURRENT MAZE[/]")))
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Cyan1)
                .Padding(1, 0);
            AnsiConsole.Write(Align.Center(headerPanel));
            AnsiConsole.WriteLine();

            // Check if maze is loaded
            var statusResult = RunCommand("current");
            if (statusResult.Contains("No maze loaded"))
            {
                ShowError("No maze loaded! Generate or load a maze first.");
                return;
            }

            var useCustomName = AnsiConsole.Confirm(
                "[cyan1]Save with custom filename?[/]", 
                false
            );
            
            string command;
            string filename;
            if (useCustomName)
            {
                filename = AnsiConsole.Prompt(
                    new TextPrompt<string>("[cyan1]Enter filename[/]:")
                        .DefaultValue("my_maze.txt")
                );
                command = $"save {filename}";
            }
            else
            {
                filename = "maze.txt";
                command = "save";
            }

            string result = "";
            AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots12)
                .SpinnerStyle(Style.Parse("cyan1 bold"))
                .Start($"[cyan1]💾 Saving maze to '{filename}'...[/]", ctx =>
                {
                    var stopwatch = Stopwatch.StartNew();
                    result = RunCommand(command);
                    EnsureMinimumDuration(stopwatch);
                });

            AnsiConsole.WriteLine();
            var resultPanel = new Panel(Align.Center(new Markup($"[green]{result}[/]")))
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Green)
                .Header("[green bold]✓ SUCCESS[/]")
                .HeaderAlignment(Justify.Center)
                .Padding(1, 0);
            AnsiConsole.Write(Align.Center(resultPanel));
        }

        private static void FindPath()
        {
            SmoothClear();
            ShowAppTitle();
            AnsiConsole.WriteLine();

            var headerPanel = new Panel(Align.Center(new Markup("[cyan1 bold]🔍 FIND PATH (A*)[/]")))
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Cyan1)
                .Padding(1, 0);
            AnsiConsole.Write(Align.Center(headerPanel));
            AnsiConsole.WriteLine();

            // Check current maze state
            var statusResult = RunCommand("current");
            if (statusResult.Contains("No maze loaded"))
            {
                ShowError("No valid maze loaded! Generate or load a maze first.");
                return;
            }

            string result = "";
            AnsiConsole.Status()
                .Spinner(Spinner.Known.Arc)
                .SpinnerStyle(Style.Parse("green bold"))
                .Start("[green]🧠 Computing optimal path...[/]", ctx =>
                {
                    var stopwatch = Stopwatch.StartNew();
                    result = RunCommand("find");
                    EnsureMinimumDuration(stopwatch);
                });

            AnsiConsole.WriteLine();
            
            if (result.Contains("ERROR: No path found") || result.Contains("No path found!"))
            {
                var resultPanel = new Panel(Align.Center(new Markup($"[red]{result}[/]")))
                    .Border(BoxBorder.Rounded)
                    .BorderColor(Color.Red)
                    .Header("[red bold]✗ NO PATH[/]")
                    .HeaderAlignment(Justify.Center)
                    .Padding(1, 0);
                AnsiConsole.Write(Align.Center(resultPanel));
            }
            else
            {
                var resultPanel = new Panel(Align.Center(new Markup($"[green]{result}[/]")))
                    .Border(BoxBorder.Rounded)
                    .BorderColor(Color.Green)
                    .Header("[green bold]✓ PATH FOUND[/]")
                    .HeaderAlignment(Justify.Center)
                    .Padding(1, 0);
                AnsiConsole.Write(Align.Center(resultPanel));
            }
        }

        private static void PrintMaze()
        {
            SmoothClear();
            ShowAppTitle();
            AnsiConsole.WriteLine();

            var headerPanel = new Panel(Align.Center(new Markup("[cyan1 bold]🖨️ CURRENT MAZE[/]")))
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Cyan1)
                .Padding(1, 0);
            AnsiConsole.Write(Align.Center(headerPanel));
            AnsiConsole.WriteLine();

            // Check current maze state
            var statusResult = RunCommand("current");
            if (statusResult.Contains("No maze loaded"))
            {
                ShowError("No maze loaded! Generate or load a maze first.");
                return;
            }

            var result = RunCommand("print");
            var mazePanel = new Panel(Align.Center(new Markup(result)))
                .Border(BoxBorder.Double)
                .BorderColor(Color.Cyan1)
                .Padding(1, 0);
            AnsiConsole.Write(Align.Center(mazePanel));
        }

        private static void ShowStatus()
        {
            SmoothClear();
            ShowAppTitle();
            AnsiConsole.WriteLine();

            var headerPanel = new Panel(Align.Center(new Markup("[cyan1 bold]📊 MAZE STATISTICS[/]")))
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Cyan1)
                .Padding(1, 0);
            AnsiConsole.Write(Align.Center(headerPanel));
            AnsiConsole.WriteLine();

            var result = RunCommand("current");
            var statusPanel = new Panel(Align.Center(new Markup(result)))
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Grey)
                .Padding(1, 0);
            AnsiConsole.Write(Align.Center(statusPanel));
        }

        private static void RaceMode()
        {
            // Check if maze is properly loaded
            var statusResult = RunCommand("current");
            if (statusResult.Contains("No maze loaded"))
            {
                ShowError("No valid maze loaded! Generate or load a maze first.");
                Thread.Sleep(2000);
                return;
            }

            // Verify maze has a path
            var pathResult = RunCommand("find");
            if (pathResult.Contains("ERROR: No path found") || pathResult.Contains("No path found!"))
            {
                ShowError("Current maze has no valid path! Cannot start race mode.");
                Thread.Sleep(2000);
                return;
            }

            bool showControl = true;
            string lastMazeState = "";
            bool needsRedraw = true;

            while (true)
            {
                var hasRace = File.Exists(RaceActiveFile);
                
                if (
                    CheckConsoleResize())
                {
                    needsRedraw = true;
                }

                if (needsRedraw)
                {
                    ShowRaceMenu(showControl, hasRace, ref lastMazeState);
                    needsRedraw = false;
                }

                if (!Console.KeyAvailable)
                {
                    Thread.Sleep(100); 
                    continue;
                }

                var key = Console.ReadKey(true);

                if (key.Key == ConsoleKey.Tab)
                {
                    showControl = !showControl;
                    needsRedraw = true;
                    continue;
                }

                if (key.Key == ConsoleKey.Escape)
                {
                    break;
                }

                try
                {
                    string choice = GetChoiceFromKey(key, showControl);
                    
                    if (string.IsNullOrEmpty(choice))
                        continue;

                    // Handle movement without active race
                    if (!showControl && !hasRace)
                    {
                        SmoothTransition(() =>
                        {
                            SmoothClear();
                            AnsiConsole.WriteLine();
                            ShowError("No active race! Start a race first.");
                        });
                        PressAnyKey();
                        showControl = true;
                        needsRedraw = true;
                        continue;
                    }

                    string result = "";
                    bool needsPause = true;
                    bool raceFinished = false;

                    switch (choice)
                    {
                        case "START":
                            result = RunCommand("race_start");
                            needsPause = true;
                            lastMazeState = "";
                            needsRedraw = true;
                            break;
                        case "RESET":
                            result = RunCommand("race_reset");
                            needsPause = true;
                            lastMazeState = "";
                            needsRedraw = true;
                            break;
                        case "RESULTS":
                            ViewRaceResults();
                            needsPause = false;
                            needsRedraw = true;
                            break;
                        case "UP":
                        case "DOWN":
                        case "LEFT":
                        case "RIGHT":
                            result = RunCommand($"race_{choice.ToLower()}");
                            needsPause = false;
                            needsRedraw = true;
                            break;
                    }

                    // Check if race finished after movement
                    if (!File.Exists(RaceActiveFile) && hasRace && !showControl)
                    {
                        raceFinished = true;
                    }

                    // Show result message for control actions
                    if (needsPause && showControl && !string.IsNullOrEmpty(result))
                    {
                        SmoothTransition(() =>
                        {
                            SmoothClear();
                            AnsiConsole.WriteLine();
                            var resultPanel = new Panel(Align.Center(new Markup($"[cyan1]{result}[/]")))
                                .Border(BoxBorder.Rounded)
                                .BorderColor(Color.Cyan1)
                                .Header("[cyan1 bold]INFO[/]")
                                .HeaderAlignment(Justify.Center)
                                .Padding(1, 0);
                            AnsiConsole.Write(Align.Center(resultPanel));
                        });
                        PressAnyKey();
                        needsRedraw = true;
                    }
                    
                    // Show results if race finished
                    if (raceFinished)
                    {
                        PressAnyKey();
                        ViewRaceResults();
                        showControl = true;
                        lastMazeState = "";
                        needsRedraw = true;
                    }
                }
                catch (Exception ex)
                {
                    SmoothTransition(() =>
                    {
                        SmoothClear();
                        AnsiConsole.WriteLine();
                        ShowError(ex.Message);
                    });
                    PressAnyKey();
                    needsRedraw = true;
                }
            }
        }

        private static void ShowRaceMenu(bool showControl, bool hasRace, ref string lastMazeState)
        {
            string currentMazeState = "";
            
            // Get current maze state if race is active
            if (hasRace)
            {
                currentMazeState = RunCommand("race_state");
            }

            // Redraw screen
            SmoothClear();
            AnsiConsole.WriteLine();

            var titlePanel = new Panel(Align.Center(new Markup("[yellow bold]🏁 RACE MODE[/]")))
                .Border(BoxBorder.Heavy)
                .BorderColor(Color.Yellow)
                .Padding(1, 0);
            AnsiConsole.Write(Align.Center(titlePanel));
            AnsiConsole.WriteLine();

            // Show maze state if race is active
            if (hasRace && !string.IsNullOrWhiteSpace(currentMazeState))
            {
                var mazePanel = new Panel(Align.Center(new Markup(currentMazeState)))
                    .Border(BoxBorder.Rounded)
                    .BorderColor(Color.Cyan1)
                    .Padding(1, 0);
                AnsiConsole.Write(Align.Center(mazePanel));
                AnsiConsole.WriteLine();
            }

            var statusBadge = hasRace ? "[green bold]ACTIVE[/]" : "[grey bold]INACTIVE[/]";
            var statusPanel = new Panel(Align.Center(new Markup($"Race Status: {statusBadge}")))
                .Border(BoxBorder.Rounded)
                .BorderColor(hasRace ? Color.Green : Color.Grey)
                .Padding(1, 0);
            AnsiConsole.Write(Align.Center(statusPanel));
            AnsiConsole.WriteLine();

            // Show current menu - horizontal layout
            if (showControl)
            {
                var controlPanel = new Panel(
                    Align.Center(
                        new Markup(
                            "[yellow bold]🎮 RACE CONTROL[/]\n\n" +
                            "[dim]1[/] 🚀 Start New Race    [dim]2[/] 🔄 Reset Race    [dim]3[/] 🏆 View Results"
                        )
                    )
                )
                    .Border(BoxBorder.Rounded)
                    .BorderColor(Color.Yellow)
                    .Padding(2, 1);
                AnsiConsole.Write(Align.Center(controlPanel));
            }
            else
            {
                var movementPanel = new Panel(
                    Align.Center(
                        new Markup(
                            "[cyan1 bold]🕹️ MOVEMENT[/]\n\n" +
                            "[dim]↑[/] Move Up    [dim]↓[/] Move Down    [dim]←[/] Move Left    [dim]→[/] Move Right"
                        )
                    )
                )
                    .Border(BoxBorder.Rounded)
                    .BorderColor(Color.Cyan1)
                    .Padding(2, 1);
                AnsiConsole.Write(Align.Center(movementPanel));
            }

            AnsiConsole.WriteLine();
            var hintPanel = new Panel(
                Align.Center(new Markup("[dim]Press [cyan1 bold]TAB[/] to switch menus | [red bold]ESC[/] to exit[/]"))
            )
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Grey)
                .Padding(1, 0);
            AnsiConsole.Write(Align.Center(hintPanel));

            lastMazeState = currentMazeState;
        }

        private static string GetChoiceFromKey(ConsoleKeyInfo key, bool isControlMenu)
        {
            if (isControlMenu)
            {
                return key.KeyChar switch
                {
                    '1' => "START",
                    '2' => "RESET",
                    '3' => "RESULTS",
                    _ => ""
                };
            }
            else
            {
                return key.Key switch
                {
                    ConsoleKey.UpArrow => "UP",
                    ConsoleKey.DownArrow => "DOWN",
                    ConsoleKey.LeftArrow => "LEFT",
                    ConsoleKey.RightArrow => "RIGHT",
                    _ => ""
                };
            }
        }

        private static void ViewRaceResults()
        {
            SmoothTransition(() =>
            {
                SmoothClear();
                AnsiConsole.WriteLine();

                var headerPanel = new Panel(Align.Center(new Markup("[gold1 bold]🏆 RACE RESULTS[/]")))
                    .Border(BoxBorder.Heavy)
                    .BorderColor(Color.Gold1)
                    .Padding(1, 0);
                AnsiConsole.Write(Align.Center(headerPanel));
                AnsiConsole.WriteLine();

                if (!File.Exists(RaceResultsFile))
                {
                    var noResultsPanel = new Panel(
                        Align.Center(
                            new Markup("[yellow]No race results found yet.[/]\n[dim]Complete a race to see your statistics![/]")
                        )
                    )
                        .Border(BoxBorder.Rounded)
                        .BorderColor(Color.Yellow)
                        .Padding(2, 1);
                    AnsiConsole.Write(Align.Center(noResultsPanel));
                }
                else
                {
                    var results = File.ReadAllText(RaceResultsFile);
                    var resultsPanel = new Panel(Align.Center(new Markup(results)))
                        .Border(BoxBorder.Double)
                        .BorderColor(Color.Gold1)
                        .Padding(1, 0);
                    AnsiConsole.Write(Align.Center(resultsPanel));
                }
            });

            PressAnyKey();
        }

        private static void ShowError(string message)
        {
            var errorPanel = new Panel(Align.Center(new Markup($"[red bold]⚠️ {message}[/]")))
                .Border(BoxBorder.Heavy)
                .BorderColor(Color.Red)
                .Header("[red bold]ERROR[/]")
                .HeaderAlignment(Justify.Center)
                .Padding(1, 0);
            AnsiConsole.Write(Align.Center(errorPanel));
        }

        private static void PressAnyKey()
        {
            AnsiConsole.WriteLine();
            AnsiConsole.Write(Align.Center(new Markup("[dim]Press [cyan1 bold]any key[/] to continue...[/]")));
            Console.ReadKey(true);
        }

        private static void EnsureMinimumDuration(Stopwatch stopwatch)
        {
            var elapsed = stopwatch.ElapsedMilliseconds;
            if (elapsed < SpinnerMinDuration)
            {
                Thread.Sleep((int)(SpinnerMinDuration - elapsed));
            }
        }

        private static void SmoothClear()
        {
            AnsiConsole.Clear();
        }

        private static void SmoothTransition(Action renderAction)
        {
            Thread.Sleep(300);
            
            // Render new content
            renderAction();
            
            // Small delay for smooth perception
            Thread.Sleep(300);
        }

        private static string RunCommand(string arguments)
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
                return $"[red]Error: {ex.Message}[/]";
            }
        }
    }
}