using System.Collections.Generic;
using HarmonyLib;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(
    typeof(DeepSpacePawnshop.CustomerBounceMod),
    "深空当铺 - 顾客立绘跳动",
    "1.3.2",
    "YourName")]

// 如果只想针对本游戏生效，可以取消注释并填写：
// [assembly: MelonGame("Questing Goose Studio", "Probably Stolen")]

namespace DeepSpacePawnshop
{
    public sealed class CustomerBounceMod : MelonMod
    {
        public override void OnInitializeMelon()
        {
            HarmonyInstance.PatchAll();
            LoggerInstance.Msg("顾客立绘跳动 Mod 已加载。");
        }
    }

    [HarmonyPatch(typeof(StoreClientMono), "Update")]
    internal static class StoreClientMonoUpdatePatch
    {
        private const float BounceSpeed = 7.8125f;
        private const float BounceAmplitude = 0.1f;
        private const float ReturnSpeed = 2f;

        // 向上时竖向拉长，向下时横向压扁（数值越大越夸张）
        private const float VerticalStretch = 0.15f;
        private const float HorizontalSquash = 0.11f;

        private static readonly Dictionary<int, float> InitialY =
            new Dictionary<int, float>();

        private static readonly Dictionary<int, SpriteRenderer> InitialYImage =
            new Dictionary<int, SpriteRenderer>();

        private static readonly Dictionary<int, Vector3> InitialScale =
            new Dictionary<int, Vector3>();

        [HarmonyPostfix]
        private static void Postfix(StoreClientMono __instance)
        {
            if (__instance == null || __instance.image == null)
                return;

            SpriteRenderer image = __instance.image;
            StoreClient storeClient = GetCurrentStoreClient();

            bool isDialogue =
                storeClient != null &&
                storeClient.clientIntent == StoreClient.ClientIntent.DIALOGUE;

            Transform imageTransform = image.transform;
            Vector3 localPosition = imageTransform.localPosition;
            int instanceId = __instance.GetInstanceID();

            if (!isDialogue)
            {
                if (InitialY.TryGetValue(instanceId, out float originalY) &&
                    InitialYImage.TryGetValue(instanceId, out SpriteRenderer originalImage) &&
                    InitialScale.TryGetValue(instanceId, out Vector3 originalScale) &&
                    object.ReferenceEquals(originalImage, image))
                {
                    if (!Mathf.Approximately(localPosition.y, originalY))
                    {
                        localPosition.y = Mathf.MoveTowards(
                            localPosition.y,
                            originalY,
                            ReturnSpeed * Time.deltaTime);

                        imageTransform.localPosition = localPosition;
                    }

                    Vector3 currentScale = imageTransform.localScale;
                    if ((currentScale - originalScale).sqrMagnitude > 0.000001f)
                    {
                        imageTransform.localScale = Vector3.MoveTowards(
                            currentScale,
                            originalScale,
                            ReturnSpeed * Time.deltaTime);
                    }
                }

                return;
            }

            Vector3 initialScale;

            if (!InitialY.TryGetValue(instanceId, out float initialY) ||
                !InitialYImage.TryGetValue(instanceId, out SpriteRenderer cachedImage) ||
                !InitialScale.TryGetValue(instanceId, out initialScale) ||
                !object.ReferenceEquals(cachedImage, image))
            {
                initialY = localPosition.y;
                initialScale = imageTransform.localScale;

                InitialY[instanceId] = initialY;
                InitialYImage[instanceId] = image;
                InitialScale[instanceId] = initialScale;
            }

            float phase = Mathf.Sin(Time.time * BounceSpeed);

            // 上下弹跳
            localPosition.y = initialY + phase * BounceAmplitude;
            imageTransform.localPosition = localPosition;

            // 向上时竖向拉伸，向下时横向拉伸
            Vector3 scale = initialScale;
            scale.x *= 1f - phase * HorizontalSquash;
            scale.y *= 1f + phase * VerticalStretch;
            imageTransform.localScale = scale;
        }

        private static StoreClient GetCurrentStoreClient()
        {
            PlayerStore playerStore = PlayerStore.Instance;

            if (playerStore == null ||
                playerStore.currentClientInstance == null)
            {
                return null;
            }

            return playerStore.currentClientInstance.GetClientBlueprint();
        }
    }
}