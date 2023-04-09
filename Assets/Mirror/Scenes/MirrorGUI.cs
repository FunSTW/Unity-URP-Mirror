using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FunS.Demo
{
    public class MirrorGUI : MonoBehaviour
    {
        [SerializeField] private Mirror mirror;
        [SerializeField] private UnityEngine.UI.Slider slider;
        [SerializeField] private TMPro.TextMeshProUGUI tmp;

        private bool m_inited;

        private void Update()
        {
            if (!mirror.IsRendering) return;

            if (!m_inited)
            {
                m_inited = true;
                slider.onValueChanged.AddListener((f) => {
                    mirror.ScreenScaleFactor = f;
                    tmp.text = "( " + mirror.ScreenScaleFactor.ToString("F2") + "x ) " + mirror.RenderSize.ToString();
                });
                slider.value = mirror.ScreenScaleFactor;
                slider.onValueChanged.Invoke(slider.value);
            }
        }
    }
}