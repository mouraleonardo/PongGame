using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Text;

namespace PongGame
{
    public class PongGame : GameWindow
    {
        // ----- Game state -----
        private float paddle1Y = 0f; // Y-position of player 1's paddle
        private float paddle2Y = 0f; // Y-position of player 2's paddle
        private float ballX = 0f; // Ball X-position
        private float ballY = 0f; // Ball Y-position
        private float ballVelX = 0.008f; // Ball X-velocity
        private float ballVelY = 0.006f; // Ball Y-velocity
        private int score1 = 0; // Player 1 score
        private int score2 = 0; // Player 2 score
        private bool gameStarted = false; // Game start flag
        private bool gameOver = false; // Game over flag
        private string winner = ""; // Winner message
        private const float PaddleSpeed = 0.05f; // Paddle movement speed
        private const float PaddleHeight = 0.3f; // Paddle height
        private const float PaddleWidth = 0.05f; // Paddle width
        private const float BallSize = 0.03f; // Ball size

        // ----- Rendering objects -----
        private int vao, vbo, shaderProgram; // Vertex Array Object, Vertex Buffer Object, and shader program for paddles/ball
        private int textVAO, textVBO, textShaderProgram; // VAO, VBO, and shader program for text rendering

        // Constructor: Initialize window with VSync enabled
        public PongGame(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
            : base(gameWindowSettings, nativeWindowSettings)
        {
            VSync = VSyncMode.On; // Enable vertical sync to prevent screen tearing
        }

        // Initialize OpenGL resources
        protected override void OnLoad()
        {
            base.OnLoad();
            GL.ClearColor(0f, 0f, 0f, 1f); // Set clear color to black
            GL.Enable(EnableCap.Blend); // Enable alpha blending for text transparency
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha); // Set blend function

            // Define quad vertices for paddles and ball (normalized device coordinates)
            float[] quadVertices = {
                -0.5f, -0.5f, // Bottom-left
                 0.5f, -0.5f, // Bottom-right
                 0.5f, 0.5f,  // Top-right
                -0.5f, 0.5f   // Top-left
            };

            // Setup VAO and VBO for quad rendering
            vao = GL.GenVertexArray(); // Create VAO to store vertex attribute state
            vbo = GL.GenBuffer(); // Create VBO to store vertex data
            GL.BindVertexArray(vao); // Bind VAO
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo); // Bind VBO
            GL.BufferData(BufferTarget.ArrayBuffer, quadVertices.Length * sizeof(float), quadVertices, BufferUsageHint.StaticDraw); // Upload quad vertices
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), 0); // Define vertex attribute layout
            GL.EnableVertexAttribArray(0); // Enable vertex attribute
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0); // Unbind VBO
            GL.BindVertexArray(0); // Unbind VAO

            // Vertex shader for paddles/ball: Scales and translates quad
            string vertexShaderSrc = @"
                #version 330 core
                layout (location = 0) in vec2 aPosition;
                uniform vec2 uPosition; // Object position
                uniform vec2 uScale; // Object scale
                void main()
                {
                    gl_Position = vec4(aPosition * uScale + uPosition, 0.0, 1.0); // Transform vertices to NDC
                }";

            // Fragment shader for paddles/ball: Solid white color
            string fragmentShaderSrc = @"
                #version 330 core
                out vec4 FragColor;
                void main()
                {
                    FragColor = vec4(1.0, 1.0, 1.0, 1.0); // White color
                }";

            shaderProgram = CreateProgram(vertexShaderSrc, fragmentShaderSrc); // Compile and link shaders

            // Vertex shader for text: Applies model matrix and passes texture coordinates
            string textVertexShaderSrc = @"
                #version 330 core
                layout(location=0) in vec2 aPos;
                layout(location=1) in vec2 aTex;
                uniform mat4 model; // Transformation matrix
                out vec2 TexCoord; // Texture coordinates
                void main()
                {
                    gl_Position = model * vec4(aPos,0.0,1.0); // Transform to NDC
                    TexCoord = aTex; // Pass texture coords to fragment shader
                }";

            // Fragment shader for text: Samples texture
            string textFragmentShaderSrc = @"
                #version 330 core
                in vec2 TexCoord;
                out vec4 FragColor;
                uniform sampler2D tex0; // Text texture
                void main()
                {
                    FragColor = texture(tex0, TexCoord); // Sample texture
                }";

            textShaderProgram = CreateProgram(textVertexShaderSrc, textFragmentShaderSrc); // Compile and link text shaders

            // Setup text quad vertices (position + texture coords)
            float[] textVertices = {
                0f, 0f, 0f, 1f, // Bottom-left
                1f, 0f, 1f, 1f, // Bottom-right
                1f, 1f, 1f, 0f, // Top-right
                0f, 0f, 0f, 1f, // Bottom-left (triangle 1)
                1f, 1f, 1f, 0f, // Top-right (triangle 1)
                0f, 1f, 0f, 0f  // Top-left (triangle 2)
            };

            // Setup VAO and VBO for text rendering
            textVAO = GL.GenVertexArray();
            textVBO = GL.GenBuffer();
            GL.BindVertexArray(textVAO);
            GL.BindBuffer(BufferTarget.ArrayBuffer, textVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, textVertices.Length * sizeof(float), textVertices, BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0); // Position attribute
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float)); // Texture coord attribute
            GL.EnableVertexAttribArray(1);
            GL.BindVertexArray(0);
        }

        // Create and link shader program
        private int CreateProgram(string vsSrc, string fsSrc)
        {
            int vs = GL.CreateShader(ShaderType.VertexShader); // Create vertex shader
            GL.ShaderSource(vs, vsSrc); // Set source
            GL.CompileShader(vs); // Compile
            GL.GetShader(vs, ShaderParameter.CompileStatus, out int s1); // Check compile status
            if (s1 == 0) Console.WriteLine("VS compile log: " + GL.GetShaderInfoLog(vs)); // Log errors

            int fs = GL.CreateShader(ShaderType.FragmentShader); // Create fragment shader
            GL.ShaderSource(fs, fsSrc);
            GL.CompileShader(fs);
            GL.GetShader(fs, ShaderParameter.CompileStatus, out int s2);
            if (s2 == 0) Console.WriteLine("FS compile log: " + GL.GetShaderInfoLog(fs));

            int prog = GL.CreateProgram(); // Create program
            GL.AttachShader(prog, vs); // Attach shaders
            GL.AttachShader(prog, fs);
            GL.LinkProgram(prog); // Link program
            GL.GetProgram(prog, GetProgramParameterName.LinkStatus, out int linked); // Check link status
            if (linked == 0) Console.WriteLine("Program link log: " + GL.GetProgramInfoLog(prog));

            GL.DeleteShader(vs); // Clean up shaders
            GL.DeleteShader(fs);
            return prog; // Return program ID
        }

        // Create texture from text string
        private int CreateTextTexture(string text, float scale)
        {
            int fontSize = (int)(24 * scale); // Scale font size
            using (Bitmap bmp = new Bitmap(512, 64, System.Drawing.Imaging.PixelFormat.Format32bppArgb)) // Create bitmap
            using (Graphics gfx = Graphics.FromImage(bmp)) // Create graphics context
            using (Font font = new Font("Consolas", fontSize, FontStyle.Bold)) // Use bold Consolas font
            {
                gfx.Clear(Color.Transparent); // Clear with transparent background
                gfx.TextRenderingHint = TextRenderingHint.AntiAlias; // Enable anti-aliasing
                gfx.DrawString(text, font, Brushes.White, 0, 0); // Draw text
                int texture = GL.GenTexture(); // Create texture
                GL.BindTexture(TextureTarget.Texture2D, texture); // Bind texture
                BitmapData data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
                    ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb); // Lock bitmap
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, bmp.Width, bmp.Height, 0,
                    OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0); // Upload texture
                bmp.UnlockBits(data); // Unlock bitmap
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear); // Set filtering
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge); // Set wrapping
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
                GL.BindTexture(TextureTarget.Texture2D, 0); // Unbind texture
                return texture; // Return texture ID
            }
        }

        // Calculate X-position to center text
        private float GetCenteredX(string text, float scale, float textWidth = 512)
        {
            float pxWidth = textWidth * scale; // Scaled text width
            return (Size.X - pxWidth) / 2f; // Center horizontally
        }

        // Render frame
        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);
            GL.Clear(ClearBufferMask.ColorBufferBit); // Clear screen

            // Draw paddles and ball
            GL.UseProgram(shaderProgram); // Use quad shader
            GL.BindVertexArray(vao); // Bind quad VAO
            DrawRect(-1 + PaddleWidth / 2, paddle1Y, PaddleWidth, PaddleHeight); // Draw paddle 1
            DrawRect(1 - PaddleWidth / 2, paddle2Y, PaddleWidth, PaddleHeight); // Draw paddle 2
            if (gameStarted && !gameOver)
                DrawRect(ballX, ballY, BallSize, BallSize); // Draw ball if game is active
            GL.BindVertexArray(0); // Unbind VAO
            GL.UseProgram(0); // Unbind shader

            // Render text
            GL.UseProgram(textShaderProgram); // Use text shader
            GL.BindVertexArray(textVAO); // Bind text VAO
            GL.ActiveTexture(TextureUnit.Texture0); // Activate texture unit
            string textToShow = "";
            float textScale = 1.0f;
            if (!gameStarted && !gameOver)
            {
                textToShow = "Press Space to Start"; // Start prompt
                textScale = 1.0f;
            }
            else if (gameOver)
            {
                textToShow = winner + "\nPress Space to Restart"; // Game over message
                textScale = 1.0f;
            }
            if (!string.IsNullOrEmpty(textToShow))
            {
                string[] lines = textToShow.Split('\n'); // Split multi-line text
                int lineCount = lines.Length;
                for (int i = 0; i < lineCount; i++)
                {
                    string line = lines[i];
                    float textureWidth = 512 * textScale;
                    float textureHeight = 64 * textScale;
                    float x = (Size.X - textureWidth) / 2f; // Center horizontally
                    float y = (Size.Y - (lineCount * textureHeight)) / 2f + i * textureHeight; // Center vertically
                    DrawText(line, x, y, textScale); // Draw text line
                }
            }
            // Draw score during gameplay
            if (gameStarted && !gameOver)
            {
                DrawText(score1.ToString(), Size.X / 2 - 40, 20, 1.2f); // Player 1 score
                DrawText("X", Size.X / 2 - 5, 20, 1.2f); // Separator
                DrawText(score2.ToString(), Size.X / 2 + 30, 20, 1.2f); // Player 2 score
            }
            GL.BindVertexArray(0); // Unbind VAO
            GL.BindTexture(TextureTarget.Texture2D, 0); // Unbind texture
            GL.UseProgram(0); // Unbind shader
            SwapBuffers(); // Swap front/back buffers
        }

        // Draw rectangle (paddle or ball)
        private void DrawRect(float x, float y, float width, float height)
        {
            int posLoc = GL.GetUniformLocation(shaderProgram, "uPosition"); // Get position uniform
            int scaleLoc = GL.GetUniformLocation(shaderProgram, "uScale"); // Get scale uniform
            GL.Uniform2(posLoc, x, y); // Set position
            GL.Uniform2(scaleLoc, width, height); // Set scale
            GL.DrawArrays(PrimitiveType.TriangleFan, 0, 4); // Draw quad
        }

        // Convert pixel coordinates to NDC for text
        private Matrix4 PixelToNDCMatrix(float xPx, float yPx, float scale, float textWidth, float textHeight)
        {
            float wPx = textWidth * scale; // Scaled width
            float hPx = textHeight * scale; // Scaled height
            float pxN = xPx / (Size.X / 2f) - 1f; // Convert X to NDC
            float pyN = (Size.Y - yPx - hPx) / (Size.Y / 2f) - 1f; // Convert Y to NDC
            float sxN = wPx / (Size.X / 2f); // Scale X to NDC
            float syN = hPx / (Size.Y / 2f); // Scale Y to NDC
            return Matrix4.CreateScale(sxN, syN, 1f) * Matrix4.CreateTranslation(pxN, pyN, 0f); // Combine scale and translation
        }

        // Draw text at specified position
        private void DrawText(string text, float xPx, float yPx, float scale)
        {
            int texture = CreateTextTexture(text, scale); // Create text texture
            GL.BindTexture(TextureTarget.Texture2D, texture); // Bind texture
            GL.Uniform1(GL.GetUniformLocation(textShaderProgram, "tex0"), 0); // Set texture unit
            Matrix4 model = PixelToNDCMatrix(xPx, yPx, scale, 512, 64); // Compute transformation
            GL.UniformMatrix4(GL.GetUniformLocation(textShaderProgram, "model"), false, ref model); // Set model matrix
            GL.DrawArrays(PrimitiveType.Triangles, 0, 6); // Draw text quad
            GL.DeleteTexture(texture); // Clean up texture
        }

        // Update game logic
        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);
            var k = KeyboardState; // Get keyboard state
            if (!gameStarted && !gameOver)
            {
                if (k.IsKeyDown(Keys.Space)) { gameStarted = true; ResetBall(); } // Start game
            }
            else if (gameOver)
            {
                if (k.IsKeyDown(Keys.Space)) { score1 = score2 = 0; gameOver = false; gameStarted = false; } // Restart game
            }
            else
            {
                // Move paddles
                if (k.IsKeyDown(Keys.W)) paddle1Y += PaddleSpeed;
                if (k.IsKeyDown(Keys.S)) paddle1Y -= PaddleSpeed;
                if (k.IsKeyDown(Keys.Up)) paddle2Y += PaddleSpeed;
                if (k.IsKeyDown(Keys.Down)) paddle2Y -= PaddleSpeed;
                paddle1Y = Math.Clamp(paddle1Y, -1 + PaddleHeight / 2, 1 - PaddleHeight / 2); // Clamp paddle 1 position
                paddle2Y = Math.Clamp(paddle2Y, -1 + PaddleHeight / 2, 1 - PaddleHeight / 2); // Clamp paddle 2 position

                // Update ball position
                ballX += ballVelX;
                ballY += ballVelY;

                // Ball collision with top/bottom
                if (ballY > 1 - BallSize / 2 || ballY < -1 + BallSize / 2) ballVelY = -ballVelY;

                // Ball collision with paddles
                if (ballX < -1 + PaddleWidth + BallSize / 2 && ballY > paddle1Y - PaddleHeight / 2 && ballY < paddle1Y + PaddleHeight / 2) ballVelX = -ballVelX;
                if (ballX > 1 - PaddleWidth - BallSize / 2 && ballY > paddle2Y - PaddleHeight / 2 && ballY < paddle2Y + PaddleHeight / 2) ballVelX = -ballVelX;

                // Ball out of bounds
                if (ballX < -1) { score2++; ResetBall(); } // Player 2 scores
                if (ballX > 1) { score1++; ResetBall(); } // Player 1 scores

                // Check for game over
                if (score1 >= 5) { gameOver = true; winner = "Player 1 Wins!"; }
                if (score2 >= 5) { gameOver = true; winner = "Player 2 Wins!"; }
            }
            if (k.IsKeyDown(Keys.Escape)) Close(); // Exit on Escape
        }

        // Reset ball to center with random Y-velocity
        private void ResetBall()
        {
            ballX = 0; ballY = 0; // Reset position
            ballVelX = Math.Sign(ballVelX) * 0.008f; // Reset X-velocity
            ballVelY = (new Random().NextDouble() > 0.5 ? 1 : -1) * 0.006f; // Random Y-velocity
        }

        // Clean up OpenGL resources
        protected override void OnUnload()
        {
            if (vbo != 0) GL.DeleteBuffer(vbo); // Delete VBO
            if (vao != 0) GL.DeleteVertexArray(vao); // Delete VAO
            if (shaderProgram != 0) GL.DeleteProgram(shaderProgram); // Delete shader program
            if (textVBO != 0) GL.DeleteBuffer(textVBO); // Delete text VBO
            if (textVAO != 0) GL.DeleteVertexArray(textVAO); // Delete text VAO
            if (textShaderProgram != 0) GL.DeleteProgram(textShaderProgram); // Delete text shader program
            base.OnUnload();
        }
    }
}