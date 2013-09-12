using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Shapes;
using Windows.UI.Xaml.Media;

namespace Match3
{
    class BackgroundStar : AnimatingElement
    {
        private static Random random = new Random();
        public double x { get; set; }
        public double y { get; set; }
        public double speed { get; set; }

        public BackgroundStar()
        {
            this.x = random.Next(0, 1366);
            this.y = random.Next(0, 768);
            this.speed = random.Next(2, 7);
            Rectangle r = new Rectangle();
            r.Width = speed / 2;
            r.Height = speed;
            r.Fill = new SolidColorBrush(Windows.UI.Color.FromArgb(
                (byte)(255 - (8 - speed) * 30),
                (byte)(random.Next(200, 255)),
                (byte)(random.Next(200, 255)),
                (byte)(random.Next(200, 255))));
            Canvas.SetZIndex(r, (int)speed);
            Canvas.SetLeft(r, x);
            background = r;
        }

        public override void Animate(long time, long delta, double ddelta)
        {
            y = y + speed * ddelta * 5;
            if (y > 768) y = -10;
            Canvas.SetTop(background, y);
        }
    }
}
