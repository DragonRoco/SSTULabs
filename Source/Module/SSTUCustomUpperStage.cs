﻿using System;
using System.Collections.Generic;
using UnityEngine;

namespace SSTUTools
{
    public class SSTUCustomUpperStage : PartModule, IPartCostModifier, IPartMassModifier
    {

        #region ----------------- REGION - Standard KSP-accessible config fields -----------------
        /// <summary>
        /// quick/dirty/easy flag to determine if should even attempt to load/manipulate split-tank elements
        /// </summary>
        [KSPField]
        public bool splitTank = true;

        /// <summary>
        /// Set to true to allow RF to update the fuel quantity/type/etc
        /// </summary>
        [KSPField]
        public bool useRF = false;

        /// <summary>
        /// how much of the usable fuel volume is reserved for the reserve fuel type (for attitude control and fuel-cell use)
        /// </summary>
        [KSPField]
        public float fuelReserveRatio = 0.025f;

        /// <summary>
        /// the type of fuel to use as the reserve fuel; may be any of the SSTUFuelTypes
        /// </summary>
        [KSPField]
        public String reserveFuelType = "MP";

        /// <summary>
        /// How much is the 'height' incremented for every 'large' tank height step? - this value is further scaled based on the currently selected tank diameter
        /// </summary>
        [KSPField]
        public float tankHeightIncrement = 0.5f;

        /// <summary>
        /// How much is the 'diameter' incremented for every 'large' tank diameter step? - this value is -not- scaled, and used as-is.
        /// </summary>
        [KSPField]
        public float tankDiameterIncrement = 1.25f;

        /// <summary>
        /// Minimum tank height that can be set through VAB; this is just the adjustable tank portion and does not include any cap height
        /// </summary>
        [KSPField]
        public float minTankHeight = 1;

        /// <summary>
        /// Maximum tank height that may be set through the VAB; this is just the adjustable portion and does not include any cap height
        /// </summary>
        [KSPField]
        public float maxTankHeight = 5;

        /// <summary>
        /// Minimum tank diameter that may be set through the VAB
        /// </summary>
        [KSPField]
        public float minTankDiameter = 1.25f;

        /// <summary>
        /// Maximum tank diameter that may be set through the VAB
        /// </summary>
        [KSPField]
        public float maxTankDiameter = 10;

        /// <summary>
        /// Determines the diameter of the upper stage part.  Used to re-scale the input model to this diameter
        /// </summary>
        [KSPField]
        public float defaultTankDiameter = 2.5f;

        /// <summary>
        /// Default 'height' of the adjustable tank portion.
        /// </summary>
        [KSPField]
        public float defaultTankHeight = 1f;

        /// <summary>
        /// The default mount model to use when the upper stage is first initialized/pulled out of the editor; can be further adjusted by user in editor
        /// Mandatory field, -must- be populated by a valid mount option for this part.
        /// </summary>
        [KSPField]
        public String defaultMount = String.Empty;

        /// <summary>
        /// The default name entry for the intertank structure definition
        /// </summary>
        [KSPField]
        public String defaultIntertank = String.Empty;

        /// <summary>
        /// The default fuel type for a fresh part in the editor
        /// </summary>
        [KSPField]
        public String defaultFuelType = String.Empty;

        /// <summary>
        /// The thrust output of the RCS system at the default tank diameter; scaled using square-scaling methods to determine 
        /// </summary>
        [KSPField]
        public float defaultRcsThrust = 1;

        /// <summary>
        /// How much electric charge does the 'defaultDiameter' sized upper-stage hold?
        /// </summary>
        [KSPField]
        public float defaultElectricCharge = 300;

        [KSPField]
        public int topFairingIndex = 0;

        [KSPField]
        public int lowerFairingIndex = 1;

        [KSPField]
        public String interstageNodeName = "interstage";

        [KSPField]
        public String baseTransformName = "SSTUCustomUpperStageBaseTransform";

        [KSPField]
        public String rcsTransformName = "SSTUMUSRCS";

        #endregion

        #region ----------------- REGION - GUI visible fields and fine tune adjustment contols - do not edit through config ----------------- 
        /// <summary>
        /// Float field for fine-adjustment of tank height in the editor.  This particular value is restored from the 'currentTankHeight' and 'tankHeightIncrement' fields
        /// </summary>
        [KSPField(guiName = "Tank Height Adjust", guiActive = false, guiActiveEditor = true), UI_FloatRange(minValue = 0f, maxValue = 0.95f, stepIncrement = 0.05f)]
        public float editorTankHeightAdjust;

        [KSPField(guiName = "Tank Diameter Adjust", guiActive = false, guiActiveEditor = true), UI_FloatRange(minValue = 0f, maxValue = 0.95f, stepIncrement = 0.05f)]
        public float editorTankDiameterAdjust;

        [KSPField(guiName = "Tank Diameter", guiActive = false, guiActiveEditor = true)]
        public float guiDiameter;

        [KSPField(guiName = "Tank Height", guiActive = false, guiActiveEditor = true)]
        public float guiTankHeight;

        [KSPField(guiName = "Total Height", guiActive = false, guiActiveEditor = true)]
        public float guiTotalHeight;

        [KSPField(guiName = "Total Volume", guiActive = false, guiActiveEditor = true)]
        public float guiRawVolume;

        [KSPField(guiName = "Usable Volume", guiActive = false, guiActiveEditor = true)]
        public float guiFuelVolume;

        [KSPField(guiName = "Dry Mass", guiActive = false, guiActiveEditor = true)]
        public float guiDryMass;

        [KSPField(guiName = "RCS Thrust", guiActive = false, guiActiveEditor = true)]
        public float guiRcsThrust;
        #endregion

        #region ----------------- REGION - persistent data fields - do not edit through config ----------------- 
        //  --  NOTE  -- 
        // Below here are non-config-editable fields, used for persistance of the current settings; do not attempt to alter/adjust these through config, or things will -not- go as you expect

        /// <summary>
        /// Current absolute tank diameter (of the upper tank for split-tank, or of the full tank for common-bulkhead types)
        /// </summary>
        [KSPField(isPersistant = true)]
        public float currentTankDiameter = -1f;

        /// <summary>
        /// Current absolute (post-scale) height of the adjustable tank portion
        /// </summary>
        [KSPField(isPersistant = true)]
        public float currentTankHeight = 0f;

        /// <summary>
        /// The currently selected/enabled mount option.
        /// </summary>
        [KSPField(isPersistant = true)]
        public String currentMount = String.Empty;

        /// <summary>
        /// The currently selected/enabled intertank option (if any).
        /// </summary>
        [KSPField(isPersistant = true)]
        public String currentIntertank = String.Empty;

        /// <summary>
        /// The current RCS thrust; this value will be 'set' into the RCS module (if found/present)
        /// </summary>
        [KSPField(isPersistant = true)]
        public float currentRcsThrust = 0f;

        [KSPField(isPersistant = true)]
        public String currentFuelType = String.Empty;

        /// <summary>
        /// Used solely to track if resources have been initialized, as this should only happen once on first part creation (regardless of if it is created in flight or in the editor);
        /// Unsure of any cleaner way to track a simple boolean value across the lifetime of a part, seems like the part-persistence data is probably it...
        /// </summary>
        [KSPField(isPersistant = true)]
        public bool initializedResources = false;

        #endregion

        #region ----------------- REGION - Public unity-serialization fields ----------------- 

        //Fields below here are public/etc to be serialized by unity...

        /// <summary>
        /// Stashed copy of the raw config node data, to hack around KSP not passing in the modules base node data after prefab construction
        /// </summary>
        [Persistent]
        public String configNodeData = String.Empty;
        
        #endregion

        #region ----------------- REGION - Private working value fields ----------------- 
        
        //cached values for editor updating of height/diameter        
        private float editorTankHeight;
        private float prevEditorTankHeightAdjust;
        private float editorTankDiameter;
        private float prevEditorTankDiameterAdjust;
        
        //geometry related values, mostly for updating of fairings        
        private float partTopY;
        private float topFairingBottomY;
        private float partBottomY;
        private float bottomFairingTopY;

        //cached values for updating of part volume and mass
        private float totalTankVolume = 0;
        private float totalFuelVolume = 0;        
        private float tankageMass = 0;
        private float moduleMass = 0;
        private float tankCost = 0;
        private float rcsThrust = 0;

        // tech limit values are updated every time the part is initialized in the editor; ignored otherwise
        private float techLimitMaxHeight;
        private float techLimitMaxDiameter;

        //Private-instance-local fields for tracking the current/loaded config; basically parsed from configNodeData when config is loaded
        //upper, rcs, and mount must be present for every part
        private SSTUCustomUpperStagePart upperModule;
        private SSTUCustomUpperStageTopCap upperTopCapModule;
        private SSTUCustomUpperStagePart upperBottomCapModule;
        private SSTUCustomUpperStageRCS rcsModule;
        private MountModelData[] mountModules;
        private MountModelData currentMountModule;
        //lower and intertank need only be present for split-tank type parts
        private SSTUCustomUpperStagePart lowerModule;
        private SSTUCustomUpperStagePart lowerBottomCapModule;
        private SSTUCustomUpperStageIntertank[] intertankModules;
        private SSTUCustomUpperStageIntertank currentIntertankModule;
        
        private FuelTypeData[] fuelTypes;
        private FuelTypeData currentFuelTypeData;
        private FuelTypeData reserveFuelTypeData;

        private TechLimitDiameterHeight[] techLimits;

        private bool initialized = false;
        private bool initialFairingUpdate = false;
        #endregion

        #region ----------------- REGION - GUI methods ----------------- 

        [KSPEvent(guiName = "Prev Tank Diameter", guiActive = false, guiActiveEditor = true, active = true)]
        public void prevTankDiameterEvent()
        {            
            setTankDiameterFromEditor(currentTankDiameter - tankDiameterIncrement, true);
        }
               
        [KSPEvent(guiName = "Next Tank Diameter", guiActive = false, guiActiveEditor = true, active = true)]
        public void nextTankDiameterEvent()
        {
            setTankDiameterFromEditor(currentTankDiameter + tankDiameterIncrement, true);            
        }

        [KSPEvent(guiName = "Prev Tank Height", guiActive = false, guiActiveEditor = true, active = true)]
        public void prevTankHeightEvent()
        {
            float scalar = currentTankDiameter / defaultTankDiameter;
            updateTankHeightFromEditor(currentTankHeight - tankHeightIncrement * scalar, true);
        }
        
        [KSPEvent(guiName = "Next Tank Height", guiActive = false, guiActiveEditor = true, active = true)]
        public void nextTankHeightEvent()
        {
            float scalar = currentTankDiameter / defaultTankDiameter;
            updateTankHeightFromEditor(currentTankHeight + tankHeightIncrement * scalar, true);
        }
        
        [KSPEvent(guiName = "Prev Mount", guiActive = false, guiActiveEditor = true, active = true)]
        public void prevMountEvent()
        {
            MountModelData newDef = SSTUUtils.findNext(mountModules, l => l == currentMountModule, true);
            updateMountModelFromEditor(newDef);

            int moduleIndex = part.Modules.IndexOf(this);
            SSTUCustomUpperStage cus = null;
            foreach (Part p in part.symmetryCounterparts)
            {
                cus = (SSTUCustomUpperStage)p.Modules[moduleIndex];
                cus.updateMountModelFromEditor(newDef);
            }
            GameEvents.onEditorShipModified.Fire(EditorLogic.fetch.ship);
        }

        [KSPEvent(guiName = "Next Mount", guiActive = false, guiActiveEditor = true, active = true)]
        public void nextMountEvent()
        {
            MountModelData newDef = SSTUUtils.findNext(mountModules, l => l == currentMountModule, false);
            updateMountModelFromEditor(newDef);

            int moduleIndex = part.Modules.IndexOf(this);
            SSTUCustomUpperStage cus = null;
            foreach (Part p in part.symmetryCounterparts)
            {
                cus = (SSTUCustomUpperStage)p.Modules[moduleIndex];
                cus.updateMountModelFromEditor(newDef);
            }
            GameEvents.onEditorShipModified.Fire(EditorLogic.fetch.ship);
        }

        [KSPEvent(guiName = "Next Intertank", guiActive = false, guiActiveEditor = true, active = true)]
        public void nextIntertankEvent()
        {
            SSTUCustomUpperStageIntertank nextDef = SSTUUtils.findNext(intertankModules, l => l == currentIntertankModule, false);
            updateIntertankModelFromEditor(nextDef);

            int moduleIndex = part.Modules.IndexOf(this);
            SSTUCustomUpperStage cus = null;
            foreach (Part p in part.symmetryCounterparts)
            {
                cus = (SSTUCustomUpperStage)p.Modules[moduleIndex];
                cus.updateIntertankModelFromEditor(nextDef);
            }
            GameEvents.onEditorShipModified.Fire(EditorLogic.fetch.ship);
        }
                
        [KSPEvent(guiName = "Next Fuel Type", guiActive = false, guiActiveEditor = true, active = true)]
        public void nextFuelTypeEvent()
        {
            FuelTypeData nextFuelType = SSTUUtils.findNext(fuelTypes, l => l == currentFuelTypeData, false);
            updateFuelTypeFromEditor(nextFuelType);

            int moduleIndex = part.Modules.IndexOf(this);
            SSTUCustomUpperStage cus = null;
            foreach (Part p in part.symmetryCounterparts)
            {
                cus = (SSTUCustomUpperStage)p.Modules[moduleIndex];
                cus.updateFuelTypeFromEditor(nextFuelType);
            }
            GameEvents.onEditorShipModified.Fire(EditorLogic.fetch.ship);
        }

        #endregion

        #region ----------------- REGION - KSP Overrides ----------------- 

        /// <summary>
        /// OnLoad override.  Loads previously saved config data, stores module config node for later reading, and does pre-start initialization
        /// </summary>
        /// <param name="node"></param>
        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            if (node.HasNode("MOUNT"))
            {
                configNodeData = node.ToString();
            }
            initialize();
        }
        
        /// <summary>
        /// OnStart override, does basic startup/init stuff, including building models and registering for editor events
        /// </summary>
        /// <param name="state"></param>
        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            initialize();
            if (HighLogic.LoadedSceneIsEditor)
            {
                GameEvents.onEditorShipModified.Fire(EditorLogic.fetch.ship);
                GameEvents.onEditorShipModified.Add(new EventData<ShipConstruct>.OnEvent(onEditorVesselModified));
            }
        }

        public override string GetInfo()
        {
            return "This part has configurable diameter, height, bottom-cap, and fairings.";
        }

        /// <summary>
        /// Unity method override, supposedly called after -all- modules have had OnStart() called.
        /// Overriden to update fairing and RCS modules, which might not exist when OnStart() is called for this module
        /// </summary>
        public void Start()
        {
            updateFairing(initialFairingUpdate);
            updateRCSThrust();
        }
        
        /// <summary>
        /// Overriden/defined in order to remove the on-editor-ship-modified event from the game-event callback queue
        /// </summary>
        public void OnDestroy()
        {
            if (HighLogic.LoadedSceneIsEditor)
            {
                GameEvents.onEditorShipModified.Remove(new EventData<ShipConstruct>.OnEvent(onEditorVesselModified));
            }
        }
        
        /// <summary>
        /// Event callback for when vessel is modified in the editor.  Used to know when the gui-fields for this module have been updated.
        /// </summary>
        /// <param name="ship"></param>
        public void onEditorVesselModified(ShipConstruct ship)
        {
            if (!HighLogic.LoadedSceneIsEditor) { return; }
            bool updated = false;
            if (prevEditorTankHeightAdjust != editorTankHeightAdjust)
            {
                prevEditorTankHeightAdjust = editorTankHeightAdjust;
                float newHeight = editorTankHeight + (tankHeightIncrement * editorTankHeightAdjust);
                newHeight *= (currentTankDiameter / defaultTankDiameter);
                updateTankHeightFromEditor(newHeight, true);
                updated = true;
            }
            if (prevEditorTankDiameterAdjust != editorTankDiameterAdjust)
            {
                prevEditorTankDiameterAdjust = editorTankDiameterAdjust;
                float newDiameter = editorTankDiameter + (editorTankDiameterAdjust * tankDiameterIncrement);
                setTankDiameterFromEditor(newDiameter, true);
                updated = true;
            }
            if (!updated)
            {
                updateNodePositions(true);   
            }
        }

        /// <summary>
        /// Return the current part cost/modifier.  Returns the pre-calculated tank cost.
        /// </summary>
        /// <param name="defaultCost"></param>
        /// <returns></returns>
        public float GetModuleCost(float defaultCost)
        {
            return tankCost;
        }

        public float GetModuleMass(float defaultMass)
        {
            return -defaultMass + tankageMass;
        }

        #endregion
        
        #region ----------------- REGION - Initialization Methods ----------------- 

        /// <summary>
        /// Basic initialization code, should only be ran once per part-instance (though, is safe to call from both start and load)
        /// </summary>
        private void initialize()
        {
            if (initialized) { return; }
            initialized = true;
            SSTUUtils.destroyChildren(part.transform.FindRecursive(baseTransformName));
            if (currentTankDiameter==-1f)
            {
                //should only run once, on the prefab part; which is fine, as the models/settings will be cloned and carried over to the editor part
                currentTankDiameter = defaultTankDiameter;
                currentTankHeight = defaultTankHeight;
                currentMount = defaultMount;
                currentIntertank = defaultIntertank;
                currentRcsThrust = defaultRcsThrust;
                currentFuelType = defaultFuelType;
            }
            loadConfigData();
            updateTechLimits();

            if (currentTankDiameter > techLimitMaxDiameter)
            {
                currentTankDiameter = techLimitMaxDiameter;
            }
            float scalar = currentTankDiameter / defaultTankDiameter;
            if (currentTankHeight > techLimitMaxHeight * scalar)
            {
                currentTankHeight = techLimitMaxHeight * scalar;
            }

            updateModules(false);
            buildSavedModel();
            updateModels();
            updateTankStats();
            //determine if this is the first time the part-instance has ever been initialized; skip the prefab part (so, must be flight or editor).            
            if (!initializedResources && (HighLogic.LoadedSceneIsFlight || HighLogic.LoadedSceneIsEditor))
            {
                updatePartResources();
                initialFairingUpdate = true;
                initializedResources = true;
            }
            restoreEditorFields();
            updateGuiState();
        }
                
        /// <summary>
        /// Restores the editor-only diameter and height-adjustment values;
        /// </summary>
        private void restoreEditorFields()
        {
            float div = currentTankDiameter / tankDiameterIncrement;
            float whole = (int)div;
            float extra = div - whole;
            editorTankDiameter = whole * tankDiameterIncrement;
            prevEditorTankDiameterAdjust = editorTankDiameterAdjust = extra;

            float scale = currentTankDiameter / defaultTankDiameter;

            div = currentTankHeight / (tankHeightIncrement * scale);
            whole = (int)div;
            extra = div - whole;
            editorTankHeight = whole * tankHeightIncrement;
            prevEditorTankHeightAdjust = editorTankHeightAdjust = extra;            
        }
        
        /// <summary>
        /// Loads all of the part definitions and values from the stashed config node data
        /// </summary>
        private void loadConfigData()
        {
            ConfigNode node = SSTUNodeUtils.parseConfigNode(configNodeData);

            fuelTypes = FuelTypeData.parseFuelTypeData(node.GetNodes("FUELTYPE"));
            currentFuelTypeData = Array.Find(fuelTypes, l => l.name == currentFuelType);
            reserveFuelTypeData = FuelTypes.INSTANCE.getFuelTypeData(reserveFuelType);

            //mandatory nodes, -all- tank types must have these
            ConfigNode tankUpperNode = node.GetNode("TANKUPPER");
            ConfigNode upperTopCapNode = node.GetNode("TANKUPPERTOPCAP");
            ConfigNode upperBottomCapNode = node.GetNode("TANKUPPERBOTTOMCAP");
            ConfigNode[] limitNodes = node.GetNodes("TECHLIMIT");

            ConfigNode rcsNode = node.GetNode("RCS");
            ConfigNode[] mountNodes = node.GetNodes("MOUNT");
            
            upperModule = new SSTUCustomUpperStagePart(tankUpperNode);
            upperTopCapModule = new SSTUCustomUpperStageTopCap(upperTopCapNode);
            upperBottomCapModule = new SSTUCustomUpperStagePart(upperBottomCapNode);
            rcsModule = new SSTUCustomUpperStageRCS(rcsNode);

            //load mount configs
            int len = mountNodes.Length;
            mountModules = new MountModelData[len];
            for (int i = 0; i < len; i++)
            {
                mountModules[i] = new MountModelData(mountNodes[i], false);
            }
            currentMountModule = Array.Find(mountModules, l => l.name == currentMount);

            if (splitTank)
            {
                //fields that are only populated by split-tank type upper-stages
                ConfigNode tankLowerNode = node.GetNode("TANKLOWER");
                ConfigNode lowerBottomCapNode = node.GetNode("TANKLOWERBOTTOMCAP");
                ConfigNode[] intertankNodes = node.GetNodes("INTERTANK");
                lowerModule = new SSTUCustomUpperStagePart(tankLowerNode);
                lowerBottomCapModule = new SSTUCustomUpperStagePart(lowerBottomCapNode);
                //load intertank configs
                len = intertankNodes.Length;
                intertankModules = new SSTUCustomUpperStageIntertank[len];
                for (int i = 0; i < len; i++)
                {
                    intertankModules[i] = new SSTUCustomUpperStageIntertank(intertankNodes[i]);
                }
                currentIntertankModule = Array.Find(intertankModules, l => l.name == currentIntertank);
            }

            len = limitNodes.Length;
            techLimits = new TechLimitDiameterHeight[len];
            for (int i = 0; i < len; i++) { techLimits[i] = new TechLimitDiameterHeight(limitNodes[i]); }

            print("loaded config data, split tank: " + splitTank);
        }

        #endregion

        #region ----------------- REGION - Module Position / Parameter Updating ----------------- 

        /// <summary>
        /// Updates the internal cached scale of each of the modules; applied to models later
        /// </summary>
        private void updateModuleScales()
        {
            upperTopCapModule.updateScaleForDiameter(currentTankDiameter);
            upperModule.updateScaleForHeightAndDiameter(currentTankHeight, currentTankDiameter);
            upperBottomCapModule.updateScaleForDiameter(currentTankDiameter);

            float mountDiameterScale = currentTankDiameter;
            if (splitTank)
            {
                currentIntertankModule.updateScaleForDiameter(currentTankDiameter);
                float lowerDiameter = currentTankDiameter * 0.75f;
                float lowerHeight = currentTankHeight * 0.75f;
                mountDiameterScale = lowerDiameter;
                lowerModule.updateScaleForHeightAndDiameter(lowerHeight, lowerDiameter);
                lowerBottomCapModule.updateScaleForDiameter(lowerDiameter);
            }            
            currentMountModule.updateScaleForDiameter(mountDiameterScale);
            rcsModule.updateScaleForDiameter(mountDiameterScale);
        }
                
        /// <summary>
        /// Updated the modules internal cached position value.  This value is used later to update the actual model positions.
        /// </summary>
        private void updateModulePositions()
        {
            float totalHeight = 0;
            totalHeight += upperTopCapModule.currentHeight;
            totalHeight += upperModule.currentHeight;
            totalHeight += upperBottomCapModule.currentHeight;

            if (splitTank)
            {
                totalHeight += currentIntertankModule.currentHeight;
                totalHeight += lowerModule.currentHeight;
                totalHeight += lowerBottomCapModule.currentHeight;
            }
            totalHeight += currentMountModule.currentHeight;
            
            float startY = totalHeight * 0.5f;
            partTopY = startY;
                        
            topFairingBottomY = partTopY - upperTopCapModule.currentHeight + (upperTopCapModule.fairingOffset * upperTopCapModule.currentHeightScale);
            partBottomY = -startY;           

            startY -= upperTopCapModule.currentHeight;
            upperTopCapModule.currentVerticalPosition = startY;            
            
            startY -= upperModule.currentHeight * 0.5f;
            upperModule.currentVerticalPosition = startY;
            startY -= upperModule.currentHeight * 0.5f;         
            upperBottomCapModule.currentVerticalPosition = startY;

            startY -= upperBottomCapModule.currentHeight;
            if (splitTank)
            {
                currentIntertankModule.currentVerticalPosition = startY;
                startY -= currentIntertankModule.currentHeight;
                startY -= lowerModule.currentHeight * 0.5f;
                lowerModule.currentVerticalPosition = startY;
                startY -= lowerModule.currentHeight * 0.5f;
                lowerBottomCapModule.currentVerticalPosition = startY;
                startY -= lowerBottomCapModule.currentHeight;
            }

            currentMountModule.currentVerticalPosition = startY;
            rcsModule.currentVerticalPosition = currentMountModule.currentVerticalPosition + (currentMountModule.mountDefinition.rcsVerticalPosition * currentMountModule.currentHeightScale);
            rcsModule.currentHorizontalPosition = currentMountModule.mountDefinition.rcsHorizontalPosition * currentMountModule.currentDiameterScale;
            rcsModule.mountVerticalRotation = currentMountModule.mountDefinition.rcsVerticalRotation;
            rcsModule.mountHorizontalRotation = currentMountModule.mountDefinition.rcsHorizontalRotation;

            if (splitTank)
            {
                bottomFairingTopY = currentIntertankModule.currentVerticalPosition;
                bottomFairingTopY -= currentIntertankModule.fairingOffset * currentMountModule.currentHeightScale;
            }
            else
            {
                bottomFairingTopY = currentMountModule.currentVerticalPosition;
                bottomFairingTopY += currentMountModule.mountDefinition.fairingTopOffset * currentMountModule.currentHeightScale;
            }
        }

        /// <summary>
        /// Blanket method for when module parameters have changed (heights, diameters, mounts, etc)
        /// updates
        /// Does not update fuel/resources/mass
        /// </summary>
        private void updateModules(bool userInput)
        {
            updateModuleScales();
            updateModulePositions();
            updateNodePositions(userInput);
            updateFairing(userInput);
        }
                
        /// <summary>
        /// Update the fairing module height and position based on current tank parameters
        /// </summary>
        private void updateFairing(bool userInput)
        {
            SSTUNodeFairing[] modules = part.GetComponents<SSTUNodeFairing>();
            if (modules == null || modules.Length < 2) { return; }
            SSTUNodeFairing topFairing = modules[topFairingIndex];
            if (topFairing != null && topFairing.initialized())
            {
                topFairing.setFairingTopY(partTopY);
                topFairing.setFairingBottomY(topFairingBottomY);
                if (userInput)
                {
                    topFairing.setFairingTopRadius(currentTankDiameter * 0.5f);
                }
                topFairing.setFairingBottomRadius(currentTankDiameter * 0.5f);
                
            }            
            SSTUNodeFairing bottomFairing = modules[lowerFairingIndex];
            if (bottomFairing != null && bottomFairing.initialized())
            {
                bottomFairing.setFairingTopRadius(currentTankDiameter * 0.5f);
                bottomFairing.setFairingTopY(bottomFairingTopY);
                if (userInput) { bottomFairing.setFairingBottomRadius(currentTankDiameter * 0.5f); }
            }
        }

        /// <summary>
        /// Update the attach node positions based on the current tank parameters.
        /// </summary>
        private void updateNodePositions(bool userInput)
        {
            AttachNode topNode = part.findAttachNode("top");
            SSTUUtils.updateAttachNodePosition(part, topNode, new Vector3(0, partTopY, 0), topNode.orientation, userInput);

            AttachNode topNode2 = part.findAttachNode("top2");
            SSTUUtils.updateAttachNodePosition(part, topNode2, new Vector3(0, topFairingBottomY, 0), topNode2.orientation, userInput);

            AttachNode bottomNode = part.findAttachNode("bottom");
            SSTUUtils.updateAttachNodePosition(part, bottomNode, new Vector3(0, partBottomY, 0), bottomNode.orientation, userInput);

            AttachNode interstage = part.findAttachNode(interstageNodeName);
            if (interstage != null)
            {                
                Vector3 pos = new Vector3(0, bottomFairingTopY, 0);
                Vector3 orientation = new Vector3(0, -1, 0);
                SSTUUtils.updateAttachNodePosition(part, interstage, pos, orientation, userInput);
            }
        }

        #endregion

        #region ----------------- REGION - Model Build / Updating ----------------- 

        /// <summary>
        /// Builds the model from the current/default settings, and/or restores object links from existing game-objects
        /// </summary>
        private void buildSavedModel()
        {
            Transform modelBase = part.transform.FindRecursive("model").FindOrCreate(baseTransformName);

            setupModel(upperTopCapModule, modelBase);
            setupModel(upperModule, modelBase);
            setupModel(upperBottomCapModule, modelBase);
            
            if (splitTank)
            {
                if (currentIntertankModule.name != defaultIntertank)
                {
                    SSTUCustomUpperStageIntertank dim = Array.Find<SSTUCustomUpperStageIntertank>(intertankModules, l => l.name == defaultIntertank);
                    dim.setupModel(part, modelBase);
                    removeCurrentModel(dim);
                }
                setupModel(currentIntertankModule, modelBase);
                setupModel(lowerModule, modelBase);
                setupModel(lowerBottomCapModule, modelBase);
            }
            if (currentMountModule.name != defaultMount)
            {
                MountModelData dmm = Array.Find<MountModelData>(mountModules, l => l.name == defaultMount);
                dmm.setupModel(part, modelBase);
                removeCurrentModel(dmm);
            }

            setupModel(currentMountModule, modelBase);
            setupModel(rcsModule, part.transform.FindRecursive("model").FindOrCreate(rcsTransformName));
        }

        /// <summary>
        /// Finds the model for the given part, if it currently exists; else it clones it
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        private void setupModel(ModelData model, Transform parent)
        {
            model.setupModel(part, parent);
        }

        /// <summary>
        /// Removes the current model of the passed in upper-stage part; used when switching mounts or intertank parts
        /// </summary>
        /// <param name="usPart"></param>
        private void removeCurrentModel(ModelData usPart)
        {
            usPart.destroyCurrentModel();
        }

        /// <summary>
        /// Updates models from module current parameters for scale and positioning
        /// </summary>
        private void updateModels()
        {
            upperTopCapModule.updateModel();
            upperModule.updateModel();
            upperBottomCapModule.updateModel();

            if (splitTank)
            {
                currentIntertankModule.updateModel();
                lowerModule.updateModel();
                lowerBottomCapModule.updateModel();
            }

            currentMountModule.updateModel();
            rcsModule.updateModel();

            SSTUUtils.updatePartHighlighting(part);
        }

        /// <summary>
        /// Editor callback method for when tank height changes.  Updates model positions, attach node/attached part positions, 
        /// </summary>
        private void updateTankHeightFromEditor(float newHeight, bool updateSymmetry)
        {
            float scalar = currentTankDiameter / defaultTankDiameter;
            if (newHeight > scalar * maxTankHeight) { newHeight = scalar * maxTankHeight; }
            if (newHeight < scalar * minTankHeight) { newHeight = scalar * minTankHeight; }
            if (SSTUUtils.isResearchGame() && newHeight > techLimitMaxHeight * scalar) { newHeight = techLimitMaxHeight*scalar; }
            currentTankHeight = newHeight;
            restoreEditorFields();
            updateModules(true);
            updateModels();
            updateTankStats();
            updatePartResources();
            updateGuiState();

            if (updateSymmetry)
            {
                foreach (Part p in part.symmetryCounterparts)
                {
                    p.GetComponent<SSTUCustomUpperStage>().updateTankHeightFromEditor(newHeight, false);
                }
            }
            GameEvents.onEditorShipModified.Fire(EditorLogic.fetch.ship);
        }

        /// <summary>
        /// Updates the current tank diameter from user input.  Subsequently updates internal and GUI variables, and redoes the setup for the part resources
        /// </summary>
        private void setTankDiameterFromEditor(float newDiameter, bool updateSymmetry)
        {
            if (newDiameter > maxTankDiameter)
            {
                newDiameter = maxTankDiameter;
            }
            if (newDiameter < minTankDiameter)
            {
                newDiameter = minTankDiameter;
            }
            if (SSTUUtils.isResearchGame() && newDiameter > techLimitMaxDiameter) { newDiameter = techLimitMaxDiameter; }

            float scalar = currentTankDiameter / defaultTankDiameter;
            float inverseScalar = 1 / scalar;

            currentTankDiameter = newDiameter;
            scalar = currentTankDiameter / defaultTankDiameter;

            float scaledHeight = currentTankHeight * inverseScalar;
            currentTankHeight = scalar * scaledHeight;


            restoreEditorFields();
            updateModules(true);
            updateModels();
            updateTankStats();
            updatePartResources();
            updateGuiState();

            if (updateSymmetry)
            {
                foreach (Part p in part.symmetryCounterparts)
                {
                    p.GetComponent<SSTUCustomUpperStage>().setTankDiameterFromEditor(newDiameter, false);
                }
            }
            GameEvents.onEditorShipModified.Fire(EditorLogic.fetch.ship);
        }

        /// <summary>
        /// Updates the selected mount model from user input
        /// </summary>
        /// <param name="nextDef"></param>
        private void updateMountModelFromEditor(MountModelData nextDef)
        {
            removeCurrentModel(currentMountModule);
            currentMountModule = nextDef;
            currentMount = nextDef.name;
            setupModel(currentMountModule, part.transform.FindRecursive("model").FindOrCreate(baseTransformName));
            updateModules(true);
            updateModels();
            updateFuelVolume();
            updatePartResources();
            updateGuiState();
        }

        /// <summary>
        /// Updates the current intertank mesh/model from user input
        /// </summary>
        /// <param name="newDef"></param>
        private void updateIntertankModelFromEditor(SSTUCustomUpperStageIntertank newDef)
        {
            removeCurrentModel(currentIntertankModule);
            currentIntertankModule = newDef;
            currentIntertank = newDef.name;
            setupModel(currentIntertankModule, part.transform.FindRecursive("model").FindOrCreate(baseTransformName));
            updateModules(true);
            updateModels();
            updateTankStats();
            updatePartResources();
            updateGuiState();
        }

        /// <summary>
        /// Updates the current fuel type from user input
        /// </summary>
        /// <param name="newFuelType"></param>
        private void updateFuelTypeFromEditor(FuelTypeData newFuelType)
        {
            currentFuelTypeData = newFuelType;
            currentFuelType = newFuelType.name;
            updateTankStats();
            updatePartResources();
            updateGuiState();
        }

        #endregion

        #region ----------------- REGION - Tank stats updating - volume/mass/cost/thrust ----------------- 

        /// <summary>
        /// Wrapper for methods that update the tanks internal statistics -- mass, volume, etc.  Does NOT update part resources.
        /// Calls: updateFuelVolume(); updatePartMass; updatePartCost; updateRCSThrust;
        /// </summary>
        private void updateTankStats()
        {
            updateFuelVolume();
            updateModuleMass();
            updateModuleCost();
            updateRCSThrust();
            updatePartMass();
        }
        
        /// <summary>
        /// Calculates the internal volume from all of the currentl selected parts, their configurations, and their current scales
        /// </summary>
        private void updateFuelVolume()
        {
            totalTankVolume = 0;

            totalTankVolume += upperTopCapModule.getModuleVolume();
            totalTankVolume += upperModule.getModuleVolume();
            totalFuelVolume += upperBottomCapModule.getModuleVolume();
            if(splitTank)
            {
                totalTankVolume += currentIntertankModule.getModuleVolume();
                totalTankVolume += lowerModule.getModuleVolume();
                totalTankVolume += lowerBottomCapModule.getModuleVolume();
            }
            totalTankVolume += currentMountModule.getModuleVolume();
            if (useRF)
            {
                SSTUUtils.updateRealFuelsPartVolume(part, totalTankVolume);
            }
            //update usable fuel volume, tankage mass, dry mass, etc
            totalFuelVolume = currentFuelTypeData.getUsableVolume(totalTankVolume);
        }

        /// <summary>
        /// Updates the cached part-mass value from the calculated masses of the current modules/tank setup.  Safe to call..whenever.
        /// Does -not- update part masses.  See -updatePartMass()- for that function.
        /// </summary>
        private void updateModuleMass()
        {
            tankageMass = currentFuelTypeData.getTankageMass(totalFuelVolume);
            moduleMass = 0;
            moduleMass += upperTopCapModule.getModuleMass() + upperModule.getModuleMass() + upperBottomCapModule.getModuleMass() + currentMountModule.getModuleMass() + rcsModule.getModuleMass();
            if (splitTank)
            {
                moduleMass += currentIntertankModule.getModuleMass() + lowerModule.getModuleMass() + lowerBottomCapModule.getModuleMass();
            }
        }
                
        /// <summary>
        /// Updates the tankCost field with the current cost for the selected fuel type and tank size, including cost for tankage
        /// </summary>
        private void updateModuleCost()
        {
            float reserveFuelVolume = totalFuelVolume * fuelReserveRatio;
            float fuelUsableVolume = totalFuelVolume - reserveFuelVolume;
            tankCost = 0;
            tankCost += currentFuelTypeData.getResourceCost(fuelUsableVolume);
            tankCost += reserveFuelTypeData.getResourceCost(reserveFuelVolume);
            tankCost += currentFuelTypeData.getDryCost(fuelUsableVolume);
            tankCost += reserveFuelTypeData.getDryCost(reserveFuelVolume);

            tankCost += upperTopCapModule.getModuleCost() + upperModule.getModuleCost() + upperBottomCapModule.getModuleCost() + currentMountModule.getModuleCost() + rcsModule.getModuleCost();
            if (splitTank)
            {
                tankCost += currentIntertankModule.getModuleCost() + lowerModule.getModuleCost() + lowerBottomCapModule.getModuleCost();
            }
        }
                
        /// <summary>
        /// update external RCS-module with thrust value;
        /// TODO - may need to cache the 'needs update' flag, and run on first OnUpdate/etc, as otherwise the RCS module will likely not exist yet
        /// </summary>
        private void updateRCSThrust()
        {
            ModuleRCS rcsMod = part.GetComponent<ModuleRCS>();
            if (rcsMod != null)
            {
                float scale = currentTankDiameter / defaultTankDiameter;
                rcsThrust = defaultRcsThrust * scale * scale;
                rcsMod.thrusterPower = rcsThrust;                
            }
        }

        /// <summary>
        /// Updates current gui button availability status as well as updating the visible GUI variables from internal state vars
        /// </summary>
        private void updateGuiState()
        {
            if (!splitTank || intertankModules == null || intertankModules.Length <= 1)
            {
                Events["nextIntertankEvent"].active = false;
            }
            if (fuelTypes.Length <= 1)
            {
                Events["nextFuelTypeEvent"].active = false;
            }
            if (useRF)
            {
                Fields["guiRawVolume"].guiActiveEditor = false;
                Fields["guiFuelVolume"].guiActiveEditor = false;
                Fields["guiDryMass"].guiActiveEditor = false;
                Events["nextFuelTypeEvent"].active = false;
            }

            guiRawVolume = totalTankVolume;
            guiFuelVolume = totalFuelVolume;
            guiDryMass = tankageMass + moduleMass;
            guiDiameter = currentTankDiameter;
            guiTotalHeight = partTopY + Math.Abs(partBottomY);
            guiTankHeight = upperModule.currentHeight;
            guiRcsThrust = rcsThrust;
        }

        #endregion

        #region ----------------- REGION - Part Updating - Resource/Mass ----------------- 

        /// <summary>
        /// Updates the min/max quantities of resource in the part based on the current 'totalFuelVolume' field and currently set fuel type
        /// </summary>
        private void updatePartResources()
        {
            if (useRF)
            {
                return;
            }
            float reserveFuelVolume = totalFuelVolume * fuelReserveRatio;
            float fuelUsableVolume = totalFuelVolume - reserveFuelVolume;
            float currentDiameterScale = currentTankDiameter / defaultTankDiameter;
            float currentHeightScale = currentTankHeight / defaultTankHeight;
            float energyReserve = defaultElectricCharge * currentDiameterScale * currentHeightScale;
            SSTUResourceList resourceList = new SSTUResourceList();
            currentFuelTypeData.addResources(fuelUsableVolume, resourceList);
            reserveFuelTypeData.addResources(reserveFuelVolume, resourceList);
            resourceList.addResource("ElectricCharge", energyReserve);
            resourceList.setResourcesToPart(part, true);
        }

        /// <summary>
        /// Update the tech limitations for this part
        /// </summary>
        private void updateTechLimits()
        {
            techLimitMaxDiameter = float.PositiveInfinity;
            techLimitMaxHeight = float.PositiveInfinity;
            if (!SSTUUtils.isResearchGame()) { return; }
            if (HighLogic.CurrentGame == null) { return; }
            techLimitMaxDiameter = 0;
            techLimitMaxHeight = 0;
            foreach (TechLimitDiameterHeight limit in techLimits)
            {
                if (limit.isUnlocked())
                {
                    if (limit.maxDiameter > techLimitMaxDiameter) { techLimitMaxDiameter = limit.maxDiameter; }
                    if (limit.maxHeight > techLimitMaxHeight) { techLimitMaxHeight = limit.maxHeight; }
                }
            }
        }

        /// <summary>
        /// Updates the current part.mass value, should only be called during first init, or on user-input/changes in the VAB; should not be re-called after that.
        /// </summary>
        private void updatePartMass()
        {
            if (!useRF)
            {
                part.mass = tankageMass + moduleMass;
            }
        }
        
        #endregion

    }

    public class SSTUCustomUpperStageIntertank : SSTUCustomUpperStagePart
    {
        public float ratio = 0.75f;
        public float fairingOffset = 0.4f;
        public SSTUCustomUpperStageIntertank(ConfigNode node) : base(node)
        {
            ratio = node.GetFloatValue("ratio", ratio);
            fairingOffset = node.GetFloatValue("fairingOffset", fairingOffset);
        }
    }

    public class SSTUCustomUpperStageTopCap : SSTUCustomUpperStagePart
    {
        public float fairingOffset = 0.0f;        
        public SSTUCustomUpperStageTopCap(ConfigNode node) : base(node)
        {
            fairingOffset = node.GetFloatValue("fairingOffset", fairingOffset);
        }
    }

    public class SSTUCustomUpperStageRCS : ModelData
    {
        public GameObject[] models;
        public float currentHorizontalPosition;
        public float modelRotation = 0;
        public float modelHorizontalZOffset = 0;
        public float modelHorizontalXOffset = 0;
        public float modelVerticalOffset = 0;

        public float mountVerticalRotation = 0;
        public float mountHorizontalRotation = 0;

        public SSTUCustomUpperStageRCS(ConfigNode node) : base(node)
        {
            modelRotation = node.GetFloatValue("modelRotation");
            modelHorizontalZOffset = node.GetFloatValue("modelHorizontalZOffset");
            modelHorizontalXOffset = node.GetFloatValue("modelHorizontalXOffset");
            modelVerticalOffset = node.GetFloatValue("modelVerticalOffset");
        }
        
        public override void setupModel(Part part, Transform parent)
        {
            models = new GameObject[4];
            Transform[] trs = part.transform.FindChildren(modelName);
            if (trs != null && trs.Length>0)
            {
                for (int i = 0; i < 4; i++)
                {
                    models[i] = trs[i].gameObject;
                }
            }
            else
            {
                for (int i = 0; i < 4; i++)
                {
                    models[i] = SSTUUtils.cloneModel(modelName);
                }
            }
            foreach (GameObject go in models)
            {
                go.transform.NestToParent(parent);
            }            
        }

        public override void updateModel()
        {
            if (models != null)
            {
                float rotation = 0;
                float posX = 0, posZ = 0, posY = 0;
                float scale = 1;
                float length = 0;
                for (int i = 0; i < 4; i++)
                {
                    rotation = (float)(i * 90) + mountVerticalRotation;
                    scale = currentDiameterScale;
                    length = currentHorizontalPosition + (scale * modelHorizontalZOffset);                    
                    posX = (float)Math.Sin(SSTUUtils.toRadians(rotation)) * length;
                    posZ = (float)Math.Cos(SSTUUtils.toRadians(rotation)) * length;                    
                    posY = currentVerticalPosition + (scale * modelVerticalOffset);
                    models[i].transform.localScale = new Vector3(currentDiameterScale, currentHeightScale, currentDiameterScale);
                    models[i].transform.localPosition = new Vector3(posX, posY, posZ);
                    models[i].transform.localRotation = Quaternion.AngleAxis(rotation + 90f, new Vector3(0, 1, 0));
                    models[i].transform.Rotate(new Vector3(0, 0, 1), mountHorizontalRotation, Space.Self);
                }
            }
        }
    }

    //TODO -- fix this to use singlemodeldata directly...
    public class SSTUCustomUpperStagePart : SingleModelData
    {
        public SSTUCustomUpperStagePart(ConfigNode node) : base(node)
        {

        }        
    }
}