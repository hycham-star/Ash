using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Ash
{
    public sealed class CombatResultsUI : MonoBehaviour
    {
        [SerializeField] private Text resultsText;
        public void Bind(Text target) { resultsText = target; Clear(); }
        public void Clear()
        {
            if (resultsText != null) resultsText.text = "COMBAT RESULTS\n\nPrepare an army, choose a camp, then fight.\n\nPreview changes no armies, hospital patients or rewards.\n\nCounters:\nInfantry > Cavalry\nCavalry > Sharpshooter\nSharpshooter > Infantry";
        }
        public void Show(CombatResult result, bool applied)
        {
            var text = new StringBuilder(applied ? "SETTLED BATTLE\n" : "PREVIEW — STATE UNCHANGED\n");
            text.Append(result.Outcome.ToString().ToUpperInvariant()).Append(" • ").Append(result.EndReason)
                .Append("\nHero: ").Append(result.HeroInstanceId).Append("\nCamp: ").Append(result.EncounterId)
                .Append("\nRounds: ").Append(result.Rounds.Count).Append("\n\nYOUR ARMY\n");
            AppendFighters(text, result.PlayerFighters,true);
            text.Append(applied ? "\nAll wounded fighters sent to hospital. Heroes remain available.\n" : "\nProjected wounded only; hospital unchanged.\n");
            text.Append("\nENEMY ARMY\n"); AppendFighters(text, result.EnemyFighters,false);
            text.Append(applied ? "\nREWARDS SETTLED\n" : "\nPROJECTED REWARDS (NOT GRANTED)\n")
                .Append("Hero XP: ").Append(result.Rewards.HeroXp).Append(" (level cap applies)\n");
            foreach (var resource in result.Rewards.Resources) text.Append(resource.Type).Append(": +").Append(resource.Amount).Append('\n');
            text.Append("Hero fragments: +").Append(result.Rewards.HeroFragments);
            if (result.Rewards.HeroFragments > 0) text.Append(" for ").Append(result.Rewards.FragmentHeroDefinitionId);
            text.Append("\n\nROUND LOG (GENERATED DAMAGE)\n");
            foreach (var round in result.Rounds)
                text.Append('#').Append(round.Number).Append(" You ").Append(round.PlayerDamage.ToString("0.##"))
                    .Append(" / Enemy ").Append(round.EnemyDamage.ToString("0.##"))
                    .Append(" • Alive ").Append(round.PlayerSurvivors).Append('/').Append(round.EnemySurvivors)
                    .Append(round.PlayerAbilityActivated ? " • ABILITY" : "").Append('\n');
            resultsText.text = text.ToString();
        }
        private static void AppendFighters(StringBuilder text, System.Collections.Generic.IEnumerable<CombatFighterResult> fighters,bool player)
        {
            foreach (var stack in fighters)
                text.Append(stack.Fighter.Definition.Type).Append(" Lv").Append(stack.Fighter.Level)
                    .Append(" • Start ").Append(stack.InitialQuantity).Append(player?" • Wounded ":" • Defeated ").Append(stack.Casualties)
                    .Append(" • Survive ").Append(stack.SurvivingQuantity).Append('\n');
        }
    }
}
