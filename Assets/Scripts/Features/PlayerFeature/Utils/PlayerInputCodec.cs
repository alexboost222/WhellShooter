using Features.CharacterFeature.Components;
using Features.Network.Components;
using Features.Network.Enums;
using Features.Network.Utils;
using Unity.Mathematics;

namespace Features.PlayerFeature.Utils
{
    /// <summary>
    /// <see cref="PlayerInput"/> ↔ intent and look (Docs/05 §Input). The server and the predicting
    /// client decode with the same code, so both simulate the same thing.
    /// </summary>
    public static class PlayerInputCodec
    {
        public static sbyte EncodeAxis(float value)
        {
            return (sbyte)math.clamp(math.round(value * 127f), -127f, 127f);
        }

        public static float DecodeAxis(sbyte value)
        {
            return value / 127f;
        }

        /// <summary>
        /// Held buttons and move replace the intent's; with <paramref name="latches"/> the pressed
        /// latches are OR-ed in (a repeated or missed input passes false).
        /// </summary>
        public static void ApplyIntent(in PlayerInput input, ref CharacterIntent intent, bool latches)
        {
            var move = new float2(DecodeAxis(input.MoveX), DecodeAxis(input.MoveY));
            intent.Move = math.lengthsq(move) > 1f ? math.normalize(move) : move;

            var buttons = (InputButtons)input.Buttons;
            intent.Sprint = (buttons & InputButtons.Sprint) != 0;
            intent.Fire = (buttons & InputButtons.Fire) != 0;
            intent.Aim = (buttons & InputButtons.Aim) != 0;
            if (latches)
            {
                intent.JumpPressed |= (buttons & InputButtons.Jump) != 0;
                intent.FirePressed |= (buttons & InputButtons.FirePressed) != 0;
            }
        }

        public static Look DecodeLook(in PlayerInput input)
        {
            return new Look
            {
                Yaw = LookCodec.DecodeYaw(input.LookYaw),
                Pitch = LookCodec.DecodePitch(input.LookPitch),
            };
        }
    }
}
