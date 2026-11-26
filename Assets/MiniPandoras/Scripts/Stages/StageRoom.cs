using UnityEngine;

namespace MiniPandoras.Stages
{
    /// <summary>
    /// 타이틀과 컷인의 무대가 서는 곳. 무대 프리팹을 필요할 때 불러와 필드와 겹치지 않는 먼 자리에 하나만 세우고,
    /// 서 있는 동안 필드의 카메라와 해를 끈다. 치우면 다시 켠다.
    /// </summary>
    public sealed class StageRoom : MonoBehaviour
    {
        private static readonly Vector3 AwayFromField = new Vector3(2000f, 0f, 0f);

        [SerializeField] private Camera fieldCamera;
        [SerializeField] private Light fieldSun;
        [Tooltip("무대를 그림(렌더 텍스처)으로 받는 동안 화면을 비우는 카메라. 화면에 그리는 카메라가 하나도 없으면 화면이 새로 그려지지 않는다")]
        [SerializeField] private Camera blankCamera;

        private GameObject stage;

        /// <summary>Resources의 무대를 세운다. 이미 선 무대가 있으면 치우고 바꾼다. 무대의 카메라를 화면 대신 그림으로 받으면 intoPicture다.</summary>
        public T Show<T>(string resource, bool intoPicture = false) where T : Component
        {
            Clear();
            T standing = Instantiate(Resources.Load<T>(resource), AwayFromField, Quaternion.identity, transform);
            stage = standing.gameObject;
            SetFieldShown(false);
            blankCamera.enabled = intoPicture;
            return standing;
        }

        /// <summary>무대를 치우고 필드를 다시 보인다.</summary>
        public void Hide()
        {
            if (stage == null)
            {
                return;
            }

            Clear();
            SetFieldShown(true);
            blankCamera.enabled = false;
            Resources.UnloadUnusedAssets();
        }

        private void Clear()
        {
            if (stage != null)
            {
                // 다음 무대가 필드의 렌더 설정을 바르게 적어 두도록, 없애기 전에 먼저 꺼서 되돌린다.
                stage.SetActive(false);
                Destroy(stage);
            }

            stage = null;
        }

        private void SetFieldShown(bool shown)
        {
            fieldCamera.enabled = shown;
            fieldSun.enabled = shown;
        }
    }
}
