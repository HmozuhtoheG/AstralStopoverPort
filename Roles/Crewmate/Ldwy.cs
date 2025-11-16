using AmongUs.GameOptions;
using Hazel;
using ASP.Roles.Core.Interfaces;
using UnityEngine;
using ASP.Roles.Core;

namespace ASP.Roles.Crewmate;
public sealed class Ldwy : RoleBase, IKiller
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Ldwy),
            player => new Ldwy(player),
            CustomRoles.Ldwy,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Crewmate,
            12312,
            SetupOptionItem,
            "兰岛物语|蓝岛|ld",
            "#0000FF",
            isDesyncImpostor: true,
            introSound: () => GetIntroSound(RoleTypes.Crewmate)
        );

    public Ldwy(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.ForRecompute
    )
    { }

    private static void SetupOptionItem()
    {

    }

    private int Meatball = 2;
    private int k = 1;
    private int dun = 0;
    private bool isMaking = false;
    private float makeTime = 0f;
    private const float MakeDuration = 120f;

    public override void Add()
    {
        k = 1;
        dun = 0;
        Meatball = 2;
        isMaking = false;
        makeTime = 0f;
    }

    public bool CanUseSabotageButton() => false;
    public float CalculateKillCooldown() => 25f;
    public bool CanUseImpostorVentButton() => true;
    public override void ApplyGameOptions(IGameOptions opt) => opt.SetVision(false);

    public override bool GetAbilityButtonText(out string text)
    {
        text = GetString("MeatballText");
        return true;
    }
    public bool OverrideVentButtonSprite(out string buttonName)
    {
        buttonName = "meatball";
        return true;
    }

    public bool OverrideKillButtonText(out string text)
    {
        text = GetString("GiveOrKill");
        return true;
    }
    public bool OverrideKillButtonSprite(out string buttonName)
    {
        buttonName = "Give";
        return true;
    }

    public override bool OnCheckMurderAsTarget(MurderInfo info)
    {
        if (dun > 0)
        {
            dun--;
            Player.Notify(GetString("Preventkilling"));
            info.AttemptKiller.ResetKillCooldown();
            info.AttemptKiller.SetKillCooldownV2(target: Player, forceAnime: true);
            return false;
        }
        return true;
    }

    public bool OnCheckMurderAsKiller(MurderInfo info)
    {
        var target = info.AttemptTarget;
        // 当有肉丸时，点击杀键给予他人肉丸
        if (Meatball > 0)
        {
            target.Notify(Utils.ColorString(Color.yellow, "您收到了一个肉丸（似乎票数出现了改变？）"));
            Meatball--;
            if (Meatball == 0)
            {
                Player.Notify(Utils.ColorString(Color.green, "您可以开始执法了..."));
            }
            return false; 
        }

        // 没有肉丸时，执行原击杀逻辑
        var targetRole = target.GetCustomRole();
        if (k == 1 && (targetRole.IsImpostor() || targetRole.IsNeutral()))
        {
            k--;
            dun++;
            Player.Notify(Utils.ColorString(Color.green, GetString("KillerGetShield")));
            return true;
        }
        if (k < 1)
        {
            Player.Notify(Utils.ColorString(Color.red, GetString("KillerNoKillChance")));
            return false;
        }
        return false;
    }

    public override void NotifyOnMeetingStart(ref List<(string, byte, string)> msgToSend)
    {
        msgToSend.Add((
            GetString("EmergencyNoticeContent"),
            255,
            Utils.ColorString(RoleInfo.RoleColor, GetString("CharacterNoticeTitle"))
        ));
    }

    public override bool OnEnterVent(PlayerPhysics physics, int ventId)
    {
        if (Meatball <= 0)
        {
            Player.Notify(Utils.ColorString(Color.red, GetString("NoMeatballLeft")));
            return false;
        }
        if (isMaking)
        {
            float remaining = MakeDuration - (Time.time - makeTime);
            Player.Notify(Utils.ColorString(Color.yellow, $"制作中...{Mathf.CeilToInt(remaining)}秒"));
            return false;
        }
        isMaking = true;
        makeTime = Time.time;
        Player.Notify(Utils.ColorString(Color.yellow, "开始制作肉丸...(2分钟)"));
        new LateTask(() =>
        {
            if (isMaking)
            {
                isMaking = false;
                Meatball--;
                k++;
                Player.Notify(Utils.ColorString(Color.green, $"获得1次击杀机会，剩余使用次数：{Meatball}"));
                SendRPC();
            }
        }, MakeDuration, "MeatballMake");
        return false;
    }

    private void SendRPC()
    {
        using var sender = CreateSender();
        sender.Writer.Write(Meatball);
        sender.Writer.Write(k);
        sender.Writer.Write(dun);
        sender.Writer.Write(isMaking);
        sender.Writer.Write(makeTime);
    }

    public override void ReceiveRPC(MessageReader reader)
    {
        Meatball = reader.ReadInt32();
        k = reader.ReadInt32();
        dun = reader.ReadInt32();
        isMaking = reader.ReadBoolean();
        makeTime = reader.ReadSingle();
    }

    public override string GetProgressText(bool comms = false)
    {
        if (isMaking)
        {
            float remaining = MakeDuration - (Time.time - makeTime);
            return Utils.ColorString(Color.yellow, $"制作中: {Mathf.CeilToInt(remaining)}s");
        }
        return Utils.ColorString(Color.green, $"剩余次数: {Meatball}");
    }
}