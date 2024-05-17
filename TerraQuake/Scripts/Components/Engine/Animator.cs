using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace TerraQuake
{
    internal class Animator : Component
    {
        public class AnimationFrame
        {
            public Texture2D Sprite;
            public Rectangle Bounds = new Rectangle(0, 0, 0, 0);
            public int Miliseconds = 1;
        }
        public class AnimationData
        {
            public string Name = "";
            public List<AnimationFrame> Frames = new List<AnimationFrame>();
            public bool DefaultLoop = false;
            public bool PinPong = false;
            public AnimationData(){}
            public AnimationData(string _Name)
            {
                Name = _Name;
            }

            public void AddFrame(AnimationFrame Frame)
            {
                Frames.Add(Frame);
            }
            public void AddFrame(Texture2D Sprite, Rectangle Bounds, int Miliseconds)
            {
                AnimationFrame Frame = new AnimationFrame();
                Frame.Sprite = Sprite;
                Frame.Bounds = Bounds;
                Frame.Miliseconds = Miliseconds;
                Frames.Add(Frame);
            }
        }
        public class Animation
        {
            public AnimationData Data = new AnimationData();
            public AnimationFrame CurrentFrame = null;
            public int CurrentFrameIndex = 0;
            public bool Playing = false;
            public bool Looped = true;
            public bool Reverse = false;
            public TimeSpan NextFrameTime = TimeSpan.Zero;
            public Animator Animator = null;

            public Animation(AnimationData _Data, Animator _Animator, bool StartReverse = false)
            {
                Data = _Data;
                Animator = _Animator;
                Reverse = StartReverse;

                if (!Reverse)
                {
                    CurrentFrameIndex = 0;
                } else
                {
                    CurrentFrameIndex = Data.Frames.Count - 1;
                }

                CurrentFrame = Data.Frames[CurrentFrameIndex];

                NextFrameTime = Animator.LastUpdate + TimeSpan.FromMilliseconds(CurrentFrame.Miliseconds);
            }

            public void DoPreviousFrame(GameTime gameTime)
            {
                Reverse = !Reverse;
                DoNextFrame(gameTime);
                Reverse = !Reverse;
            }

            public void DoNextFrame(GameTime gameTime)
            {
                AnimationFrame NextFrame = null;
                if (!Reverse)
                {
                    if (CurrentFrameIndex + 1 <= Data.Frames.Count - 1)
                    {
                        NextFrame = Data.Frames[CurrentFrameIndex + 1];
                    }
                } else
                {
                    if (CurrentFrameIndex - 1 >= 0)
                    {
                        NextFrame = Data.Frames[CurrentFrameIndex - 1];
                    }
                }

                if (NextFrame != null)
                {
                    if (!Reverse)
                    {
                        CurrentFrameIndex++;
                    } else
                    {
                        CurrentFrameIndex--;
                    }

                    NextFrameTime = gameTime.TotalGameTime + TimeSpan.FromMilliseconds(NextFrame.Miliseconds);
                } else
                {
                    if (Looped)
                    {
                        if (!Data.PinPong)
                        {
                            CurrentFrameIndex = 0;
                        } else
                        {
                            if (!Reverse)
                            {
                                CurrentFrameIndex--;
                            } else
                            {
                                CurrentFrameIndex++;
                            }
                            Reverse = !Reverse;
                        }
                        NextFrameTime = gameTime.TotalGameTime + TimeSpan.FromMilliseconds(Data.Frames[CurrentFrameIndex].Miliseconds);
                    } else
                    {
                        NextFrameTime = TimeSpan.Zero;
                        Animator.Object.OnAnimationFinished(gameTime, Data.Name, Animator.name);
                        if (Animator.Queue.Count > 0)
                        {
                            Animator.PlayAnimation(Animator.Queue[0], false);
                            Animator.Queue.RemoveAt(0);
                        }
                    }
                }
                CurrentFrame = Data.Frames[CurrentFrameIndex];
            }

            public void Update(GameTime gameTime)
            {
                if (Playing)
                {
                    if (!Animator.ByFrameDebug || Animator.ByPassDebug)
                    {
                        if (NextFrameTime < gameTime.TotalGameTime)
                        {
                            DoNextFrame(gameTime);
                        }
                        Animator.ByPassDebug = false;
                    }
                }
            }
        }

        public Dictionary<string, AnimationData> Animations = new Dictionary<string, AnimationData>();
        public Animation CurrentAnimation;
        public Renderer MyRenderer;
        public List<string> Queue = new List<string>();
        public SpriteFont DebugText = null;
        public bool DebugMode = false;
        public bool ByFrameDebug = false;
        public bool ByPassDebug = false;
        public static List<Animator> Animators = new List<Animator>(); 
        public TimeSpan LastUpdate = TimeSpan.Zero;

        public Animator()
        {
            Animators.Add(this);
        }
        public Animator(Dictionary<string, AnimationData> Anims)
        {
            Animations = Anims;
        }

        public void AddAnimation(string Name, AnimationData Data)
        {
            if(!Animations.ContainsKey(Name))
            {
                Animations.Add(Name, Data);
            }
        }

        public bool AnimationExist(string Name)
        {
            return Animations.ContainsKey(Name);
        }

        public string GetAnimationNameIfExist(string Name, string NameIfNotExist)
        {
            if(AnimationExist(Name))
            {
                return Name;
            } else
            {
                return NameIfNotExist;
            }
        }

        public void PlayAnimation(string Name, bool ResetQueue = true)
        {
            AnimationData Data;
            if (Animations.TryGetValue(Name, out Data))
            {
                if (ResetQueue)
                {
                    Queue = new List<string>();
                }
                CurrentAnimation = new Animation(Data, this);
                CurrentAnimation.Playing = true;
                CurrentAnimation.Looped = Data.DefaultLoop;
            }
        }
        public void PushAnimation(string Name)
        {
            if (Animations.ContainsKey(Name))
            {
                Queue.Add(Name);
            }
        }

        public override void Update(GameTime gameTime)
        {
            LastUpdate = gameTime.TotalGameTime;


            if (ByFrameDebug && CurrentAnimation != null)
            {
                if (Input.KeyPressed(Keys.Down))
                {
                    CurrentAnimation.DoPreviousFrame(gameTime);
                }
                if (Input.KeyPressed(Keys.Up))
                {
                    CurrentAnimation.DoNextFrame(gameTime);
                }
            }




            if (Object != null && CurrentAnimation != null && MyRenderer != null)
            {
                CurrentAnimation.Update(gameTime);

                if (CurrentAnimation.CurrentFrame != null)
                {
                    AnimationFrame Frame = CurrentAnimation.CurrentFrame;
                    MyRenderer.Sprite = Frame.Sprite;
                    MyRenderer.RenderBounds = Frame.Bounds;
                    if (DebugMode)
                    {
                        if (DebugText == null)
                        {
                            DebugText = new SpriteFont();
                            DebugText.Font = ContentManager.GetSprite("DebugFont");
                        } else
                        {
                            int BF = 0;
                            if(Frame.Bounds.X > 0)
                            {
                                BF = Frame.Bounds.X / Frame.Bounds.Width;
                            }

                            DebugText.SetText("N: "+ CurrentAnimation.Data.Name + 
                                "\nF: " + CurrentAnimation.CurrentFrameIndex + "/" + (CurrentAnimation.Data.Frames.Count - 1) +
                                "\nL: " + CurrentAnimation.Looped + " R: " + CurrentAnimation.Reverse +
                                "\nBF: " + BF +" X "+ Frame.Bounds.X+" Y "+ Frame.Bounds.Y);
                            DebugText.Position = new Vector2(Object.Position.X, Object.Position.Y - 60);
                        }
                    }else if(DebugText != null)
                    {
                        DebugText.Destory();
                        DebugText = null;
                    }
                }
            }
        }

        public override void OnDestroy()
        {
            if(DebugText != null)
            {
                DebugText.Destory();
            }
            Animators.Remove(this);
        }
    }
}
