using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiniPandoras.Progress
{
    /// <summary>누군가의 의뢰 하나의 바뀌지 않는 내용. 끝내면 골드를 받는다.</summary>
    [Serializable]
    public struct RequestInfo
    {
        [Tooltip("저장과 이야기가 가리키는 이름. Missions의 상수와 같아야 한다")]
        [SerializeField] private string id;
        [SerializeField] private string title;
        [SerializeField, TextArea] private string detail;
        [SerializeField] private string requester;
        [SerializeField] private int gold;

        public string Id => id;
        public string Title => title;
        public string Detail => detail;
        public string Requester => requester;
        public int Gold => gold;
    }

    /// <summary>의뢰 전체. 새 의뢰는 여기에 한 줄 더하고, 이야기에서 TakeRequest와 FinishRequest를 부른다.</summary>
    [CreateAssetMenu(menuName = "MiniPandoras/Request Catalog")]
    public sealed class RequestCatalog : ScriptableObject
    {
        [SerializeField] private RequestInfo[] requests;

        public IReadOnlyList<RequestInfo> All => requests;
    }
}
