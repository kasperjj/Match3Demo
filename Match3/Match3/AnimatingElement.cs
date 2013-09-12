using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace Match3
{
    class AnimatingElement
    {
        public UIElement background { get; set; }
        public virtual void Animate(long time, long delta, double ddelta)
        {
        }
        public virtual bool ShouldRemove()
        {
            return false;
        }
    }
}
