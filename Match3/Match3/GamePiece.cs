using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace Match3
{
    class GamePiece : AnimatingElement
    {
        private static int NONE = -1;
        private static int SLIDE = 0;
        private int mode = -1;

        private static int SLIDE_STEPS = 5;
        private double sx, sy, tx, ty, dx, dy;
        private int step = -1;
        private EndOffAnimationDelegate callBack;

        public int color { get; set; }
        public bool mark { get; set; }
        public bool done { get; set; }

        public override void Animate(long time, long delta, double ddelta)
        {
            if (mode == NONE) return;
            step++;
            if (mode == SLIDE)
            {
                if (step > SLIDE_STEPS)
                {
                    Canvas.SetLeft(background, tx);
                    Canvas.SetTop(background, ty);
                    mode = -1;
                    if (callBack != null)
                    {
                        callBack(this);
                    }
                }
                else
                {
                    Canvas.SetLeft(background, sx + dx * step);
                    Canvas.SetTop(background, sy + dy * step);
                }
            }
        }

        public void SlideTo(int x, int y, EndOffAnimationDelegate callBack)
        {
            tx = x;
            ty = y;
            sx = Canvas.GetLeft(background);
            sy = Canvas.GetTop(background);
            dx = (tx - sx) / SLIDE_STEPS;
            dy = (ty - sy) / SLIDE_STEPS;
            step = 0;
            this.callBack = callBack;
            mode = SLIDE;
        }

        public override bool ShouldRemove()
        {
            return done;
        }
    }
}
