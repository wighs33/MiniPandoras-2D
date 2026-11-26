using System.Collections.Generic;
using MiniPandoras.World;
using UnityEngine;
using UnityEngine.Pool;

namespace MiniPandoras.Characters
{
    /// <summary>
    /// 인물 틀(꺼 둔 CharacterBody)로 맵 위에 인물을 세운다(웹게임 field.addActor).
    /// 연출 인물, 따라다니는 동료, 전투의 적과 불러낸 동료가 모두 여기서 나온다.
    /// 몸은 풀에서 꺼낸다. 사냥터의 무리와 불러낸 동료는 싸우는 동안 몇 번이고 사라졌다가 다시 서므로, 다 쓴 몸을 <see cref="Release"/>로 돌려받아 다시 쓴다.
    /// 연출 인물은 돌려받지 않고 지운다. 연출 걸음(<see cref="CharacterBody.Walk"/>)은 다른 컴포넌트의 코루틴이라 몸을 붙잡은 채 남을 수 있는데,
    /// 지운 몸이면 걸음이 스스로 멈추지만 돌려받은 몸이면 다음 인물이 쓰는 동안 옛 걸음이 그 몸을 옮긴다.
    /// </summary>
    public sealed class ActorFactory : MonoBehaviour
    {
        [Tooltip("그림·그림자·감정 아이콘이 달린 CharacterBody. 꺼 둔 채로 둔다")]
        [SerializeField] private CharacterBody template;
        [SerializeField] private List<CharacterSheet> sheets = new List<CharacterSheet>();

        private ObjectPool<CharacterBody> pool;

        // 쓰지 않는 몸은 꺼 둔 채 이 오브젝트 아래에서 기다린다. 맵 아래에 두지 않아서 맵을 바꿔도 지워지지 않는다.
        private ObjectPool<CharacterBody> Pool => pool ??= new ObjectPool<CharacterBody>(
            createFunc: () => Instantiate(template, transform),
            actionOnRelease: actor =>
            {
                actor.gameObject.SetActive(false);
                actor.transform.SetParent(transform, false);
            },
            actionOnDestroy: actor => Destroy(actor.gameObject));

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
            CharacterBody actor = Pool.Get();
            actor.transform.SetParent(parent, false);
            actor.name = actorName;
            actor.SetSheet(Sheet(sheetId));
            actor.gameObject.SetActive(true);
            // 돌려받은 몸에는 지난 싸움의 자세·투명도·실루엣이 남아 있다. 켠 뒤에 되돌려야 자식들의 Awake가 먼저 돈다.
            actor.ResetForReuse();
            actor.PlaceAt(feet);
            actor.Face(facing);
            return actor;
        }

        /// <summary>
        /// 다 쓴 몸을 돌려받는다. 다음 <see cref="Spawn"/>이 처음 만든 몸처럼 되돌려 다시 쓴다.
        /// 같은 몸을 두 번 돌려주면 에디터에서 풀이 예외를 던진다.
        /// </summary>
        public void Release(CharacterBody actor)
        {
            Pool.Release(actor);
        }
    }
}
