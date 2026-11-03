using System.Collections.Generic;
using MiniPandoras.World;
using UnityEngine;

namespace MiniPandoras.Characters
{
    /// <summary>
    /// 인물 틀(꺼 둔 CharacterBody)을 복제해 시트를 끼우고 맵 위에 세운다(웹게임 field.addActor).
    /// 연출 인물, 따라다니는 동료, 전투의 적과 불러낸 동료가 모두 여기서 나온다.
    /// </summary>
    public sealed class ActorFactory : MonoBehaviour
    {
        [Tooltip("그림·그림자·감정 아이콘이 달린 CharacterBody. 꺼 둔 채로 둔다")]
        [SerializeField] private CharacterBody template;
        [SerializeField] private List<CharacterSheet> sheets = new List<CharacterSheet>();

        public CharacterSheet Sheet(string id)
        {
            CharacterSheet sheet = sheets.Find(candidate => candidate.Id == id);
            if (sheet == null)
            {
                throw new KeyNotFoundException($"인물 시트 {id}가 없다. 웹게임에서 뽑아 MiniPandoras/Import Web Game으로 들여온다.");
            }

            return sheet;
        }

        /// <param name="parent">맵을 바꾸면 같이 사라지도록 지금 맵 아래에 둔다.</param>
        public CharacterBody Spawn(string actorName, string sheetId, Transform parent, Vector2 feet, Facing facing)
        {
            CharacterBody actor = Instantiate(template, parent);
            actor.name = actorName;
            actor.SetSheet(Sheet(sheetId));
            actor.gameObject.SetActive(true);
            actor.PlaceAt(feet);
            actor.Face(facing);
            return actor;
        }
    }
}
