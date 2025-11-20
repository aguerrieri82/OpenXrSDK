namespace XrEngine.Media.Windows
{
    public static class MFAttributesGuid
    {
        public static  Guid MajorType = new("48eba18e-f8c9-4687-bf11-0a74c9f96a8f"); // MF_MT_MAJOR_TYPE
        public static  Guid Subtype = new("f7e34c9a-42e8-4714-b74b-cb29d72c35e5"); // MF_MT_SUBTYPE

        public static  Guid AudioNumChannels = new("37e48bf5-645e-4c5b-89de-ada9e29b696a"); // MF_MT_AUDIO_NUM_CHANNELS
        public static  Guid AudioSamplesPerSecond = new("5faeeae7-0290-4c31-9e8a-c534f68d9dba"); // MF_MT_AUDIO_SAMPLES_PER_SECOND
        public static  Guid AudioAvgBytesPerSecond = new("1aab75c8-cfef-451c-ab95-ac034b8e1731"); // MF_MT_AUDIO_AVG_BYTES_PER_SECOND
        public static  Guid AudioBlockAlignment = new("322de230-9eeb-43bd-ab7a-ff412251541d"); // MF_MT_AUDIO_BLOCK_ALIGNMENT
        public static  Guid AudioBitsPerSample = new("f2deb57f-40fa-4764-aa33-ed4f2d1ff669"); // MF_MT_AUDIO_BITS_PER_SAMPLE
    }
    public static class MFMajorTypes
    {
        public static  Guid Audio = new("73647561-0000-0010-8000-00AA00389B71");
        public static  Guid Video = new("73646976-0000-0010-8000-00AA00389B71");
    }

    public static class MFSubtypes
    {
        public static Guid PCM = new("00000001-0000-0010-8000-00AA00389B71");

        public static Guid Float = new("00000003-0000-0010-8000-00AA00389B71");
    }
}
