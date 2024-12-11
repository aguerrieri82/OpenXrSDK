namespace XrEngine.Devices
{
    public struct BleUUID
    {

        public static Guid FromInt(uint value)
        {
            return new Guid(value, 0x0000, 0x1000, 0x80, 0x00, 0x00, 0x80, 0x5F, 0x9B, 0x34, 0xFB);
        }
    }
}
