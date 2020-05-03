// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Scoring;
using osu.Game.Screens.Play;
using osu.Game.Skinning;
using osuTK;

namespace osu.Game.Rulesets.Taiko.Skinning
{
    public class LegacyTaikoScroller : CompositeDrawable
    {
        public LegacyTaikoScroller()
        {
            RelativeSizeAxes = Axes.Both;
        }

        [BackgroundDependencyLoader(true)]
        private void load(GameplayBeatmap gameplayBeatmap)
        {
            if (gameplayBeatmap != null)
                ((IBindable<JudgementResult>)LastResult).BindTo(gameplayBeatmap.LastJudgementResult);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            LastResult.BindValueChanged(result =>
            {
                foreach (var sprite in InternalChildren.OfType<ScrollerSprite>())
                    sprite.Passing = result.NewValue == null || result.NewValue.Type > HitResult.Miss;
            }, true);
        }

        public Bindable<JudgementResult> LastResult = new Bindable<JudgementResult>();

        protected override void Update()
        {
            base.Update();

            while (true)
            {
                float? additiveX = null;

                foreach (var sprite in InternalChildren)
                {
                    // add the x coordinates and perform re-layout on all sprites as spacing may change with gameplay scale.
                    sprite.X = additiveX ??= sprite.X - (float)Time.Elapsed * 0.1f;

                    additiveX += sprite.DrawWidth - 1;

                    if (sprite.X + sprite.DrawWidth < 0)
                        sprite.Expire();
                }

                var last = InternalChildren.LastOrDefault();

                // only break from this loop once we have saturated horizontal space completely.
                if (last != null && last.ScreenSpaceDrawQuad.TopRight.X >= ScreenSpaceDrawQuad.TopRight.X)
                    break;

                AddInternal(new ScrollerSprite());
            }
        }

        private class ScrollerSprite : CompositeDrawable
        {
            private Sprite passingSprite;
            private Sprite failingSprite;

            private bool passing = true;

            public bool Passing
            {
                get => passing;
                set
                {
                    if (value == passing)
                        return;

                    passing = value;

                    if (passing)
                    {
                        passingSprite.Show();
                        failingSprite.FadeOut(200);
                    }
                    else
                    {
                        failingSprite.FadeIn(200);
                        passingSprite.Delay(200).FadeOut();
                    }
                }
            }

            [BackgroundDependencyLoader]
            private void load(ISkinSource skin)
            {
                AutoSizeAxes = Axes.X;
                RelativeSizeAxes = Axes.Y;

                FillMode = FillMode.Fit;

                InternalChildren = new Drawable[]
                {
                    passingSprite = new Sprite { Texture = skin.GetTexture("taiko-slider") },
                    failingSprite = new Sprite { Texture = skin.GetTexture("taiko-slider-fail"), Alpha = 0 },
                };
            }

            protected override void Update()
            {
                base.Update();

                foreach (var c in InternalChildren)
                    c.Scale = new Vector2(DrawHeight / c.Height);
            }
        }
    }
}
