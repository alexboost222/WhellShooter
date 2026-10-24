using Features.Bots.Enums;
using Scellecs.Morpeh;

namespace Features.Bots.Components
{
    /// <summary>
    /// Utility-AI bookkeeping. On a think tick <c>BotThinkSystem</c> opens a round, every score
    /// system <see cref="Offer"/>s its action, and <c>BotDecideSystem</c> switches to the best bid.
    /// </summary>
    public struct BotBrain : IComponent
    {
        public BotAction Action;

        /// <summary>Seconds in the current action.</summary>
        public float ActionTime;

        /// <summary>Seconds until the next think.</summary>
        public float NextThink;

        /// <summary>True during a think tick, between <c>BotThinkSystem</c> and <c>BotDecideSystem</c>.</summary>
        public bool Thinking;

        public BotAction BestAction;
        public float BestScore;

        /// <summary>Bonus for bidding on the running action — hysteresis, set each round from the config.</summary>
        public float Commitment;

        /// <summary>
        /// A bid. The highest one this round wins; ties go to the earlier bidder. A positive bid for
        /// the running action gets <see cref="Commitment"/> on top, so near-equal options don't flicker.
        /// </summary>
        public void Offer(BotAction action, float score)
        {
            if (action == Action && score > 0f)
            {
                score += Commitment;
            }

            if (score > BestScore)
            {
                BestScore = score;
                BestAction = action;
            }
        }
    }
}
