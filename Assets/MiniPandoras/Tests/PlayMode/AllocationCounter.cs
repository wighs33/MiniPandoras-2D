using System;
using NUnit.Framework;
using Unity.Profiling;

namespace MiniPandoras.Tests
{
    /// <summary>
    /// 이 스레드의 GC 할당 수를 센다. Unity의 Is.Not.AllocatingGCMemory()는 WebGL 타겟에서 스레드 필터 없이 컴파일되고
    /// 측정마다 공유 레코더를 받아 에디터 다른 스레드의 할당까지 센다.
    /// </summary>
    internal static class AllocationCounter
    {
        public static int CountOnThisThread(Action action)
        {
            // 처음부터 이 스레드만 모으는 전용 레코더라서 에디터 다른 스레드의 할당이 섞이지 않는다.
            using (var recorder = new ProfilerRecorder(ProfilerCategory.Memory, "GC.Alloc", 1,
                       ProfilerRecorderOptions.CollectOnlyOnCurrentThread | ProfilerRecorderOptions.SumAllSamplesInFrame))
            {
                // 마커를 못 찾으면 아무것도 세지 못한 채 0이 나와 테스트가 헛되이 통과한다.
                Assert.That(recorder.Valid, Is.True, "GC.Alloc 프로파일러 마커를 찾지 못했다.");
                recorder.Start();
                action();
                recorder.Stop();
                return (int)recorder.GetSample(0).Count;
            }
        }
    }
}
