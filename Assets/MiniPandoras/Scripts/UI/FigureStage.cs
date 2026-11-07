using MiniPandoras.Dialogue;
using MiniPandoras.World;
using UnityEngine;
using UnityEngine.UI;

namespace MiniPandoras.UI
{
    /// <summary>
    /// 대화창 위에 인물 일러스트를 크게 세운다(웹게임 renderFigures). 주인공은 왼쪽, 상대는 오른쪽 자리다.
    /// 말하는 쪽은 밝게, 듣는 쪽은 어둡게 둔다. 연출로 대화가 0.75초 넘게 끊기면 물러나고, 4초가 지나면 비운다.
    /// 이야기가 맵을 새로 띄우거나 전투를 시작하면 바로 비운다.
    /// 크기와 높이는 인물마다 얼굴 칸을 같은 크기(26)·같은 높이(84)에 맞춘다. 좌표는 가상 화면(384×216) 단위다.
    /// </summary>
    public sealed class FigureStage : MonoBehaviour
    {
        private const float FaceSize = 26f;
        private const float FaceY = 84f;
        private const float DialogueTop = 157f;
        private const float SlotInset = 40f;
        private const float StepBackSeconds = 0.75f;
        private const float ClearSeconds = 4f;

        [SerializeField] private Conversation conversation;
        [Tooltip("왼쪽(주인공) 자리, 오른쪽(상대) 자리")]
        [SerializeField] private Image leadSlot;
        [SerializeField] private Image otherSlot;

        private string leadSpeaker;
        private string otherSpeaker;
        private string speaking;
        private float lastLineTime = float.NegativeInfinity;
        private bool talking;

        private void OnEnable()
        {
            conversation.LineShown += OnLineShown;
            conversation.LineClosed += OnLineClosed;
            conversation.FiguresCleared += OnFiguresCleared;
        }

        private void OnDisable()
        {
            conversation.LineShown -= OnLineShown;
            conversation.LineClosed -= OnLineClosed;
            conversation.FiguresCleared -= OnFiguresCleared;
        }

        private void Awake()
        {
            leadSlot.color = Color.clear;
            otherSlot.color = Color.clear;
        }

        private void OnLineShown(DialogueLine line)
        {
            talking = true;
            speaking = line.SpeakerId;
            if (!line.Figure.HasValue)
            {
                return;
            }

            Figure figure = line.Figure.Value;
            if (figure.Lead)
            {
                leadSpeaker = line.SpeakerId;
                Stand(leadSlot, figure, SlotInset);
            }
            else
            {
                otherSpeaker = line.SpeakerId;
                Stand(otherSlot, figure, -SlotInset);
            }
        }

        private void OnLineClosed()
        {
            talking = false;
            lastLineTime = Time.time;
        }

        private void OnFiguresCleared()
        {
            leadSpeaker = null;
            otherSpeaker = null;
            speaking = null;
            leadSlot.color = Color.clear;
            otherSlot.color = Color.clear;
        }

        private static void Stand(Image slot, Figure figure, float centreX)
        {
            Sprite image = figure.Image;
            float scale = FaceSize / figure.FaceSize;
            Vector2 size = image.rect.size * scale;
            float top = figure.Whole ? DialogueTop + 6f - size.y : FaceY - figure.FaceCenter.y * scale;
            slot.sprite = image;
            slot.rectTransform.sizeDelta = size;
            // 자리는 화면 왼쪽(+)·오른쪽(-) 끝에서 잰 그림 가운데, 높이는 화면 위에서 잰 그림 윗변이다.
            slot.rectTransform.anchoredPosition = new Vector2(centreX, -top);
        }

        private void Update()
        {
            bool onStage = talking || Time.time - lastLineTime < StepBackSeconds;
            if (!talking && Time.time - lastLineTime > ClearSeconds)
            {
                leadSpeaker = null;
                otherSpeaker = null;
            }

            Fade(leadSlot, leadSpeaker, onStage);
            Fade(otherSlot, otherSpeaker, onStage);
        }

        private void Fade(Image slot, string speaker, bool onStage)
        {
            bool visible = onStage && speaker != null;
            bool lit = speaker != null && speaker == speaking;
            float brightness = lit ? 1f : 0.5f;
            Color target = visible ? new Color(brightness, brightness, brightness, lit ? 1f : 0.72f) : new Color(brightness, brightness, brightness, 0f);
            // 웹게임은 60fps 한 프레임에 남은 차이의 22%씩 다가간다.
            slot.color = Color.Lerp(slot.color, target, FrameTime.Ease(0.22f, Time.deltaTime));
        }
    }
}
