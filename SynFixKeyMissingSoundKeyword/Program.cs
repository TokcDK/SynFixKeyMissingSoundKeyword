using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Plugins;

namespace SynFixKeyMissingSoundKeyword
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            return await SynthesisPipeline.Instance
                .AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch)
                .SetTypicalOpen(GameRelease.SkyrimSE, "YourPatcher.esp")
                .Run(args);
        }

        public static void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            var vendorItemKeyFormKey = FormKey.Factory("0914EF:Skyrim.esm");
            var iTMKeyUpSDFormKey = FormKey.Factory("03ED75:Skyrim.esm"); // ITMKeyUpSD [SNDR:0003ED75]
            var iTMKeyDownSDFormKey = FormKey.Factory("03ED78:Skyrim.esm"); // ITMKeyDownSD [SNDR:0003ED78]
            var iTMKeyUpSDFormlink = new FormLinkNullable<ISoundDescriptorGetter>(iTMKeyUpSDFormKey);
            var iTMKeyDownSDFormlink = new FormLinkNullable<ISoundDescriptorGetter>(iTMKeyDownSDFormKey);
            var iTMKeyUpSD = iTMKeyUpSDFormlink.Resolve(state.LinkCache);
            var iTMKeyDownSD = iTMKeyDownSDFormlink.Resolve(state.LinkCache);

            Console.WriteLine($"iTMKeyUpSD={iTMKeyUpSD.EditorID}");
            Console.WriteLine($"iTMKeyDownSD1={iTMKeyDownSD.EditorID}");
            int patchedCount = 0;
            foreach (var keyGetter in state.LoadOrder.PriorityOrder.Key().WinningOverrides())
            {
                bool isNeedToFixPickUpSound = keyGetter.PickUpSound.IsNull || keyGetter.PickUpSound.FormKey == FormKey.Null;
                bool isNeedToFixPutDownSound = keyGetter.PutDownSound.IsNull || keyGetter.PutDownSound.FormKey == FormKey.Null;
                bool isNeedToFixMissingKeyword = keyGetter.Keywords == null || keyGetter.Keywords.Count == 0 || !keyGetter.Keywords.Contains(vendorItemKeyFormKey);

                if (!isNeedToFixPickUpSound && !isNeedToFixPutDownSound && !isNeedToFixMissingKeyword) continue;

                patchedCount++;

                var keyToPatch = state.PatchMod.Keys.GetOrAddAsOverride(keyGetter);

                if (isNeedToFixPickUpSound)
                {
                    keyToPatch.PickUpSound.SetTo(iTMKeyUpSD);
                    Console.WriteLine($"PickUpSound sound set to {keyToPatch.PickUpSound}");
                }
                if (isNeedToFixPutDownSound) keyToPatch.PickUpSound.SetTo(iTMKeyDownSD);
                if (isNeedToFixMissingKeyword)
                {
                    if (keyToPatch.Keywords == null) keyToPatch.Keywords = new Noggog.ExtendedList<IFormLinkGetter<IKeywordGetter>>();

                    keyToPatch.Keywords.Add(vendorItemKeyFormKey);
                }
            }

            Console.WriteLine($"Fixes {patchedCount} records");
        }
    }
}
