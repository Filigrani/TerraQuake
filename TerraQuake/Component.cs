using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerraQuake
{
    abstract class Component
    {
        public string name = "unknown";
        public GameObject Object = null;

        public virtual void Update(GameTime gameTime) { }
        public virtual void OnDestroy() { }
        public virtual void OnAttached() { }
        public virtual void OnAnimationFinished(GameTime gameTime, string AnimationName, string SenderName) { }
    }
}
