using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;
using System.Diagnostics;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Match3
{
    delegate void EndOffAnimationDelegate(AnimatingElement ae);

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        Random random;
        Stopwatch timer;
        long frameCounter;
        long lastFrame, currentFrame, lastAnimFrame;
        long animGap = 1000 / 30;

        List<AnimatingElement> animList = new List<AnimatingElement>();
        int boardX, boardY;
        static int columns = 8;
        static int rows = 8;
        GamePiece[,] board = new GamePiece[columns, rows];
        static int pieceSize = 64;
        static int pieceGap = 4;
        static int blockSize = pieceSize + pieceGap;
        static int colorCount = 8;
        Brush[] colors = new Brush[colorCount];

        static int NONE = 0;
        static int SELECTED = 1;
        static int SWAP = 2;

        int state = NONE;
        int sourceX, sourceY, targetX, targetY;

        public MainPage()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.  The Parameter
        /// property is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            random = new Random();
            CreateStarField();
            boardX = 1366 / 2 - (columns * blockSize) / 2;
            boardY = 768 / 2 - (rows * blockSize) / 2;
            CreateColorSet();

            for (int ix = 0; ix < columns; ix++)
            {
                for (int iy = 0; iy < rows; iy++)
                {
                    GamePiece g = CreateGamePiece();
                    SetPiece(g, ix, iy);
                }
            }
            CompositionTarget.Rendering += OnRender;
            if (CheckMatches(false) > 0)
            {
                DropPieces();
            }
        }

        private void CreateColorSet()
        {
            colors[0] = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 200, 0, 50));
            colors[1] = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 240, 90, 0));
            colors[2] = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 240, 200, 50));
            colors[3] = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 90, 180, 50));
            colors[4] = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 180, 200));
            colors[5] = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 100, 0, 180));
            colors[6] = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 240, 50, 180));
            colors[7] = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 50, 50, 50));
        }

        private void SetPiece(GamePiece g, int x, int y)
        {
            Canvas.SetLeft(g.background, boardX + x * blockSize);
            Canvas.SetTop(g.background, boardY + y * blockSize);
            board[x, y] = g;
        }

        private GamePiece CreateGamePiece()
        {
            int color = random.Next(0, colorCount);
            Rectangle r = new Rectangle();
            r.Width = pieceSize;
            r.Height = pieceSize;
            r.Fill = colors[color];
            GamePiece g = new GamePiece();
            g.color = color;
            g.done = false;
            g.background = r;
            Canvas.SetZIndex(r, 10);
            gameCanvas.Children.Add(g.background);
            animList.Add(g);
            return g;
        }

        private void CreateStarField()
        {
            for (int i = 0; i < 256; i++)
            {
                BackgroundStar star = new BackgroundStar();
                gameCanvas.Children.Add(star.background);
                animList.Add(star);
            }
        }

        private void SwapPieces()
        {
            state = SWAP;
            GamePiece g1 = board[sourceX, sourceY];
            GamePiece g2 = board[targetX, targetY];
            board[sourceX, sourceY] = g2;
            board[targetX, targetY] = g1;
            g1.SlideTo(boardX + targetX * blockSize, boardY + targetY * blockSize, null);
            g2.SlideTo(boardX + sourceX * blockSize, boardY + sourceY * blockSize, new EndOffAnimationDelegate(SwapEnded));
        }

        private int GetColor(int x, int y)
        {
            if (x < 0 || x >= columns) return -1;
            if (y < 0 || y >= rows) return -1;
            if (board[x, y] == null) return -1;
            return board[x, y].color;
        }

        private void CheckMatch(int x, int y)
        {
            int c = GetColor(x, y);
            // XOX
            if (GetColor(x - 1, y) == c && GetColor(x + 1, y) == c)
            {
                board[x, y].mark = true; board[x - 1, y].mark = true; board[x + 1, y].mark = true; return;
            }
            // XXO and OXX will also be caught by XOX

            // X
            // O
            // X
            if (GetColor(x, y - 1) == c && GetColor(x, y + 1) == c)
            {
                board[x, y].mark = true; board[x, y - 1].mark = true; board[x, y + 1].mark = true; return;
            }
            // This will catch the two other options like the previous

            // X.
            // OX
            if (GetColor(x, y - 1) == c && GetColor(x + 1, y) == c)
            {
                board[x, y].mark = true; board[x, y - 1].mark = true; board[x + 1, y].mark = true; return;
            }
            // .X
            // XO
            if (GetColor(x, y - 1) == c && GetColor(x - 1, y) == c)
            {
                board[x, y].mark = true; board[x, y - 1].mark = true; board[x - 1, y].mark = true; return;
            }
            // OX
            // X.
            if (GetColor(x, y + 1) == c && GetColor(x + 1, y) == c)
            {
                board[x, y].mark = true; board[x, y + 1].mark = true; board[x + 1, y].mark = true; return;
            }
            // XO
            // .X
            if (GetColor(x, y + 1) == c && GetColor(x - 1, y) == c)
            {
                board[x, y].mark = true; board[x, y + 1].mark = true; board[x - 1, y].mark = true; return;
            }

        }

        private int CheckMatches(bool checkSwap)
        {
            // first remove any marks
            for (int iy = 0; iy < rows; iy++)
            {
                for (int ix = 0; ix < columns; ix++)
                {
                    if (board[ix, iy] != null) board[ix, iy].mark = false;
                }
            }
            // go through each color looking for matches
            for (int i = 0; i < colorCount; i++)
            {
                for (int iy = 0; iy < rows; iy++)
                {
                    for (int ix = 0; ix < columns; ix++)
                    {
                        if (GetColor(ix, iy) == i)
                        {
                            CheckMatch(ix, iy);
                        }
                    }
                }
            }
            // check that one of the swapped pieces were part of a match
            if (checkSwap)
            {
                if (!(board[sourceX, sourceY].mark || board[targetX, targetY].mark))
                {
                    return 0;
                }
            }
            // go through and remove matches
            int removed = 0;
            for (int iy = 0; iy < rows; iy++)
            {
                for (int ix = 0; ix < columns; ix++)
                {
                    if (board[ix, iy] != null && board[ix, iy].mark)
                    {
                        board[ix, iy].done = true;
                        // animList.Remove(board[ix,iy]);
                        board[ix, iy] = null;
                        removed++;
                    }
                }
            }
            return removed;
        }

        private void AddNewPieces()
        {
            for (int ix = 0; ix < columns; ix++)
            {
                for (int iy = rows - 1; iy >= 0; iy--)
                {
                    if (board[ix, iy] == null)
                    {

                        GamePiece g = CreateGamePiece();
                        Canvas.SetLeft(g.background, boardX + ix * (pieceSize + pieceGap));
                        Canvas.SetTop(g.background, boardY - (pieceSize + pieceGap));
                        board[ix, iy] = g;
                        g.SlideTo(boardX + ix * (pieceSize + pieceGap), boardY + iy * (pieceSize + pieceGap), null);

                    }
                }
            }
        }

        private void DropEnded(AnimatingElement ae)
        {
            this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                AddNewPieces();

                if (CheckMatches(false) > 0)
                {
                    DropPieces();
                }
                else
                {
                    state = NONE;
                }
            });
        }

        private void DropPieces()
        {
            EndOffAnimationDelegate dropCallBack = new EndOffAnimationDelegate(DropEnded);
            // Slide all pieces down, use callback to fill in new and run a checkMatches
            for (int ix = 0; ix < columns; ix++)
            {
                for (int iy = rows - 1; iy >= 0; iy--)
                {
                    if (board[ix, iy] == null)
                    {
                        for (int i = iy - 1; i >= 0; i--)
                        {
                            if (board[ix, i] != null)
                            {
                                board[ix, iy] = board[ix, i];
                                board[ix, i] = null;
                                board[ix, iy].SlideTo(boardX + ix * (pieceSize + pieceGap), boardY + iy * (pieceSize + pieceGap), dropCallBack);
                                dropCallBack = null;
                                break;
                            }
                        }
                    }
                }
            }
            if (dropCallBack != null) dropCallBack(null);
        }

        private void SwapEnded(AnimatingElement ae)
        {
            if (CheckMatches(true) > 0)
            {
                DropPieces();
            }
            else
            {
                GamePiece g1 = board[sourceX, sourceY];
                GamePiece g2 = board[targetX, targetY];
                board[sourceX, sourceY] = g2;
                board[targetX, targetY] = g1;
                g1.SlideTo(boardX + targetX * blockSize, boardY + targetY * blockSize, null);
                g2.SlideTo(boardX + sourceX * blockSize, boardY + sourceY * blockSize, null);
                state = NONE;
            }
        }

        private void PointerMoved(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {

        }
        private void PointerPressed(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            Windows.UI.Input.PointerPoint pointer = e.GetCurrentPoint(gameCanvas);
            Point p = pointer.Position;
            int px = (int)p.X;
            int py = (int)p.Y;
            if (px > boardX && px < boardX + columns * blockSize)
            {
                if (py > boardY && py < boardY + rows * blockSize)
                {
                    int bx = (px - boardX) / blockSize;
                    int by = (py - boardY) / blockSize;
                    System.Diagnostics.Debug.WriteLine("Pressed " + px + "," + py + " -> " + bx + "," + by);
                    if (state == NONE)
                    {
                        state = SELECTED;
                        sourceX = bx;
                        sourceY = by;
                    }
                }
            }
        }

        private void PointerReleased(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (state == SELECTED)
            {
                Windows.UI.Input.PointerPoint pointer = e.GetCurrentPoint(gameCanvas);
                Point p = pointer.Position;
                int px = (int)p.X;
                int py = (int)p.Y;
                if (px > boardX && px < boardX + columns * blockSize)
                {
                    if (py > boardY && py < boardY + rows * blockSize)
                    {
                        int bx = (px - boardX) / blockSize;
                        int by = (py - boardY) / blockSize;
                        if (bx >= sourceX - 1 && bx <= sourceX + 1)
                        {
                            if (by >= sourceY - 1 && by <= sourceY + 1)
                            {
                                // We are in the right box of squares, check that we have not moved diagonally
                                if (bx == sourceX || by == sourceY)
                                {
                                    // Check that we haven't released in the same square
                                    if (!(bx == sourceX && by == sourceY))
                                    {
                                        System.Diagnostics.Debug.WriteLine("Correct release, swap: " + sourceX + "," + sourceY + " -> " + bx + "," + by);
                                        targetX = bx;
                                        targetY = by;
                                        SwapPieces();
                                        return;
                                    }
                                }
                            }
                        }
                    }
                }
                // Invalid release, drop selection
                state = NONE;
            }
        }

        private void OnRender(object sender, object e)
        {
            if (timer == null) timer = Stopwatch.StartNew();
            frameCounter++;
            frameRate.Text = "FPS : " + (int)(frameCounter / timer.Elapsed.TotalSeconds);
            currentFrame = timer.ElapsedMilliseconds;


            if (currentFrame - lastAnimFrame >= animGap)
            {


                List<AnimatingElement> deleteList = null;
                if (animList.Count > 0)
                {
                    long delta = currentFrame - lastAnimFrame;
                    double ddelta = delta / 1000.0;
                    foreach (AnimatingElement ae in animList)
                    {

                        ae.Animate(currentFrame, delta, ddelta);
                        if (ae.ShouldRemove())
                        {
                            if (deleteList == null) deleteList = new List<AnimatingElement>();
                            deleteList.Add(ae);
                        }
                    }
                }
                if (deleteList != null)
                {
                    foreach (AnimatingElement ae in deleteList)
                    {
                        animList.Remove(ae);
                        if (ae.background != null)
                        {
                            gameCanvas.Children.Remove(ae.background);
                        }
                    }
                }
                lastAnimFrame = currentFrame;
            }
            lastFrame = currentFrame;
        }


    }
}
