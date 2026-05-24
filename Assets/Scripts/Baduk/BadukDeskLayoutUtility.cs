using System.Collections.Generic;
using UnityEngine;

namespace Baduk
{
    public static class BadukDeskLayoutUtility
    {
        struct DeskAnchor
        {
            public Vector3 CameraPosition;
            public Vector3 Forward;
        }

        static readonly Dictionary<string, DeskAnchor> AnchorsByScene = new();

        public static void ApplyDeskLayout(
            Transform boardTransform,
            float cx,
            float cy,
            float boardSizeTarget,
            float boardDistance,
            float tableHeightOffset,
            string sceneKey,
            Camera cam,
            out Vector3 boardCenter,
            out float tableY)
        {
            Vector3 fallbackCameraPosition = cam != null ? cam.transform.position : new Vector3(0f, 1.0f, 0f);
            Vector3 fallbackForward = cam != null
                ? Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized
                : Vector3.forward;

            if (fallbackForward == Vector3.zero)
                fallbackForward = Vector3.forward;

            if (!AnchorsByScene.TryGetValue(sceneKey, out var anchor))
            {
                anchor = new DeskAnchor
                {
                    CameraPosition = fallbackCameraPosition,
                    Forward = fallbackForward
                };
                AnchorsByScene[sceneKey] = anchor;
            }

            float boardWorldMax = Mathf.Max(cx * 2f, cy * 2f);
            float scale = boardWorldMax > 0f ? Mathf.Min(1f, boardSizeTarget / boardWorldMax) : 1f;

            boardTransform.localScale = Vector3.one * scale;
            boardTransform.rotation = Quaternion.LookRotation(anchor.Forward, Vector3.up);

            tableY = anchor.CameraPosition.y - tableHeightOffset;
            boardCenter = anchor.CameraPosition + anchor.Forward * boardDistance;
            boardCenter.y = tableY + 0.012f;

            Vector3 localBoardCenter = new Vector3(cx, 0f, -cy);
            Vector3 rotatedCenterOffset = boardTransform.rotation * Vector3.Scale(localBoardCenter, boardTransform.localScale);
            boardTransform.position = boardCenter - rotatedCenterOffset;

            var boardRenderer = boardTransform.GetComponentInChildren<Renderer>();
            if (boardRenderer != null)
            {
                float topDelta = boardRenderer.bounds.max.y - boardTransform.position.y;
                if (topDelta < 0.01f || topDelta > 0.12f)
                {
                    Vector3 corrected = boardTransform.position;
                    corrected.y = tableY + 0.028f;
                    boardTransform.position = corrected;
                }
            }
        }
    }
}
