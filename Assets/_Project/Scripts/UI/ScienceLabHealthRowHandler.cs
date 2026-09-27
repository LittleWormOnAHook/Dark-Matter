using System;
using System.Collections.Generic;
using Project.Pioneers;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Project.UI
{
    internal class ScienceLabHealthRowHandler : MonoBehaviour, IPointerClickHandler
    {
        private string pioneerId;
        private BuildingControlPanelUI panel;

        public void Configure(BuildingControlPanelUI ownerPanel, string id)
        {
            panel = ownerPanel;
            pioneerId = id;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Right || panel == null)
                return;

            ScienceLabHealthContextMenu menu = ScienceLabHealthContextMenu.EnsureExists(
                panel.transform,
                panel);
            menu.ShowInjuredRow(pioneerId, eventData.position);
        }
    }
}
