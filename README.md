Pong OpenTK Game
========

A simple 2D Pong game implemented in C# using OpenTK for OpenGL rendering. Features two-player controls (W/S for Player 1, Up/Down for Player 2), scoring to 5 points, and text rendering for UI.

Requirements
------------

*   .NET 6.0 or later (tested on .NET 8.0).
*   OpenTK 4.x (via NuGet).
*   System.Drawing.Common (via NuGet; Windows-only for full text rendering; on non-Windows, install libgdiplus via package manager, e.g., `sudo apt install libgdiplus` on Ubuntu).
*   Visual Studio 2022 or VS Code with C# extension (for development).

Project Structure
-----------------

        PongGame/

                ├── PongGame.csproj          # Project file with NuGet packages

                ├── Program.cs               # Entry point: Sets up window and runs game

                ├── PongGame.cs              # Main game class: Handles rendering, input, and logic

                ├── README.md                # This file

                ├── .gitignore               # Git ignore rules

                └── LICENSE                  # (Optional) MIT License or similar
    

Installation
------------

1.  Clone the repo:
    
        git clone https://github.com/mouraleonardo/PongGame.git
        cd PongGame
    
2.  Restore packages:
    
        dotnet restore
    

How to Run
----------

1.  Build the project:
    
        dotnet build
    
2.  Run the application:
    
        dotnet run
    

*   Window opens at 800x600 resolution.
*   Press Space to start/restart.
*   Press Escape to quit.
*   Game ends at 5 points; winner displayed.

For Visual Studio: Open `PongGame.sln` and press F5.

Controls
--------

*   Player 1: W (up), S (down)
*   Player 2: Up Arrow (up), Down Arrow (down)
*   Space: Start/Restart
*   Escape: Exit

Libraries
---------

*   **OpenTK**: Cross-platform OpenGL bindings, windowing, and input. Installed via `<PackageReference Include="OpenTK" Version="4.8.2" />`.
*   **System.Drawing.Common**: For text bitmap generation. Installed via `<PackageReference Include="System.Drawing.Common" Version="8.0.0" />`. Note: On non-Windows, runtime may throw `PlatformNotSupportedException` unless libgdiplus is installed.

Building for Release
--------------------

    dotnet publish -c Release -r win-x64 --self-contained

Adjust runtime identifier (RID) for your platform (e.g., `linux-x64`).

Known Issues
------------

*   Text rendering limited on non-Windows without libgdiplus.
*   Uses OpenGL 3.3 core profile; ensure compatible GPU.

Contributing
------------

Fork, branch, PR. Focus on bug fixes or features like AI player.

License
-------

MIT License. See LICENSE file.
