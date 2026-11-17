using Microsoft.AspNetCore.Builder;
using MiniPandoras.Protocol;
using MiniPandoras.Server.Rules;

// 온라인 1:1 대전 서버(Docs/DESIGN_OnlineDuel.md). 0단계: 대전 규칙을 읽고, 상태 확인 주소만 연다.
// 소켓·매칭·방은 1단계에서 더한다.
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
DuelRules rules = DuelRules.Load(builder.Configuration["RulesPath"] ?? DuelRules.DefaultPath);

WebApplication app = builder.Build();
app.MapGet("/health", () => new
{
    protocol = Packet.Version,
    pandoras = rules.Pandoras.Count,
    skills = rules.Skills.Count,
});
app.Run();
