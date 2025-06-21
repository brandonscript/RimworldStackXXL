using HugsLib;
using HugsLib.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;

namespace StackXXL
{
    public class StackXXLMod : ModBase
    {
        public override string ModIdentifier => "StackXXL";

        // Settings handles
        private SettingHandle<double> sizeXL;
        private SettingHandle<double> sizeXXL;
        private SettingHandle<SizeEnum> resourcesStack;
        private SettingHandle<SizeEnum> medicineStack;
        private SettingHandle<SizeEnum> silverStack;
        private SettingHandle<SizeEnum> textilesStack;
        private SettingHandle<SizeEnum> drugsStack;
        private SettingHandle<SizeEnum> meatStack;
        private SettingHandle<SizeEnum> rawStack;
        private SettingHandle<SizeEnum> mealsStack;
        private SettingHandle<SizeEnum> bodyPartsStack;
        private SettingHandle<SizeEnum> othersStackableStack;
        private SettingHandle<SizeEnum> othersSingleStack;
        private SettingHandle<bool> debugMode;

        // Pre-calculated multipliers for performance
        private double silverMultiplier;
        private double medicineMultiplier;
        private double resourcesMultiplier;
        private double textilesMultiplier;
        private double drugsMultiplier;
        private double meatMultiplier;
        private double rawMultiplier;
        private double mealsMultiplier;
        private double bodyPartsMultiplier;
        private double othersStackableMultiplier;
        private double othersSingleMultiplier;

        // Cached category references for performance
        private static ThingCategoryDef manufacturedCategory;
        private static ThingCategoryDef resourcesRawCategory;
        private static ThingCategoryDef stoneBlocksCategory;
        private static ThingCategoryDef medicineCategory;
        private static ThingCategoryDef leathersCategory;
        private static ThingCategoryDef meatRawCategory;

        // Advanced caching for asset changes and selective updates
        private static Dictionary<ThingDef, CategoryType> cachedCategoryTypes = new Dictionary<ThingDef, CategoryType>();
        private static Dictionary<ThingDef, int> originalStackLimits = new Dictionary<ThingDef, int>();
        private static HashSet<CategoryType> changedCategories = new HashSet<CategoryType>();
        private static int lastAssetVersion = -1;
        private static bool isInitialized = false;

        // Previous setting values to detect changes
        private SizeEnum lastResourcesStack = SizeEnum.Default;
        private SizeEnum lastMedicineStack = SizeEnum.Default;
        private SizeEnum lastSilverStack = SizeEnum.Default;
        private SizeEnum lastTextilesStack = SizeEnum.Default;
        private SizeEnum lastDrugsStack = SizeEnum.Default;
        private SizeEnum lastMeatStack = SizeEnum.Default;
        private SizeEnum lastRawStack = SizeEnum.Default;
        private SizeEnum lastMealsStack = SizeEnum.Default;
        private SizeEnum lastBodyPartsStack = SizeEnum.Default;
        private SizeEnum lastOthersStackableStack = SizeEnum.Default;
        private SizeEnum lastOthersSingleStack = SizeEnum.Default;
        private double lastSizeXL = 10.0;
        private double lastSizeXXL = 20.0;

        private enum SizeEnum
        {
            Default,
            XL,
            XXL
        }

        // Category type enum for faster switching
        private enum CategoryType
        {
            Silver,
            Medicine,
            Resources,
            Textiles,
            Drugs,
            Meat,
            Raw,
            Meals,
            BodyParts,
            OthersStackable,
            OthersSingle,
            Skip
        }

        private static bool StackIncreaseAllowed(ThingDef d)
        {
            if (d.thingCategories.NullOrEmpty())
                return false;

            return d.IsStuff || d.isTechHediff ||
                (d.category == ThingCategory.Item &&
                 !d.isUnfinishedThing &&
                 !d.IsCorpse &&
                 !d.destroyOnDrop &&
                 !d.IsRangedWeapon &&
                 !d.IsApparel &&
                 d.stackLimit > 1);
        }

        private static CategoryType DetermineCategoryType(ThingDef d)
        {
            // Silver check first (most specific)
            if (d == ThingDefOf.Silver)
                return CategoryType.Silver;

            // Get first category once and cache it
            var primaryCategory = d.thingCategories[0];
            var categoryName = primaryCategory.defName;

            // Medicine check (subset of resources, check before resources)
            if (primaryCategory == medicineCategory)
                return CategoryType.Medicine;

            // Resources check
            if (primaryCategory == manufacturedCategory ||
                primaryCategory == resourcesRawCategory ||
                primaryCategory == stoneBlocksCategory)
                return CategoryType.Resources;

            // Textiles check
            if (primaryCategory == leathersCategory || categoryName == "Textiles")
                return CategoryType.Textiles;

            // Drugs check
            if (categoryName == "Drugs")
                return CategoryType.Drugs;

            // Meat check
            if (primaryCategory == meatRawCategory)
                return CategoryType.Meat;

            // Raw food check - optimized string operations
            if (categoryName.EndsWith("FoodRaw") ||
                categoryName == "PlantMatter" ||
                categoryName == "AnimalProductRaw" ||
                categoryName == "AnimalFeed" ||
                categoryName.StartsWith("Eggs") ||
                categoryName == "CookingSupplies")
                return CategoryType.Raw;

            // Meals check
            if (categoryName.EndsWith("Meals") || categoryName == "Foods")
                return CategoryType.Meals;

            // Body parts check
            if (categoryName.EndsWith("Prostheses") ||
                categoryName.StartsWith("BodyParts") ||
                categoryName.EndsWith("Organs"))
                return CategoryType.BodyParts;

            // Others stackable/single
            return d.stackLimit > 1 ? CategoryType.OthersStackable : CategoryType.OthersSingle;
        }

        private double GetSizeMultiplier(SizeEnum sizeType)
        {
            switch (sizeType)
            {
                case SizeEnum.XL:
                    return sizeXL.Value;
                case SizeEnum.XXL:
                    return sizeXXL.Value;
                default:
                    return 1.0;
            }
        }

        private void DetectChangedCategories()
        {
            changedCategories.Clear();

            // Check if XL/XXL multipliers changed (affects all categories)
            bool multipliersChanged = lastSizeXL != sizeXL.Value || lastSizeXXL != sizeXXL.Value;

            if (multipliersChanged)
            {
                // If base multipliers changed, all categories are affected
                changedCategories.Add(CategoryType.Silver);
                changedCategories.Add(CategoryType.Medicine);
                changedCategories.Add(CategoryType.Resources);
                changedCategories.Add(CategoryType.Textiles);
                changedCategories.Add(CategoryType.Drugs);
                changedCategories.Add(CategoryType.Meat);
                changedCategories.Add(CategoryType.Raw);
                changedCategories.Add(CategoryType.Meals);
                changedCategories.Add(CategoryType.BodyParts);
                changedCategories.Add(CategoryType.OthersStackable);
                changedCategories.Add(CategoryType.OthersSingle);
            }
            else
            {
                // Check individual category changes
                if (lastResourcesStack != resourcesStack.Value) changedCategories.Add(CategoryType.Resources);
                if (lastMedicineStack != medicineStack.Value) changedCategories.Add(CategoryType.Medicine);
                if (lastSilverStack != silverStack.Value) changedCategories.Add(CategoryType.Silver);
                if (lastTextilesStack != textilesStack.Value) changedCategories.Add(CategoryType.Textiles);
                if (lastDrugsStack != drugsStack.Value) changedCategories.Add(CategoryType.Drugs);
                if (lastMeatStack != meatStack.Value) changedCategories.Add(CategoryType.Meat);
                if (lastRawStack != rawStack.Value) changedCategories.Add(CategoryType.Raw);
                if (lastMealsStack != mealsStack.Value) changedCategories.Add(CategoryType.Meals);
                if (lastBodyPartsStack != bodyPartsStack.Value) changedCategories.Add(CategoryType.BodyParts);
                if (lastOthersStackableStack != othersStackableStack.Value) changedCategories.Add(CategoryType.OthersStackable);
                if (lastOthersSingleStack != othersSingleStack.Value) changedCategories.Add(CategoryType.OthersSingle);
            }

            // Update last known values
            lastResourcesStack = resourcesStack.Value;
            lastMedicineStack = medicineStack.Value;
            lastSilverStack = silverStack.Value;
            lastTextilesStack = textilesStack.Value;
            lastDrugsStack = drugsStack.Value;
            lastMeatStack = meatStack.Value;
            lastRawStack = rawStack.Value;
            lastMealsStack = mealsStack.Value;
            lastBodyPartsStack = bodyPartsStack.Value;
            lastOthersStackableStack = othersStackableStack.Value;
            lastOthersSingleStack = othersSingleStack.Value;
            lastSizeXL = sizeXL.Value;
            lastSizeXXL = sizeXXL.Value;
        }

        private void PreCalculateMultipliers()
        {
            silverMultiplier = GetSizeMultiplier(silverStack.Value);
            medicineMultiplier = GetSizeMultiplier(medicineStack.Value);
            resourcesMultiplier = GetSizeMultiplier(resourcesStack.Value);
            textilesMultiplier = GetSizeMultiplier(textilesStack.Value);
            drugsMultiplier = GetSizeMultiplier(drugsStack.Value);
            meatMultiplier = GetSizeMultiplier(meatStack.Value);
            rawMultiplier = GetSizeMultiplier(rawStack.Value);
            mealsMultiplier = GetSizeMultiplier(mealsStack.Value);
            bodyPartsMultiplier = GetSizeMultiplier(bodyPartsStack.Value);
            othersStackableMultiplier = GetSizeMultiplier(othersStackableStack.Value);
            othersSingleMultiplier = GetSizeMultiplier(othersSingleStack.Value);
        }

        private static void CacheCategoryReferences()
        {
            manufacturedCategory = ThingCategoryDefOf.Manufactured;
            resourcesRawCategory = ThingCategoryDefOf.ResourcesRaw;
            stoneBlocksCategory = ThingCategoryDefOf.StoneBlocks;
            medicineCategory = ThingCategoryDefOf.Medicine;
            leathersCategory = ThingCategoryDefOf.Leathers;
            meatRawCategory = ThingCategoryDefOf.MeatRaw;
        }

        private static void UpdateStackLimit(ThingDef thing, double multiplier)
        {
            if (multiplier != 1.0)
            {
                // Use original stack limit if available, otherwise current limit
                int baseLimit = originalStackLimits.ContainsKey(thing) ? originalStackLimits[thing] : thing.stackLimit;
                thing.stackLimit = Math.Max(1, (int)Math.Round(baseLimit * multiplier));
            }
            else if (originalStackLimits.ContainsKey(thing))
            {
                // Reset to original if multiplier is 1.0
                thing.stackLimit = originalStackLimits[thing];
            }
        }

        private void LogCategoryOptimized(ThingDef d, string categoryName)
        {
            if (debugMode.Value)
            {
                // Use StringBuilder to avoid string concatenation overhead
                var sb = new StringBuilder(64);
                sb.Append(d.defName);
                sb.Append(" ");
                sb.Append(d.thingCategories[0].defName);
                sb.Append(" ");
                sb.Append(categoryName);
                Logger.Message(sb.ToString());
            }
        }

        private bool HasAssetVersionChanged()
        {
            // Simple version check based on ThingDef count - could be enhanced
            int currentVersion = DefDatabase<ThingDef>.AllDefsListForReading.Count;
            if (currentVersion != lastAssetVersion)
            {
                lastAssetVersion = currentVersion;
                return true;
            }
            return false;
        }

        private void BuildCacheIfNeeded()
        {
            // Only rebuild cache if assets changed or first run
            if (!isInitialized || HasAssetVersionChanged())
            {
                if (debugMode.Value)
                {
                    Logger.Message("StackXXL: Building/rebuilding cache due to asset changes");
                }

                cachedCategoryTypes.Clear();
                originalStackLimits.Clear();

                // Cache category references
                CacheCategoryReferences();

                // Build cache for all valid ThingDefs
                foreach (var thing in DefDatabase<ThingDef>.AllDefs)
                {
                    if (StackIncreaseAllowed(thing))
                    {
                        cachedCategoryTypes[thing] = DetermineCategoryType(thing);
                        originalStackLimits[thing] = thing.stackLimit;
                    }
                }

                isInitialized = true;

                if (debugMode.Value)
                {
                    Logger.Message($"StackXXL: Cache built for {cachedCategoryTypes.Count} items");
                }
            }
        }

        private void ModifyStackSizesSelective()
        {
            // Build cache if needed (only on asset changes)
            BuildCacheIfNeeded();

            // Detect which categories have changed
            DetectChangedCategories();

            if (changedCategories.Count == 0)
            {
                if (debugMode.Value)
                {
                    Logger.Message("StackXXL: No changes detected, skipping update");
                }
                return;
            }

            // Pre-calculate multipliers for changed categories only
            PreCalculateMultipliers();

            var processedCount = 0;
            var skippedCount = 0;

            // Only process items in changed categories
            foreach (var kvp in cachedCategoryTypes)
            {
                var thing = kvp.Key;
                var categoryType = kvp.Value;

                // Skip if this category hasn't changed
                if (!changedCategories.Contains(categoryType))
                {
                    skippedCount++;
                    continue;
                }

                double multiplier;
                string categoryName;

                // Use switch for better performance than if-else chain
                switch (categoryType)
                {
                    case CategoryType.Silver:
                        multiplier = silverMultiplier;
                        categoryName = "silver";
                        break;
                    case CategoryType.Medicine:
                        multiplier = medicineMultiplier;
                        categoryName = "medicine";
                        break;
                    case CategoryType.Resources:
                        multiplier = resourcesMultiplier;
                        categoryName = "resources";
                        break;
                    case CategoryType.Textiles:
                        multiplier = textilesMultiplier;
                        categoryName = "textiles";
                        break;
                    case CategoryType.Drugs:
                        multiplier = drugsMultiplier;
                        categoryName = "drugs";
                        break;
                    case CategoryType.Meat:
                        multiplier = meatMultiplier;
                        categoryName = "meat";
                        break;
                    case CategoryType.Raw:
                        multiplier = rawMultiplier;
                        categoryName = "raw";
                        break;
                    case CategoryType.Meals:
                        multiplier = mealsMultiplier;
                        categoryName = "meals";
                        break;
                    case CategoryType.BodyParts:
                        multiplier = bodyPartsMultiplier;
                        categoryName = "bodyParts";
                        break;
                    case CategoryType.OthersStackable:
                        multiplier = othersStackableMultiplier;
                        categoryName = "othersStackable";
                        break;
                    case CategoryType.OthersSingle:
                        multiplier = othersSingleMultiplier;
                        categoryName = "othersSingle";
                        break;
                    default:
                        skippedCount++;
                        continue;
                }

                UpdateStackLimit(thing, multiplier);
                LogCategoryOptimized(thing, categoryName);
                processedCount++;
            }

            if (debugMode.Value)
            {
                Logger.Message($"StackXXL: Selectively updated {processedCount} items in {changedCategories.Count} categories, skipped {skippedCount} items");
            }
        }

        public override void DefsLoaded()
        {
            // Initialize settings
            sizeXL = Settings.GetHandle<double>("sizeXL", "StackXXL.XLSize.Title".Translate(), "StackXXL.XLSize.Desc".Translate(), 10.0, Validators.FloatRangeValidator(1, float.MaxValue));
            sizeXXL = Settings.GetHandle<double>("sizeXXL", "StackXXL.XXLSize.Title".Translate(), "StackXXL.XXLSize.Desc".Translate(), 20.0, Validators.FloatRangeValidator(1, float.MaxValue));

            resourcesStack = Settings.GetHandle<SizeEnum>("resourcesStack", "StackXXL.Stack.Resources.Title".Translate(), "StackXXL.Stack.Resources.Desc".Translate(), SizeEnum.XL, null, "StackXXL.Size.");
            medicineStack = Settings.GetHandle<SizeEnum>("medicineStack", "StackXXL.Stack.Medicine.Title".Translate(), "StackXXL.Stack.Medicine.Desc".Translate(), resourcesStack.Value, null, "StackXXL.Size.");
            silverStack = Settings.GetHandle<SizeEnum>("silverStack", "StackXXL.Stack.Silver.Title".Translate(), "StackXXL.Stack.Silver.Desc".Translate(), resourcesStack.Value, null, "StackXXL.Size.");
            textilesStack = Settings.GetHandle<SizeEnum>("textilesStack", "StackXXL.Stack.Textiles.Title".Translate(), "StackXXL.Stack.Textiles.Desc".Translate(), SizeEnum.XL, null, "StackXXL.Size.");
            drugsStack = Settings.GetHandle<SizeEnum>("drugsStack", "StackXXL.Stack.Drugs.Title".Translate(), "StackXXL.Stack.Drugs.Desc".Translate(), SizeEnum.XL, null, "StackXXL.Size.");
            meatStack = Settings.GetHandle<SizeEnum>("meatStack", "StackXXL.Stack.Meat.Title".Translate(), "StackXXL.Stack.Meat.Desc".Translate(), SizeEnum.XL, null, "StackXXL.Size.");
            rawStack = Settings.GetHandle<SizeEnum>("rawStack", "StackXXL.Stack.Raw.Title".Translate(), "StackXXL.Stack.Raw.Desc".Translate(), SizeEnum.XL, null, "StackXXL.Size.");
            mealsStack = Settings.GetHandle<SizeEnum>("mealsStack", "StackXXL.Stack.Meals.Title".Translate(), "StackXXL.Stack.Meals.Desc".Translate(), SizeEnum.XL, null, "StackXXL.Size.");
            bodyPartsStack = Settings.GetHandle<SizeEnum>("bodyPartsStack", "StackXXL.Stack.BodyParts.Title".Translate(), "StackXXL.Stack.BodyParts.Desc".Translate(), SizeEnum.Default, null, "StackXXL.Size.");
            othersStackableStack = Settings.GetHandle<SizeEnum>("othersStackableStack", "StackXXL.Stack.Others.Stackable.Title".Translate(), "StackXXL.Stack.Others.Stackable.Desc".Translate(), SizeEnum.XL, null, "StackXXL.Size.");
            othersSingleStack = Settings.GetHandle<SizeEnum>("othersSingleStack", "StackXXL.Stack.Others.Single.Title".Translate(), "StackXXL.Stack.Others.Single.Desc".Translate(), SizeEnum.Default, null, "StackXXL.Size.");

            debugMode = Settings.GetHandle<bool>("debugMode", "StackXXL.DebugMode.Title".Translate(), "StackXXL.DebugMode.Desc".Translate(), false);

            // Add change callbacks for real-time updates
            sizeXL.ValueChanged += (val) => ModifyStackSizesSelective();
            sizeXXL.ValueChanged += (val) => ModifyStackSizesSelective();
            resourcesStack.ValueChanged += (val) => ModifyStackSizesSelective();
            medicineStack.ValueChanged += (val) => ModifyStackSizesSelective();
            silverStack.ValueChanged += (val) => ModifyStackSizesSelective();
            textilesStack.ValueChanged += (val) => ModifyStackSizesSelective();
            drugsStack.ValueChanged += (val) => ModifyStackSizesSelective();
            meatStack.ValueChanged += (val) => ModifyStackSizesSelective();
            rawStack.ValueChanged += (val) => ModifyStackSizesSelective();
            mealsStack.ValueChanged += (val) => ModifyStackSizesSelective();
            bodyPartsStack.ValueChanged += (val) => ModifyStackSizesSelective();
            othersStackableStack.ValueChanged += (val) => ModifyStackSizesSelective();
            othersSingleStack.ValueChanged += (val) => ModifyStackSizesSelective();

            if (!ModIsActive)
                return;

            // Initial load - force all categories to be processed
            changedCategories.Add(CategoryType.Silver);
            changedCategories.Add(CategoryType.Medicine);
            changedCategories.Add(CategoryType.Resources);
            changedCategories.Add(CategoryType.Textiles);
            changedCategories.Add(CategoryType.Drugs);
            changedCategories.Add(CategoryType.Meat);
            changedCategories.Add(CategoryType.Raw);
            changedCategories.Add(CategoryType.Meals);
            changedCategories.Add(CategoryType.BodyParts);
            changedCategories.Add(CategoryType.OthersStackable);
            changedCategories.Add(CategoryType.OthersSingle);

            ModifyStackSizesSelective();
            Logger.Message("StackXXL: Loaded with advanced caching and selective updates");
        }

        public override void Initialize()
        {
            // Empty - all initialization happens in DefsLoaded
        }
    }
}
