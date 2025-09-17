using System;
using OpenTK.Windowing.Desktop;

namespace PongGame
{
    class Program
    {
        // Entry point of the application
        static void Main(string[] args)
        {
            // Configure window settings for OpenGL context
            var nativeWindowSettings = new NativeWindowSettings()
            {
                Size = new OpenTK.Mathematics.Vector2i(800, 600), // Set window size to 800x600 pixels
                Title = "Pong", // Set window title
                APIVersion = new Version(3, 3), // Specify OpenGL version 3.3
                Flags = OpenTK.Windowing.Common.ContextFlags.ForwardCompatible // Use forward-compatible context for modern OpenGL
            };

            // Create and run the game window
            using (var game = new PongGame(GameWindowSettings.Default, nativeWindowSettings))
            {
                game.Run(); // Start the game loop (handles rendering and updates)
            }
        }
    }
}