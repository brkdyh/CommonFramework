using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace EasyAsset
{
    public class EA_GUIStyle
    {
        static GUIStyle _mid_label;//居中label
        public static GUIStyle mid_label
        {
            get
            {
                if (_mid_label == null)
                {
                    //_mid_label = GUI.skin.GetStyle("label");
                    _mid_label = new GUIStyle("label");
                    _mid_label.alignment = TextAnchor.MiddleCenter;
                    _mid_label.fontSize = 24;
                }
                return _mid_label;
            }
        }

        public static GUIStyle mid_label_min
        {
            get
            {
                if (_mid_label == null)
                {
                    //_mid_label = GUI.skin.GetStyle("label");
                    _mid_label = new GUIStyle("label");
                    _mid_label.alignment = TextAnchor.MiddleCenter;
                    _mid_label.fontSize = 12;
                }
                return _mid_label;
            }
        }

        public static void Release()
        {
            _mid_label = null;
        }
    }
}