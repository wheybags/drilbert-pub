using System.Collections.Generic;
using System.Linq;

namespace Drilbert
{
    public class AnimationStage
    {
        public long lengthMs;
        public bool shake;
        public Tilemap startTilemap;
        public Tilemap endTilemap;
    }

    public class AnimationPoint
    {
        public AnimationStage stage;
        public float lerpAlpha;
        public bool isDone = false;
    }

    public static class LevelAnimation
    {
        public static List<AnimationStage> calculateAnimationStages(EvaluationResult previousResult, EvaluationResult currentResult)
        {
            List<AnimationStage> stages = new List<AnimationStage>();
            for (int i = 0; i < currentResult.tilemaps.Count; i++)
            {
                Tilemap start = i == 0 ? previousResult.tilemaps.Last() : currentResult.tilemaps[i - 1];
                Tilemap end = currentResult.tilemaps[i];

                stages.Add(new AnimationStage()
                {
                    lengthMs = Constants.moveInterpolationMs,
                    shake = false,
                    startTilemap = start,
                    endTilemap = end,
                });

                if (end.shakeScreen)
                {
                    stages.Add(new AnimationStage()
                    {
                        lengthMs = Constants.hangBetweenStatesMs,
                        shake = true,
                        startTilemap = end,
                        endTilemap = end,
                    });
                }
            }

            stages.Add(new AnimationStage()
            {
                lengthMs = 0,
                shake = false,
                startTilemap = currentResult.tilemaps.Last(),
                endTilemap = currentResult.tilemaps.Last(),
            });

            return stages;
        }

        public static AnimationPoint calculateAnimationPoint(long timestampMs, List<AnimationStage> stages)
        {
            long currentMs = 0;
            foreach (AnimationStage stage in stages)
            {
                currentMs += stage.lengthMs;
                if (timestampMs < currentMs)
                {
                    long remaining = currentMs - timestampMs;
                    float lerpAlpha = 1.0f - ((float) remaining / (float) stage.lengthMs);

                    return new AnimationPoint()
                    {
                        stage = stage,
                        lerpAlpha = lerpAlpha,
                        isDone = false,
                    };
                }
            }

            return new AnimationPoint()
            {
                stage = stages.Last(),
                lerpAlpha = 1.0f,
                isDone = true,
            };
        }
    }
}