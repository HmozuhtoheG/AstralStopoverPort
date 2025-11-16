using AmongUs.GameOptions;
using ASP.Roles.Core.Interfaces;

namespace ASP.Roles.Vanilla;

public sealed class Phantom : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.CreateForVanilla(
            typeof(Phantom),
            player => new Phantom(player),
            RoleTypes.Phantom
        );
    public Phantom(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }
}
