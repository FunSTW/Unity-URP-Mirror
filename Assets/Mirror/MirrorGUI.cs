using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FunS.Demo
{
    public class MirrorGUI : MonoBehaviour
    {
        [SerializeField] private Mirror mirror;
        [SerializeField] private UnityEngine.UI.Toggle toggle;
        [SerializeField] private UnityEngine.UI.Toggle toggleShadow;
        [SerializeField] private UnityEngine.UI.Slider slider;
        [SerializeField] private TMPro.TextMeshProUGUI renderScaleTMP;
        [SerializeField] private UnityEngine.UI.Button B1;
        [SerializeField] private UnityEngine.UI.Button B2;
        [SerializeField] private UnityEngine.UI.Button B4;
        [SerializeField] private UnityEngine.UI.Button B8;

        private bool m_inited;

        private void Update()
        {
            if (!mirror.IsRendering) return;

            if (!m_inited)
            {
                m_inited = true;
                slider.onValueChanged.AddListener((f) =>
                {
                    f = Mathf.FloorToInt(f * 100) / 100f;
                    mirror.ScreenScaleFactor = f;
                    renderScaleTMP.text = "( " + mirror.ScreenScaleFactor.ToString("F2") + "x ) " + mirror.RenderingScreenSize.ToString();
                });
                slider.value = mirror.ScreenScaleFactor;
                slider.onValueChanged.Invoke(slider.value);

                mirror.enabled = toggle.isOn;
                toggle.onValueChanged.AddListener((b) =>
                {
                    mirror.enabled = b;
                });

                mirror.UseShadow = toggleShadow.isOn;
                toggleShadow.onValueChanged.AddListener((b) =>
                {
                    mirror.UseShadow = b;
                });

                B1.onClick.AddListener(() => { mirror.MSAA = UnityEngine.Rendering.MSAASamples.None; });
                B2.onClick.AddListener(() => { mirror.MSAA = UnityEngine.Rendering.MSAASamples.MSAA2x; });
                B4.onClick.AddListener(() => { mirror.MSAA = UnityEngine.Rendering.MSAASamples.MSAA4x; });
                B8.onClick.AddListener(() => { mirror.MSAA = UnityEngine.Rendering.MSAASamples.MSAA8x; });
            }
        }
    }
}