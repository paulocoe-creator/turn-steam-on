namespace TurnSteamOn.Core;

public enum DeviceTriggerDecision
{
    Eligible,
    NotAConnectionTransition,
    UnsupportedDevice,
    NotSelected,
    Disabled
}

public interface IDeviceTriggerPolicy
{
    DeviceTriggerDecision Evaluate(
        DeviceConnectionChanged change,
        IReadOnlyCollection<DeviceSelection> selections);
}

public sealed class DeviceTriggerPolicy : IDeviceTriggerPolicy
{
    public DeviceTriggerDecision Evaluate(
        DeviceConnectionChanged change,
        IReadOnlyCollection<DeviceSelection> selections)
    {
        ArgumentNullException.ThrowIfNull(change);
        ArgumentNullException.ThrowIfNull(selections);

        if (!change.IsConnectionTransition)
        {
            return DeviceTriggerDecision.NotAConnectionTransition;
        }

        if (!change.Device.IsSupported)
        {
            return DeviceTriggerDecision.UnsupportedDevice;
        }

        var selection = selections.FirstOrDefault(candidate =>
            string.Equals(candidate.StableId, change.Device.StableId, StringComparison.Ordinal));

        if (selection is null)
        {
            return DeviceTriggerDecision.NotSelected;
        }

        return selection.Enabled
            ? DeviceTriggerDecision.Eligible
            : DeviceTriggerDecision.Disabled;
    }
}
