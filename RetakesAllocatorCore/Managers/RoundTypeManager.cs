using RetakesAllocatorCore.Config;

namespace RetakesAllocatorCore.Managers;

public class RoundTypeManager
{
    #region Instance management

    private static RoundTypeManager? _instance;

    public static RoundTypeManager Instance => _instance ??= new RoundTypeManager();

    #endregion

    private string? _map;
    
    private RoundType? _nextRoundTypeOverride;
    private RoundType? _currentRoundType;

    private RoundTypeSelectionOption _roundTypeSelection;
    private readonly List<RoundType> _roundsOrder = new();
    private int _roundTypeManualOrderingPosition;

    private RoundTypeManager()
    {
        Initialize();
    }

    public void Initialize()
    {
        _nextRoundTypeOverride = null;
        _currentRoundType = null;
        _roundTypeSelection = Configs.GetConfigData().RoundTypeSelection;

        _roundsOrder.Clear();
        switch (_roundTypeSelection)
        {
            case RoundTypeSelectionOption.RandomFixedCounts:
                foreach (var (roundType, fixedCount) in Configs.GetConfigData().RoundTypeRandomFixedCounts)
                {
                    for (var i = 0; i < fixedCount; i++)
                    {
                        _roundsOrder.Add(roundType);
                    }
                }
                Utils.Shuffle(_roundsOrder);
                break;
            case RoundTypeSelectionOption.ManualOrdering:
                foreach (var item in Configs.GetConfigData().RoundTypeManualOrdering)
                {
                    for (var i = 0; i < item.Count; i++)
                    {
                        _roundsOrder.Add(item.Type);
                    }
                }
                break;
        }
        _roundTypeManualOrderingPosition = 0;
    }

    public void SetMap(string map)
    {
        _map = map;
    }

    public string? Map => _map;

    public RoundType GetNextRoundType()
    {
        if (_nextRoundTypeOverride is not null)
        {
            return _nextRoundTypeOverride.Value;
        }

        switch (_roundTypeSelection)
        {
            case RoundTypeSelectionOption.Random:
                return GetRandomRoundType();
            case RoundTypeSelectionOption.ManualOrdering:
            case RoundTypeSelectionOption.RandomFixedCounts:
                return GetNextRoundTypeInOrder();
        }

        throw new Exception("No round type selection type was found.");
    }

    private RoundType GetNextRoundTypeInOrder()
    {
        if (_roundTypeManualOrderingPosition >= _roundsOrder.Count)
        {
            _roundTypeManualOrderingPosition = 0;
        }
        return _roundsOrder[_roundTypeManualOrderingPosition++];
    }

    private RoundType GetRandomRoundType()
    {
        var randomValue = new Random().NextDouble();

        var pistolPercentage = Configs.GetConfigData().GetRoundTypePercentage(RoundType.Pistol);
        var halfbuyPercentage = Configs.GetConfigData().GetRoundTypePercentage(RoundType.HalfBuy) + pistolPercentage;
        var fullbuyPercentage = Configs.GetConfigData().GetRoundTypePercentage(RoundType.FullBuy) + halfbuyPercentage;

        if (randomValue < pistolPercentage)
        {
            return RoundType.Pistol;
        }

        if (randomValue < halfbuyPercentage)
        {
            return RoundType.HalfBuy;
        }

        if (randomValue < fullbuyPercentage)
        {
            return RoundType.FullBuy;
        }

        return RoundType.Snipers;
    }

    public void SetNextRoundTypeOverride(RoundType? nextRoundType)
    {
        _nextRoundTypeOverride = nextRoundType;
    }

    public RoundType? GetCurrentRoundType()
    {
        return _currentRoundType;
    }

    public void SetCurrentRoundType(RoundType? currentRoundType)
    {
        _currentRoundType = currentRoundType;
    }
}
