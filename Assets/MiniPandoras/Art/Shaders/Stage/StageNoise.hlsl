// 타이틀 무대의 셰이더들이 함께 쓰는 잡음.
#ifndef MINIPANDORAS_STAGE_NOISE
#define MINIPANDORAS_STAGE_NOISE

float StageHash(float2 p)
{
    p = frac(p * float2(0.1031, 0.1030));
    p += dot(p, p.yx + 33.33);
    return frac((p.x + p.y) * p.x);
}

float StageNoise(float2 p)
{
    float2 cell = floor(p);
    float2 f = frac(p);
    f = f * f * (3 - 2 * f);
    float a = StageHash(cell);
    float b = StageHash(cell + float2(1, 0));
    float c = StageHash(cell + float2(0, 1));
    float d = StageHash(cell + float2(1, 1));
    return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
}

float StageFbm(float2 p)
{
    float sum = 0;
    float weight = 0.5;
    for (int i = 0; i < 4; i++)
    {
        sum += StageNoise(p) * weight;
        p = float2(p.x * 1.6 + p.y * 1.2, p.y * 1.6 - p.x * 1.2) + 17.3;
        weight *= 0.5;
    }

    return sum;
}

// 타이틀에서 게임으로 뛰어드는 정도(0 = 타이틀, 1 = 다 들어감). TitleStage가 바꾼다.
float _StageDive;

// 컷인에서 이야기가 차오르게 하는 빛(0~1). 재질의 _GlowShare만큼 이 값을 따른다(CutInStage가 바꾼다).
float _StageGlow;

// 가운데에서 양옆으로 펼쳐지는 그림(날개). from은 처음부터 보이는 폭이다. 다 펼치면(1) 모두 보인다.
float RevealMask(float2 uv, float reveal, float from)
{
    // 펼칠 일이 없는 그림은 uv가 0~1을 넘어도 그대로 둔다(땅과 벽의 이어 붙는 그림).
    if (from > 1 || reveal >= 1)
    {
        return 1;
    }

    float reach = abs(uv.x - 0.5) * 2 + (StageNoise(uv * float2(7, 11)) - 0.5) * 0.25;
    float edge = lerp(from, 1.3, reveal);
    return smoothstep(edge, edge - 0.12, reach);
}

float StageGlowAmount(float share)
{
    return lerp(1, _StageGlow, share);
}

#endif
