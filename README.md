# Compilation Instructions 

## C++ Backend Compilation

### Required Files
- `main.cpp` - Main entry point with race commands
- `maze.cpp` + `maze.h` - Maze generation and management
- `astar.cpp` + `astar.h` - A* pathfinding algorithm
- `racemode.cpp` + `racemode.h` - NEW: Race mode functionality
- `matrix.cpp + matrix.h` - Matrix operations

### Compile Command (MSVC)

```bash
cl /EHsc /std:c++20 /Fe:maze.exe main.cpp matrix.cpp maze.cpp astar.cpp racemode.cpp 
```

### Compile Command (GCC/MinGW)

```bash
g++ -std=c++20 -o maze.exe main.cpp matrix.cpp maze.cpp astar.cpp racemode.cpp
```

### Or compile with included CMake

## C# GUI Compilation

Just build the solution:

```bash
dotnet build
```

Or in Visual Studio: **Build → Build Solution (Ctrl+Shift+B)**

---

## How Race Mode Works

### Architecture
1. **C++ Backend** handles:
   - Maze state management
   - Player movement validation
   - Move counting and timing
   - A* algorithm execution
   - Results comparison

2. **C# GUI** provides:
   - Visual interface with buttons
   - Race mode activation
   - Movement controls (↑↓←→)
   - Console output display

### Race Mode Commands

#### Starting a Race
```bash
maze.exe race_start
```
- Initializes race timer
- Sets player at entrance
- Creates `race_active.tmp` flag file

#### Movement Commands
```bash
maze.exe race_up      # Move up
maze.exe race_down    # Move down
maze.exe race_left    # Move left
maze.exe race_right   # Move right
```

#### Other Commands
```bash
maze.exe race_reset   # Reset current race
maze.exe race_state   # Show current position
```

### Workflow

1. **Generate/Load Maze**
   ```bash
   maze.exe gen 10 15
   ```

2. **GUI: Select "🏁 START RACE MODE"**
   - Race control panel appears
   - Arrow buttons become active

3. **Press START Button**
   - Race begins, timer starts
   - Player at entrance position

4. **Use Arrow Buttons**
   - Navigate through maze
   - Each move is counted
   - Invalid moves are blocked by walls

5. **Reach Exit**
   - Timer stops automatically
   - A* algorithm runs
   - Comparison displayed:
     - Player moves vs A* moves
     - Player time vs A* time  
     - Efficiency percentage
     - Funny comparison messages

6. **Results Saved**
   - Automatically saved to `race_results.txt`
   - Contains detailed statistics

### Output Format

When player finishes:
```
╔════════════════════════════════════════════════╗
║         🏁 RACE RESULTS COMPARISON 🏁          ║
╠════════════════════════════════════════════════╣
║  MOVES:                                        ║
║    👤 Player:   42 moves                       ║
║    🤖 A*:       35 moves (optimal)             ║
║    📊 Your efficiency: 83.3%                   ║
║    👍 GOOD! Decent performance!                ║
╠════════════════════════════════════════════════╣
║  TIME:                                         ║
║    👤 Player:   45.230 sec                     ║
║    🤖 A*:       0.003214 sec                   ║
║    ⚡ A* was 14073x faster!                    ║
╠════════════════════════════════════════════════╣
║  You made 7 extra moves                        ║
╚════════════════════════════════════════════════╝
```

### Performance Ratings

- **100% efficiency** = ⭐ PERFECT! Optimal path found!
- **90-99%** = 🌟 EXCELLENT! Very close to optimal!
- **75-89%** = 👍 GOOD! Decent performance!
- **50-74%** = 📈 Not bad, but room for improvement!
- **<50%** = 💪 Keep practicing!

---

## File Structure

```
project/
├── maze.exe      # Main executable
├── maze_temp.txt            # Current maze state
├── race_active.tmp          # Race mode flag (temp)
├── race_results.txt         # Last race results
└── MazeDemoApp/
    └── Program.cs           # GUI with race controls
```

---

## Testing Race Mode

### Quick Test Sequence

1. **Start Application**
2. **Generate small maze**: "1. Generate New Maze" → `5 5`
3. **Activate Race Mode**: "8. 🏁 START RACE MODE"
4. **Press START button**
5. **Use arrow buttons** to solve
6. **View comparison** when finished

### Tips for Users

- Start with small mazes (5x5, 7x7) to learn
- Try to minimize moves
- Remember path patterns
- Compare with A* to improve strategy

---

## Troubleshooting

### Issue: "No maze loaded for race mode"
**Solution**: Generate or load a maze first (options 1-2)

### Issue: Race buttons not responding
**Solution**: Make sure race is started (press START button)

### Issue: Invalid move errors
**Solution**: Wall is blocking that direction

### Issue: Results not saving
**Solution**: Check write permissions in directory

---

## Future Enhancements (Optional)

- Add difficulty levels
- Implement leaderboard
- Add hints system
- Show A* path after completion
- Add maze replay feature
- Export race statistics to CSV
