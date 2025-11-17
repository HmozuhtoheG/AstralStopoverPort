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
    private static Dictionary<byte, bool> TempVoteAdd = new();

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
        if (Meatball > 0)
        {
            target.Notify(Utils.ColorString(Color.yellow, GetString("LdwyReceiveMeatball")));
            TempVoteAdd[target.PlayerId] = true;
            Meatball--;
            if (Meatball == 0)
            {
                Player.Notify(Utils.ColorString(Color.green, GetString("LdwyMeatballExhausted")));
            }
            return false;
        }

        var targetRole = target.GetCustomRole();
        if (k >= 1 && targetRole.IsCrewmate())
        {
            PlayerState.GetByPlayerId(Player.PlayerId).DeathReason = CustomDeathReason.Suicide;
            Player.RpcMurderPlayer(Player);
            return false;
        }
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

    public override (byte? votedForId, int? numVotes, bool doVote) ModifyVote(byte voterId, byte sourceVotedForId, bool isIntentional)
    {
        var (votedForId, numVotes, doVote) = base.ModifyVote(voterId, sourceVotedForId, isIntentional);
        if (TempVoteAdd.TryGetValue(voterId, out bool hasAdd) && hasAdd)
        {
            numVotes = 2; 
        }
        return (votedForId, numVotes, doVote);
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
            Player.Notify(Utils.ColorString(Color.yellow, string.Format(GetString("LdwyMakingInProgress"), Mathf.CeilToInt(remaining))));
            return false;
        }
        isMaking = true;
        makeTime = Time.time;
        Player.Notify(Utils.ColorString(Color.yellow, GetString("LdwyStartMakeMeatball")));
        new LateTask(() =>
        {
            if (isMaking)
            {
                isMaking = false;
                Meatball--;
                k++;
                Player.Notify(Utils.ColorString(Color.green, string.Format(GetString("LdwyGetKillChance"), Meatball)));
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
            return Utils.ColorString(Color.yellow, string.Format(GetString("LdwyMaking"), Mathf.CeilToInt(remaining)));
        }
        return Utils.ColorString(Color.green, string.Format(GetString("LdwyRemainingMeatball"), Meatball));
    }
}