// Chinese text and timings adapted from LC-Chinese-Project, copyright (c) 2026 Auuueser.
// MIT notice: licenses/LC-Chinese-Project-MIT.txt. English wording requires listening validation.
namespace Sensus.Captions;
internal static class SpokenTracks
{
    internal readonly struct Line
    {
        internal readonly string Clip, Chinese, English;
        internal readonly float Start, End;
        internal Line(string clip,float start,float end,string cn,string en)
        { Clip=clip; Start=start; End=end; Chinese=cn; English=en; }
    }
    internal static readonly Line[] Lines = {
        new("IntroCompanySpeech", 0.0f, 5.0f, "[公司提示音]", "[Company chime]"),
        new("IntroCompanySpeech", 7.31f, 10.05f, "欢迎来到上班第一天！", "Welcome to your first day on the job!"),
        new("IntroCompanySpeech", 10.14f, 12.35f, "这是配发给你们的自动驾驶飞船，", "This is your very own autopilot ship,"),
        new("IntroCompanySpeech", 12.44f, 15.2f, "合同期间，你们将在这里用餐和休息。", "where you will eat and sleep for the duration of your contract."),
        new("IntroCompanySpeech", 15.28f, 19.75f, "[高速播放的模糊语音]", "[Rapid, indistinct speech]"),
        new("IntroCompanySpeech", 19.94f, 21.6f, "请把这里当作自己的家。", "Make yourself at home."),
        new("IntroCompanySpeech", 21.69f, 23.7f, "要完成入职流程，", "To complete the onboarding process,"),
        new("IntroCompanySpeech", 23.77f, 25.6f, "请查看操作手册，", "please read the instruction manual"),
        new("IntroCompanySpeech", 25.65f, 28.2f, "并登录飞船上的电脑终端。", "and sign into your ship's computer terminal."),
        new("IntroCompanySpeech", 28.29f, 31.0f, "我们相信，你会成为公司的宝贵资产。", "We trust you will be a great asset to the Company."),
        new("IntroCompanySpeech", 31.035f, 32.75f, "宝贵……公司的宝贵资产……", "Great asset… great asset to the Company…"),
        new("IntroCompanySpeech", 32.83f, 35.1f, "资产……宝贵……宝贵……公司的宝贵资产……", "Asset… great… great… great asset to the Company…"),
        new("IntroCompanySpeech", 35.235f, 37.129f, "宝贵……资……资产……", "Great… asset… asset…"),
        new("0DaysLeftAlert", 0.0f, 4.8f, "[公司提示音]", "[Company chime]"),
        new("0DaysLeftAlert", 4.969f, 7.1f, "请立即前往公司大楼，", "Please report to the Company building immediately"),
        new("0DaysLeftAlert", 7.189f, 9.6f, "出售废料和其他物品。", "to sell your scrap metal and other goods."),
        new("0DaysLeftAlert", 9.758f, 13.0f, "距离完成利润指标的期限只剩零天。", "You have zero days left to meet the profit quota."),
        new("0DaysLeftAlert", 13.085f, 14.8f, "你可以通过终端，", "You can use the terminal"),
        new("0DaysLeftAlert", 14.874f, 17.061f, "让自动驾驶系统前往公司大楼。", "to route the autopilot to the Company building."),
        new("FiredVoiceline", 0.15f, 2.18f, "由于你们未能完成利润指标，", "Because you did not meet the profit quota,"),
        new("FiredVoiceline", 2.2f, 4.3f, "工作表现被评定为低于标准。", "your performance has been rated below standard."),
        new("FiredVoiceline", 5.47f, 7.52f, "欢迎进入公司的惩戒流程。", "Welcome to the Company's disciplinary process."),
        new("SnareFleaTipChannel", 0.33f, 2.75f, "如果某个实体接触了船员，", "If an entity comes into contact with a crew member,"),
        new("SnareFleaTipChannel", 2.83f, 5.2f, "请不要立即采取自卫措施。", "do not immediately take defensive action."),
        new("SnareFleaTipChannel", 5.3f, 7.4f, "请先询问该船员以下问题：", "First, ask the crew member these questions:"),
        new("SnareFleaTipChannel", 7.495f, 9.32f, "“这个实体有攻击性吗？”", "\"Is this entity aggressive?\""),
        new("SnareFleaTipChannel", 9.41f, 10.58f, "“你受伤了吗？”", "\"Are you injured?\""),
        new("SnareFleaTipChannel", 10.635f, 11.67f, "“你需要帮助吗？”", "\"Do you need help?\""),
        new("SnareFleaTipChannel", 11.73f, 13.84f, "如果这些问题的答案都是“是”，", "If the answer to all these questions is yes,"),
        new("SnareFleaTipChannel", 13.92f, 15.82f, "再开始采取自卫措施。", "then take defensive action."),
        new("SnareFleaTipChannel", 15.9f, 17.12f, "如果船员感到紧张，", "If the crew member is anxious,"),
        new("SnareFleaTipChannel", 17.19f, 19.54f, "可以问一句：“今天过得怎么样？”", "you can ask: \"How was your day?\""),
        new("SnareFleaTipChannel", 19.62f, 22.147f, "感谢配合，祝你旅途愉快！", "Thank you for your cooperation. Have a safe journey!"),
        new("Mic1", 0.0f, 9999.0f, "你们的工作让公司十分满意。", "Your work keeps the Company happy."),
        new("Mic2", 0.0f, 9999.0f, "我们重视你们的投入。", "We value your commitment."),
        new("Mic4", 0.0f, 9999.0f, "你们的辛勤工作对公司极具价值。", "Your hard work is invaluable to the Company."),
        new("Mic9", 0.0f, 9999.0f, "你们诚实的工作对公司极具价值。", "Your honest work is invaluable to the Company."),
        new("Mic10", 0.0f, 9999.0f, "你们是真正的专业人士。", "You are true professionals."),
        new("Mic3", 0.0f, 9999.0f, "我们需要你们…… [信号故障]", "We need you… [Signal glitch]"),
        new("Mic6", 0.0f, 9999.0f, "[失真的喊声]", "[Distorted shouting]"),
        new("Mic7", 0.0f, 9999.0f, "公司必须保持满意……公司必…… [信号中断]", "The Company must stay happy… [Signal cuts out]"),
        new("Mic8", 0.0f, 9999.0f, "这面墙无法困住…… [信号故障]", "These walls cannot contain… [Signal glitch]"),
        new("Mic11", 0.0f, 9999.0f, "让我们的投资者满意。", "Keep our investors happy."),
    };
    internal static bool MatchesLength(string clip,float length) => clip switch
    {
        "SnareFleaTipChannel" => System.Math.Abs(length-22.146833f)<0.1f,
        "0DaysLeftAlert" => System.Math.Abs(length-17.061417f)<0.1f,
        "IntroCompanySpeech" => System.Math.Abs(length-37.128708f)<0.1f,
        "FiredVoiceline" => System.Math.Abs(length-10.540292f)<0.1f,
        "Mic9" => System.Math.Abs(length-3.311063f)<0.1f,
        "Mic2" => System.Math.Abs(length-1.575125f)<0.1f,
        "Mic1" => System.Math.Abs(length-1.971229f)<0.1f,
        "Mic4" => System.Math.Abs(length-2.111625f)<0.1f,
        "Mic6" => System.Math.Abs(length-0.799833f)<0.1f,
        "Mic7" => System.Math.Abs(length-2.572854f)<0.1f,
        "Mic11" => System.Math.Abs(length-1.598521f)<0.1f,
        "Mic10" => System.Math.Abs(length-1.625937f)<0.1f,
        "Mic8" => System.Math.Abs(length-5.318687f)<0.1f,
        "Mic3" => System.Math.Abs(length-2.527583f)<0.1f,
        _ => false
    };
    internal static bool Known(string clip) { foreach(var line in Lines) if(line.Clip==clip) return true; return false; }
    internal static string At(string clip,float time,bool chinese)
    { foreach(var line in Lines) if(line.Clip==clip && time>=line.Start && time<line.End) return chinese ? line.Chinese : line.English; return ""; }
}
