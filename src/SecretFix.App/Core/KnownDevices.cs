namespace SecretFix.Core;

public static class KnownDevices
{
    public static readonly KnownDevice GenericMouse = new(DeviceKind.Mouse, "secret.fix", "Generic", "", "", "/Assets/Devices/Mice/hero-logitech-g.png");
    public static readonly KnownDevice GenericKeyboard = new(DeviceKind.Keyboard, "secret.fix", "Generic Keyboard", "", "", "/Assets/Devices/Keyboards/hero-keyboard.png");

    public static IReadOnlyList<KnownDevice> All { get; } =
    [
        new(DeviceKind.Mouse, "Logitech", "G Pro X Superlight", "046D", "C094", "/Assets/Devices/Mice/logitech-g-pro-x.png"),
        new(DeviceKind.Mouse, "Logitech", "G Pro X Superlight 2", "046D", "C09B", "/Assets/Devices/Mice/logitech-g-pro-x.png"),
        new(DeviceKind.Mouse, "Logitech", "G502 X", "046D", "C099", "/Assets/Devices/Mice/hero-logitech-g.png"),
        new(DeviceKind.Mouse, "Razer", "Viper V3 Pro", "1532", "00B6", "/Assets/Devices/Mice/razer-viper-v3.png"),
        new(DeviceKind.Mouse, "Razer", "DeathAdder V3", "1532", "00AA", "/Assets/Devices/Mice/hero-logitech-g.png"),
        new(DeviceKind.Mouse, "Finalmouse", "Starlight", "361C", "0101", "/Assets/Devices/Mice/finalmouse-ultralightx.png"),
        new(DeviceKind.Mouse, "Zowie", "FK2", "04A5", "8001", "/Assets/Devices/Mice/zowie-ec2cw.png"),
        new(DeviceKind.Mouse, "Pulsar", "X2H", "3554", "F58C", "/Assets/Devices/Mice/hero-logitech-g.png"),
        new(DeviceKind.Mouse, "Lamzu", "Atlantis", "3554", "F57C", "/Assets/Devices/Mice/lamzu-atlantis.png"),
        new(DeviceKind.Mouse, "Attack Shark", "X3", "258A", "2019", "/Assets/Devices/Mice/hero-logitech-g.png"),

        new(DeviceKind.Keyboard, "Wooting", "60HE", "31E3", "1100", "/Assets/Devices/Keyboards/wooting-60he.png"),
        new(DeviceKind.Keyboard, "DrunkDeer", "A75", "3554", "F808", "/Assets/Devices/Keyboards/drunkdeer-a75.png"),
        new(DeviceKind.Keyboard, "HyperX", "Alloy Origins", "03F0", "098F", "/Assets/Devices/Keyboards/hyperx-alloy-origins.png"),
        new(DeviceKind.Keyboard, "Redragon", "Fizz", "258A", "0049", "/Assets/Devices/Keyboards/redragon-fizz.png"),
        new(DeviceKind.Keyboard, "Anne Pro", "2", "04D9", "A0F8", "/Assets/Devices/Keyboards/anne-pro-2.png"),
        new(DeviceKind.Keyboard, "Keychron", "K2", "3434", "0121", "/Assets/Devices/Keyboards/keychron-k2.png"),
        new(DeviceKind.Keyboard, "SteelSeries", "Apex Pro", "1038", "1610", "/Assets/Devices/Keyboards/hyperx-alloy-origins.png"),
        new(DeviceKind.Keyboard, "Razer", "Huntsman Mini", "1532", "0257", "/Assets/Devices/Keyboards/redragon-fizz.png")
    ];

    public static KnownDevice? Match(DeviceKind kind, string? vid, string? pid)
    {
        if (string.IsNullOrWhiteSpace(vid) || string.IsNullOrWhiteSpace(pid))
            return null;

        return All.FirstOrDefault(device =>
            device.Kind == kind &&
            string.Equals(device.Vid, vid, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(device.Pid, pid, StringComparison.OrdinalIgnoreCase));
    }
}
