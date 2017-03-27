﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SSTUTools
{
    public class CraftRecolorGUI : MonoBehaviour
    {
        private static int graphWidth = 640;
        private static int graphHeight = 250;
        private static int scrollHeight = 480;
        private static int margin = 20;
        private static int id;
        private static Rect windowRect = new Rect(Screen.width - 900, 40, graphWidth + margin, graphHeight + scrollHeight + margin);
        private static Vector2 scrollPos;

        private List<ModuleRecolorData> moduleRecolorData = new List<ModuleRecolorData>();

        private SectionRecolorGUI sectionGUI;

        private bool open = false;

        internal void openGui()
        {
            //TODO populate module data for parts on editor craft
            List<Part> uniqueParts = new List<Part>();
            foreach (Part p in EditorLogic.fetch.ship.Parts)
            {
                if (p.symmetryCounterparts == null || p.symmetryCounterparts.Count == 0)
                {
                    uniqueParts.Add(p);
                }
                else
                {
                    bool found = false;
                    foreach (Part p1 in p.symmetryCounterparts)
                    {
                        if (uniqueParts.Contains(p1))
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        uniqueParts.Add(p);
                    }
                }
            }
            foreach (Part p in uniqueParts)
            {
                List<IRecolorable> mods = p.FindModulesImplementing<IRecolorable>();
                foreach (IRecolorable mod in mods)
                {
                    ModuleRecolorData data = new ModuleRecolorData((PartModule)mod, mod);
                    moduleRecolorData.Add(data);
                }
            }
            open = true;
        }

        internal void closeGui()
        {
            open = false;
            closeSectionGUI();//just in case it was open
            moduleRecolorData.Clear();
        }

        public void OnGUI()
        {
            if (open)
            {
                windowRect = GUI.Window(id, windowRect, drawWindow, "Part Recoloring");
            }
        }

        private void drawWindow(int id)
        {
            GUILayout.BeginVertical();
            scrollPos = GUILayout.BeginScrollView(scrollPos);
            int len = moduleRecolorData.Count;
            Color old = GUI.contentColor;
            for (int i = 0; i < len; i++)
            {
                int len2 = moduleRecolorData[i].sectionData.Length;
                for (int k = 0; k < len2; k++)
                {
                    GUILayout.BeginHorizontal();
                    GUI.color = moduleRecolorData[i].sectionData[k].color;
                    if (GUILayout.Button("Recolor") && sectionGUI == null)
                    {
                        openSectionGUI(moduleRecolorData[i].sectionData[k]);
                    }
                    GUI.color = old;
                    GUILayout.Label(moduleRecolorData[i].module + "-" + moduleRecolorData[i].sectionData[k].sectionName);
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        private void openSectionGUI(SectionRecolorData section)
        {
            sectionGUI = gameObject.AddComponent<SectionRecolorGUI>();
            sectionGUI.sectionData = section;
            sectionGUI.onCloseAction = closeSectionGUI;
        }

        private void closeSectionGUI()
        {
            Component.DestroyImmediate(sectionGUI);
        }

    }

    public class SectionRecolorGUI : MonoBehaviour
    {
        private static int graphWidth = 320;
        private static int graphHeight = 240;
        private int id = 1;
        private Rect windowRect = new Rect(Screen.width - 900, 40, graphWidth, graphHeight);

        internal SectionRecolorData sectionData;
        internal Action onCloseAction;

        float r, g, b;

        public void Awake()
        {
            id = GetInstanceID();
        }

        public void OnGUI()
        {
            windowRect = GUI.Window(id, windowRect, drawWindow, "Section Recoloring");
        }

        private void drawWindow(int id)
        {
            GUILayout.BeginVertical();

            //red
            GUILayout.BeginHorizontal();
            GUILayout.Label("Red", GUILayout.Width(100));
            r = GUILayout.HorizontalSlider(sectionData.color.r, 0, 1, GUILayout.Width(200));
            if (r != sectionData.color.r)
            {
                sectionData.color.r = r;
                sectionData.updateColor();
            }
            GUILayout.EndHorizontal();

            //green
            GUILayout.BeginHorizontal();
            GUILayout.Label("Green", GUILayout.Width(100));
            g = GUILayout.HorizontalSlider(sectionData.color.r, 0, 1, GUILayout.Width(200));
            if (g != sectionData.color.g)
            {
                sectionData.color.g = g;
                sectionData.updateColor();
            }
            GUILayout.EndHorizontal();

            //blue
            GUILayout.BeginHorizontal();
            GUILayout.Label("Blue", GUILayout.Width(100));
            b = GUILayout.HorizontalSlider(sectionData.color.r, 0, 1, GUILayout.Width(200));
            if (b != sectionData.color.b)
            {
                sectionData.color.b = b;
                sectionData.updateColor();
            }
            GUILayout.EndHorizontal();

            //TODO color selection presets, one button per preset.  Should possibly make a small white texture to be shared by each button and use the GUI.color to tint it to the preset color.

            bool shouldClose = false;
            if (GUILayout.Button("Close"))
            {
                shouldClose = true;
            }

            GUILayout.EndVertical();
            GUI.DragWindow();

            if (shouldClose)
            {
                onCloseAction();
            }
        }
    }

    public class ModuleRecolorData
    {
        public PartModule module;//must implement IRecolorable
        public IRecolorable iModule;//interface version of module
        public SectionRecolorData[] sectionData;

        public ModuleRecolorData(PartModule module, IRecolorable iModule)
        {
            this.module = module;
            this.iModule = iModule;
            string[] names = iModule.getSectionNames();
            Color[] colors = iModule.getSectionColors();
            int len = names.Length;
            sectionData = new SectionRecolorData[len];
            for (int i = 0; i < len; i++)
            {
                sectionData[i] = new SectionRecolorData(iModule, names[i], colors[i]);
            }
        }

        public void updateColors()
        {
            int len = sectionData.Length;
            for (int i = 0; i < len; i++)
            {
                iModule.setSectionColor(sectionData[i].sectionName, sectionData[i].color);
            }
        }
    }

    public class SectionRecolorData
    {
        public readonly IRecolorable owner;
        public readonly string sectionName;
        public Color color;

        public SectionRecolorData(IRecolorable owner, string name, Color color)
        {
            this.owner = owner;
            this.sectionName = name;
            this.color = color;
        }

        public void updateColor()
        {
            owner.setSectionColor(sectionName, color);
        }
    }

}
