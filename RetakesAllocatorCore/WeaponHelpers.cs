using System.Collections;
using System.ComponentModel.DataAnnotations;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using RetakesAllocatorCore.Config;
using RetakesAllocatorCore.Db;

namespace RetakesAllocatorCore;

public enum WeaponAllocationType
{
    SniperPrimary,
    FullBuyPrimary,
    HalfBuyPrimary,
    Secondary,
    PistolRound,

    // eg. AWP is a preferred gun - you cant always get it even if its your preference
    // Right now its only snipers, but if we make this configurable, we need to change:
    // - CoercePreferredTeam
    // - "your turn" wording in the weapon command handler
    Preferred,
}

public enum ItemSlotType
{
    Primary,
    Secondary,
    Util
}

public static class WeaponHelpers
{
    private static readonly ICollection<CsItem> _sharedPistols = new HashSet<CsItem>
    {
        CsItem.Deagle,
        CsItem.P250,
        CsItem.CZ,
        CsItem.Dualies,
        CsItem.R8,
    };

    private static readonly ICollection<CsItem> _tPistols = new HashSet<CsItem>
    {
        CsItem.Glock,
        CsItem.Tec9,
    };


    private static readonly ICollection<CsItem> _ctPistols = new HashSet<CsItem>
    {
        CsItem.USPS,
        CsItem.P2000,
        CsItem.FiveSeven,
    };

    private static readonly ICollection<CsItem> _pistolsForT =
        _sharedPistols.Concat(_tPistols).ToHashSet();

    private static readonly ICollection<CsItem> _pistolsForCt =
        _sharedPistols.Concat(_ctPistols).ToHashSet();

    private static readonly ICollection<CsItem> _sharedMidRange = new HashSet<CsItem>
    {
        CsItem.UMP45,
        CsItem.MP7,
        CsItem.Bizon,
        CsItem.MP5,
        CsItem.Nova,
    };

    private static readonly ICollection<CsItem> _tMidRange = new HashSet<CsItem>
    {
        CsItem.Mac10,
        CsItem.SawedOff,
    };


    private static readonly ICollection<CsItem> _ctMidRange = new HashSet<CsItem>
    {
        CsItem.MP9,
        CsItem.MAG7,
    };

    private static readonly ICollection<CsItem> _midRangeForCt = _sharedMidRange.Concat(_ctMidRange).ToHashSet();
    private static readonly ICollection<CsItem> _midRangeForT = _sharedMidRange.Concat(_tMidRange).ToHashSet();

    private static readonly int _maxSmgItemValue = (int)CsItem.UMP;

    private static readonly ICollection<CsItem> _smgsForT =
        _sharedMidRange.Concat(_tMidRange).Where(i => (int)i <= _maxSmgItemValue).ToHashSet();

    private static readonly ICollection<CsItem> _smgsForCt =
        _sharedMidRange.Concat(_ctMidRange).Where(i => (int)i <= _maxSmgItemValue).ToHashSet();

    private static readonly ICollection<CsItem> _sharedRifles = new HashSet<CsItem>
    {
        CsItem.P90,
        CsItem.XM1014,
        CsItem.Scout,
    };
    
    private static readonly ICollection<CsItem> _tRifles = new HashSet<CsItem>
    {
        CsItem.AK47,
        CsItem.Galil,
        CsItem.Krieg,
    };

    private static readonly ICollection<CsItem> _ctRifles = new HashSet<CsItem>
    {
        CsItem.M4A1S,
        CsItem.M4A4,
        CsItem.Famas,
        CsItem.AUG,
    };
    
    private static readonly ICollection<CsItem> _riflesForT = _sharedRifles.ToHashSet().Concat(_tRifles).ToHashSet();
    private static readonly ICollection<CsItem> _riflesForCT = _sharedRifles.ToHashSet().Concat(_ctRifles).ToHashSet();

    private static readonly ICollection<CsItem> _sharedPreferred = new HashSet<CsItem>
    {
        CsItem.AWP,
    };

    private static readonly ICollection<CsItem> _tPreferred = new HashSet<CsItem>
    {
        CsItem.AutoSniperT,
    };

    private static readonly ICollection<CsItem> _ctPreferred = new HashSet<CsItem>
    {
        CsItem.AutoSniperCT,
    };

    private static readonly ICollection<CsItem> _preferredForT = _sharedPreferred.Concat(_tPreferred).ToHashSet();
    private static readonly ICollection<CsItem> _preferredForCt = _sharedPreferred.Concat(_ctPreferred).ToHashSet();

    private static readonly ICollection<CsItem> _allPreferred =
        _preferredForT.Concat(_preferredForCt).ToHashSet();

    private static readonly ICollection<CsItem> _heavys = new HashSet<CsItem>
    {
        CsItem.M249,
        CsItem.Negev,
    };

    private static readonly ICollection<CsItem> _fullBuyPrimaryForT =
        _riflesForT.Concat(_heavys).ToHashSet();

    private static readonly ICollection<CsItem> _fullBuyPrimaryForCt =
        _riflesForCT.Concat(_heavys).ToHashSet();

    private static readonly ICollection<CsItem> _allWeapons = Enum.GetValues<CsItem>()
        .Where(item => (int)item >= 200 && (int)item < 500)
        .ToHashSet();

    private static readonly ICollection<CsItem> _allFullBuy =
        _allPreferred.Concat(_heavys).Concat(_sharedRifles).Concat(_tRifles).Concat(_ctRifles).ToHashSet();

    private static readonly ICollection<CsItem> _allHalfBuy =
        _midRangeForT.Concat(_midRangeForCt).ToHashSet();

    private static readonly ICollection<CsItem> _allPistols =
        _pistolsForT.Concat(_pistolsForCt).ToHashSet();

    private static readonly ICollection<CsItem> _allPrimary = _allPreferred
        .Concat(_allFullBuy)
        .Concat(_allHalfBuy)
        .ToHashSet();

    private static readonly ICollection<CsItem> _allSecondary = _allPistols.ToHashSet();

    private static readonly ICollection<CsItem> _allUtil = new HashSet<CsItem>
    {
        CsItem.Flashbang,
        CsItem.HE,
        CsItem.Molotov,
        CsItem.Incendiary,
        CsItem.Smoke,
        CsItem.Decoy,
    };

    private static readonly Dictionary<RoundType, ICollection<WeaponAllocationType>>
        _validAllocationTypesForRound = new()
        {
            {RoundType.Pistol, new HashSet<WeaponAllocationType> {WeaponAllocationType.PistolRound}},
            {
                RoundType.HalfBuy,
                new HashSet<WeaponAllocationType> {WeaponAllocationType.Secondary, WeaponAllocationType.HalfBuyPrimary}
            },
            {
                RoundType.FullBuy,
                new HashSet<WeaponAllocationType>
                {
                    WeaponAllocationType.Secondary, WeaponAllocationType.FullBuyPrimary, WeaponAllocationType.Preferred
                }
            },
            {
                RoundType.Snipers,
                new HashSet<WeaponAllocationType>
                {
                    WeaponAllocationType.Secondary, WeaponAllocationType.Preferred
                }
            },
        };

    private static readonly Dictionary<
        CsTeam,
        Dictionary<WeaponAllocationType, ICollection<CsItem>>
    > _validWeaponsByTeamAndAllocationType = new()
    {
        {
            CsTeam.Terrorist, new()
            {
                {WeaponAllocationType.PistolRound, _pistolsForT},
                {WeaponAllocationType.Secondary, _pistolsForT},
                {WeaponAllocationType.HalfBuyPrimary, _midRangeForT},
                {WeaponAllocationType.FullBuyPrimary, _fullBuyPrimaryForT},
                {WeaponAllocationType.Preferred, _preferredForT},
            }
        },
        {
            CsTeam.CounterTerrorist, new()
            {
                {WeaponAllocationType.PistolRound, _pistolsForCt},
                {WeaponAllocationType.Secondary, _pistolsForCt},
                {WeaponAllocationType.HalfBuyPrimary, _midRangeForCt},
                {WeaponAllocationType.FullBuyPrimary, _fullBuyPrimaryForCt},
                {WeaponAllocationType.Preferred, _preferredForCt},
            }
        }
    };

    private static readonly Dictionary<
        CsTeam,
        Dictionary<WeaponAllocationType, CsItem>
    > _defaultWeaponsByTeamAndAllocationType = new()
    {
        {
            CsTeam.Terrorist, new()
            {
                {WeaponAllocationType.SniperPrimary, CsItem.AWP},
                {WeaponAllocationType.FullBuyPrimary, CsItem.AK47},
                {WeaponAllocationType.HalfBuyPrimary, CsItem.Mac10},
                {WeaponAllocationType.Secondary, CsItem.Deagle},
                {WeaponAllocationType.PistolRound, CsItem.Glock},
            }
        },
        {
            CsTeam.CounterTerrorist, new()
            {
                {WeaponAllocationType.SniperPrimary, CsItem.AWP},
                {WeaponAllocationType.FullBuyPrimary, CsItem.M4A1S},
                {WeaponAllocationType.HalfBuyPrimary, CsItem.MP9},
                {WeaponAllocationType.Secondary, CsItem.Deagle},
                {WeaponAllocationType.PistolRound, CsItem.USPS},
            }
        }
    };

    private static readonly Dictionary<string, CsItem> _weaponNameSearchOverrides = new()
    {
        {"m4a1", CsItem.M4A1S},
        {"m4a1-s", CsItem.M4A1S},
    };

    private static readonly Dictionary<CsItem, string> _weaponNameOverrides = new()
    {
        {CsItem.M4A4, "M4A4"},
    };

    public static List<WeaponAllocationType> WeaponAllocationTypes =>
        Enum.GetValues<WeaponAllocationType>().ToList();

    public static Dictionary<
        CsTeam,
        Dictionary<WeaponAllocationType, CsItem>
    > DefaultWeaponsByTeamAndAllocationType => new(_defaultWeaponsByTeamAndAllocationType);

    public static List<CsItem> AllWeapons => _allWeapons.ToList();

    public static bool IsWeapon(CsItem item) => _allWeapons.Contains(item);

    public static string GetName(this CsItem item) =>
        _weaponNameOverrides.TryGetValue(item, out var overrideName)
            ? overrideName
            : item.ToString();

    public static ItemSlotType? GetSlotTypeForItem(CsItem? item)
    {
        if (item is null)
        {
            return null;
        }

        if (_allSecondary.Contains(item.Value))
        {
            return ItemSlotType.Secondary;
        }

        if (_allPrimary.Contains(item.Value))
        {
            return ItemSlotType.Primary;
        }

        if (_allUtil.Contains(item.Value))
        {
            return ItemSlotType.Util;
        }

        return null;
    }

    public static string GetSlotNameForSlotType(ItemSlotType? slotType)
    {
        return slotType switch
        {
            ItemSlotType.Primary => "slot1",
            ItemSlotType.Secondary => "slot2",
            ItemSlotType.Util => "slot4",
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public static ICollection<CsItem> GetPossibleWeaponsForAllocationType(WeaponAllocationType allocationType,
        CsTeam team)
    {
        if (team != CsTeam.Terrorist && team != CsTeam.CounterTerrorist)
        {
            return new List<CsItem>();
        }
        return _validWeaponsByTeamAndAllocationType[team][allocationType].Where(IsUsableWeapon).ToList();
    }

    public static bool IsAllocationTypeValidForRound(WeaponAllocationType? allocationType, RoundType? roundType)
    {
        if (allocationType is null || roundType is null)
        {
            return false;
        }

        return _validAllocationTypesForRound[roundType.Value].Contains(allocationType.Value);
    }

    public static bool IsPreferred(CsTeam team, CsItem weapon)
    {
        return team switch
        {
            CsTeam.Terrorist => _preferredForT.Contains(weapon),
            CsTeam.CounterTerrorist => _preferredForCt.Contains(weapon),
            _ => false,
        };
    }

    public static IList<T> SelectPreferredPlayers<T>(IEnumerable<T> players, Func<T, bool> isVip, CsTeam team)
    {
        if (Configs.GetConfigData().AllowPreferredWeaponForEveryone)
        {
            return new List<T>(players);
        }

        var playersList = players.ToList();

        if (Configs.GetConfigData().MinPlayersPerTeamForPreferredWeapon.TryGetValue(team, out var minTeamPlayers))
        {
            if (playersList.Count < minTeamPlayers)
            {
                return new List<T>();
            }
        }

        if (!Configs.GetConfigData().MaxPreferredWeaponsPerTeam.TryGetValue(team, out var maxPerTeam))
        {
            maxPerTeam = 1;
        }

        if (maxPerTeam == 0)
        {
            return new List<T>();
        }

        var choicePlayers = new List<T>();
        foreach (var p in playersList)
        {
            if (Configs.GetConfigData().NumberOfExtraVipChancesForPreferredWeapon == -1)
            {
                if (isVip(p))
                {
                    choicePlayers.Add(p);
                }
            }
            else
            {
                choicePlayers.Add(p);
                if (isVip(p))
                {
                    for (var i = 0; i < Configs.GetConfigData().NumberOfExtraVipChancesForPreferredWeapon; i++)
                    {
                        choicePlayers.Add(p);
                    }
                }
            }
        }

        Utils.Shuffle(choicePlayers);
        return new HashSet<T>(choicePlayers).Take(maxPerTeam).ToList();
    }

    public static bool IsUsableWeapon(CsItem weapon)
    {
        return Configs.GetConfigData().UsableWeapons.Contains(weapon);
    }

    public static CsItem? CoercePreferredTeam(CsItem? item, CsTeam team)
    {
        if (item == null || !_allPreferred.Contains(item.Value))
        {
            return null;
        }

        if (team != CsTeam.Terrorist && team != CsTeam.CounterTerrorist)
        {
            return null;
        }

        if (item == CsItem.AWP)
        {
            return item;
        }

        // Right now these are the only other preferred guns
        // If we make preferred guns configurable, we'll have to change this
        return team == CsTeam.Terrorist ? CsItem.AutoSniperT : CsItem.AutoSniperCT;
    }

    public static ICollection<RoundType> GetRoundTypesForWeapon(CsItem weapon)
    {
        if (_allPistols.Contains(weapon))
        {
            return new HashSet<RoundType> {RoundType.Pistol, RoundType.HalfBuy, RoundType.FullBuy, RoundType.Snipers};
        }

        if (_allHalfBuy.Contains(weapon))
        {
            return new HashSet<RoundType> {RoundType.HalfBuy};
        }

        if (_allFullBuy.Contains(weapon))
        {
            return new HashSet<RoundType> {RoundType.FullBuy};
        }

        if (_allPreferred.Contains(weapon))
        {
            return new HashSet<RoundType> {RoundType.Snipers};
        }

        return new HashSet<RoundType>();
    }

    public static ICollection<CsItem> FindValidWeaponsByName(string needle)
    {
        return FindItemsByName(needle)
            .Where(item => _allWeapons.Contains(item))
            .ToList();
    }

    public static WeaponAllocationType? GetWeaponAllocationTypeForWeaponAndRound(RoundType? roundType, CsTeam team,
        CsItem weapon)
    {
        if (team != CsTeam.Terrorist && team != CsTeam.CounterTerrorist)
        {
            return null;
        }

        // First populate all allocation types that could match
        // For a pistol this could be multiple allocation types, for any other weapon type only one can match
        var potentialAllocationTypes = new HashSet<WeaponAllocationType>();
        foreach (var (allocationType, items) in _validWeaponsByTeamAndAllocationType[team])
        {
            if (items.Contains(weapon))
            {
                potentialAllocationTypes.Add(allocationType);
            }
        }

        // If theres only 1 to choose from, return that, or return null if there are none
        if (potentialAllocationTypes.Count == 1)
        {
            return potentialAllocationTypes.First();
        }

        if (potentialAllocationTypes.Count == 0)
        {
            return null;
        }

        // For a pistol, the set will be {PistolRound, Secondary}
        // We need to find which of those matches the current round type
        foreach (var allocationType in potentialAllocationTypes)
        {
            if (roundType is null || IsAllocationTypeValidForRound(allocationType, roundType))
            {
                return allocationType;
            }
        }

        return null;
    }

    /**
     * This function should only be used when you have an item that you want to find out what *replacement*
     * allocation type it belongs to. Eg. if you have a Preferred, it should be replaced with a PrimaryFullBuy
     */
    public static WeaponAllocationType? GetReplacementWeaponAllocationTypeForWeapon(RoundType? roundType)
    {
        return roundType switch
        {
            RoundType.Pistol => WeaponAllocationType.PistolRound,
            RoundType.HalfBuy => WeaponAllocationType.HalfBuyPrimary,
            RoundType.FullBuy => WeaponAllocationType.FullBuyPrimary,
            RoundType.Snipers => WeaponAllocationType.SniperPrimary,
            _ => null,
        };
    }

    public static ICollection<CsItem> GetWeaponsForRoundType(
        RoundType roundType,
        CsTeam team,
        UserSetting? userSetting,
        bool givePreferred
    )
    {
        WeaponAllocationType? primaryWeaponAllocation =
            givePreferred
                ? WeaponAllocationType.Preferred
                : roundType switch
                {
                    RoundType.HalfBuy => WeaponAllocationType.HalfBuyPrimary,
                    RoundType.FullBuy => WeaponAllocationType.FullBuyPrimary,
                    RoundType.Snipers => WeaponAllocationType.SniperPrimary,
                    _ => null,
                };

        var secondaryWeaponAllocation = roundType switch
        {
            RoundType.Pistol => WeaponAllocationType.PistolRound,
            _ => WeaponAllocationType.Secondary,
        };

        var weapons = new List<CsItem>();
        var secondary = GetWeaponForAllocationType(secondaryWeaponAllocation, team, userSetting);
        if (secondary is not null)
        {
            weapons.Add(secondary.Value);
        }

        if (primaryWeaponAllocation is null)
        {
            return weapons;
        }

        var primary = GetWeaponForAllocationType(primaryWeaponAllocation.Value, team, userSetting);
        if (primary is not null)
        {
            weapons.Add(primary.Value);
        }

        return weapons;
    }

    private static ICollection<CsItem> FindItemsByName(string needle)
    {
        needle = needle.ToLower();
        if (_weaponNameSearchOverrides.TryGetValue(needle, out var nameOverride))
        {
            return new List<CsItem> {nameOverride};
        }

        return Enum.GetNames<CsItem>()
            .Where(name => name.ToLower().Contains(needle))
            .Select(Enum.Parse<CsItem>)
            .ToList();
    }

    private static CsItem? GetDefaultWeaponForAllocationType(WeaponAllocationType allocationType, CsTeam team)
    {
        if (team is CsTeam.None or CsTeam.Spectator)
        {
            return null;
        }

        if (allocationType == WeaponAllocationType.Preferred)
        {
            return null;
        }

        CsItem? defaultWeapon = null;

        var configDefaultWeapons = Configs.GetConfigData().DefaultWeapons;
        if (configDefaultWeapons.TryGetValue(team, out var teamDefaults))
        {
            if (teamDefaults.TryGetValue(allocationType, out var configuredDefaultWeapon))
            {
                defaultWeapon = configuredDefaultWeapon;
            }
        }

        defaultWeapon ??= _defaultWeaponsByTeamAndAllocationType[team][allocationType];

        return IsUsableWeapon(defaultWeapon.Value) ? defaultWeapon : null;
    }

    private static CsItem GetRandomWeaponForAllocationType(WeaponAllocationType allocationType, CsTeam team)
    {
        if (team != CsTeam.Terrorist && team != CsTeam.CounterTerrorist)
        {
            return CsItem.Deagle;
        }

        var collectionToCheck = allocationType switch
        {
            WeaponAllocationType.PistolRound => team == CsTeam.Terrorist ? _pistolsForT : _pistolsForCt,
            WeaponAllocationType.Secondary => team == CsTeam.Terrorist ? _pistolsForT : _pistolsForCt,
            WeaponAllocationType.HalfBuyPrimary => team == CsTeam.Terrorist ? _smgsForT : _smgsForCt,
            WeaponAllocationType.FullBuyPrimary => team == CsTeam.Terrorist ? _riflesForT : _riflesForCT,
            WeaponAllocationType.SniperPrimary => team == CsTeam.Terrorist ? _preferredForT : _preferredForCt,
            WeaponAllocationType.Preferred => team == CsTeam.Terrorist ? _preferredForT : _preferredForCt,
            _ => _sharedPistols,
        };
        return Utils.Choice(collectionToCheck.Where(IsUsableWeapon).ToList());
    }

    private static CsItem? GetWeaponForAllocationType(WeaponAllocationType allocationType, CsTeam team,
        UserSetting? userSetting)
    {
        CsItem? weapon = null;

        if (Configs.GetConfigData().CanPlayersSelectWeapons() && userSetting is not null)
        {
            var weaponPreference = userSetting.GetWeaponPreference(team, allocationType);
            if (weaponPreference is not null && IsUsableWeapon(weaponPreference.Value))
            {
                weapon = weaponPreference;
            }
        }

        if (weapon is null && Configs.GetConfigData().CanAssignRandomWeapons())
        {
            weapon = GetRandomWeaponForAllocationType(allocationType, team);
        }

        if (weapon is null && Configs.GetConfigData().CanAssignDefaultWeapons())
        {
            weapon = GetDefaultWeaponForAllocationType(allocationType, team);
        }

        return weapon;
    }

    public static bool IsWeaponAllocationAllowed(bool isFreezePeriod)
    {
        return Configs.GetConfigData().AllowAllocationAfterFreezeTime || isFreezePeriod;
    }
}
