﻿using System;
using System.Collections.Generic;

using static PKHeX.Core.Legal;

namespace PKHeX.Core
{
    public static class EncounterEggGenerator
    {
        // EncounterEgg
        public static IEnumerable<EncounterEgg> GenerateEggs(PKM pkm, bool all = false)
        {
            var table = EvolutionTree.GetEvolutionTree(pkm, Math.Max(2, pkm.Format));
            int maxSpeciesOrigin = GetMaxSpeciesOrigin(pkm.GenNumber);
            var evos = table.GetValidPreEvolutions(pkm, maxLevel: 100, maxSpeciesOrigin: maxSpeciesOrigin, skipChecks: true);
            return GenerateEggs(pkm, evos, all);
        }

        public static IEnumerable<EncounterEgg> GenerateEggs(PKM pkm, IReadOnlyList<EvoCriteria> chain, bool all = false)
        {
            int species = pkm.Species;
            if (NoHatchFromEgg.Contains(species))
                yield break;

            int gen = pkm.GenNumber;
            if (gen <= 1)
                yield break; // can't get eggs
            if (NoHatchFromEggForm(species, pkm.AltForm, gen))
                yield break; // can't originate from eggs

            // version is a true indicator for all generation 3-5 origins
            var ver = (GameVersion)pkm.Version;
            int lvl = gen <= 3 ? 5 : 1;
            int max = GetMaxSpeciesOrigin(gen);

            var e = EvoBase.GetBaseSpecies(chain, 0);
            if (e.Species <= max && !NoHatchFromEggFormGen(e.Species, e.Form, ver))
            {
                yield return new EncounterEgg(e.Species, e.Form, lvl, gen, ver);
                if (gen > 5 && (pkm.WasTradedEgg || all) && HasOtherGamePair(ver))
                    yield return new EncounterEgg(e.Species, e.Form, lvl, gen, GetOtherTradePair(ver));
            }

            if (!GetSplitBreedGeneration(gen).Contains(species))
                yield break; // no other possible species

            var o = EvoBase.GetBaseSpecies(chain, 1);
            if (o.Species == e.Species)
                yield break;

            if (o.Species <= max && !NoHatchFromEggFormGen(o.Species, o.Form, ver))
            {
                yield return new EncounterEggSplit(o.Species, o.Form, lvl, gen, ver, e.Species);
                if (gen > 5 && (pkm.WasTradedEgg || all) && HasOtherGamePair(ver))
                    yield return new EncounterEggSplit(o.Species, o.Form, lvl, gen, GetOtherTradePair(ver), e.Species);
            }
        }

        private static bool NoHatchFromEggForm(int species, int form, int gen)
        {
            if (form == 0)
                return false;
            if (FormConverter.IsTotemForm(species, form, gen))
                return true;
            if (species == (int) Species.Pichu)
                return true; // can't get Spiky Ear Pichu eggs
            if (species == (int) Species.Sinistea || species == (int) Species.Polteageist) // Antique = impossible
                return true; // can't get Antique eggs
            return false;
        }

        private static bool NoHatchFromEggFormGen(int species, int form, GameVersion game)
        {
            // Sanity check form for origin
            var gameInfo = GameData.GetPersonal(game);
            var entry = gameInfo.GetFormeEntry(species, form);
            return form >= entry.FormeCount && !(species == (int)Species.Rotom && form <= 5);
        }

        // Gen6+ update the origin game when hatched. Quick manip for X.Y<->A.O | S.M<->US.UM, ie X->A
        private static GameVersion GetOtherTradePair(GameVersion ver)
        {
            if (ver <= GameVersion.OR) // gen6
                return (GameVersion)((int)ver ^ 2);
            if (ver <= GameVersion.MN) // gen7
                return ver + 2;
            return ver - 2;
        }

        private static bool HasOtherGamePair(GameVersion ver)
        {
            return ver < GameVersion.GP; // lgpe and sw/sh don't have a sister pair
        }
    }
}
