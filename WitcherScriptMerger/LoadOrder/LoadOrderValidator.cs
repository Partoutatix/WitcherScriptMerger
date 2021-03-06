﻿using System.Linq;
using System.Windows.Forms;

namespace WitcherScriptMerger.LoadOrder
{
    static class LoadOrderValidator
    {
        public static void ValidateAndFix(CustomLoadOrder loadOrder)
        {
            if (!loadOrder.Mods.Any())
                return;

            var mergedModName = Paths.RetrieveMergedModName();
            var mergedMod = loadOrder.Mods.Find(m => m.ModName.EqualsIgnoreCase(mergedModName));

            if (mergedMod != null && mergedMod == loadOrder.GetTopPriorityEnabledMod())
                return;

            var choice = PromptToPrioritizeMergedMod(loadOrder.FilePath);
            if (choice == DialogResult.Yes)
            {
                PrioritizeMergedMod(loadOrder, mergedMod);
            }
            else if (choice == DialogResult.Cancel)  // Never
            {
                Program.Settings.Set("ValidateCustomLoadOrder", false);
                Program.Settings.Save();
            }
        }

        static DialogResult PromptToPrioritizeMergedMod(string modsSettingsPath)
        {
            MessageBoxManager.Cancel = "Ne&ver";
            MessageBoxManager.Register();

            var choice = MessageBox.Show(
                $"{modsSettingsPath}\n\n" +
                "Detected custom load order in the file above, and merged files aren't configured to load first.\n\n" +
                "Would you like Script Merger to modify your custom load order so that your merged files have top priority?",
                "Custom Load Order Problem",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Exclamation,
                MessageBoxDefaultButton.Button2);

            MessageBoxManager.Unregister();
            return choice;
        }

        static void PrioritizeMergedMod(CustomLoadOrder loadOrder, ModLoadSetting mergedModSetting)
        {
            // Priority of min - 1 will be incremented to min
            var priority = CustomLoadOrder.TopPriority - 1;

            if (mergedModSetting != null)
            {
                mergedModSetting.IsEnabled = true;
                mergedModSetting.Priority = priority;
            }
            else
            {
                loadOrder.Mods.Insert(0, new ModLoadSetting
                {
                    ModName = Paths.RetrieveMergedModName(),
                    IsEnabled = true,
                    Priority = priority
                });
            }

            IncrementLeadingContiguousPriorities(loadOrder, priority);

            loadOrder.Save();
        }

        static void IncrementLeadingContiguousPriorities(CustomLoadOrder loadOrder, int startingPriority)
        {
            var nextPriority = startingPriority + 1;
            var modsToIncrement = loadOrder.Mods.Where(mod => mod.Priority == startingPriority).ToArray();
            var displacedMods = loadOrder.Mods.Where(mod => mod.Priority == nextPriority).ToArray();

            if (!modsToIncrement.Any())
                return;

            if (displacedMods.Any() &&
                nextPriority < CustomLoadOrder.BottomPriority)
            {
                IncrementLeadingContiguousPriorities(loadOrder, nextPriority);
            }

            foreach (var mod in modsToIncrement)
                ++mod.Priority;
        }
    }
}
