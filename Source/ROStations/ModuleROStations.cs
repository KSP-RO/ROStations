using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KSPShaderTools;
using ROLib;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace ROStations
{
    [SuppressMessage("ReSharper", "InvertIf")]
    public class ModuleROStations : PartModule, IPartCostModifier, IPartMassModifier, IRecolorable, IContainerVolumeContributor
    {
        private const string GroupDisplayName = "RO-Stations";
        private const string GroupName = "ModuleROStations";

        #region KSPFields

        [KSPField] public float diameterLargeStep = 0.1f;
        [KSPField] public float diameterSmallStep = 0.1f;
        [KSPField] public float diameterSlideStep = 0.001f;
        [KSPField] public float minDiameter = 0.1f;
        [KSPField] public float maxDiameter = 100.0f;
        [KSPField] public bool enableVScale = true;
        [KSPField] public bool enableTopVScale = false;
        [KSPField] public bool enableUpperVScale = false;
        [KSPField] public bool enableLowerVScale = false;
        [KSPField] public bool enableBottomVScale = false;
        [KSPField] public int coreContainerIndex = 0;
        [KSPField] public int topContainerIndex = 0;
        [KSPField] public int upperContainerIndex = 0;
        [KSPField] public int lowerContainerIndex = 0;
        [KSPField] public int bottomContainerIndex = 0;
        [KSPField] public float habMinPercent = 5f;
        [KSPField] public float habMaxPercent = 95f;
        [KSPField] public string coreManagedNodes = string.Empty;
        [KSPField] public string topManagedNodes = string.Empty;
        [KSPField] public string upperManagedNodes = string.Empty;
        [KSPField] public string lowerManagedNodes = string.Empty;
        [KSPField] public string bottomManagedNodes = string.Empty;
        [KSPField] public string topInterstageNode = "topinterstage";
        [KSPField] public string bottomInterstageNode = "bottominterstage";
        [KSPField(isPersistant = true)] public bool isHabitatModified = true;
        [KSPField(isPersistant = true)] public double modifiedVolume;
        [KSPField(isPersistant = true)] public double modifiedSurface;
        [KSPField(isPersistant = true)] public bool exerciseEnabled = false;
        [KSPField(isPersistant = true)] public bool panoramaEnabled = false;
        [KSPField(isPersistant = true)] public bool plantsEnabled = false;
        [KSPField] public bool validateTop = true;
        [KSPField] public bool validateUpper = true;
        [KSPField] public bool validateLower = true;
        [KSPField] public bool validateBottom = true;
        [KSPField] public bool noHab = false;


        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "Diameter", guiUnits = "m", groupName = GroupName, groupDisplayName = GroupDisplayName),
         UI_FloatEdit(sigFigs = 4, suppressEditorShipModified = true)]
        public float currentDiameter = 2.0f;

        [KSPField(isPersistant = true, guiName = "V.ScaleAdj", groupName = GroupName),
         UI_FloatEdit(sigFigs = 4, suppressEditorShipModified = true, minValue = -1, maxValue = 1, incrementLarge = 0.25f, incrementSmall = 0.05f, incrementSlide = 0.001f)]
        public float currentVScale = 0f;

        [KSPField(isPersistant = true, guiName = "Top V.Scale", groupName = GroupName),
         UI_FloatEdit(sigFigs = 4, suppressEditorShipModified = true, minValue = -1, maxValue = 1, incrementLarge = 0.25f, incrementSmall = 0.05f, incrementSlide = 0.001f)]
        public float currentTopVScale = 0f;

        [KSPField(isPersistant = true, guiName = "Upper V.Scale", groupName = GroupName),
         UI_FloatEdit(sigFigs = 4, suppressEditorShipModified = true, minValue = -1, maxValue = 1, incrementLarge = 0.25f, incrementSmall = 0.05f, incrementSlide = 0.001f)]
        public float currentUpperVScale = 0f;

        [KSPField(isPersistant = true, guiName = "Lower V.Scale", groupName = GroupName),
         UI_FloatEdit(sigFigs = 4, suppressEditorShipModified = true, minValue = -1, maxValue = 1, incrementLarge = 0.25f, incrementSmall = 0.05f, incrementSlide = 0.001f)]
        public float currentLowerVScale = 0f;

        [KSPField(isPersistant = true, guiName = "Bottom V.Scale", groupName = GroupName),
         UI_FloatEdit(sigFigs = 4, suppressEditorShipModified = true, minValue = -1, maxValue = 1, incrementLarge = 0.25f, incrementSmall = 0.05f, incrementSlide = 0.001f)]
        public float currentBottomVScale = 0f;

        [KSPField(isPersistant = true, guiActiveEditor = false, guiName = "Top Rot.", groupName = GroupName),
         UI_FloatEdit(sigFigs = 0, suppressEditorShipModified = true, minValue = -180f, maxValue = 180f, incrementLarge = 45f, incrementSmall = 15f, incrementSlide = 1f)]
        public float currentTopRotation = 0f;

        [KSPField(isPersistant = true, guiActiveEditor = false, guiName = "Bottom Rot.", groupName = GroupName),
         UI_FloatEdit(sigFigs = 0, suppressEditorShipModified = true, minValue = -180f, maxValue = 180f, incrementLarge = 45f, incrementSmall = 15f, incrementSlide = 1f)]
        public float currentBottomRotation = 0f;

        [KSPField(isPersistant = true, guiActiveEditor = false, guiName = "Habitat %", guiUnits = "%", groupName = GroupName),
         UI_FloatEdit(sigFigs = 0, suppressEditorShipModified = true, minValue = 5f, maxValue = 95f, incrementLarge = 15f, incrementSmall = 5f, incrementSlide = 1f)]
        public float currentHabitat = 90f;

        [KSPEvent(guiActiveEditor = true, guiName = "Add Exercise Comfort", groupName = GroupName)]
        public void ToggleExercise()
        {
            exerciseEnabled = !exerciseEnabled;
            UpdateHabitatVolume();
            UpdateMass();
            UpdateCost();
            this.Events["ToggleExercise"].guiName = exerciseEnabled ? "Remove Exercise Comfort" : "Add Exercise Comfort";
        }

        [KSPField(isPersistant = true, guiName = "Variant", guiActiveEditor = true, groupName = GroupName),
         UI_ChooseOption(suppressEditorShipModified = true)]
        public string currentVariant = "Default";

        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "Core", groupName = GroupName),
         UI_ChooseOption(suppressEditorShipModified = true)]
        public string currentCore = "Mount-None";

        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "Top", groupName = GroupName),
         UI_ChooseOption(suppressEditorShipModified = true)]
        public string currentTop = "Mount-None";

        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "Upper", groupName = GroupName),
         UI_ChooseOption(suppressEditorShipModified = true)]
        public string currentUpper = "Mount-None";

        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "Lower", groupName = GroupName),
         UI_ChooseOption(suppressEditorShipModified = true)]
        public string currentLower = "Mount-None";

        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "Bottom", groupName = GroupName),
         UI_ChooseOption(suppressEditorShipModified = true)]
        public string currentBottom = "Mount-None";

        //------------------------------------------TEXTURE SET PERSISTENCE-----------------------------------------------//

        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "Core Tex", groupName = GroupName),
         UI_ChooseOption(suppressEditorShipModified = true)]
        public string currentCoreTexture = "default";

        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "Top Tex", groupName = GroupName),
         UI_ChooseOption(suppressEditorShipModified = true)]
        public string currentTopTexture = "default";

        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "Upper Tex", groupName = GroupName),
         UI_ChooseOption(suppressEditorShipModified = true)]
        public string currentUpperTexture = "default";

        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "Lower Tex", groupName = GroupName),
         UI_ChooseOption(suppressEditorShipModified = true)]
        public string currentLowerTexture = "default";

        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "Bottom Tex", groupName = GroupName),
         UI_ChooseOption(suppressEditorShipModified = true)]
        public string currentBottomTexture = "default";

        //-------------------------------------RESETTING MODEL TO ORIGINAL SIZE------------------------------------------//
        [KSPEvent(guiActiveEditor = true, guiName = "Reset Model to Original", groupName = GroupName)]
        public void ResetModel()
        {
            currentDiameter = coreModule.definition.diameter;
            currentVScale = 0f;
            currentTopVScale = 0f;
            currentUpperVScale = 0f;
            currentLowerVScale = 0f;
            currentBottomVScale = 0f;
            currentTopRotation = 0f;
            currentBottomRotation = 0f;
            currentHabitat = 90f;

            this.ROLupdateUIFloatEditControl(nameof(currentDiameter), minDiameter, maxDiameter, diameterLargeStep, diameterSmallStep, diameterSlideStep);
            this.ROLupdateUIFloatEditControl(nameof(currentVScale), -1, 1, 0.25f, 0.05f, 0.001f);
            this.ROLupdateUIFloatEditControl(nameof(currentTopVScale), -1, 1, 0.25f, 0.05f, 0.001f);
            this.ROLupdateUIFloatEditControl(nameof(currentUpperVScale), -1, 1, 0.25f, 0.05f, 0.001f);
            this.ROLupdateUIFloatEditControl(nameof(currentLowerVScale), -1, 1, 0.25f, 0.05f, 0.001f);
            this.ROLupdateUIFloatEditControl(nameof(currentBottomVScale), -1, 1, 0.25f, 0.05f, 0.001f);
            this.ROLupdateUIFloatEditControl(nameof(currentTopRotation), -180f, 180f, 45f, 15f, 1f);
            this.ROLupdateUIFloatEditControl(nameof(currentBottomRotation), -180f, 180f, 45f, 15f, 1f);
            this.ROLupdateUIFloatEditControl(nameof(currentHabitat), habMinPercent, habMaxPercent, 15f, 5f, 1f);

            ModelChangedHandlerWithSymmetry(true, true);
            MonoUtilities.RefreshPartContextWindow(part);
        }

        //------------------------------------------RECOLORING PERSISTENCE-----------------------------------------------//

        // Persistent data for modules; stores colors
        [KSPField(isPersistant = true)] public string coreModulePersistentData = string.Empty;
        [KSPField(isPersistant = true)] public string topModulePersistentData = string.Empty;
        [KSPField(isPersistant = true)] public string upperModulePersistentData = string.Empty;
        [KSPField(isPersistant = true)] public string lowerModulePersistentData = string.Empty;
        [KSPField(isPersistant = true)] public string bottomModulePersistentData = string.Empty;

        #endregion KSPFields

        #region Private Variables

        [Persistent]
        public string configNodeData = string.Empty;
        private bool initialized = false;
        private float modifiedMass = -1;
        private float modifiedCost = -1;
        private readonly float volumeScalingPower = 3f;

        // Previous diameter value, used for surface attach position updates.
        private float prevDiameter = 0;
        private float prevCore = 0;
        private float prevTop = 0;
        private float prevUpper = 0;
        private float prevLower = 0;
        private float prevBottom = 0;

        private string[] coreNodeNames;
        private string[] topNodeNames;
        private string[] upperNodeNames;
        private string[] lowerNodeNames;
        private string[] bottomNodeNames;

        // Values based on the different parts of the Integrated Truss Assembly on the ISS
        private const float trussMassMultiplier = 0.1f;
        private const float trussCostMultiplier = 10f;
        // Values based on the ISS Modules
        private const float habMassMultiplier = 0.11f;
        private const float habCostBaseMultiplier = 7000f;
        private const float habCostFactor = 0.5f;
        // Values based on ISS Modules and assuming a Crew Tube would have less mass and cost than a full Module
        private const float crewTubeMassMultiplier = 0.1f;
        private const float crewTubeCostMultiplier = 24.5f;

        // Values are based on CEVIS, COLBERT and RED from the ISS
        private const float exerciseMass = 1.344f;
        private const float exerciseVolume = 1.586f;
        private const float exerciseWatts = 0.5f; // 360 watts for CEVIS and COLBERT (peak rates are much higher, but let's leave it at something reasonable)

        private PartResource atmoResource;
        private PartResource wasteAtmoResource;
        private PartResource shieldingResource;

        // Main module slots for top / core / bottom
        internal ROLModelModule<ModuleROStations> coreModule;
        internal ROLModelModule<ModuleROStations> topModule;
        internal ROLModelModule<ModuleROStations> upperModule;
        internal ROLModelModule<ModuleROStations> lowerModule;
        internal ROLModelModule<ModuleROStations> bottomModule;
        private List<ROLModelModule<ModuleROStations>> moduleList = new List<ROLModelModule<ModuleROStations>>();

        // Mapping of all of the variant sets available for this part.  When variant list length > 0, an additional 'variant' UI slider is added to allow for switching between variants.
        private readonly Dictionary<string, ModelDefinitionVariantSet> variantSets = new Dictionary<string, ModelDefinitionVariantSet>();

        // Helper method to get or create a variant set for the input variant name.  If no set currently exists, a new set is empty set is created and returned.
        private ModelDefinitionVariantSet GetVariantSet(string name)
        {
            if (!variantSets.TryGetValue(name, out ModelDefinitionVariantSet set))
            {
                set = new ModelDefinitionVariantSet(name);
                variantSets.Add(name, set);
            }
            return set;
        }

        public const KeyCode hoverRecolorKeyCode = KeyCode.J;
        // FIXME: Need to add GUI
        // public const KeyCode hoverPresetKeyCode = KeyCode.N;

        // Find the first variant set containing a definition with ModelDefinitionLayoutOptions def.  Will not create a new set if not found.
        private ModelDefinitionVariantSet GetVariantSet(ModelDefinitionLayoutOptions def) =>
            variantSets.Values.FirstOrDefault(a => a.definitions.Contains(def));

        internal ModelDefinitionLayoutOptions[] coreDefs;
        internal ModelDefinitionLayoutOptions[] topDefs;
        internal ModelDefinitionLayoutOptions[] upperDefs;
        internal ModelDefinitionLayoutOptions[] lowerDefs;
        internal ModelDefinitionLayoutOptions[] bottomDefs;

        internal void ModelChangedHandler(bool pushNodes)
        {
            if (validateTop || validateUpper || validateLower || validateBottom) ValidateModules();
            ValidateRotation();
            ValidateVScale();
            UpdateModulePositions();
            UpdateModelMeshes();
            UpdateAttachNodes(pushNodes);
            UpdateAvailableVariants();            
            SetPreviousModuleLength();
            UpdateDragCubes();
            UpdateHabitatVolume();
            UpdateMass();
            UpdateCost();
            ROLStockInterop.UpdatePartHighlighting(part);
            if (HighLogic.LoadedSceneIsEditor)
                GameEvents.onEditorShipModified.Fire(EditorLogic.fetch.ship);
        }

        internal void ModelChangedHandlerWithSymmetry(bool pushNodes, bool symmetry)
        {
            ModelChangedHandler(pushNodes);
            if (symmetry)
            {
                foreach (Part p in part.symmetryCounterparts)
                {
                    p.FindModuleImplementing<ModuleROStations>().ModelChangedHandler(pushNodes);
                }
            }
        }

        #endregion Private Variables

        #region Standard KSP Overrides

        public override void OnLoad(ConfigNode node)
        {
            if (string.IsNullOrEmpty(configNodeData)) { configNodeData = node.ToString(); }
            Initialize();
            UpdateModulePositions();
            UpdateModelMeshes();
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            Initialize();
            ModelChangedHandler(false);
            InitializeUI();
        }

        public override void OnStartFinished(StartState state)
        {
            if (KerbalismInterface.isEnabled && isHabitatModified) KerbalismHabitatOnStartFinished();
        }

        // FIXME
        /*
        public void OnDestroy()
        {
            if (HighLogic.LoadedSceneIsEditor)
            {
                GameEvents.onPartActionUIDismiss.Remove(OnPawClose);
            }
        }
        */

        // IPartMass / CostModifier overrides
        public ModifierChangeWhen GetModuleMassChangeWhen() => ModifierChangeWhen.FIXED;
        public ModifierChangeWhen GetModuleCostChangeWhen() => ModifierChangeWhen.FIXED;
        public float GetModuleMass(float defaultMass, ModifierStagingSituation sit) => Mathf.Max(0, modifiedMass);
        public float GetModuleCost(float defaultCost, ModifierStagingSituation sit) => Mathf.Max(0, modifiedCost);

        #endregion Standard KSP Overrides

        #region IRecolorable and IContainerVolumeContributor Overrides

        public string[] getSectionNames() => new string[] { "Top", "Upper", "Core", "Lower", "Bottom" };

        public RecoloringData[] getSectionColors(string section) => section switch
        {
            "Top" => topModule.recoloringData,
            "Upper" => upperModule.recoloringData,
            "Core" => coreModule.recoloringData,
            "Lower" => lowerModule.recoloringData,
            "Bottom" => bottomModule.recoloringData,
            _ => coreModule.recoloringData,
        };

        public void setSectionColors(string section, RecoloringData[] colors)
        {
            if (section == "Top") topModule.setSectionColors(colors);
            else if (section == "Upper") upperModule.setSectionColors(colors);
            else if (section == "Core") coreModule.setSectionColors(colors);
            else if (section == "Lower") lowerModule.setSectionColors(colors);
            else if (section == "Bottom") bottomModule.setSectionColors(colors);
        }

        public TextureSet getSectionTexture(string section) => section switch
        {
            "Top" => topModule.textureSet,
            "Upper" => upperModule.textureSet,
            "Core" => coreModule.textureSet,
            "Lower" => lowerModule.textureSet,
            "Bottom" => bottomModule.textureSet,
            _ => coreModule.textureSet,
        };

        // IContainerVolumeContributor override
        public ContainerContribution[] getContainerContributions()
        {
            ContainerContribution[] cts;
            ContainerContribution ct0 = GetCC("core", coreContainerIndex, coreModule.moduleVolume * 1000f);
            ContainerContribution ct1 = GetCC("top", topContainerIndex, topModule.moduleVolume * 1000f);
            ContainerContribution ct2 = GetCC("upper", upperContainerIndex, upperModule.moduleVolume * 1000f);
            ContainerContribution ct3 = GetCC("lower", lowerContainerIndex, lowerModule.moduleVolume * 1000f);
            ContainerContribution ct4 = GetCC("bottom", bottomContainerIndex, bottomModule.moduleVolume * 1000f);
            cts = new ContainerContribution[5] { ct0, ct1, ct2, ct3, ct4 };
            return cts;
        }

        private ContainerContribution GetCC(string name, int index, float vol)
        {
            return new ContainerContribution(name, index, vol);
        }

        #endregion IRecolorable and IContainerVolumeContributor Overrides

        #region Custom Update Methods

        /// <summary>
        /// Initialization method.  Sets up model modules, loads their configs from the input config node.  Does all initial linking of part-modules.<para/>
        /// Does NOT set up their UI interaction -- that is all handled during OnStart()
        /// </summary>
        private void Initialize()
        {
            if (initialized) { return; }
            initialized = true;

            coreNodeNames = ROLUtils.parseCSV(coreManagedNodes);
            topNodeNames = ROLUtils.parseCSV(topManagedNodes);
            upperNodeNames = ROLUtils.parseCSV(upperManagedNodes);
            lowerNodeNames = ROLUtils.parseCSV(lowerManagedNodes);
            bottomNodeNames = ROLUtils.parseCSV(bottomManagedNodes);

            //model-module setup/initialization
            ConfigNode node = ROLConfigNodeUtils.ParseConfigNode(configNodeData);

            //list of CORE model nodes from config
            //each one may contain multiple 'model=modelDefinitionName' entries
            //but must contain no more than a single 'variant' entry.
            //if no variant is specified, they are added to the 'Default' variant.
            List<ModelDefinitionLayoutOptions> coreDefList = new List<ModelDefinitionLayoutOptions>();
            foreach (ConfigNode n in node.GetNodes("CORE"))
            {
                string variantName = n.ROLGetStringValue("variant", "Default");
                coreDefs = ROLModelData.getModelDefinitionLayouts(n.ROLGetStringValues("model"));
                coreDefList.AddUniqueRange(coreDefs);
                ModelDefinitionVariantSet mdvs = GetVariantSet(variantName);
                mdvs.AddModels(coreDefs);
            }
            coreDefs = coreDefList.ToArray();

            //model defs - brought here so we can capture the array rather than the config node+method call
            topDefs = ROLModelData.getModelDefinitions(node.GetNodes("TOP"));
            upperDefs = ROLModelData.getModelDefinitions(node.GetNodes("UPPER"));
            lowerDefs = ROLModelData.getModelDefinitions(node.GetNodes("LOWER"));
            bottomDefs = ROLModelData.getModelDefinitions(node.GetNodes("BOTTOM"));

            coreModule = new ROLModelModule<ModuleROStations>(part, this, ROLUtils.GetRootTransform(part, "ModularPart-CORE"), ModelOrientation.CENTRAL, nameof(currentCore), null, nameof(currentCoreTexture), nameof(coreModulePersistentData));
            coreModule.name = "ModuleROStations-Core";
            coreModule.getSymmetryModule = m => m.coreModule;
            coreModule.getValidOptions = () => GetVariantSet(currentVariant).definitions;

            upperModule = new ROLModelModule<ModuleROStations>(part, this, ROLUtils.GetRootTransform(part, "ModularPart-UPPER"), ModelOrientation.TOP, nameof(currentUpper), null, nameof(currentUpperTexture), nameof(upperModulePersistentData));
            upperModule.name = "ModuleROStations-Upper";
            upperModule.getSymmetryModule = m => m.upperModule;
            if (validateUpper)
                upperModule.getValidOptions = () => upperModule.getValidModels(upperDefs, coreModule.definition.style);
            else
                upperModule.getValidOptions = () => upperDefs;

            lowerModule = new ROLModelModule<ModuleROStations>(part, this, ROLUtils.GetRootTransform(part, "ModularPart-LOWER"), ModelOrientation.BOTTOM, nameof(currentLower), null, nameof(currentLowerTexture), nameof(lowerModulePersistentData));
            lowerModule.name = "ModuleROStations-Lower";
            lowerModule.getSymmetryModule = m => m.lowerModule;
            if (validateLower)
                lowerModule.getValidOptions = () => lowerModule.getValidModels(lowerDefs, coreModule.definition.style);
            else
                lowerModule.getValidOptions = () => lowerDefs;

            topModule = new ROLModelModule<ModuleROStations>(part, this, ROLUtils.GetRootTransform(part, "ModularPart-TOP"), ModelOrientation.TOP, nameof(currentTop), null, nameof(currentTopTexture), nameof(topModulePersistentData));
            topModule.name = "ModuleROStations-Top";
            topModule.getSymmetryModule = m => m.topModule;
            if (validateTop)
                topModule.getValidOptions = () => topModule.getValidModels(topDefs, upperModule.definition.style);
            else
                topModule.getValidOptions = () => topDefs;

            bottomModule = new ROLModelModule<ModuleROStations>(part, this, ROLUtils.GetRootTransform(part, "ModularPart-BOTTOM"), ModelOrientation.BOTTOM, nameof(currentBottom), null, nameof(currentBottomTexture), nameof(bottomModulePersistentData));
            bottomModule.name = "ModuleROStations-Bottom";
            bottomModule.getSymmetryModule = m => m.bottomModule;
            if (validateBottom)
                bottomModule.getValidOptions = () => bottomModule.getValidModels(bottomDefs, lowerModule.definition.style);
            else
                bottomModule.getValidOptions = () => bottomDefs;

            coreModule.volumeScalar = volumeScalingPower;
            topModule.volumeScalar = volumeScalingPower;
            upperModule.volumeScalar = volumeScalingPower;
            lowerModule.volumeScalar = volumeScalingPower;
            bottomModule.volumeScalar = volumeScalingPower;

            prevDiameter = currentDiameter;
            prevCore = coreModule.moduleHeight;
            prevTop = 0;
            prevUpper = 0;
            prevLower = 0;
            prevBottom = 0;

            moduleList.Add(coreModule);
            moduleList.Add(topModule);
            moduleList.Add(upperModule);
            moduleList.Add(lowerModule);
            moduleList.Add(bottomModule);

            // Set up the model lists and load the currently selected model
            coreModule.setupModelList(coreDefs);
            topModule.setupModelList(topDefs);
            upperModule.setupModelList(upperDefs);
            lowerModule.setupModelList(lowerDefs);
            bottomModule.setupModelList(bottomDefs);
            coreModule.setupModel();
            topModule.setupModel();
            upperModule.setupModel();
            lowerModule.setupModel();
            bottomModule.setupModel();
            if (validateTop || validateUpper || validateLower || validateBottom) ValidateModules();

            atmoResource = part.Resources["Atmosphere"];
            wasteAtmoResource = part.Resources["WasteAtmosphere"];
            shieldingResource = part.Resources["Shielding"];
        }

        /// <summary>
        /// Initialize the UI controls, including default values, and specifying delegates for their 'onClick' methods.<para/>
        /// All UI based interaction code will be defined/run through these delegates.
        /// </summary>
        public void InitializeUI()
        {
            //set up the core variant UI control
            string[] variantNames = ROLUtils.getNames(variantSets.Values, m => m.variantName);
            this.ROLupdateUIChooseOptionControl(nameof(currentVariant), variantNames, variantNames);
            Fields[nameof(currentVariant)].guiActiveEditor = variantSets.Count > 1;

            Fields[nameof(currentVariant)].uiControlEditor.onFieldChanged = (a, b) =>
            {
                //TODO find variant set for the currently enabled core model
                //query the index from that variant set
                ModelDefinitionVariantSet prevMdvs = GetVariantSet(coreModule.definition.name);
                //this is the index of the currently selected model within its variant set
                int previousIndex = prevMdvs.IndexOf(coreModule.layoutOptions);
                //grab ref to the current/new variant set
                ModelDefinitionVariantSet mdvs = GetVariantSet(currentVariant);
                //and a reference to the model from same index out of the new set ([] call does validation internally for IAOOBE)
                ModelDefinitionLayoutOptions newCoreDef = mdvs[previousIndex];
                //now, call model-selected on the core model to update for the changes, including symmetry counterpart updating.
                this.ROLactionWithSymmetry(m =>
                {
                    m.coreModule.modelSelected(newCoreDef.definition.name);
                    ModelChangedHandler(true);
                });
                MonoUtilities.RefreshPartContextWindow(part);
            };

            Fields[nameof(currentDiameter)].uiControlEditor.onFieldChanged =
            Fields[nameof(currentDiameter)].uiControlEditor.onSymmetryFieldChanged = OnDiameterChanged;

            Fields[nameof(currentVScale)].uiControlEditor.onFieldChanged =
            Fields[nameof(currentVScale)].uiControlEditor.onSymmetryFieldChanged = (a, b) =>
            {
                ModelChangedHandler(true);
            };

            Fields[nameof(currentTopVScale)].uiControlEditor.onFieldChanged =
            Fields[nameof(currentTopVScale)].uiControlEditor.onSymmetryFieldChanged = (a, b) =>
            {
                ModelChangedHandler(true);
            };

            Fields[nameof(currentUpperVScale)].uiControlEditor.onFieldChanged =
            Fields[nameof(currentUpperVScale)].uiControlEditor.onSymmetryFieldChanged = (a, b) =>
            {
                ModelChangedHandler(true);
            };

            Fields[nameof(currentLowerVScale)].uiControlEditor.onFieldChanged =
            Fields[nameof(currentLowerVScale)].uiControlEditor.onSymmetryFieldChanged = (a, b) =>
            {
                ModelChangedHandler(true);
            };

            Fields[nameof(currentBottomVScale)].uiControlEditor.onFieldChanged =
            Fields[nameof(currentBottomVScale)].uiControlEditor.onSymmetryFieldChanged = (a, b) =>
            {
                ModelChangedHandler(true);
            };

            Fields[nameof(currentTopRotation)].uiControlEditor.onFieldChanged =
            Fields[nameof(currentTopRotation)].uiControlEditor.onSymmetryFieldChanged = (a, b) =>
            {
                ModelChangedHandler(true);
            };

            Fields[nameof(currentBottomRotation)].uiControlEditor.onFieldChanged =
            Fields[nameof(currentBottomRotation)].uiControlEditor.onSymmetryFieldChanged = (a, b) =>
            {
                ModelChangedHandler(true);
            };

            Fields[nameof(currentHabitat)].uiControlEditor.onFieldChanged =
            Fields[nameof(currentHabitat)].uiControlEditor.onSymmetryFieldChanged = (a, b) =>
            {
                //UpdateHabitatVolume();
                ModelChangedHandler(true);
            };

            Fields[nameof(currentCore)].uiControlEditor.onFieldChanged =
                Fields[nameof(currentCore)].uiControlEditor.onSymmetryFieldChanged = OnModelSelectionChanged;
            Fields[nameof(currentTop)].uiControlEditor.onFieldChanged =
                Fields[nameof(currentTop)].uiControlEditor.onSymmetryFieldChanged = OnModelSelectionChanged;            
            Fields[nameof(currentUpper)].uiControlEditor.onFieldChanged =
                Fields[nameof(currentUpper)].uiControlEditor.onSymmetryFieldChanged = OnModelSelectionChanged;
            Fields[nameof(currentLower)].uiControlEditor.onFieldChanged =
                Fields[nameof(currentLower)].uiControlEditor.onSymmetryFieldChanged = OnModelSelectionChanged;
            Fields[nameof(currentBottom)].uiControlEditor.onFieldChanged =
                Fields[nameof(currentBottom)].uiControlEditor.onSymmetryFieldChanged = OnModelSelectionChanged;

            //------------------MODEL DIAMETER / LENGTH SWITCH UI INIT---------------------//
            this.ROLupdateUIFloatEditControl(nameof(currentDiameter), minDiameter, maxDiameter, diameterLargeStep, diameterSmallStep, diameterSlideStep);
            this.ROLupdateUIFloatEditControl(nameof(currentHabitat), habMinPercent, habMaxPercent, 15f, 5f, 1f);

            Fields[nameof(currentDiameter)].guiActiveEditor = maxDiameter != minDiameter;
            Fields[nameof(currentVScale)].guiActiveEditor = enableVScale;
            Fields[nameof(currentTopVScale)].guiActiveEditor = enableTopVScale;
            Fields[nameof(currentUpperVScale)].guiActiveEditor = enableUpperVScale;
            Fields[nameof(currentLowerVScale)].guiActiveEditor = enableLowerVScale;
            Fields[nameof(currentBottomVScale)].guiActiveEditor = enableBottomVScale;
            Fields[nameof(currentTopRotation)].guiActiveEditor = topModule.moduleCanRotate;
            Fields[nameof(currentBottomRotation)].guiActiveEditor = bottomModule.moduleCanRotate;
            Fields[nameof(currentHabitat)].guiActiveEditor = coreModule.moduleCanAdjustHab;
            Events[nameof(ToggleExercise)].guiActiveEditor = coreModule.moduleCanExercise;

            //------------------MODULE TEXTURE SWITCH UI INIT---------------------//
            Fields[nameof(currentTopTexture)].uiControlEditor.onFieldChanged = topModule.textureSetSelected;
            Fields[nameof(currentUpperTexture)].uiControlEditor.onFieldChanged = upperModule.textureSetSelected;
            Fields[nameof(currentCoreTexture)].uiControlEditor.onFieldChanged = coreModule.textureSetSelected;
            Fields[nameof(currentLowerTexture)].uiControlEditor.onFieldChanged = lowerModule.textureSetSelected;
            Fields[nameof(currentBottomTexture)].uiControlEditor.onFieldChanged = bottomModule.textureSetSelected;

            // FIXME
            /*
            if (HighLogic.LoadedSceneIsEditor)
            {
                GameEvents.onPartActionUIDismiss.Add(OnPawClose);
            }
            */
        }

        private void ValidateModules()
        {
            string coreStyle = coreModule.definition.style;
            if (validateUpper && !coreModule.isValidModel(upperModule, coreStyle))
            {
                ROLModelDefinition def = coreModule.findFirstValidModel(upperModule, coreStyle);
                if (def == null) ROLLog.error("Could not locate valid definition for UPPER");
                upperModule.modelSelected(def.name);
            }
            if (validateLower && !coreModule.isValidModel(lowerModule, coreStyle))
            {
                ROLModelDefinition def = coreModule.findFirstValidModel(lowerModule, coreStyle);
                if (def == null) ROLLog.error("Could not locate valid definition for LOWER");
                lowerModule.modelSelected(def.name);
            }
            if (validateTop && !upperModule.isValidModel(topModule, upperModule.definition.style))
            {
                ROLModelDefinition def = upperModule.findFirstValidModel(topModule, upperModule.definition.style);
                if (def == null) ROLLog.error("Could not locate valid definition for TOP");
                topModule.modelSelected(def.name);
            }
            if (validateBottom && !lowerModule.isValidModel(bottomModule, lowerModule.definition.style))
            {
                ROLModelDefinition def = lowerModule.findFirstValidModel(bottomModule, lowerModule.definition.style);
                if (def == null) ROLLog.error("Could not locate valid definition for BOTTOM");
                bottomModule.modelSelected(def.name);
            }
        }

        private void SetPreviousModuleLength()
        {
            prevDiameter = currentDiameter;
            prevTop = topModule.moduleHeight;
            prevUpper = upperModule.moduleHeight;
            prevCore = coreModule.moduleHeight;
            prevLower = lowerModule.moduleHeight;
            prevBottom = bottomModule.moduleHeight;
        }

        public void OnModelSelectionChanged(BaseField f, object o)
        {
            if (f.name == Fields[nameof(currentCore)].name) coreModule.modelSelected(currentCore);
            else if (f.name == Fields[nameof(currentUpper)].name) upperModule.modelSelected(currentUpper);
            else if (f.name == Fields[nameof(currentLower)].name) lowerModule.modelSelected(currentLower);
            else if (f.name == Fields[nameof(currentTop)].name) topModule.modelSelected(currentTop);
            else if (f.name == Fields[nameof(currentBottom)].name) bottomModule.modelSelected(currentBottom);

            ModelChangedHandler(true);
            MonoUtilities.RefreshPartContextWindow(part);
        }

        private void OnDiameterChanged(BaseField f, object o)
        {
            // KSP 1.7.3 bug, symmetry invocations will have o=newValue instead of previousValue
            if ((float)f.GetValue(this) == prevDiameter) return;
            ModelChangedHandler(true);
        }

        /// <summary>
        /// Update the scale and position values for all currently configured models.  Does no validation, only updates positions.<para/>
        /// After calling this method, all models will be scaled and positioned according to their internal position/scale values and the orientations/offsets defined in the models.
        /// </summary>
        public void UpdateModulePositions()
        {
            //scales for modules depend on the module above/below them
            //first set the scale for the core module -- this depends directly on the UI specified 'diameter' value.

            coreModule.RescaleToDiameter(currentDiameter, coreModule.definition.diameter, currentVScale);
            upperModule.RescaleToDiameter(coreModule.moduleUpperDiameter, upperModule.moduleLowerDiameter / upperModule.moduleHorizontalScale, currentUpperVScale);
            lowerModule.RescaleToDiameter(coreModule.moduleLowerDiameter, lowerModule.moduleUpperDiameter / lowerModule.moduleHorizontalScale, currentLowerVScale);
            topModule.RescaleToDiameter(upperModule.moduleUpperDiameter, topModule.moduleLowerDiameter / topModule.moduleHorizontalScale, currentTopVScale);
            bottomModule.RescaleToDiameter(lowerModule.moduleLowerDiameter, bottomModule.moduleUpperDiameter / bottomModule.moduleHorizontalScale, currentBottomVScale);

            float totalHeight = topModule.moduleHeight + upperModule.moduleHeight + coreModule.moduleHeight + lowerModule.moduleHeight + bottomModule.moduleHeight;

            //position of each module is set such that the vertical center of the models is at part origin/COM
            float pos = totalHeight * 0.5f;//abs top of model
            Vector3 rot = new Vector3(0f, 0f, 0f);

            pos -= topModule.moduleHeight; //bottom of Top Model
            rot.y = currentTopRotation;
            topModule.SetPosition(pos);
            topModule.SetRotation(rot);

            pos -= upperModule.moduleHeight;
            upperModule.SetPosition(pos);

            pos -= coreModule.moduleHeight * 0.5f;//center of 'core' model
            coreModule.SetPosition(pos);

            pos -= coreModule.moduleHeight * 0.5f;//bottom of 'core' model
            lowerModule.SetPosition(pos);

            pos -= lowerModule.moduleHeight;
            rot.y = currentBottomRotation;
            bottomModule.SetPosition(pos);
            bottomModule.SetRotation(rot);
        }

        public void UpdateModelMeshes()
        {
            coreModule.UpdateModelScalesAndLayoutPositions();
            topModule.UpdateModelScalesAndLayoutPositions();
            upperModule.UpdateModelScalesAndLayoutPositions();
            lowerModule.UpdateModelScalesAndLayoutPositions();
            bottomModule.UpdateModelScalesAndLayoutPositions();
        }

        /// <summary>
        /// Update the cached modifiedMass field values.  Used with stock mass modifier interface.<para/>
        /// </summary>
        public void UpdateMass()
        {
            modifiedMass = coreModule.moduleMass + topModule.moduleMass + upperModule.moduleMass + lowerModule.moduleMass + bottomModule.moduleMass;
            if (exerciseEnabled) modifiedMass += exerciseMass;
            modifiedMass += StationTypeMass(coreModule) + StationTypeMass(topModule) + StationTypeMass(upperModule) + StationTypeMass(lowerModule) + StationTypeMass(bottomModule);
        }

        /// <summary>
        /// Update the cached modifiedCost field values.  Used with stock cost modifier interface.<para/>
        /// </summary>
        public void UpdateCost()
        {
            float totalHabVol = 0f;
            float totalTrussVol = 0f;
            float totalCrewTubeVol = 0f;

            foreach (var mod in moduleList)
            {
                switch (mod.moduleStationType)
                {
                    case StationType.Hab:
                        totalHabVol += mod.moduleTotalVolume;
                        break;
                    case StationType.Truss:
                        totalTrussVol += mod.moduleTotalVolume;
                        break;
                    case StationType.CrewTube:
                        totalCrewTubeVol += mod.moduleTotalVolume;
                        break;
                    default:
                        break;
                }
            }

            ROLLog.debug($"modifiedVolume: {modifiedVolume}");
            modifiedCost = coreModule.moduleCost + topModule.moduleCost + upperModule.moduleCost + lowerModule.moduleCost + bottomModule.moduleCost;
            modifiedCost += StationTypeCost((float)modifiedVolume, StationType.Hab) + StationTypeCost(totalTrussVol, StationType.Truss) + StationTypeCost(totalCrewTubeVol, StationType.CrewTube);
        }

        public float StationTypeMass(ROLModelModule<ModuleROStations> module)
        {
            float tempMass = 0f;
            switch (module.moduleStationType)
            {
                case StationType.Hab:
                    tempMass = module.moduleTotalVolume * habMassMultiplier;
                    break;
                case StationType.Truss:
                    tempMass = module.moduleTotalVolume * trussMassMultiplier;
                    break;
                case StationType.CrewTube:
                    tempMass = module.moduleTotalVolume * crewTubeMassMultiplier;
                    break;
                default:
                    break;
            }
            return tempMass;
        }
        public float StationTypeCost(float vol, StationType station)
        {
            float tempCost = 0f;
            switch (station)
            {
                case StationType.Hab:
                    tempCost = Mathf.Pow(vol, habCostFactor) * habCostBaseMultiplier;
                    break;
                case StationType.Truss:
                    tempCost = vol * trussCostMultiplier;
                    break;
                case StationType.CrewTube:
                    tempCost = vol * crewTubeCostMultiplier;
                    break;
                default:
                    break;
            }
            return tempCost;
        }

        /// <summary>
        /// Update the attach nodes for the current model-module configuration.
        /// The 'nose' module is responsible for updating of upper attach nodes, while the 'mount' module is responsible for lower attach nodes.
        /// Also includes updating of 'interstage' nose/mount attach nodes.
        /// Also includes updating of surface-attach node position.
        /// Also includes updating of any parts that are surface attached to this part.
        /// </summary>
        /// <param name="userInput"></param>
        public void UpdateAttachNodes(bool userInput)
        {
            //update the standard top and bottom attach nodes, using the node position(s) defined in the nose and mount modules
            topModule.UpdateAttachNode("top", ModelOrientation.TOP, userInput);
            bottomModule.UpdateAttachNode("bottom", ModelOrientation.BOTTOM, userInput);

            //update the model-module specific attach nodes, using the per-module node definitions from the part
            coreModule.updateAttachNodeBody(coreNodeNames, userInput);
            topModule.updateAttachNodeBody(topNodeNames, userInput);
            upperModule.updateAttachNodeBody(upperNodeNames, userInput);
            lowerModule.updateAttachNodeBody(lowerNodeNames, userInput);
            bottomModule.updateAttachNodeBody(bottomNodeNames, userInput);

            // Update the Top Interstage Node
            int nodeSize = Mathf.RoundToInt(coreModule.moduleDiameter) + 1;
            Vector3 pos = new Vector3(0, coreModule.ModuleTop, 0);
            ROLSelectableNodes.updateNodePosition(part, topInterstageNode, pos);
            if (part.FindAttachNode(topInterstageNode) is AttachNode topInterstage)
                ROLAttachNodeUtils.UpdateAttachNodePosition(part, topInterstage, pos, Vector3.up, userInput, nodeSize);

            // Update the Bottom Interstage Node
            nodeSize = Mathf.RoundToInt(coreModule.moduleDiameter) + 1;
            pos = new Vector3(0, coreModule.ModuleBottom, 0);
            ROLSelectableNodes.updateNodePosition(part, bottomInterstageNode, pos);
            if (part.FindAttachNode(bottomInterstageNode) is AttachNode bottomInterstage)
                ROLAttachNodeUtils.UpdateAttachNodePosition(part, bottomInterstage, pos, Vector3.down, userInput, nodeSize);

            // Update surface attach node position, part position, and any surface attached children
            if (part.srfAttachNode is AttachNode surfaceNode)
                coreModule.UpdateSurfaceAttachNode(surfaceNode, prevDiameter, prevTop, prevUpper, prevCore, prevLower, prevBottom, topModule.moduleHeight, upperModule.moduleHeight, coreModule.moduleHeight, lowerModule.moduleHeight, bottomModule.moduleHeight, userInput);
        }

        /// <summary>
        /// Return the total height of this part in its current configuration.  This will be the distance from the bottom attach node to the top attach node, and may not include any 'extra' structure. TOOLING
        /// </summary>
        /// <returns></returns>
        private float GetTotalHeight()
        {
            float totalHeight = topModule.moduleHeight + upperModule.moduleHeight + coreModule.moduleHeight + lowerModule.moduleHeight + bottomModule.moduleHeight;
            return totalHeight;
        }

        /// <summary>
        /// Update the UI visibility for the currently available selections.<para/>
        /// Will hide/remove UI fields for slots with only a single option (models, textures, layouts).
        /// </summary>
        public void UpdateAvailableVariants()
        {
            coreModule.updateSelections();
            upperModule.updateSelections();
            lowerModule.updateSelections();
            topModule.updateSelections();            
            bottomModule.updateSelections();
        }

        /// <summary>
        /// Calls the generic ROT procedural drag-cube updating routines.  Will update the drag cubes for whatever the current model state is.
        /// </summary>
        private void UpdateDragCubes() => ROLModInterop.OnPartGeometryUpdate(part, true);

        private float GetPartTopY()
        {
            return GetTotalHeight() * 0.5f;
        }

        private void ValidateRotation()
        {
            Fields[nameof(currentTopRotation)].guiActiveEditor = topModule.moduleCanRotate;
            Fields[nameof(currentBottomRotation)].guiActiveEditor = bottomModule.moduleCanRotate;
            if (!topModule.moduleCanRotate) currentTopRotation = 0f;
            if (!bottomModule.moduleCanRotate) currentBottomRotation = 0f;
        }

        private void ValidateVScale()
        {
            enableTopVScale = topModule.moduleCanVScale;
            enableUpperVScale = upperModule.moduleCanVScale;
            enableLowerVScale = lowerModule.moduleCanVScale;
            enableBottomVScale = bottomModule.moduleCanVScale;
            Fields[nameof(currentTopVScale)].guiActiveEditor = enableTopVScale;
            Fields[nameof(currentUpperVScale)].guiActiveEditor = enableUpperVScale;
            Fields[nameof(currentLowerVScale)].guiActiveEditor = enableLowerVScale;
            Fields[nameof(currentBottomVScale)].guiActiveEditor = enableBottomVScale;
            if (!enableTopVScale) currentTopVScale = 0f;
            if (!enableUpperVScale) currentUpperVScale = 0f;
            if (!enableLowerVScale) currentLowerVScale = 0f;
            if (!enableBottomVScale) currentBottomVScale = 0f;
        }

        public void SendVolumeChangedEvent(float newVol)
        {
            var data = new BaseEventDetails(BaseEventDetails.Sender.USER);
            data.Set<string>("volName", "Tankage");
            data.Set<double>("newTotalVolume", newVol);
            part.SendEvent("OnPartVolumeChanged", data, 0);
        }

        private void UpdateAmount(PartResource resource, double newAmount)
        {
            if (newAmount != resource.amount)
            {
                resource.amount = newAmount;
                RaiseResourceInitialChanged(resource.part, resource, newAmount);
            }

            if (resource.part.PartActionWindow?.ListItems.FirstOrDefault(r => r is UIPartActionResourceEditor e && resource == e.Resource) is UIPartActionResourceEditor resItem && resItem != null)
                resItem.resourceAmnt.text = KSPUtil.LocalizeNumber(newAmount, "F1");
        }

        private void UpdateMaxAmount(PartResource resource, double newAmount)
        {
            if (newAmount != resource.amount)
            {
                resource.maxAmount = newAmount;
                RaiseResourceMaxChanged(resource.part, resource, newAmount);
            }

            if (resource.part.PartActionWindow?.ListItems.FirstOrDefault(r => r is UIPartActionResourceEditor e && resource == e.Resource) is UIPartActionResourceEditor resItem && resItem != null)
                resItem.resourceMax.text = KSPUtil.LocalizeNumber(newAmount, "F1");
        }

        public void RaiseResourceInitialChanged(Part part, PartResource resource, double amount)
        {
            var data = new BaseEventDetails(BaseEventDetails.Sender.USER);
            data.Set<PartResource>("resource", resource);
            data.Set<double>("amount", amount);
            part.SendEvent("OnResourceInitialChanged", data, 0);
        }

        public void RaiseResourceMaxChanged(Part part, PartResource resource, double amount)
        {
            var data = new BaseEventDetails(BaseEventDetails.Sender.USER);
            data.Set<PartResource>("resource", resource);
            data.Set<double>("amount", amount);
            part.SendEvent("OnResourceMaxChanged", data, 0);
        }

        private void UpdateHabitatVolume()
        {
            float totalVol = 0f;

            if (noHab)
            {
                totalVol = coreModule.moduleVolume + upperModule.moduleVolume + lowerModule.moduleVolume + topModule.moduleVolume + bottomModule.moduleVolume;
                SendVolumeChangedEvent(totalVol);
                return;
            }

            float currentHabPercent = currentHabitat * 0.01f;
            float modVol = 0f;

            if (coreModule.moduleCanAdjustHab)
            {
                totalVol += coreModule.moduleVolume * (1 - currentHabPercent);
                modVol += coreModule.moduleVolume * currentHabPercent;
            }
            else
            {
                totalVol += coreModule.moduleVolume;
            }

            if (topModule.moduleCanAdjustHab)
            {
                totalVol += topModule.moduleVolume * (1 - currentHabPercent);
                modVol += topModule.moduleVolume * currentHabPercent;
            }
            else
            {
                totalVol += topModule.moduleVolume;
            }

            if (upperModule.moduleCanAdjustHab)
            {
                totalVol += upperModule.moduleVolume * (1 - currentHabPercent);
                modVol += upperModule.moduleVolume * currentHabPercent;
            }
            else
            {
                totalVol += upperModule.moduleVolume;
            }

            if (lowerModule.moduleCanAdjustHab)
            {
                totalVol += lowerModule.moduleVolume * (1 - currentHabPercent);
                modVol += lowerModule.moduleVolume * currentHabPercent;
            }
            else
            {
                totalVol += lowerModule.moduleVolume;
            }

            if (bottomModule.moduleCanAdjustHab)
            {
                totalVol += bottomModule.moduleVolume * (1 - currentHabPercent);
                modVol += bottomModule.moduleVolume * currentHabPercent;
            }
            else
            {
                totalVol += bottomModule.moduleVolume;
            }

            modifiedVolume = modVol;
            SendVolumeChangedEvent(totalVol);
            UpdateKerbalism();
        }

        private void UpdateKerbalism()
        {
            if (noHab) return;
            
            isHabitatModified = true;
            modifiedSurface = coreModule.moduleSurfaceArea + topModule.moduleSurfaceArea + upperModule.moduleSurfaceArea + lowerModule.moduleSurfaceArea + bottomModule.moduleSurfaceArea;

            if (exerciseEnabled)
            {
                if (modifiedVolume <= exerciseVolume) modifiedVolume = 0.0;

                modifiedVolume -= exerciseVolume;
            }

            if (coreModule.moduleHasPanorama) panoramaEnabled = true;
            if (coreModule.moduleHasPlants) plantsEnabled = true;

            OnEditorVolumeAndSurfaceModified();
        }

        private void KerbalismHabitatOnStartFinished()
        {
            if (!KerbalismInterface.isEnabled || noHab) return;

            PartModule habitat = part.Modules["Habitat"];

            if (habitat == null) return;

            KerbalismInterface.SetHabVolume(habitat, modifiedVolume);
            KerbalismInterface.SetHabSurface(habitat, modifiedSurface);

            KerbalismInterface.SetHabVolumeStr(habitat, KerbalismInterface.HumanReadableVolume(modifiedVolume));
            KerbalismInterface.SetHabSurfaceStr(habitat, KerbalismInterface.HumanReadableVolume(modifiedSurface));

            KerbalismInterface.SetHabShieldingCost(habitat, (float)modifiedSurface * PartResourceLibrary.Instance.GetDefinition("Shielding").unitCost);

            foreach (PartModule pm in part.Modules)
            {
                if (pm.GUIName == "Comfort")
                {
                    string bonus = KerbalismInterface.GetComfortBonus(pm);
                    if (bonus == "exercise" && exerciseEnabled) KerbalismInterface.SetComfortEnabled(pm, exerciseEnabled);
                    if (bonus == "panorama" && panoramaEnabled) KerbalismInterface.SetComfortEnabled(pm, panoramaEnabled);
                    if (bonus == "plants" && plantsEnabled) KerbalismInterface.SetComfortEnabled(pm, plantsEnabled);
                }
            }
        }

        private void OnEditorVolumeAndSurfaceModified()
        {
            if (!KerbalismInterface.isEnabled || noHab) return;

            PartModule habitat = part.Modules["Habitat"];

            if (habitat == null) return;

            if (atmoResource == null || wasteAtmoResource == null || shieldingResource == null)
            {
                ROLLog.debug($"Kerbalism habitat resources not found on {part.partInfo.name}");
                return;
            }

            KerbalismInterface.SetHabVolume(habitat, modifiedVolume);
            KerbalismInterface.SetHabSurface(habitat, modifiedSurface);

            KerbalismInterface.SetHabVolumeStr(habitat, KerbalismInterface.HumanReadableVolume(modifiedVolume));
            KerbalismInterface.SetHabSurfaceStr(habitat, KerbalismInterface.HumanReadableVolume(modifiedSurface));

            KerbalismInterface.SetHabShieldingCost(habitat, (float)modifiedSurface * PartResourceLibrary.Instance.GetDefinition("Shielding").unitCost);

            double modVolumeUnits = modifiedVolume * 1e3;
            double volumeFactor = modVolumeUnits / atmoResource.maxAmount;

            atmoResource.maxAmount = atmoResource.amount = modVolumeUnits;

            wasteAtmoResource.maxAmount = modVolumeUnits;
            wasteAtmoResource.amount = 0;

            UpdateMaxAmount(shieldingResource, modifiedSurface);
            double surfaceFactor = modifiedSurface / shieldingResource.maxAmount;
            UpdateAmount(shieldingResource, shieldingResource.amount *= surfaceFactor);

            foreach (PartModule pm in this.part.Modules)
            {
                if (pm.GUIName == "Comfort")
                {
                    string bonus = KerbalismInterface.GetComfortBonus(pm);
                    if (bonus == "exercise" && exerciseEnabled) KerbalismInterface.SetComfortEnabled(pm, exerciseEnabled);
                    if (bonus == "panorama" && panoramaEnabled) KerbalismInterface.SetComfortEnabled(pm, panoramaEnabled);
                    if (bonus == "plants" && plantsEnabled) KerbalismInterface.SetComfortEnabled(pm, plantsEnabled);
                }
            }

            GameEvents.onPartResourceListChange.Fire(part);
            GameEvents.onEditorShipModified.Fire(EditorLogic.fetch.ship);
        }

        #endregion ENDREGION - Custom Update Methods
    }
}
